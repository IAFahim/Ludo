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
            _board.TokenPositions[0] = 10;
            _board.TokenPositions[4] = 23;

            var success = _board.TryCaptureOpponent(4, out int captured);
            Assert.That(success, Is.True);
            Assert.That(captured, Is.EqualTo(0));
        }

        [Test]
        public void TryCaptureOpponent_OnSafeTile_NoCapture()
        {
            _board.TokenPositions[0] = 1;
            _board.TokenPositions[4] = 14;

            _board.TryMoveToken(4, 1, out _, out _);
            var success = _board.TryCaptureOpponent(4, out int captured);

            Assert.That(success, Is.True);
            Assert.That(captured, Is.EqualTo(-1));
        }

        [Test]
        public void TryCaptureOpponent_MultipleOpponentsOnSameTile_NoCapture()
        {
            _board.TokenPositions[0] = 10;
            _board.TokenPositions[4] = 10;
            _board.TokenPositions[8] = 10;

            var success = _board.TryCaptureOpponent(0, out int captured);
            Assert.That(success, Is.True);
            Assert.That(captured, Is.EqualTo(-1));
        }
    }
}
