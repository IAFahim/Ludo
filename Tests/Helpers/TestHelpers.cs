using Ludo;

namespace Ludo.Tests.Helpers
{
    /// <summary>
    /// Helper methods for setting up test scenarios
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a board with all tokens for a player at home
        /// </summary>
        public static LudoBoard CreateBoardWithPlayerWon(int playerIndex, int playerCount = 4)
        {
            var board = LudoBoard.Create(playerCount);
            int tokenStart = playerIndex * TestConstants.TokensPerPlayer;

            for (int i = 0; i < TestConstants.TokensPerPlayer; i++)
            {
                board.TokenPositions[tokenStart + i] = TestConstants.HomePosition;
            }

            return board;
        }

        /// <summary>
        /// Creates a board with tokens at specific positions
        /// </summary>
        public static LudoBoard CreateBoardWithTokens(params (int tokenIndex, byte position)[] tokens)
        {
            var board = LudoBoard.Create(4);

            foreach (var (tokenIndex, position) in tokens)
            {
                if (tokenIndex >= 0 && tokenIndex < board.TokenPositions.Length)
                {
                    board.TokenPositions[tokenIndex] = position;
                }
            }

            return board;
        }

        /// <summary>
        /// Moves a token out of base (simulates rolling a 6)
        /// </summary>
        public static void MoveTokenOutOfBase(LudoBoard board, int tokenIndex)
        {
            board.TryMoveToken(tokenIndex, TestConstants.ExitDiceValue, out _, out _);
        }

        /// <summary>
        /// Creates a game state with a specific player's turn
        /// </summary>
        public static LudoState CreateStateWithCurrentPlayer(int playerIndex, int playerCount = 4)
        {
            var state = LudoState.Create((byte)playerCount);
            while (state.CurrentPlayer != playerIndex)
            {
                state.AdvanceTurn();
            }
            return state;
        }
    }
}
