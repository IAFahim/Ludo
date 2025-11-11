using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Integration
{
    /// <summary>
    /// Integration tests - Game rules enforcement
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("GameRules")]
    public class GameRulesIntegrationTests
    {
        [Test]
        public void ConsecutiveSixes_HandledCorrectly()
        {
            var state = LudoState.Create(2);

            state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(state.consecutiveSixes, Is.EqualTo(1));

            state.hasRolled = false;
            state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(state.consecutiveSixes, Is.EqualTo(2));

            state.hasRolled = false;
            state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(state.consecutiveSixes, Is.EqualTo(3));

            state.ClearTurnAfterMove(0);
            Assert.That(state.currentPlayer, Is.EqualTo(1));
            Assert.That(state.consecutiveSixes, Is.EqualTo(0));
        }

        [Test]
        public void PlayerTurnRotation_WorksCorrectly()
        {
            var game = LudoGame.Create(4);

            for (int turn = 0; turn < 8; turn++)
            {
                int expectedPlayer = turn % 4;
                int currentPlayer = game.CurrentPlayer;
                Assert.That(currentPlayer, Is.EqualTo(expectedPlayer));

                game.state.AdvanceTurn();
            }
        }

        [Test]
        public void TokenCapture_ResendsTokenToBase()
        {
            var board = LudoBoard.Create(2);

            board.tokenPositions[0] = 10;
            board.tokenPositions[4] = 10;

            var result = board.TryCaptureOpponent(0);
            Assert.That(result.IsOk, Is.True);
        }

        [Test]
        public void AllPlayersTokens_InitializeAtBase()
        {
            var board = LudoBoard.Create(4);

            for (int player = 0; player < 4; player++)
            {
                for (int token = 0; token < 4; token++)
                {
                    int tokenIndex = player * 4 + token;
                    Assert.That(board.IsAtBase(tokenIndex), Is.True);
                }
            }
        }

        [Test]
        public void WinCondition_RequiresAllTokensHome()
        {
            var board = LudoBoard.Create(2);

            board.tokenPositions[0] = 57;
            board.tokenPositions[1] = 57;
            board.tokenPositions[2] = 57;

            var result = board.HasPlayerWon(0);
            Assert.That(result.Unwrap(), Is.False);

            board.tokenPositions[3] = 57;
            result = board.HasPlayerWon(0);
            Assert.That(result.Unwrap(), Is.True);
        }

        [Test]
        public void SafeTiles_PreventCapture()
        {
            var board = LudoBoard.Create(4);

            board.tokenPositions[0] = 1;
            Assert.That(board.IsOnSafeTile(0), Is.True);

            board.tokenPositions[4] = 1;

            var result = board.TryCaptureOpponent(4);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(-1));
        }

        [Test]
        public void HomeStretch_IsSafe()
        {
            var board = LudoBoard.Create(2);

            board.tokenPositions[0] = 52;
            Assert.That(board.IsOnHomeStretch(0), Is.True);
            Assert.That(board.IsOnSafeTile(0), Is.True);
        }
    }
}
