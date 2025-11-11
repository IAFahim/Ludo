using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for LudoBoard class - Game logic (movable tokens, win conditions)
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("LudoBoard")]
    [Category("GameLogic")]
    public class LudoBoardGameLogicTests
    {
        private LudoBoard _board;

        [SetUp]
        public void Setup()
        {
            _board = LudoBoard.Create(4);
        }

        [Test]
        public void GetMovableTokens_WithNoMovableTokens_ReturnsZeroMask()
        {
            var result = _board.GetMovableTokens(0, 3);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(MovableTokens.None));
        }

        [Test]
        public void GetMovableTokens_WithMovableTokens_ReturnsCorrectMask()
        {
            var result = _board.GetMovableTokens(0, 6);

            Assert.That(result.IsOk, Is.True);
            var mask = result.Unwrap();
            Assert.That(mask, Is.EqualTo(MovableTokens.T0 | MovableTokens.T1 | MovableTokens.T2 | MovableTokens.T3));
        }

        [Test]
        public void GetMovableTokens_SomeTokensMovable_ReturnsPartialMask()
        {
            _board.tokenPositions[0] = 1;
            _board.tokenPositions[1] = 1;

            var result = _board.GetMovableTokens(0, 3);
            Assert.That(result.IsOk, Is.True);
            var mask = result.Unwrap();
            Assert.That(mask.HasFlag(MovableTokens.T0), Is.True);
            Assert.That(mask.HasFlag(MovableTokens.T1), Is.True);
        }

        [Test]
        public void HasPlayerWon_WithAllTokensHome_ReturnsTrue()
        {
            for (int i = 0; i < 4; i++)
            {
                _board.tokenPositions[i] = 57;
            }

            var result = _board.HasPlayerWon(0);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.True);
        }

        [Test]
        public void HasPlayerWon_WithNotAllTokensHome_ReturnsFalse()
        {
            _board.tokenPositions[0] = 57;
            _board.tokenPositions[1] = 57;
            _board.tokenPositions[2] = 57;

            var result = _board.HasPlayerWon(0);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.False);
        }
    }
}
