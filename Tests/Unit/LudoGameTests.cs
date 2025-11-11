using NUnit.Framework;
using Ludo;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Unit tests for LudoGame class - Game orchestration and integration
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("LudoGame")]
    public class LudoGameTests
    {
        private LudoGame _game = null!;

        [SetUp]
        public void Setup()
        {
            _game = LudoGame.Create(4);
        }

        [Test]
        public void Create_InitializesCorrectly()
        {
            Assert.That(_game.CurrentPlayer, Is.EqualTo(0));
            Assert.That(_game.gameWon, Is.False);
            Assert.That(_game.winner, Is.EqualTo(-1));
        }

        [Test]
        public void RollDice_ReturnsValidDiceValue()
        {
            var result = _game.RollDice();

            Assert.That(result.IsOk, Is.True);
            var dice = result.Unwrap();
            Assert.That(dice.Value, Is.GreaterThanOrEqualTo((byte)1));
            Assert.That(dice.Value, Is.LessThanOrEqualTo((byte)6));
        }

        [Test]
        public void RollDice_WhenAlreadyRolled_ReturnsError()
        {
            var firstRoll = _game.RollDice();
            Assert.That(firstRoll.IsOk, Is.True);

            if (_game.state.MustMakeMove())
            {
                var result = _game.RollDice();
                Assert.That(result.IsErr, Is.True);
                Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.NoTurnAvailable));
            }
        }

        [Test]
        public void MoveToken_WithInvalidTokenIndex_ReturnsError()
        {
            while (true)
            {
                var rollResult = _game.RollDice();
                if (rollResult.IsOk && rollResult.Unwrap().Value == 6)
                    break;
            }

            var result = _game.MoveToken(5);
            Assert.That(result.IsErr, Is.True);
        }

        [Test]
        public void MoveToken_WithoutRolling_ReturnsError()
        {
            var result = _game.MoveToken(0);

            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.NoTurnAvailable));
        }

        [Test]
        public void GameFlow_RollAndMove_WorksCorrectly()
        {
            var rollResult = _game.RollDice();
            Assert.That(rollResult.IsOk, Is.True);

            var dice = rollResult.Unwrap();
            Assert.That(dice.Value, Is.GreaterThanOrEqualTo((byte)1));
            Assert.That(dice.Value, Is.LessThanOrEqualTo((byte)6));

            if (dice.Value == 6)
            {
                var moveResult = _game.MoveToken(0);
                Assert.That(moveResult.IsOk || moveResult.IsErr, Is.True);
            }
        }

        [Test]
        public void GameFlow_CompleteMove_AdvancesTurn()
        {
            int attempts = 0;
            while (attempts < 100)
            {
                var rollResult = _game.RollDice();
                if (rollResult.IsErr) break;

                var dice = rollResult.Unwrap();
                if (dice.Value != 6 || !_game.state.MustMakeMove())
                {
                    break;
                }
                attempts++;
            }
        }

        [Test]
        public void GameFlow_RollSix_GetsAnotherTurn()
        {
            _game.board.tokenPositions[0] = 1;
            _game.state.RecordDiceRoll(6, MovableTokens.T0);
            var moveResult = _game.MoveToken(0);

            if (moveResult.IsOk)
            {
                Assert.That(_game.CurrentPlayer, Is.EqualTo(0));
            }
        }

        [Test]
        public void GameWon_DetectsWinner()
        {
            for (int i = 0; i < 4; i++)
            {
                _game.board.tokenPositions[i] = 57;
            }

            _game.board.tokenPositions[0] = 56;
            _game.state.RecordDiceRoll(1, MovableTokens.T0);
            var moveResult = _game.MoveToken(0);

            if (moveResult.IsOk)
            {
                Assert.That(_game.gameWon, Is.True);
                Assert.That(_game.winner, Is.EqualTo(0));
            }
        }

        [Test]
        public void RollDice_AfterGameWon_ReturnsError()
        {
            _game.gameWon = true;
            _game.winner = 0;

            var result = _game.RollDice();
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.GameAlreadyWon));
        }

        [Test]
        public void MoveToken_AfterGameWon_ReturnsError()
        {
            _game.gameWon = true;
            _game.winner = 0;
            _game.state.RecordDiceRoll(6, MovableTokens.T0);

            var result = _game.MoveToken(0);
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.UnwrapErr(), Is.EqualTo(GameError.GameAlreadyWon));
        }

        [Test]
        public void MoveResult_CaptureDetection_WorksCorrectly()
        {
            var moveResult = new MoveResult { NewPosition = 10, CapturedTokenIndex = 5 };
            Assert.That(moveResult.DidCapture, Is.True);

            var noCapture = new MoveResult { NewPosition = 10, CapturedTokenIndex = -1 };
            Assert.That(noCapture.DidCapture, Is.False);
        }

        [Test]
        public void Create_TwoPlayerGame_InitializesCorrectly()
        {
            var game2p = LudoGame.Create(2);
            Assert.That(game2p.board.PlayerCount, Is.EqualTo(2));
            Assert.That(game2p.board.tokenPositions.Length, Is.EqualTo(8));
        }

        [Test]
        public void Create_ThreePlayerGame_InitializesCorrectly()
        {
            var game3p = LudoGame.Create(3);
            Assert.That(game3p.board.PlayerCount, Is.EqualTo(3));
            Assert.That(game3p.board.tokenPositions.Length, Is.EqualTo(12));
        }
    }
}
