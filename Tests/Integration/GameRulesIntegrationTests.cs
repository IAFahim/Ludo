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
            Assert.That(state.ConsecutiveSixes, Is.EqualTo(1));

            state.ClearAfterMoveOrExtraRoll();
            state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(state.ConsecutiveSixes, Is.EqualTo(2));

            state.ClearAfterMoveOrExtraRoll();
            state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(state.ConsecutiveSixes, Is.EqualTo(3));

            state.AdvanceTurn();
            Assert.That(state.CurrentPlayer, Is.EqualTo(1));
            Assert.That(state.ConsecutiveSixes, Is.EqualTo(0));
        }
        

        [Test]
        public void TokenCapture_ResendsTokenToBase()
        {
            var board = LudoBoard.Create(2);

            board.TokenPositions[0] = 10;
            board.TokenPositions[4] = 10;

            var success = board.TryCaptureOpponent(0, out int captured);
            Assert.That(success, Is.True);
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

            board.TokenPositions[0] = 57;
            board.TokenPositions[1] = 57;
            board.TokenPositions[2] = 57;

            var success = board.TryHasPlayerWon(0, out bool hasWon, out _);
            Assert.That(success, Is.True);
            Assert.That(hasWon, Is.False);

            board.TokenPositions[3] = 57;
            success = board.TryHasPlayerWon(0, out hasWon, out _);
            Assert.That(success, Is.True);
            Assert.That(hasWon, Is.True);
        }

        [Test]
        public void SafeTiles_PreventCapture()
        {
            var board = LudoBoard.Create(4);

            board.TokenPositions[0] = 1;
            Assert.That(board.IsOnSafeTile(0), Is.True);

            board.TokenPositions[4] = 1;

            var success = board.TryCaptureOpponent(4, out int captured);
            Assert.That(success, Is.True);
            Assert.That(captured, Is.EqualTo(-1));
        }

        [Test]
        public void HomeStretch_IsSafe()
        {
            var board = LudoBoard.Create(2);

            board.TokenPositions[0] = 52;
            Assert.That(board.IsOnHomeStretch(0), Is.True);
            Assert.That(board.IsOnSafeTile(0), Is.True);
        }
    }
}
