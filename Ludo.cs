namespace Ludo
{
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


        public byte[] TokenPositions;
        public readonly int PlayerCount;

        public LudoBoard(int numberOfPlayers)
        {
            PlayerCount = numberOfPlayers;
            TokenPositions = new byte[PlayerCount * TokensPerPlayer];
        }

        public bool IsAtBase(int tokenIndex) => TokenPositions[tokenIndex] == BasePosition;

        public bool IsOnMainTrack(int tokenIndex) => TokenPositions[tokenIndex] >= StartPosition &&
                                                     TokenPositions[tokenIndex] <= TotalMainTrackTiles;

        public bool IsOnHomeStretch(int tokenIndex) => TokenPositions[tokenIndex] >= HomeStretchStartPosition &&
                                                       TokenPositions[tokenIndex] < HomePosition;

        public bool IsHome(int tokenIndex) => TokenPositions[tokenIndex] == HomePosition;
        
        public bool IsOnSafeTile(int tokenIndex)
        {
            if (IsOnHomeStretch(tokenIndex)) return true;
            if (!IsOnMainTrack(tokenIndex)) return false;

            var absolutePosition = GetAbsolutePosition(tokenIndex);
            return SafeAbsoluteTiles.Contains((byte)absolutePosition);
        }

        public bool HasWon(int playerIndex)
        {
            var playerTokenStartIndex = playerIndex * TokensPerPlayer;
            for (int i = 0; i < TokensPerPlayer; i++)
            {
                if (!IsHome(playerTokenStartIndex + i)) return false;
            }

            return true;
        }

        
        public List<int> GetMovableTokens(int playerIndex, int diceRoll)
        {
            var movableTokens = new List<int>();
            if (diceRoll < 1 || diceRoll > 6) return movableTokens;

            var playerTokenStartIndex = playerIndex * TokensPerPlayer;
            for (int i = 0; i < TokensPerPlayer; i++)
            {
                int tokenIndex = playerTokenStartIndex + i;
                if (CanMoveToken(tokenIndex, diceRoll))
                    movableTokens.Add(tokenIndex);
            }

            return movableTokens;
        }

        
        public void GetOutOfBase(int tokenIndex)
        {
            ValidateTokenIndex(tokenIndex);
            if (!IsAtBase(tokenIndex)) return;

            int playerIndex = tokenIndex / TokensPerPlayer;
            int startAbsolute = ToAbsoluteMainTrack(StartPosition, playerIndex);

            if (IsTileBlocked(startAbsolute, playerIndex)) return;

            TokenPositions[tokenIndex] = StartPosition;
        }

        
        public void MoveToken(int tokenIndex, int steps)
        {
            ValidateTokenIndex(tokenIndex);
            if (steps <= 0) return;

            if (!TryGetNewPosition(tokenIndex, steps, out byte newPosition))
                return;

            TokenPositions[tokenIndex] = newPosition;

            if (IsOnMainTrack(tokenIndex) && !IsOnSafeTile(tokenIndex))
            {
                CaptureTokensAt(tokenIndex);
            }
        }

        
        private bool TryGetNewPosition(int tokenIndex, int steps, out byte newPosition)
        {
            newPosition = TokenPositions[tokenIndex];

            if (IsHome(tokenIndex)) return false;

            int playerIndex = tokenIndex / TokensPerPlayer;

            if (IsAtBase(tokenIndex))
            {
                if (steps != ExitFromBaseAtRoll) return false;

                int startAbs = ToAbsoluteMainTrack(StartPosition, playerIndex);
                if (IsTileBlocked(startAbs, playerIndex)) return false;

                newPosition = StartPosition;
                return true;
            }

            byte current = TokenPositions[tokenIndex];

            if (IsOnMainTrack(tokenIndex))
            {
                int relativeTarget = current + steps;

                int stepsOnTrack = Math.Min(steps, TotalMainTrackTiles - current);
                for (int i = 1; i <= stepsOnTrack; i++)
                {
                    byte nextRelative = (byte)(current + i);
                    int nextAbsolute = ToAbsoluteMainTrack(nextRelative, playerIndex);
                    if (IsTileBlocked(nextAbsolute, playerIndex))
                        return false;
                }

                if (relativeTarget <= TotalMainTrackTiles)
                {
                    newPosition = (byte)relativeTarget;
                    return true;
                }

                
                int stepsIntoHome = relativeTarget - TotalMainTrackTiles; 
                int target = HomeStretchStartPosition + stepsIntoHome - 1; 
                if (target > HomePosition) return false;
                newPosition = (byte)target;
                return true;
            }

            if (IsOnHomeStretch(tokenIndex))
            {
                int target = current + steps;
                if (target > HomePosition) return false; 
                newPosition = (byte)target;
                return true;
            }

            return false;
        }

        private bool CanMoveToken(int tokenIndex, int steps) =>
            steps > 0 && TryGetNewPosition(tokenIndex, steps, out _);

        
        private void CaptureTokensAt(int movedTokenIndex)
        {
            if (!IsOnMainTrack(movedTokenIndex)) return;
            if (IsOnSafeTile(movedTokenIndex)) return;

            var movedTokenPlayerIndex = movedTokenIndex / TokensPerPlayer;
            var newAbsolutePosition = GetAbsolutePosition(movedTokenIndex);

            for (int i = 0; i < TokenPositions.Length; i++)
            {
                if (movedTokenPlayerIndex == (i / TokensPerPlayer)) continue;
                if (!IsOnMainTrack(i)) continue;

                var opponentAbsolutePosition = GetAbsolutePosition(i);
                if (newAbsolutePosition == opponentAbsolutePosition)
                {
                    TokenPositions[i] = BasePosition;
                }
            }
        }

        
        private bool IsTileBlocked(int absolutePosition, int movingPlayerIndex)
        {
            for (int opponentPlayerIndex = 0; opponentPlayerIndex < PlayerCount; opponentPlayerIndex++)
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
            var relativePosition = TokenPositions[tokenIndex];
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
            if (PlayerCount == 2) return playerIndex * 2 * PlayerTrackOffset;
            return playerIndex * PlayerTrackOffset;
        }

        
        private byte GetHomeEntryTile(int playerIndex)
        {
            int playerOffset = GetPlayerTrackOffset(playerIndex);
            if (playerOffset == 0) return TotalMainTrackTiles;
            return (byte)playerOffset;
        }

        private void ValidateTokenIndex(int tokenIndex)
        {
            if (tokenIndex < 0 || tokenIndex >= TokenPositions.Length)
                throw new ArgumentOutOfRangeException(nameof(tokenIndex));
        }
    }
}