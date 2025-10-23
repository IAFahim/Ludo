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

        
        public static readonly byte[] SafeAbsoluteTiles = { 1, 14, 27, 40 };

        
        public byte[] TokenPositions;

        public readonly int PlayerCount;

        public LudoBoard(int numberOfPlayers)
        {
            PlayerCount = numberOfPlayers;
            TokenPositions = new byte[PlayerCount * TokensPerPlayer];
        }

        public bool IsAtBase(int tokenIndex) => TokenPositions[tokenIndex] == BasePosition;

        public bool IsOnMainTrack(int tokenIndex)
        {
            var pos = TokenPositions[tokenIndex];
            return pos is >= StartPosition and <= TotalMainTrackTiles;
        }

        public bool IsOnSafeTile(int tokenIndex)
        {
            if (!IsOnMainTrack(tokenIndex)) return false;

            var playerIndex = tokenIndex / TokensPerPlayer;
            var absolutePosition = GetAbsolutePosition(tokenIndex, playerIndex);
            return SafeAbsoluteTiles.Contains((byte)absolutePosition);
        }

        public bool IsOnHomeStretch(int tokenIndex)
        {
            var tokenPosition = TokenPositions[tokenIndex];
            return tokenPosition >= HomeStretchStartPosition && tokenPosition < HomePosition;
        }

        public bool IsHome(int tokenIndex) => TokenPositions[tokenIndex] == HomePosition;

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
            var playerTokenStartIndex = playerIndex * TokensPerPlayer;

            for (int i = 0; i < TokensPerPlayer; i++)
            {
                int tokenIndex = playerTokenStartIndex + i;
                if (CanMoveToken(tokenIndex, diceRoll))
                {
                    movableTokens.Add(tokenIndex);
                }
            }
            return movableTokens;
        }

        
        public void GetOutOfBase(int tokenIndex)
        {
            if (IsAtBase(tokenIndex)) TokenPositions[tokenIndex] = StartPosition;
        }

        public void SendToBase(int tokenIndex) => TokenPositions[tokenIndex] = BasePosition;


        public void MoveToken(int tokenIndex, int steps, bool checkForCapture = true)
        {
            if (IsAtBase(tokenIndex) || IsHome(tokenIndex) || steps <= 0) return;

            var currentPosition = TokenPositions[tokenIndex];
            int playerIndex = tokenIndex / TokensPerPlayer;

            byte newPosition;

            if (IsOnMainTrack(tokenIndex))
            {
                byte homeEntryTile = TotalMainTrackTiles;
                if (currentPosition + steps > homeEntryTile)
                {
                    int stepsIntoHome = (currentPosition + steps) - homeEntryTile;
                    newPosition = (byte)(HomeStretchStartPosition + stepsIntoHome - 1);
                }
                else
                {
                    newPosition = (byte)(currentPosition + steps);
                }
            }
            else
            {
                newPosition = (byte)(currentPosition + steps);
            }

            if (newPosition > HomePosition) return;
            TokenPositions[tokenIndex] = newPosition;
            if (checkForCapture && IsOnMainTrack(tokenIndex) && !IsOnSafeTile(tokenIndex))
            {
                var newAbsolutePosition = GetAbsolutePosition(tokenIndex, playerIndex);

                for (int i = 0; i < TokenPositions.Length; i++)
                {
                    if (playerIndex == (i / TokensPerPlayer)) continue;

                    if (IsOnMainTrack(i))
                    {
                        var opponentPlayerIndex = i / TokensPerPlayer;
                        var opponentAbsolutePosition = GetAbsolutePosition(i, opponentPlayerIndex);

                        if (newAbsolutePosition == opponentAbsolutePosition)
                        {
                            SendToBase(i);
                        }
                    }
                }
            }
        }

        
        private bool CanMoveToken(int tokenIndex, int steps)
        {
            if (IsHome(tokenIndex)) return false;

            if (IsAtBase(tokenIndex))
            {
                return steps == ExitFromBaseAtRoll;
            }

            var currentPosition = TokenPositions[tokenIndex];
            if (IsOnHomeStretch(tokenIndex))
            {
                return currentPosition + steps <= HomePosition;
            }

            return true;
        }

        private int GetAbsolutePosition(int tokenIndex, int playerIndex)
        {
            if (!IsOnMainTrack(tokenIndex)) return -1;

            var relativePosition = TokenPositions[tokenIndex];
            int absolutePosition = (relativePosition - 1 + (playerIndex * PlayerTrackOffset)) % TotalMainTrackTiles + 1;
            return absolutePosition;
        }

        public bool IsStartTileForAnyPlayer(int absolutePosition)
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                if (absolutePosition == (i * PlayerTrackOffset) + 1)
                {
                    return true;
                }
            }
            return false;
        }

        
        public int IsOccupiedByOpponent(int absolutePosition, int currentPlayerIndex)
        {
            for (int i = 0; i < TokenPositions.Length; i++)
            {
                int playerIndex = i / TokensPerPlayer;
                if (playerIndex != currentPlayerIndex && IsOnMainTrack(i))
                {
                    if (GetAbsolutePosition(i, playerIndex) == absolutePosition)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }
}