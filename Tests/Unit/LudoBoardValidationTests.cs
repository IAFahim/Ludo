using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for LudoBoard class - Validation and error handling
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("LudoBoard")]
    [Category("Validation")]
    public class LudoBoardValidationTests
    {
        private LudoBoard _board;

        [SetUp]
        public void Setup()
        {
            _board = LudoBoard.Create(4);
        }

        [Test]
        public void MoveToken_InvalidTokenIndex_ReturnsError()
        {
            var result = _board.MoveToken(20, 6);

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.InvalidTokenIndex));
        }

        [Test]
        public void MoveToken_InvalidDiceRoll_ReturnsError()
        {
            var result = _board.MoveToken(0, 7);

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.InvalidDiceRoll));
        }

        [Test]
        public void MoveToken_AlreadyHome_ReturnsError()
        {
            _board.tokenPositions[0] = 57;
            var result = _board.MoveToken(0, 3);

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.TokenAlreadyHome));
        }

        [Test]
        public void MoveToken_WouldOvershootHome_ReturnsError()
        {
            _board.tokenPositions[0] = 55;
            var result = _board.MoveToken(0, 6);

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.WouldOvershootHome));
        }

        [Test]
        public void GetMovableTokens_InvalidPlayerIndex_ReturnsError()
        {
            var result = _board.GetMovableTokens(5, 6);
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.InvalidPlayerIndex));
        }

        [Test]
        public void GetMovableTokens_InvalidDiceRoll_ReturnsError()
        {
            var result = _board.GetMovableTokens(0, 7);
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.InvalidDiceRoll));
        }

        [Test]
        public void HasPlayerWon_InvalidPlayerIndex_ReturnsError()
        {
            var result = _board.HasPlayerWon(5);
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.InvalidPlayerIndex));
        }
    }
}
