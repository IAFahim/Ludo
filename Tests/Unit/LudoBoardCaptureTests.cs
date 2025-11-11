using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for LudoBoard class - Token capture mechanics
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("LudoBoard")]
    [Category("Capture")]
    public class LudoBoardCaptureTests
    {
        private LudoBoard _board;

        [SetUp]
        public void Setup()
        {
            _board = LudoBoard.Create(4);
        }

        [Test]
        public void TryCaptureOpponent_OnSameTile_CapturesOpponent()
        {
            _board.tokenPositions[0] = 10;
            _board.tokenPositions[4] = 23;
            _board.tokenPositions[4] = 10;

            var result = _board.TryCaptureOpponent(4);
            Assert.That(result.IsOk, Is.True);
        }

        [Test]
        public void TryCaptureOpponent_OnSafeTile_NoCapture()
        {
            _board.tokenPositions[0] = 1;
            _board.tokenPositions[4] = 14;

            _board.MoveToken(4, 1);
            var result = _board.TryCaptureOpponent(4);

            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(-1));
        }

        [Test]
        public void TryCaptureOpponent_MultipleOpponentsOnSameTile_NoCapture()
        {
            _board.tokenPositions[0] = 10;
            _board.tokenPositions[4] = 10;
            _board.tokenPositions[8] = 10;

            var result = _board.TryCaptureOpponent(0);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(-1));
        }
    }
}
