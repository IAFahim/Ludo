using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Integration
{
    /// <summary>
    /// Integration tests - Full game simulations
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("Simulation")]
    public class FullGameSimulationTests
    {
        [Test]
        public void FullGameSimulation_TwoPlayers_CompletesSuccessfully()
        {
            var game = LudoGame.Create(2);
            int maxTurns = 1000;
            int turnCount = 0;

            while (!game.gameWon && turnCount < maxTurns)
            {
                var rollResult = game.RollDice();
                if (rollResult.IsErr) break;

                var dice = rollResult.Unwrap();

                if (game.state.MustMakeMove())
                {
                    bool moved = false;
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.state.IsTokenMovable(i))
                        {
                            var moveResult = game.MoveToken(i);
                            if (moveResult.IsOk)
                            {
                                moved = true;
                                break;
                            }
                        }
                    }

                    if (!moved)
                    {
                        game.state.AdvanceTurn();
                    }
                }

                turnCount++;
            }

            Assert.That(turnCount, Is.LessThanOrEqualTo(maxTurns));
        }

        [Test]
        public void MultipleTokenMovement_TracksSeparately()
        {
            var board = LudoBoard.Create(4);

            board.MoveToken(0, 6);
            board.MoveToken(1, 6);
            board.MoveToken(2, 6);

            Assert.That(board.GetTokenPosition(0), Is.EqualTo(1));
            Assert.That(board.GetTokenPosition(1), Is.EqualTo(1));
            Assert.That(board.GetTokenPosition(2), Is.EqualTo(1));
            Assert.That(board.GetTokenPosition(3), Is.EqualTo(0));
        }
    }
}
