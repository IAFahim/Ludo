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
            Assert.That(_state.PlayerCount, Is.EqualTo(4));
            Assert.That(_state.CurrentPlayer, Is.EqualTo(0));
            Assert.That(_state.ConsecutiveSixes, Is.EqualTo(0));
            Assert.That(_state.HasRolled, Is.False);
            Assert.That(_state.MustMove, Is.False);
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

            Assert.That(_state.LastDiceRoll, Is.EqualTo(5));
            Assert.That(_state.MovableTokensMask, Is.EqualTo(MovableTokens.T0 | MovableTokens.T1));
            Assert.That(_state.HasRolled, Is.True);
            Assert.That(_state.MustMove, Is.True);
        }

        [Test]
        public void RecordDiceRoll_WithSix_IncrementsConsecutiveSixes()
        {
            _state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(_state.ConsecutiveSixes, Is.EqualTo(1));

            _state.ClearAfterMoveOrExtraRoll();
            _state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(_state.ConsecutiveSixes, Is.EqualTo(2));
        }

        [Test]
        public void RecordDiceRoll_WithoutSix_ResetsConsecutiveSixes()
        {
            _state.RecordDiceRoll(6, MovableTokens.T0);
            _state.ClearAfterMoveOrExtraRoll();
            _state.RecordDiceRoll(3, MovableTokens.T0);

            Assert.That(_state.ConsecutiveSixes, Is.EqualTo(0));
        }

        [Test]
        public void AdvanceTurn_UpdatesCurrentPlayer()
        {
            _state.AdvanceTurn();
            Assert.That(_state.CurrentPlayer, Is.EqualTo(1));

            _state.AdvanceTurn();
            Assert.That(_state.CurrentPlayer, Is.EqualTo(2));
        }

        [Test]
        public void AdvanceTurn_WrapsAround()
        {
            _state.CurrentPlayer = 3;
            _state.AdvanceTurn();
            Assert.That(_state.CurrentPlayer, Is.EqualTo(0));
        }

        [Test]
        public void ClearTurnAfterMove_WithoutSix_AdvancesTurn()
        {
            _state.RecordDiceRoll(4, MovableTokens.T0);
            _state.AdvanceTurn();

            Assert.That(_state.CurrentPlayer, Is.EqualTo(1));
            Assert.That(_state.HasRolled, Is.False);
            Assert.That(_state.MustMove, Is.False);
        }

        [Test]
        public void ClearAfterMoveOrExtraRoll_WithSix_KeepsCurrentPlayer()
        {
            int initialPlayer = _state.CurrentPlayer;
            _state.RecordDiceRoll(6, MovableTokens.T0);
            _state.ClearAfterMoveOrExtraRoll();

            Assert.That(_state.CurrentPlayer, Is.EqualTo(initialPlayer));
        }

        [Test]
        public void AdvanceTurn_ResetsConsecutiveSixes()
        {
            _state.RecordDiceRoll(6, MovableTokens.T0);
            _state.AdvanceTurn();

            Assert.That(_state.CurrentPlayer, Is.EqualTo(1));
            Assert.That(_state.ConsecutiveSixes, Is.EqualTo(0));
        }
    }
}
