using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for LudoUtil static helper class
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("LudoUtil")]
    public class LudoUtilTests
    {
        [Test]
        public void IsValidDiceRoll_ReturnsCorrectValue()
        {
            Assert.That(LudoUtil.IsValidDiceRoll(0), Is.False);
            Assert.That(LudoUtil.IsValidDiceRoll(1), Is.True);
            Assert.That(LudoUtil.IsValidDiceRoll(6), Is.True);
            Assert.That(LudoUtil.IsValidDiceRoll(7), Is.False);
        }

        [Test]
        public void IsValidPlayerIndex_ReturnsCorrectValue()
        {
            Assert.That(LudoUtil.IsValidPlayerIndex(-1, 4), Is.False);
            Assert.That(LudoUtil.IsValidPlayerIndex(0, 4), Is.True);
            Assert.That(LudoUtil.IsValidPlayerIndex(3, 4), Is.True);
            Assert.That(LudoUtil.IsValidPlayerIndex(4, 4), Is.False);
        }

        [Test]
        public void IsValidTokenIndex_ReturnsCorrectValue()
        {
            Assert.That(LudoUtil.IsValidTokenIndex(-1, 16), Is.False);
            Assert.That(LudoUtil.IsValidTokenIndex(0, 16), Is.True);
            Assert.That(LudoUtil.IsValidTokenIndex(15, 16), Is.True);
            Assert.That(LudoUtil.IsValidTokenIndex(16, 16), Is.False);
        }

        [Test]
        public void GetPlayerFromToken_ReturnsCorrectPlayer()
        {
            Assert.That(LudoUtil.GetPlayerFromToken(0), Is.EqualTo(0));
            Assert.That(LudoUtil.GetPlayerFromToken(3), Is.EqualTo(0));
            Assert.That(LudoUtil.GetPlayerFromToken(4), Is.EqualTo(1));
            Assert.That(LudoUtil.GetPlayerFromToken(7), Is.EqualTo(1));
            Assert.That(LudoUtil.GetPlayerFromToken(8), Is.EqualTo(2));
            Assert.That(LudoUtil.GetPlayerFromToken(12), Is.EqualTo(3));
        }

        [Test]
        public void GetPlayerTokenStart_ReturnsCorrectStart()
        {
            Assert.That(LudoUtil.GetPlayerTokenStart(0), Is.EqualTo(0));
            Assert.That(LudoUtil.GetPlayerTokenStart(1), Is.EqualTo(4));
            Assert.That(LudoUtil.GetPlayerTokenStart(2), Is.EqualTo(8));
            Assert.That(LudoUtil.GetPlayerTokenStart(3), Is.EqualTo(12));
        }

        [Test]
        public void IsSamePlayer_ReturnsCorrectValue()
        {
            Assert.That(LudoUtil.IsSamePlayer(0, 3), Is.True);
            Assert.That(LudoUtil.IsSamePlayer(0, 4), Is.False);
            Assert.That(LudoUtil.IsSamePlayer(4, 7), Is.True);
            Assert.That(LudoUtil.IsSamePlayer(4, 8), Is.False);
        }
    }
}
