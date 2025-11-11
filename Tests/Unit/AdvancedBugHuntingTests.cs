using NUnit.Framework;
using Ludo;
using System;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Advanced bug hunting tests - complex scenarios and race conditions
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("BugHunting")]
    public class AdvancedBugHuntingTests
    {
        // ========== Absolute Position Calculation Tests ==========

        [Test]
        public void TwoPlayerGame_Player0_Position1_AbsolutePosition1()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 1;
            Assert.That(board.IsOnMainTrack(0), Is.True);
        }

        [Test]
        public void TwoPlayerGame_Player1_Position1_DifferentAbsolutePosition()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[4] = 1; // Player 1's first token
            Assert.That(board.IsOnMainTrack(4), Is.True);
        }

        [Test]
        public void Capture_TwoPlayerGame_DifferentStartingPositions()
        {
            var board = LudoBoard.Create(2);
            // In 2-player game, players start at opposite sides
            board.tokenPositions[0] = 27; // Player 0
            board.tokenPositions[4] = 1;  // Player 1
            
            // They shouldn't be on same absolute position
            var result = board.TryCaptureOpponent(0);
            Assert.That(result.IsOk, Is.True);
            Assert.That(board.GetTokenPosition(4), Is.EqualTo(1)); // Not captured
        }

        // ========== Home Stretch Entry Calculation ==========

        [Test]
        public void HomeStretchEntry_ExactBoundary_Position51()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 51;
            
            var result = board.MoveToken(0, 1);
            Assert.That(result.IsOk, Is.True);
            // 51 + 1 = 52, which is first home stretch position
            Assert.That(result.Unwrap(), Is.EqualTo(52));
            Assert.That(board.IsOnMainTrack(0), Is.False);
            Assert.That(board.IsOnHomeStretch(0), Is.True);
        }

        [Test]
        public void HomeStretchEntry_FromPosition50_WithDice1()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 50;
            
            var result = board.MoveToken(0, 1);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(51));
            Assert.That(board.IsOnMainTrack(0), Is.True); // Still on main track
        }

        [Test]
        public void HomeStretchCalculation_OffByOne_Position51Plus6()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 51;
            
            var result = board.MoveToken(0, 6);
            Assert.That(result.IsOk, Is.True);
            // 51 + 6 = 57, target is past main track
            // Steps into home: 57 - 51 = 6
            // Home pos = 52 + (6-1) = 57 (exactly home)
            Assert.That(result.Unwrap(), Is.EqualTo(57));
            Assert.That(board.IsHome(0), Is.True);
        }

        [Test]
        public void HomeStretchCalculation_OffByOne_Position50Plus6()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 50;
            
            var result = board.MoveToken(0, 6);
            Assert.That(result.IsOk, Is.True);
            // 50 + 6 = 56
            // Steps into home: 56 - 51 = 5
            // Home pos = 52 + (5-1) = 56
            Assert.That(result.Unwrap(), Is.EqualTo(56));
            Assert.That(board.IsOnHomeStretch(0), Is.True);
        }

        [Test]
        public void HomeStretchCalculation_Position49Plus6()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 49;
            
            var result = board.MoveToken(0, 6);
            Assert.That(result.IsOk, Is.True);
            // 49 + 6 = 55
            // Steps into home: 55 - 51 = 4
            // Home pos = 52 + (4-1) = 55
            Assert.That(result.Unwrap(), Is.EqualTo(55));
            Assert.That(board.IsOnHomeStretch(0), Is.True);
        }

        // ========== Off-By-One Error Detection ==========

        [Test]
        public void OffByOne_Position52Plus4_ShouldBe56()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 52;
            
            var result = board.MoveToken(0, 4);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(56));
        }

        [Test]
        public void OffByOne_Position52Plus5_ShouldBe57()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 52;
            
            var result = board.MoveToken(0, 5);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(57));
            Assert.That(board.IsHome(0), Is.True);
        }

        [Test]
        public void OffByOne_Position53Plus4_ShouldBe57()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 53;
            
            var result = board.MoveToken(0, 4);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(57));
            Assert.That(board.IsHome(0), Is.True);
        }

        [Test]
        public void OffByOne_Position54Plus3_ShouldBe57()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 54;
            
            var result = board.MoveToken(0, 3);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(57));
            Assert.That(board.IsHome(0), Is.True);
        }

        [Test]
        public void OffByOne_Position55Plus2_ShouldBe57()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 55;
            
            var result = board.MoveToken(0, 2);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(57));
            Assert.That(board.IsHome(0), Is.True);
        }

        // ========== Capturing Validation ==========

        [Test]
        public void Capture_ExactSamePosition_DifferentPlayers()
        {
            var board = LudoBoard.Create(4);
            board.tokenPositions[0] = 25; // Player 0
            board.tokenPositions[4] = 25; // Player 1
            
            var result = board.TryCaptureOpponent(0);
            Assert.That(result.IsOk, Is.True);
            
            // Should capture unless it's a safe tile or multiple opponents
            // Position 25 is not a safe tile in absolute coordinates
            // But need to check if they're actually on the same absolute position
            // due to different starting positions per player
        }

        [Test]
        public void Capture_SelfTokensNeverCapture()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 10; // Player 0 token 0
            board.tokenPositions[1] = 10; // Player 0 token 1
            board.tokenPositions[2] = 10; // Player 0 token 2
            
            var result = board.TryCaptureOpponent(0);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(-1));
            Assert.That(board.GetTokenPosition(1), Is.EqualTo(10));
            Assert.That(board.GetTokenPosition(2), Is.EqualTo(10));
        }

        // ========== State Corruption Tests ==========

        [Test]
        public void State_MustMove_ClearsAfterMove()
        {
            var state = LudoState.Create(2);
            state.RecordDiceRoll(3, MovableTokens.T0);
            
            Assert.That(state.mustMove, Is.True);
            
            state.ClearTurnAfterMove(0);
            Assert.That(state.mustMove, Is.False);
        }

        [Test]
        public void State_HasRolled_ClearsAfterMove()
        {
            var state = LudoState.Create(2);
            state.RecordDiceRoll(3, MovableTokens.T0);
            
            Assert.That(state.hasRolled, Is.True);
            
            state.ClearTurnAfterMove(0);
            Assert.That(state.hasRolled, Is.False);
        }

        [Test]
        public void State_MovableTokensMask_ClearsAfterMove()
        {
            var state = LudoState.Create(2);
            state.RecordDiceRoll(6, MovableTokens.T0 | MovableTokens.T1);
            
            Assert.That(state.movableTokensMask, Is.Not.EqualTo(MovableTokens.None));
            
            state.ClearTurnAfterMove(0);
            Assert.That(state.movableTokensMask, Is.EqualTo(MovableTokens.None));
        }

        // ========== Turn Advancement Logic ==========

        [Test]
        public void TurnAdvancement_WithSix_PlayerKeepsTurn()
        {
            var state = LudoState.Create(2);
            state.RecordDiceRoll(6, MovableTokens.T0);
            
            int playerBefore = state.currentPlayer;
            state.ClearTurnAfterMove(0);
            int playerAfter = state.currentPlayer;
            
            Assert.That(playerBefore, Is.EqualTo(playerAfter));
        }

        [Test]
        public void TurnAdvancement_WithoutSix_TurnAdvances()
        {
            var state = LudoState.Create(2);
            state.RecordDiceRoll(5, MovableTokens.T0);
            
            Assert.That(state.currentPlayer, Is.EqualTo(0));
            state.ClearTurnAfterMove(0);
            Assert.That(state.currentPlayer, Is.EqualTo(1));
        }

        [Test]
        public void TurnAdvancement_ThreeSixes_TurnAdvances()
        {
            var state = LudoState.Create(2);
            state.consecutiveSixes = 2;
            state.RecordDiceRoll(6, MovableTokens.T0);
            
            Assert.That(state.consecutiveSixes, Is.EqualTo(3));
            Assert.That(state.currentPlayer, Is.EqualTo(0));
            
            state.ClearTurnAfterMove(0);
            Assert.That(state.currentPlayer, Is.EqualTo(1));
            Assert.That(state.consecutiveSixes, Is.EqualTo(0));
        }

        // ========== Game Win Detection ==========

        [Test]
        public void WinDetection_LastTokenEntersHome_DetectsWin()
        {
            var game = LudoGame.Create(2);
            game.board.tokenPositions[0] = 57;
            game.board.tokenPositions[1] = 57;
            game.board.tokenPositions[2] = 57;
            game.board.tokenPositions[3] = 56;
            
            game.state.RecordDiceRoll(1, MovableTokens.T3);
            var result = game.MoveToken(3);
            
            Assert.That(result.IsOk, Is.True);
            Assert.That(game.gameWon, Is.True);
            Assert.That(game.winner, Is.EqualTo(0));
        }

        [Test]
        public void WinDetection_BeforeLastMove_NoWin()
        {
            var game = LudoGame.Create(2);
            game.board.tokenPositions[0] = 57;
            game.board.tokenPositions[1] = 57;
            game.board.tokenPositions[2] = 56;
            game.board.tokenPositions[3] = 55;
            
            game.state.RecordDiceRoll(1, MovableTokens.T2);
            var result = game.MoveToken(2);
            
            Assert.That(result.IsOk, Is.True);
            Assert.That(game.gameWon, Is.False);
        }

        // ========== Player Token Index Calculation ==========

        [Test]
        public void TokenIndexCalculation_Player0()
        {
            var board = LudoBoard.Create(4);
            for (int i = 0; i < 4; i++)
            {
                int tokenIndex = i;
                int playerIndex = LudoUtil.GetPlayerFromToken(tokenIndex);
                Assert.That(playerIndex, Is.EqualTo(0));
            }
        }

        [Test]
        public void TokenIndexCalculation_Player1()
        {
            var board = LudoBoard.Create(4);
            for (int i = 4; i < 8; i++)
            {
                int tokenIndex = i;
                int playerIndex = LudoUtil.GetPlayerFromToken(tokenIndex);
                Assert.That(playerIndex, Is.EqualTo(1));
            }
        }

        [Test]
        public void TokenIndexCalculation_Player3()
        {
            var board = LudoBoard.Create(4);
            for (int i = 12; i < 16; i++)
            {
                int tokenIndex = i;
                int playerIndex = LudoUtil.GetPlayerFromToken(tokenIndex);
                Assert.That(playerIndex, Is.EqualTo(3));
            }
        }

        // ========== Movable Tokens Complex Scenarios ==========

        [Test]
        public void MovableTokens_MixedPositions_CorrectMask()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 0;  // At base - needs 6
            board.tokenPositions[1] = 10; // On track - can move with anything
            board.tokenPositions[2] = 56; // Near home - can move with 1
            board.tokenPositions[3] = 57; // At home - cannot move
            
            var result6 = board.GetMovableTokens(0, 6);
            Assert.That(result6.IsOk, Is.True);
            var mask6 = result6.Unwrap();
            Assert.That(mask6.HasFlag(MovableTokens.T0), Is.True);  // Can exit
            Assert.That(mask6.HasFlag(MovableTokens.T1), Is.True);  // Can move
            Assert.That(mask6.HasFlag(MovableTokens.T2), Is.False); // Would overshoot
            Assert.That(mask6.HasFlag(MovableTokens.T3), Is.False); // Already home
            
            var result1 = board.GetMovableTokens(0, 1);
            Assert.That(result1.IsOk, Is.True);
            var mask1 = result1.Unwrap();
            Assert.That(mask1.HasFlag(MovableTokens.T0), Is.False); // Can't exit with 1
            Assert.That(mask1.HasFlag(MovableTokens.T1), Is.True);  // Can move
            Assert.That(mask1.HasFlag(MovableTokens.T2), Is.True);  // Exact to home
            Assert.That(mask1.HasFlag(MovableTokens.T3), Is.False); // Already home
        }

        // ========== Safe Tile Detection ==========

        [Test]
        public void SafeTile_AbsolutePosition1_IsSafe()
        {
            var board = LudoBoard.Create(4);
            board.tokenPositions[0] = 1;
            Assert.That(board.IsOnSafeTile(0), Is.True);
        }

        [Test]
        public void SafeTile_AbsolutePosition14_IsSafe()
        {
            var board = LudoBoard.Create(4);
            board.tokenPositions[0] = 14;
            Assert.That(board.IsOnSafeTile(0), Is.True);
        }

        [Test]
        public void SafeTile_AbsolutePosition27_IsSafe()
        {
            var board = LudoBoard.Create(4);
            board.tokenPositions[0] = 27;
            Assert.That(board.IsOnSafeTile(0), Is.True);
        }

        [Test]
        public void SafeTile_AbsolutePosition40_IsSafe()
        {
            var board = LudoBoard.Create(4);
            board.tokenPositions[0] = 40;
            Assert.That(board.IsOnSafeTile(0), Is.True);
        }

        [Test]
        public void SafeTile_HomeStretch_IsSafe()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 52;
            Assert.That(board.IsOnSafeTile(0), Is.True);
            
            board.tokenPositions[0] = 56;
            Assert.That(board.IsOnSafeTile(0), Is.True);
        }

        // ========== Roll With No Movable Tokens ==========

        [Test]
        public void Game_RollWithNoMoves_AutoAdvancesTurn()
        {
            var game = LudoGame.Create(2);
            // All tokens at base for both players
            
            int player0 = game.CurrentPlayer;
            
            // Roll until we get a non-6 (which will auto-advance turn)
            int rollCount = 0;
            while (rollCount < 100)
            {
                if (!game.state.CanRollDice())
                {
                    // Should not happen in this test setup
                    Assert.Fail("Cannot roll dice in initial state");
                }
                
                var result = game.RollDice();
                Assert.That(result.IsOk, Is.True);
                var dice = result.Unwrap();
                
                if (dice.Value != 6)
                {
                    // Non-6 roll with no movable tokens should auto-advance turn
                    int player1 = game.CurrentPlayer;
                    Assert.That(player1, Is.Not.EqualTo(player0), "Turn should have advanced");
                    Assert.That(game.state.CanRollDice(), Is.True, "New player should be able to roll");
                    return; // Test passed
                }
                
                // Got a 6, tokens are movable
                if (game.state.MustMakeMove())
                {
                    // Move a token to continue testing
                    int firstMovable = -1;
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.state.IsTokenMovable(i))
                        {
                            firstMovable = i;
                            break;
                        }
                    }
                    if (firstMovable >= 0)
                    {
                        var moveResult = game.MoveToken(firstMovable);
                        Assert.That(moveResult.IsOk, Is.True);
                        // After moving with a 6, player gets another turn
                        player0 = game.CurrentPlayer;
                    }
                }
                
                rollCount++;
            }
            
            Assert.Fail("Failed to roll a non-6 in 100 attempts");
        }

        // ========== Multiple Token Same Position ==========

        [Test]
        public void MultipleTokens_SamePlayer_SamePosition_Allowed()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 10;
            board.tokenPositions[1] = 10;
            board.tokenPositions[2] = 10;
            board.tokenPositions[3] = 10;
            
            // All tokens of same player can be on same position
            Assert.That(board.GetTokenPosition(0), Is.EqualTo(10));
            Assert.That(board.GetTokenPosition(1), Is.EqualTo(10));
            Assert.That(board.GetTokenPosition(2), Is.EqualTo(10));
            Assert.That(board.GetTokenPosition(3), Is.EqualTo(10));
        }

        // ========== Stress Test - Full Game Simulation ==========

        [Test]
        public void StressTest_PlayUntilWin_NoErrors()
        {
            var game = LudoGame.Create(2, seed: 12345);
            int maxMoves = 10000;
            int moves = 0;
            
            while (!game.gameWon && moves < maxMoves)
            {
                var rollResult = game.RollDice();
                if (rollResult.IsErr)
                {
                    Assert.Fail($"Roll failed: {rollResult.UnwrapErr()}");
                }
                
                if (game.state.MustMakeMove() && game.state.HasMovableTokens())
                {
                    // Find first movable token
                    int firstMovable = -1;
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.state.IsTokenMovable(i))
                        {
                            firstMovable = i;
                            break;
                        }
                    }
                    
                    if (firstMovable >= 0)
                    {
                        var moveResult = game.MoveToken(firstMovable);
                        if (moveResult.IsErr)
                        {
                            Assert.Fail($"Move failed: {moveResult.UnwrapErr()}");
                        }
                    }
                }
                
                moves++;
            }
            
            // Game should eventually complete
            Assert.That(moves, Is.LessThan(maxMoves), "Game took too long or didn't complete");
        }

        // ========== Edge Case: All tokens in home stretch ==========

        [Test]
        public void AllTokensInHomeStretch_OnlyExactRollsWork()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 54; // Needs 3 to reach home
            board.tokenPositions[1] = 55; // Needs 2
            board.tokenPositions[2] = 56; // Needs 1
            board.tokenPositions[3] = 53; // Needs 4
            
            var result6 = board.GetMovableTokens(0, 6);
            Assert.That(result6.IsOk, Is.True);
            Assert.That(result6.Unwrap(), Is.EqualTo(MovableTokens.None)); // All would overshoot
            
            var result4 = board.GetMovableTokens(0, 4);
            Assert.That(result4.IsOk, Is.True);
            Assert.That(result4.Unwrap().HasFlag(MovableTokens.T3), Is.True);
        }

        // ========== Zero-based vs One-based indexing ==========

        [Test]
        public void Position_StartsAtOne_NotZero()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 0; // At base
            
            var result = board.MoveToken(0, 6);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(1)); // First position is 1, not 0
        }

        // ========== Player turn wrap-around ==========

        [Test]
        public void TurnWrapAround_FourPlayers_WrapsCorrectly()
        {
            var state = LudoState.Create(4);
            Assert.That(state.currentPlayer, Is.EqualTo(0));
            
            state.AdvanceTurn();
            Assert.That(state.currentPlayer, Is.EqualTo(1));
            
            state.AdvanceTurn();
            Assert.That(state.currentPlayer, Is.EqualTo(2));
            
            state.AdvanceTurn();
            Assert.That(state.currentPlayer, Is.EqualTo(3));
            
            state.AdvanceTurn();
            Assert.That(state.currentPlayer, Is.EqualTo(0)); // Wraps to 0
        }

        [Test]
        public void TurnWrapAround_TwoPlayers_WrapsCorrectly()
        {
            var state = LudoState.Create(2);
            Assert.That(state.currentPlayer, Is.EqualTo(0));
            
            state.AdvanceTurn();
            Assert.That(state.currentPlayer, Is.EqualTo(1));
            
            state.AdvanceTurn();
            Assert.That(state.currentPlayer, Is.EqualTo(0)); // Wraps to 0
        }
    }
}
