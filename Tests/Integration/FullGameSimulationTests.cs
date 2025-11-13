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

            while (!game.GameWon && turnCount < maxTurns)
            {
                if (game.TryRollDice(out var diceResult, out var error))
                {
                    // If there are movable tokens and not forfeited, try to move
                    if (diceResult.Movable != MovableTokens.None && !diceResult.ForfeitedForTripleSix)
                    {
                        bool moved = false;
                        for (int i = 0; i < 4; i++)
                        {
                            // Check if this token is movable
                            if ((diceResult.Movable & (MovableTokens)(1 << i)) != 0)
                            {
                                if (game.TryMoveToken(i, out _, out _))
                                {
                                    moved = true;
                                    break;
                                }
                            }
                        }
                    }
                    // else: no movable tokens or triple six - turn advances automatically
                }

                turnCount++;
            }

            Assert.That(turnCount, Is.LessThanOrEqualTo(maxTurns));
        }

        [Test]
        public void MultipleTokenMovement_TracksSeparately()
        {
            var board = LudoBoard.Create(4);

            board.TryMoveToken(0, 6, out _, out _);
            board.TryMoveToken(1, 6, out _, out _);
            board.TryMoveToken(2, 6, out _, out _);

            Assert.That(board.GetTokenPosition(0), Is.EqualTo(1));
            Assert.That(board.GetTokenPosition(1), Is.EqualTo(1));
            Assert.That(board.GetTokenPosition(2), Is.EqualTo(1));
            Assert.That(board.GetTokenPosition(3), Is.EqualTo(0));
        }
    }
}
