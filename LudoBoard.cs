using System.Text;

namespace Ludo
{
    public enum GameError : byte
    {
        None = 0,
        InvalidTokenIndex,
        InvalidPlayerIndex,
        InvalidDiceRoll,
        TokenNotMovable,
        TokenAlreadyHome,
        TokenNotAtBase,
        WouldOvershootHome,
        NoTurnAvailable,
        GameAlreadyWon
    }

    // -----------------------------------------
    // Value types
    // -----------------------------------------
    public readonly record struct PlayerId(byte Value)
    {
        public override string ToString() => $"P{Value}";
        public static implicit operator byte(PlayerId p) => p.Value;
    }

    public readonly record struct TokenIndex(int Value)
    {
        public override string ToString() => $"T#{Value}";
        public static implicit operator int(TokenIndex i) => i.Value;
    }

    public readonly record struct Dice(byte Value)
    {
        public override string ToString() => Value.ToString();
        public static implicit operator byte(Dice d) => d.Value;
        public static Dice FromByte(byte b) => new(b);
    }

    [Flags]
    public enum MovableTokens : sbyte
    {
        None = 0,
        T0 = 1 << 0,
        T1 = 1 << 1,
        T2 = 1 << 2,
        T3 = 1 << 3
    }

    // -----------------------------------------
    // Utilities
    // -----------------------------------------
    public static class LudoUtil
    {
        public const byte MinDiceValue = 1;
        public const byte MaxDiceValue = 6;
        public const byte TokensPerPlayer = 4;
        public const byte MaxConsecutiveSixes = 3;

        public static bool IsValidDiceRoll(int diceRoll) =>
            diceRoll is >= MinDiceValue and <= MaxDiceValue;

        public static bool IsValidPlayerIndex(int playerIndex, int playerCount) =>
            playerIndex >= 0 && playerIndex < playerCount;

        public static bool IsValidTokenIndex(int tokenIndex, int totalTokens) =>
            tokenIndex >= 0 && tokenIndex < totalTokens;

        public static int GetPlayerFromToken(int tokenIndex) =>
            tokenIndex / TokensPerPlayer;

        public static int GetPlayerTokenStart(int playerIndex) =>
            playerIndex * TokensPerPlayer;

        public static bool IsSamePlayer(int tokenIndex1, int tokenIndex2) =>
            GetPlayerFromToken(tokenIndex1) == GetPlayerFromToken(tokenIndex2);
    }

    // -----------------------------------------
    // State
    // -----------------------------------------
    [Serializable]
    public struct LudoState
    {
        public byte currentPlayer;
        public byte playerCount;
        public byte consecutiveSixes;
        public byte lastDiceRoll;
        public bool hasRolled;
        public bool mustMove;
        public MovableTokens movableTokensMask;

        public static LudoState Create(byte playerCount)
        {
            return new LudoState
            {
                playerCount = playerCount,
                currentPlayer = 0,
                consecutiveSixes = 0,
                lastDiceRoll = 0,
                hasRolled = false,
                mustMove = false,
                movableTokensMask = MovableTokens.None
            };
        }

        public bool CanRollDice() => !hasRolled && !mustMove;
        public bool MustMakeMove() => mustMove;
        public bool HasMovableTokens() => movableTokensMask != MovableTokens.None;

        public void RecordDiceRoll(byte diceValue, MovableTokens movableMask)
        {
            lastDiceRoll = diceValue;
            movableTokensMask = movableMask;
            hasRolled = true;
            mustMove = movableMask != MovableTokens.None;

            if (diceValue == 6) consecutiveSixes++;
            else consecutiveSixes = 0;
        }

        public void AdvanceTurn()
        {
            currentPlayer = (byte)((currentPlayer + 1) % playerCount);
            lastDiceRoll = 0;
            hasRolled = false;
            mustMove = false;
            movableTokensMask = MovableTokens.None;
        }

        public void ClearTurnAfterMove(int _movedTokenIndex)
        {
            hasRolled = false;
            mustMove = false;
            movableTokensMask = MovableTokens.None;

            // If rolled a 6, player gets another turn unless exceeded max consecutive sixes
            if (lastDiceRoll != 6 || consecutiveSixes >= LudoUtil.MaxConsecutiveSixes)
            {
                if (consecutiveSixes >= LudoUtil.MaxConsecutiveSixes)
                {
                    consecutiveSixes = 0;
                }
                AdvanceTurn();
            }
        }

        public bool IsTokenMovable(int tokenLocalIndex) =>
            (movableTokensMask & (MovableTokens)(1 << tokenLocalIndex)) != MovableTokens.None;
    }

    // -----------------------------------------
    // Board (pure domain, TryX methods)
    // -----------------------------------------
    [Serializable]
    public struct LudoBoard
    {
        // Position constants
        private const byte BasePosition = 0;
        private const byte StartPosition = 1;
        private const byte MainTrackLength = 51;
        private const byte HomeStretchStart = 52;
        private const byte HomeStretchLength = 5;
        private const byte HomePosition = HomeStretchStart + HomeStretchLength; // 57

        // Absolute track constants
        private const byte AbsoluteTrackLength = 52;
        private const byte ExitDiceValue = 6;
        private const byte QuarterTrackLength = AbsoluteTrackLength / 4; // 13

        // Safe tiles on absolute track
        private static readonly byte[] SafeAbsoluteTiles = { 1, 14, 27, 40 };

        public byte[] tokenPositions;
        public int PlayerCount => tokenPositions.Length / LudoUtil.TokensPerPlayer;

        public static LudoBoard Create(int playerCount)
        {
            if (playerCount < 2 || playerCount > 4)
                throw new ArgumentException("Player count must be 2-4", nameof(playerCount));

            return new LudoBoard
            {
                tokenPositions = new byte[playerCount * LudoUtil.TokensPerPlayer]
            };
        }

        // -----------------
        // TryX operations
        // -----------------

        public bool TryMoveToken(int tokenIndex, byte diceRoll, out byte newPosition, out GameError error)
        {
            newPosition = 0;
            error = GameError.None;

            if (!LudoUtil.IsValidTokenIndex(tokenIndex, tokenPositions.Length))
            {
                error = GameError.InvalidTokenIndex; return false;
            }

            if (!LudoUtil.IsValidDiceRoll(diceRoll))
            {
                error = GameError.InvalidDiceRoll; return false;
            }

            if (IsHome(tokenIndex))
            {
                error = GameError.TokenAlreadyHome; return false;
            }

            // Exit from base
            if (IsAtBase(tokenIndex))
            {
                if (diceRoll != ExitDiceValue)
                {
                    error = GameError.TokenNotAtBase; return false;
                }

                tokenPositions[tokenIndex] = StartPosition;
                newPosition = StartPosition;
                return true;
            }

            if (!TryCalculateNewPosition(tokenIndex, diceRoll, out var pos, out error))
                return false;

            tokenPositions[tokenIndex] = pos;
            newPosition = pos;
            return true;
        }

        public bool TryCaptureOpponent(int movedTokenIndex, out int capturedTokenIndex, out GameError error)
        {
            error = GameError.None;
            capturedTokenIndex = -1;

            // Non-failing "no capture" is considered success.
            if (!IsOnMainTrack(movedTokenIndex) || IsOnSafeTile(movedTokenIndex))
                return true;

            int landingPosition = GetAbsolutePosition(movedTokenIndex);

            int opponentIndex = -1;
            int opponentCount = 0;

            for (int i = 0; i < tokenPositions.Length; i++)
            {
                if (!IsOnMainTrack(i)) continue;
                if (LudoUtil.IsSamePlayer(i, movedTokenIndex)) continue;
                if (GetAbsolutePosition(i) != landingPosition) continue;

                opponentCount++;
                if (opponentCount == 1)
                    opponentIndex = i;
                else
                    return true; // multiple opponents block capture (safe "no capture")
            }

            if (opponentCount == 1)
            {
                tokenPositions[opponentIndex] = BasePosition;
                capturedTokenIndex = opponentIndex;
            }

            return true;
        }

        public bool TryGetMovableTokens(int playerIndex, byte diceRoll, out MovableTokens mask, out GameError error)
        {
            mask = MovableTokens.None;
            error = GameError.None;

            if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
            {
                error = GameError.InvalidPlayerIndex; return false;
            }

            if (!LudoUtil.IsValidDiceRoll(diceRoll))
            {
                error = GameError.InvalidDiceRoll; return false;
            }

            int tokenStart = LudoUtil.GetPlayerTokenStart(playerIndex);

            for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
            {
                int tokenIndex = tokenStart + i;
                if (CanMoveToken(tokenIndex, diceRoll))
                {
                    mask |= (MovableTokens)(1 << i);
                }
            }

            return true;
        }

        public bool TryHasPlayerWon(int playerIndex, out bool hasWon, out GameError error)
        {
            hasWon = false;
            error = GameError.None;

            if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
            {
                error = GameError.InvalidPlayerIndex; return false;
            }

            int tokenStart = LudoUtil.GetPlayerTokenStart(playerIndex);
            for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
            {
                if (!IsHome(tokenStart + i))
                {
                    hasWon = false;
                    return true;
                }
            }

            hasWon = true;
            return true;
        }

        public byte GetTokenPosition(int tokenIndex) =>
            LudoUtil.IsValidTokenIndex(tokenIndex, tokenPositions.Length) ? tokenPositions[tokenIndex] : (byte)0;

        // -------------
        // Position checks
        // -------------
        public bool IsAtBase(int tokenIndex) =>
            tokenPositions[tokenIndex] == BasePosition;

        public bool IsOnMainTrack(int tokenIndex)
        {
            byte pos = tokenPositions[tokenIndex];
            return pos >= StartPosition && pos <= MainTrackLength;
        }

        public bool IsOnHomeStretch(int tokenIndex)
        {
            byte pos = tokenPositions[tokenIndex];
            return pos >= HomeStretchStart && pos < HomePosition;
        }

        public bool IsHome(int tokenIndex) =>
            tokenPositions[tokenIndex] == HomePosition;

        public bool IsOnSafeTile(int tokenIndex)
        {
            if (IsOnHomeStretch(tokenIndex))
                return true;

            if (!IsOnMainTrack(tokenIndex))
                return false;

            int absolutePos = GetAbsolutePosition(tokenIndex);
            return IsSafeAbsolute((byte)absolutePos);
        }

        // -------------
        // Helpers
        // -------------
        private bool CanMoveToken(int tokenIndex, byte diceRoll)
        {
            if (IsHome(tokenIndex)) return false;
            if (IsAtBase(tokenIndex)) return diceRoll == ExitDiceValue;

            return TryCalculateNewPosition(tokenIndex, diceRoll, out _, out _);
        }

        private bool TryCalculateNewPosition(int tokenIndex, byte diceRoll, out byte newPos, out GameError error)
        {
            newPos = 0;
            error = GameError.None;

            byte currentPos = tokenPositions[tokenIndex];

            // On main track
            if (IsOnMainTrack(tokenIndex))
            {
                int targetPos = currentPos + diceRoll;

                if (targetPos <= MainTrackLength)
                {
                    newPos = (byte)targetPos;
                    return true;
                }

                // Entering home stretch
                int stepsIntoHome = targetPos - MainTrackLength;
                int homePos = HomeStretchStart + stepsIntoHome - 1;

                if (homePos > HomePosition)
                {
                    error = GameError.WouldOvershootHome; return false;
                }

                newPos = (byte)homePos;
                return true;
            }

            // On home stretch
            if (IsOnHomeStretch(tokenIndex))
            {
                int targetPos = currentPos + diceRoll;

                if (targetPos > HomePosition)
                {
                    error = GameError.WouldOvershootHome; return false;
                }

                newPos = (byte)targetPos;
                return true;
            }

            error = GameError.TokenNotMovable;
            return false;
        }

        private int GetAbsolutePosition(int tokenIndex)
        {
            if (!IsOnMainTrack(tokenIndex))
                return -1;

            int playerIndex = LudoUtil.GetPlayerFromToken(tokenIndex);
            int relativePos = tokenPositions[tokenIndex];
            int offset = GetPlayerTrackOffset(playerIndex);

            return (relativePos - 1 + offset) % AbsoluteTrackLength + 1;
        }

        private int GetPlayerTrackOffset(int playerIndex)
        {
            // For 2-player games, use opposite sides of the board
            if (PlayerCount == 2)
                return playerIndex * 2 * QuarterTrackLength;

            return playerIndex * QuarterTrackLength;
        }

        private static bool IsSafeAbsolute(byte absolutePos)
        {
            foreach (byte safe in SafeAbsoluteTiles)
            {
                if (safe == absolutePos)
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Players: ").Append(PlayerCount).Append(" | ");

            for (int p = 0; p < PlayerCount; p++)
            {
                if (p > 0) sb.Append(" || ");
                sb.Append("P").Append(p).Append(": ");

                int start = LudoUtil.GetPlayerTokenStart(p);
                for (int t = 0; t < LudoUtil.TokensPerPlayer; t++)
                {
                    if (t > 0) sb.Append(", ");

                    int tokenIndex = start + t;
                    byte pos = tokenPositions[tokenIndex];

                    if (IsAtBase(tokenIndex))
                    {
                        sb.Append("Base");
                    }
                    else if (IsHome(tokenIndex))
                    {
                        sb.Append("Home✨");
                    }
                    else if (IsOnHomeStretch(tokenIndex))
                    {
                        int step = pos - HomeStretchStart + 1;
                        sb.Append("Stretch-").Append(step);
                    }
                    else
                    {
                        int abs = GetAbsolutePosition(tokenIndex);
                        sb.Append(pos).Append("@").Append(abs);
                        if (IsOnSafeTile(tokenIndex))
                            sb.Append("⭐");
                    }
                }
            }

            return sb.ToString();
        }
    }

    // -----------------------------------------
    // Public Game API (TryX, no events)
    // -----------------------------------------
    [Serializable]
    public class LudoGame
    {
        public LudoBoard board;
        public LudoState state;

        public int CurrentPlayer => state.currentPlayer;
        public bool gameWon;
        public int winner;

        private readonly Random _rng;

        private LudoGame(int playerCount, int? seed = null)
        {
            board = LudoBoard.Create(playerCount);
            state = LudoState.Create((byte)playerCount);
            gameWon = false;
            winner = -1;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public static LudoGame Create(int playerCount, int? seed = null) => new(playerCount, seed);

        /// <summary>
        /// Roll dice for the current player. Mutates state and may auto-advance the turn
        /// when no moves are available.
        /// </summary>
        public bool TryRollDice(out Dice dice, out GameError error)
        {
            dice = default;
            error = GameError.None;

            if (gameWon)
            {
                error = GameError.GameAlreadyWon; return false;
            }

            if (!state.CanRollDice())
            {
                error = GameError.NoTurnAvailable; return false;
            }

            // Generate roll
            byte rollValue = (byte)_rng.Next(LudoUtil.MinDiceValue, LudoUtil.MaxDiceValue + 1);
            dice = Dice.FromByte(rollValue);

            // Compute movable tokens
            if (!board.TryGetMovableTokens(state.currentPlayer, rollValue, out var mask, out error))
                return false;

            // Update state
            state.RecordDiceRoll(rollValue, mask);

            // If no moves available, advance turn immediately
            if (mask == MovableTokens.None)
            {
                state.AdvanceTurn();
            }

            return true;
        }

        /// <summary>
        /// Move a token for the current player. Clears/advances the turn according to rules.
        /// </summary>
        public bool TryMoveToken(int tokenLocalIndex, out MoveResult result, out GameError error)
        {
            result = default;
            error = GameError.None;

            if (gameWon)
            {
                error = GameError.GameAlreadyWon; return false;
            }

            if (!state.MustMakeMove())
            {
                error = GameError.NoTurnAvailable; return false;
            }

            if (!state.IsTokenMovable(tokenLocalIndex))
            {
                error = GameError.TokenNotMovable; return false;
            }

            int absIndex = LudoUtil.GetPlayerTokenStart(state.currentPlayer) + tokenLocalIndex;

            // 1) Move
            if (!board.TryMoveToken(absIndex, state.lastDiceRoll, out var newPos, out error))
                return false;

            // 2) Capture (non-failing)
            if (!board.TryCaptureOpponent(absIndex, out var capturedIndex, out _))
                capturedIndex = -1;

            // 3) Clear / Advance turn depending on 6s rule
            state.ClearTurnAfterMove(absIndex);

            // 4) Win check for the moving player
            var me = new PlayerId(state.currentPlayer); // note: may be same or next depending on turn advance
            if (board.TryHasPlayerWon((byte)((absIndex) / LudoUtil.TokensPerPlayer), out var hasWon, out _)
                && hasWon)
            {
                gameWon = true;
                winner = (absIndex) / LudoUtil.TokensPerPlayer;
            }

            // Result
            result = new MoveResult { NewPosition = newPos, CapturedTokenIndex = capturedIndex };
            return true;
        }
    }

    public struct MoveResult
    {
        public byte NewPosition;
        public int CapturedTokenIndex;
        public bool DidCapture => CapturedTokenIndex >= 0;

        public static MoveResult CreateWithoutCapture(byte newPosition)
        {
            return new MoveResult { NewPosition = newPosition, CapturedTokenIndex = -1 };
        }

        public static MoveResult CreateWithCapture(byte newPosition, int capturedIndex)
        {
            return new MoveResult { NewPosition = newPosition, CapturedTokenIndex = capturedIndex };
        }
    }
}