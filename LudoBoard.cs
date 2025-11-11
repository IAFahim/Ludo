using System;
using System.Text;
using static Ludo.ResultExtensions;

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

    // Generic Result<T, TError> for Railway Oriented Programming
    public readonly struct Result<T, TError>
    {
        private readonly bool _isOk;
        private readonly T _value;
        private readonly TError _error;

        private Result(bool isOk, T value, TError error)
        {
            _isOk = isOk;
            _value = value;
            _error = error;
        }

        public static Result<T, TError> Ok(T value) => new Result<T, TError>(true, value, default!);
        public static Result<T, TError> Err(TError error) => new Result<T, TError>(false, default!, error);

        public bool IsOk => _isOk;
        public bool IsErr => !_isOk;

        public T Unwrap()
        {
            if (!_isOk) throw new InvalidOperationException($"Called Unwrap on Err: {_error}");
            return _value;
        }

        public T UnwrapOr(T defaultValue) => _isOk ? _value : defaultValue;

        public T UnwrapOrElse(Func<TError, T> defaultFunc) => _isOk ? _value : defaultFunc(_error);

        public TError UnwrapErr()
        {
            if (_isOk) throw new InvalidOperationException("Called UnwrapErr on Ok");
            return _error;
        }

        public Result<U, TError> Map<U>(Func<T, U> mapper) =>
            _isOk ? Result<U, TError>.Ok(mapper(_value)) : Result<U, TError>.Err(_error);

        public Result<T, F> MapErr<F>(Func<TError, F> mapper) =>
            _isOk ? Result<T, F>.Ok(_value) : Result<T, F>.Err(mapper(_error));

        public Result<U, TError> AndThen<U>(Func<T, Result<U, TError>> binder) =>
            _isOk ? binder(_value) : Result<U, TError>.Err(_error);

        public bool TryGetValue(out T value, out TError error)
        {
            value = _value;
            error = _error;
            return _isOk;
        }
    }

    // Small helpers for “railway” flow
    public readonly struct Unit
    {
        public static readonly Unit Value = new();
    }

    public static class ResultExtensions
    {
        public static Result<Unit, E> Ensure<E>(bool condition, E error) =>
            condition ? Result<Unit, E>.Ok(Unit.Value) : Result<Unit, E>.Err(error);

        public static Result<T, E> Tap<T, E>(this Result<T, E> r, Action<T> action)
        {
            if (r.IsOk) action(r.Unwrap());
            return r;
        }
    }

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

    [Serializable]
    public struct LudoState
    {
        public byte currentPlayer;
        public byte playerCount;
        public byte consecutiveSixes;
        public byte lastDiceRoll;
        public bool hasRolled;
        public bool mustMove;
        public sbyte movableTokensMask;

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
                movableTokensMask = 0
            };
        }

        public bool CanRollDice() => !hasRolled && !mustMove;
        public bool MustMakeMove() => mustMove;
        public bool HasMovableTokens() => movableTokensMask != 0;

        public void RecordDiceRoll(byte diceValue, sbyte movableMask)
        {
            lastDiceRoll = diceValue;
            movableTokensMask = movableMask;
            hasRolled = true;
            mustMove = movableMask != 0;

            if (diceValue == 6) consecutiveSixes++;
            else consecutiveSixes = 0;
        }

        public void AdvanceTurn()
        {
            currentPlayer = (byte)((currentPlayer + 1) % playerCount);
            lastDiceRoll = 0;
            hasRolled = false;
            mustMove = false;
            movableTokensMask = 0;
        }

        public void ClearTurnAfterMove(int movedTokenIndex)
        {
            hasRolled = false;
            mustMove = false;
            movableTokensMask = 0;

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

        public bool IsTokenMovable(int tokenLocalIndex) => (movableTokensMask & (1 << tokenLocalIndex)) != 0;
    }

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

        // ---------------- Core Move Operations (Railway) ----------------

        public Result<byte, GameError> MoveToken(int tokenIndex, byte diceRoll)
        {
            if (!LudoUtil.IsValidTokenIndex(tokenIndex, tokenPositions.Length))
                return Result<byte, GameError>.Err(GameError.InvalidTokenIndex);

            if (!LudoUtil.IsValidDiceRoll(diceRoll))
                return Result<byte, GameError>.Err(GameError.InvalidDiceRoll);

            if (IsHome(tokenIndex))
                return Result<byte, GameError>.Err(GameError.TokenAlreadyHome);

            // Handle exit from base
            if (IsAtBase(tokenIndex))
            {
                if (diceRoll != ExitDiceValue)
                    return Result<byte, GameError>.Err(GameError.TokenNotAtBase);

                tokenPositions[tokenIndex] = StartPosition;
                return Result<byte, GameError>.Ok(StartPosition);
            }

            var res = CalculateNewPosition(tokenIndex, diceRoll);
            if (res.IsOk)
            {
                var newPosition = res.Unwrap();
                tokenPositions[tokenIndex] = newPosition;
            }
            return res;
        }


        public Result<int, GameError> TryCaptureOpponent(int movedTokenIndex)
        {
            // Non-failing: Ok(-1) indicates no capture
            if (!IsOnMainTrack(movedTokenIndex) || IsOnSafeTile(movedTokenIndex))
                return Result<int, GameError>.Ok(-1);

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
                    return Result<int, GameError>.Ok(-1); // Multiple opponents block capture
            }

            if (opponentCount == 1)
            {
                tokenPositions[opponentIndex] = BasePosition;
                return Result<int, GameError>.Ok(opponentIndex);
            }

            return Result<int, GameError>.Ok(-1);
        }

        // ---------------- Query Operations (Railway) ----------------

        public Result<sbyte, GameError> GetMovableTokens(int playerIndex, byte diceRoll)
        {
            if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
                return Result<sbyte, GameError>.Err(GameError.InvalidPlayerIndex);

            if (!LudoUtil.IsValidDiceRoll(diceRoll))
                return Result<sbyte, GameError>.Err(GameError.InvalidDiceRoll);

            sbyte movableMask = 0;
            int tokenStart = LudoUtil.GetPlayerTokenStart(playerIndex);

            for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
            {
                int tokenIndex = tokenStart + i;
                if (CanMoveToken(tokenIndex, diceRoll))
                {
                    movableMask |= (sbyte)(1 << i);
                }
            }

            return Result<sbyte, GameError>.Ok(movableMask);
        }

        public Result<bool, GameError> HasPlayerWon(int playerIndex)
        {
            if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
                return Result<bool, GameError>.Err(GameError.InvalidPlayerIndex);

            int tokenStart = LudoUtil.GetPlayerTokenStart(playerIndex);
            for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
            {
                if (!IsHome(tokenStart + i))
                    return Result<bool, GameError>.Ok(false);
            }

            return Result<bool, GameError>.Ok(true);
        }

        public byte GetTokenPosition(int tokenIndex) =>
            LudoUtil.IsValidTokenIndex(tokenIndex, tokenPositions.Length) ? tokenPositions[tokenIndex] : (byte)0;

        // ---------------- Position Checks ----------------

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

        // ---------------- Private Helpers ----------------

        private bool CanMoveToken(int tokenIndex, byte diceRoll)
        {
            if (IsHome(tokenIndex))
                return false;

            if (IsAtBase(tokenIndex))
                return diceRoll == ExitDiceValue;

            var result = CalculateNewPosition(tokenIndex, diceRoll);
            return result.IsOk;
        }

        private Result<byte, GameError> CalculateNewPosition(int tokenIndex, byte diceRoll)
        {
            byte currentPos = tokenPositions[tokenIndex];

            // On main track
            if (IsOnMainTrack(tokenIndex))
            {
                int targetPos = currentPos + diceRoll;

                if (targetPos <= MainTrackLength)
                    return Result<byte, GameError>.Ok((byte)targetPos);

                // Entering home stretch
                int stepsIntoHome = targetPos - MainTrackLength;
                int homePos = HomeStretchStart + stepsIntoHome - 1;

                if (homePos > HomePosition)
                    return Result<byte, GameError>.Err(GameError.WouldOvershootHome);

                return Result<byte, GameError>.Ok((byte)homePos);
            }

            // On home stretch
            if (IsOnHomeStretch(tokenIndex))
            {
                int targetPos = currentPos + diceRoll;

                if (targetPos > HomePosition)
                    return Result<byte, GameError>.Err(GameError.WouldOvershootHome);

                return Result<byte, GameError>.Ok((byte)targetPos);
            }

            return Result<byte, GameError>.Err(GameError.TokenNotMovable);
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

    [Serializable]
    public class LudoGame
    {
        public LudoBoard board;
        public LudoState state;

        public int CurrentPlayer => state.currentPlayer;
        public bool gameWon;
        public int winner;

        private readonly Random _rng = new();

        private LudoGame(int playerCount)
        {
            board = LudoBoard.Create(playerCount);
            state = LudoState.Create((byte)playerCount);
            gameWon = false;
            winner = -1;
        }

        public static LudoGame Create(int playerCount) => new(playerCount);

        public Result<byte, GameError> RollDice()
        {
            return Ensure(!gameWon, GameError.GameAlreadyWon)
                .AndThen(_ => Ensure(state.CanRollDice(), GameError.NoTurnAvailable))
                .Map(_ => (byte)_rng.Next(LudoUtil.MinDiceValue, LudoUtil.MaxDiceValue + 1))
                .AndThen(dice =>
                    board.GetMovableTokens(state.currentPlayer, dice)
                         .Map(mask => (dice, mask))
                )
                .Tap(t =>
                {
                    state.RecordDiceRoll(t.dice, t.mask);
                    if (t.mask == 0) state.AdvanceTurn(); // auto-advance if no movable tokens
                })
                .Map(t => t.dice);
        }

        public Result<MoveResult, GameError> MoveToken(int tokenLocalIndex)
        {
            return Ensure(!gameWon, GameError.GameAlreadyWon)
                .AndThen(_ => Ensure(state.MustMakeMove(), GameError.NoTurnAvailable))
                .AndThen(_ => Ensure(state.IsTokenMovable(tokenLocalIndex), GameError.TokenNotMovable))
                .Map(_ => LudoUtil.GetPlayerTokenStart(state.currentPlayer) + tokenLocalIndex) // absolute token index
                .AndThen(tokenIndex =>
                    board.MoveToken(tokenIndex, state.lastDiceRoll)
                         .Map(newPos => (tokenIndex, newPos))
                )
                .AndThen(t =>
                    board.TryCaptureOpponent(t.tokenIndex)
                         .Map(captured => (t.tokenIndex, t.newPos, captured))
                )
                .Tap(t => state.ClearTurnAfterMove(t.tokenIndex))
                .Tap(t =>
                {
                    var win = board.HasPlayerWon(LudoUtil.GetPlayerFromToken(t.tokenIndex));
                    if (win.IsOk && win.Unwrap())
                    {
                        gameWon = true;
                        winner = LudoUtil.GetPlayerFromToken(t.tokenIndex);
                    }
                })
                .Map(t => new MoveResult
                {
                    NewPosition = t.newPos,
                    CapturedTokenIndex = t.captured
                });
        }
    }

    public struct MoveResult
    {
        public byte NewPosition;
        public int CapturedTokenIndex;

        public bool DidCapture => CapturedTokenIndex >= 0;
    }
}
