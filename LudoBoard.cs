using Ludo;

namespace Ludo
{
    // Error types for Ludo game
    public enum LudoError
    {
        InvalidTokenIndex,
        TokenNotMovable,
        InvalidDiceRoll,
        TokenAlreadyHome,
        TokenNotAtBase,
        PathBlocked,
        WouldOvershootHome,
        InvalidPlayerIndex
    }

    public struct LudoBoard
    {
        private const byte BasePosition = 0;
        private const byte StartPosition = 1;
        private const byte TotalMainTrackTiles = 52;
        private const byte HomeStretchStartPosition = 53;
        public const byte StepsToHome = 6;
        private const byte HomePosition = HomeStretchStartPosition + StepsToHome;
        private const byte ExitFromBaseAtRoll = 6;
        private const byte TokensPerPlayer = 4;
        private const byte PlayerTrackOffset = TotalMainTrackTiles / 4;

        public static readonly byte[] SafeAbsoluteTiles = [1, 14, 27, 40];

        public byte[] tokenPositions;
        public readonly int playerCount;

        public Result<byte, LudoError> GetTokenPosition(int tokenIndex)
        {
            if (!IsValidTokenIndex(tokenIndex))
                return Result<byte, LudoError>.Err(LudoError.InvalidTokenIndex);

            return Result<byte, LudoError>.Ok(tokenPositions[tokenIndex]);
        }

        public Result<bool, LudoError> SetTokenPosition(int tokenIndex, byte position)
        {
            if (!IsValidTokenIndex(tokenIndex))
                return Result<bool, LudoError>.Err(LudoError.InvalidTokenIndex);

            tokenPositions[tokenIndex] = position;
            return Result<bool, LudoError>.Ok(true);
        }

        public LudoBoard(int numberOfPlayers)
        {
            playerCount = numberOfPlayers;
            tokenPositions = new byte[playerCount * TokensPerPlayer];
        }

        public bool IsAtBase(int tokenIndex) => tokenPositions[tokenIndex] == BasePosition;

        public bool IsOnMainTrack(int tokenIndex) => tokenPositions[tokenIndex] >= StartPosition &&
                                                     tokenPositions[tokenIndex] <= TotalMainTrackTiles;

        public bool IsOnHomeStretch(int tokenIndex) => tokenPositions[tokenIndex] >= HomeStretchStartPosition &&
                                                       tokenPositions[tokenIndex] < HomePosition;

        public bool IsHome(int tokenIndex) => tokenPositions[tokenIndex] == HomePosition;

        public bool IsOnSafeTile(int tokenIndex)
        {
            if (IsOnHomeStretch(tokenIndex)) return true;
            if (!IsOnMainTrack(tokenIndex)) return false;

            var absolutePosition = GetAbsolutePosition(tokenIndex);
            return SafeAbsoluteTiles.Contains((byte)absolutePosition);
        }

        public Result<bool, LudoError> HasWon(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerCount)
                return Result<bool, LudoError>.Err(LudoError.InvalidPlayerIndex);

            var playerTokenStartIndex = playerIndex * TokensPerPlayer;
            for (int i = 0; i < TokensPerPlayer; i++)
            {
                if (!IsHome(playerTokenStartIndex + i))
                    return Result<bool, LudoError>.Ok(false);
            }

            return Result<bool, LudoError>.Ok(true);
        }

        // Keep original for backwards compatibility with tests
        public bool HasWon_Legacy(int playerIndex)
        {
            var result = HasWon(playerIndex);
            return result.UnwrapOr(false);
        }

        public Result<List<int>, LudoError> GetMovableTokens(int playerIndex, int diceRoll)
        {
            if (playerIndex < 0 || playerIndex >= playerCount)
                return Result<List<int>, LudoError>.Err(LudoError.InvalidPlayerIndex);

            if (diceRoll < 1 || diceRoll > 6)
                return Result<List<int>, LudoError>.Err(LudoError.InvalidDiceRoll);

            var movableTokens = new List<int>();
            var playerTokenStartIndex = playerIndex * TokensPerPlayer;

            for (int i = 0; i < TokensPerPlayer; i++)
            {
                int tokenIndex = playerTokenStartIndex + i;
                if (CanMoveToken(tokenIndex, diceRoll))
                    movableTokens.Add(tokenIndex);
            }

            return Result<List<int>, LudoError>.Ok(movableTokens);
        }

        public Result<bool, LudoError> GetOutOfBase(int tokenIndex)
        {
            if (!IsValidTokenIndex(tokenIndex))
                return Result<bool, LudoError>.Err(LudoError.InvalidTokenIndex);

            if (!IsAtBase(tokenIndex))
                return Result<bool, LudoError>.Err(LudoError.TokenNotAtBase);

            int playerIndex = tokenIndex / TokensPerPlayer;
            int startAbsolute = ToAbsoluteMainTrack(StartPosition, playerIndex);

            if (IsTileBlocked(startAbsolute, playerIndex))
                return Result<bool, LudoError>.Err(LudoError.PathBlocked);

            tokenPositions[tokenIndex] = StartPosition;
            return Result<bool, LudoError>.Ok(true);
        }

        public Result<byte, LudoError> MoveToken(int tokenIndex, int steps)
        {
            if (!IsValidTokenIndex(tokenIndex))
                return Result<byte, LudoError>.Err(LudoError.InvalidTokenIndex);

            if (steps <= 0)
                return Result<byte, LudoError>.Err(LudoError.InvalidDiceRoll);

            if (IsHome(tokenIndex))
                return Result<byte, LudoError>.Err(LudoError.TokenAlreadyHome);

            var newPosResult = TryGetNewPosition(tokenIndex, steps);
            if (newPosResult.IsErr)
                return newPosResult;

            byte newPosition = newPosResult.Unwrap();
            tokenPositions[tokenIndex] = newPosition;

            if (IsOnMainTrack(tokenIndex) && !IsOnSafeTile(tokenIndex))
            {
                CaptureTokensAt(tokenIndex);
            }

            return Result<byte, LudoError>.Ok(newPosition);
        }

        private Result<byte, LudoError> TryGetNewPosition(int tokenIndex, int steps)
        {
            byte currentPosition = tokenPositions[tokenIndex];

            if (IsHome(tokenIndex))
                return Result<byte, LudoError>.Err(LudoError.TokenAlreadyHome);

            int playerIndex = tokenIndex / TokensPerPlayer;

            if (IsAtBase(tokenIndex))
            {
                if (steps != ExitFromBaseAtRoll)
                    return Result<byte, LudoError>.Err(LudoError.TokenNotMovable);

                int startAbs = ToAbsoluteMainTrack(StartPosition, playerIndex);
                if (IsTileBlocked(startAbs, playerIndex))
                    return Result<byte, LudoError>.Err(LudoError.PathBlocked);

                return Result<byte, LudoError>.Ok(StartPosition);
            }

            if (IsOnMainTrack(tokenIndex))
            {
                int relativeTarget = currentPosition + steps;

                int stepsOnTrack = Math.Min(steps, TotalMainTrackTiles - currentPosition);
                for (int i = 1; i <= stepsOnTrack; i++)
                {
                    byte nextRelative = (byte)(currentPosition + i);
                    int nextAbsolute = ToAbsoluteMainTrack(nextRelative, playerIndex);
                    if (IsTileBlocked(nextAbsolute, playerIndex))
                        return Result<byte, LudoError>.Err(LudoError.PathBlocked);
                }

                if (relativeTarget <= TotalMainTrackTiles)
                {
                    return Result<byte, LudoError>.Ok((byte)relativeTarget);
                }

                int stepsIntoHome = relativeTarget - TotalMainTrackTiles;
                int target = HomeStretchStartPosition + stepsIntoHome - 1;

                if (target > HomePosition)
                    return Result<byte, LudoError>.Err(LudoError.WouldOvershootHome);

                return Result<byte, LudoError>.Ok((byte)target);
            }

            if (IsOnHomeStretch(tokenIndex))
            {
                int target = currentPosition + steps;
                if (target > HomePosition)
                    return Result<byte, LudoError>.Err(LudoError.WouldOvershootHome);

                return Result<byte, LudoError>.Ok((byte)target);
            }

            return Result<byte, LudoError>.Err(LudoError.TokenNotMovable);
        }

        private bool CanMoveToken(int tokenIndex, int steps)
        {
            if (steps <= 0) return false;
            var result = TryGetNewPosition(tokenIndex, steps);
            return result.IsOk;
        }

        private void CaptureTokensAt(int movedTokenIndex)
        {
            if (!IsOnMainTrack(movedTokenIndex)) return;
            if (IsOnSafeTile(movedTokenIndex)) return;

            var movedTokenPlayerIndex = movedTokenIndex / TokensPerPlayer;
            var newAbsolutePosition = GetAbsolutePosition(movedTokenIndex);

            for (int i = 0; i < tokenPositions.Length; i++)
            {
                if (movedTokenPlayerIndex == (i / TokensPerPlayer)) continue;
                if (!IsOnMainTrack(i)) continue;

                var opponentAbsolutePosition = GetAbsolutePosition(i);
                if (newAbsolutePosition == opponentAbsolutePosition)
                {
                    tokenPositions[i] = BasePosition;
                }
            }
        }

        private bool IsTileBlocked(int absolutePosition, int movingPlayerIndex)
        {
            for (int opponentPlayerIndex = 0; opponentPlayerIndex < playerCount; opponentPlayerIndex++)
            {
                if (opponentPlayerIndex == movingPlayerIndex) continue;

                int opponentStartIndex = opponentPlayerIndex * TokensPerPlayer;
                int countOnTile = 0;

                for (int i = 0; i < TokensPerPlayer; i++)
                {
                    int tokenIndex = opponentStartIndex + i;
                    if (IsOnMainTrack(tokenIndex) && GetAbsolutePosition(tokenIndex) == absolutePosition)
                    {
                        countOnTile++;
                        if (countOnTile >= 2) return true;
                    }
                }
            }

            return false;
        }

        private int GetAbsolutePosition(int tokenIndex)
        {
            if (!IsOnMainTrack(tokenIndex)) return -1;

            var playerIndex = tokenIndex / TokensPerPlayer;
            var relativePosition = tokenPositions[tokenIndex];
            int playerOffset = GetPlayerTrackOffset(playerIndex);

            int absolutePosition = (relativePosition - 1 + playerOffset) % TotalMainTrackTiles + 1;
            return absolutePosition;
        }

        private int ToAbsoluteMainTrack(byte relativeMainTrackTile, int playerIndex)
        {
            int playerOffset = GetPlayerTrackOffset(playerIndex);
            return (relativeMainTrackTile - 1 + playerOffset) % TotalMainTrackTiles + 1;
        }

        private int GetPlayerTrackOffset(int playerIndex)
        {
            if (playerCount == 2) return playerIndex * 2 * PlayerTrackOffset;
            return playerIndex * PlayerTrackOffset;
        }

        private byte GetHomeEntryTile(int playerIndex)
        {
            int playerOffset = GetPlayerTrackOffset(playerIndex);
            if (playerOffset == 0) return TotalMainTrackTiles;
            return (byte)playerOffset;
        }

        private bool IsValidTokenIndex(int tokenIndex)
        {
            return tokenIndex >= 0 && tokenIndex < tokenPositions.Length;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('P').Append(playerCount).Append(" | ");
            for (int p = 0; p < playerCount; p++)
            {
                if (p > 0) sb.Append(" || ");
                sb.Append('p').Append(p).Append(':');

                int start = p * TokensPerPlayer;
                for (int t = 0; t < TokensPerPlayer; t++)
                {
                    if (t > 0) sb.Append(',');
                    int idx = start + t;
                    byte pos = tokenPositions[idx];

                    if (IsAtBase(idx))
                    {
                        sb.Append('B');
                        continue;
                    }

                    if (IsHome(idx))
                    {
                        sb.Append('H');
                        continue;
                    }

                    if (IsOnHomeStretch(idx))
                    {
                        int step = pos - HomeStretchStartPosition + 1; // 1..StepsToHome
                        sb.Append('S').Append(step);
                        if (IsOnSafeTile(idx)) sb.Append('*');
                        continue;
                    }

                    // On main track
                    int abs = GetAbsolutePosition(idx);
                    sb.Append(pos).Append('@').Append(abs);
                    if (IsOnSafeTile(idx)) sb.Append('*');
                }
            }

            return sb.ToString();
        }
    }
}