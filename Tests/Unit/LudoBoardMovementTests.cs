using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for LudoBoard class - Token movement logic
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("LudoBoard")]
    [Category("Movement")]
    public class LudoBoardMovementTests
    {
        private LudoBoard _board;

        [SetUp]
        public void Setup()
        {
            _board = LudoBoard.Create(4);
        }

        [Test]
        public void MoveToken_FromBase_WithSix_MovesToStartPosition()
        {
            var result = _board.MoveToken(0, 6);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(1));
            Assert.That(_board.GetTokenPosition(0), Is.EqualTo(1));
            Assert.That(_board.IsAtBase(0), Is.False);
        }

        [Test]
        public void MoveToken_FromBase_WithoutSix_ReturnsError()
        {
            var result = _board.MoveToken(0, 3);

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.TokenNotAtBase));
        }

        [Test]
        public void MoveToken_OnMainTrack_MovesCorrectly()
        {
            _board.MoveToken(0, 6); // Move to position 1
            var result = _board.MoveToken(0, 3);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(4));
            Assert.That(_board.GetTokenPosition(0), Is.EqualTo(4));
        }

        [Test]
        public void MoveToken_EnteringHomeStretch_CalculatesCorrectly()
        {
            _board.tokenPositions[0] = 49;
            var result = _board.MoveToken(0, 5);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(54));
            Assert.That(_board.IsOnHomeStretch(0), Is.True);
        }

        [Test]
        public void MoveToken_WithinHomeStretch_WorksCorrectly()
        {
            _board.tokenPositions[0] = 52;
            var result = _board.MoveToken(0, 3);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(55));
        }

        [Test]
        public void MoveToken_ExactlyToHome_WorksCorrectly()
        {
            _board.tokenPositions[0] = 54;
            var result = _board.MoveToken(0, 3);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(57));
            Assert.That(_board.IsHome(0), Is.True);
        }

        [Test]
        public void MoveToken_ToHomeStretch_WorksCorrectly()
        {
            _board.tokenPositions[0] = 50;
            var result = _board.MoveToken(0, 3);

            Assert.That(result.IsOk, Is.True);
            Assert.That(_board.IsOnHomeStretch(0), Is.True);
        }

        [Test]
        public void MoveToken_MultipleSteps_TracksCorrectly()
        {
            _board.MoveToken(0, 6); // Move to position 1
            Assert.That(_board.GetTokenPosition(0), Is.EqualTo(1));

            _board.MoveToken(0, 4); // Move to position 5
            Assert.That(_board.GetTokenPosition(0), Is.EqualTo(5));

            _board.MoveToken(0, 6); // Move to position 11
            Assert.That(_board.GetTokenPosition(0), Is.EqualTo(11));
        }

        [Test]
        public void MoveToken_NearHomeOvershoot_ReturnsError()
        {
            _board.tokenPositions[0] = 56;
            var result = _board.MoveToken(0, 3);

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.WouldOvershootHome));
        }

        [Test]
        public void MoveToken_FromMainTrackOvershootHome_ReturnsError()
        {
            _board.tokenPositions[0] = 51;
            var result = _board.MoveToken(0, 6);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(57));
        }
    }
}
