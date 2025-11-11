using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for LudoState struct - Game state management
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("LudoState")]
    public class LudoStateTests
    {
        private LudoState _state;

        [SetUp]
        public void Setup()
        {
            _state = LudoState.Create(4);
        }

        [Test]
        public void Create_InitializesCorrectly()
        {
            Assert.That(_state.playerCount, Is.EqualTo(4));
            Assert.That(_state.currentPlayer, Is.EqualTo(0));
            Assert.That(_state.consecutiveSixes, Is.EqualTo(0));
            Assert.That(_state.hasRolled, Is.False);
            Assert.That(_state.mustMove, Is.False);
        }

        [Test]
        public void CanRollDice_InitialState_ReturnsTrue()
        {
            Assert.That(_state.CanRollDice(), Is.True);
        }

        [Test]
        public void CanRollDice_AfterRolling_ReturnsFalse()
        {
            _state.RecordDiceRoll(5, MovableTokens.T0);
            Assert.That(_state.CanRollDice(), Is.False);
        }

        [Test]
        public void RecordDiceRoll_UpdatesState()
        {
            _state.RecordDiceRoll(5, MovableTokens.T0 | MovableTokens.T1);

            Assert.That(_state.lastDiceRoll, Is.EqualTo(5));
            Assert.That(_state.movableTokensMask, Is.EqualTo(MovableTokens.T0 | MovableTokens.T1));
            Assert.That(_state.hasRolled, Is.True);
            Assert.That(_state.mustMove, Is.True);
        }

        [Test]
        public void RecordDiceRoll_WithSix_IncrementsConsecutiveSixes()
        {
            _state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(_state.consecutiveSixes, Is.EqualTo(1));

            _state.hasRolled = false;
            _state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(_state.consecutiveSixes, Is.EqualTo(2));
        }

        [Test]
        public void RecordDiceRoll_WithoutSix_ResetsConsecutiveSixes()
        {
            _state.RecordDiceRoll(6, MovableTokens.T0);
            _state.hasRolled = false;
            _state.RecordDiceRoll(3, MovableTokens.T0);

            Assert.That(_state.consecutiveSixes, Is.EqualTo(0));
        }

        [Test]
        public void AdvanceTurn_UpdatesCurrentPlayer()
        {
            _state.AdvanceTurn();
            Assert.That(_state.currentPlayer, Is.EqualTo(1));

            _state.AdvanceTurn();
            Assert.That(_state.currentPlayer, Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTurn_WrapsAround()
        {
            _state.currentPlayer = 3;
            _state.AdvanceTurn();
            Assert.That(_state.currentPlayer, Is.EqualTo(0));
        }

        [Test]
        public void ClearTurnAfterMove_WithoutSix_AdvancesTurn()
        {
            _state.RecordDiceRoll(4, MovableTokens.T0);
            _state.ClearTurnAfterMove(0);

            Assert.That(_state.currentPlayer, Is.EqualTo(1));
            Assert.That(_state.hasRolled, Is.False);
            Assert.That(_state.mustMove, Is.False);
        }

        [Test]
        public void ClearTurnAfterMove_WithSix_DoesNotAdvanceTurn()
        {
            _state.RecordDiceRoll(6, MovableTokens.T0);
            _state.ClearTurnAfterMove(0);

            Assert.That(_state.currentPlayer, Is.EqualTo(0));
        }

        [Test]
        public void ClearTurnAfterMove_WithThreeConsecutiveSixes_AdvancesTurn()
        {
            _state.consecutiveSixes = 3;
            _state.RecordDiceRoll(6, MovableTokens.T0);
            _state.ClearTurnAfterMove(0);

            Assert.That(_state.currentPlayer, Is.EqualTo(1));
            Assert.That(_state.consecutiveSixes, Is.EqualTo(0));
        }

        [Test]
        public void IsTokenMovable_ReturnsCorrectValue()
        {
            _state.movableTokensMask = MovableTokens.T0 | MovableTokens.T2; // Binary 0101

            Assert.That(_state.IsTokenMovable(0), Is.True);
            Assert.That(_state.IsTokenMovable(1), Is.False);
            Assert.That(_state.IsTokenMovable(2), Is.True);
            Assert.That(_state.IsTokenMovable(3), Is.False);
        }
    }
}
