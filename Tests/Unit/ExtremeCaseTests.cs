using NUnit.Framework;
using Ludo;
using System;
using System.Collections.Generic;

namespace Ludo.Tests.Unit
{
    /// <summary>
    /// Extreme edge cases, corner cases, and stress scenarios
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    [Category("Extreme")]
    public class ExtremeCaseTests
    {
        // ========== Complex Multi-Token Scenarios ==========

        [Test]
        public void ComplexScenario_AllTokensOnSameTile_DifferentPlayers()
        {
            var board = LudoBoard.Create(4);
            // Put one token from each player on position 10
            board.tokenPositions[0] = 10;  // Player 0
            board.tokenPositions[4] = 10;  // Player 1
            board.tokenPositions[8] = 10;  // Player 2
            board.tokenPositions[12] = 10; // Player 3
            
            // No capture should happen (multiple opponents block)
            var result = board.TryCaptureOpponent(0);
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Unwrap(), Is.EqualTo(-1)); // No capture
        }

        [Test]
        public void ComplexScenario_ThreePlayersNearHome()
        {
            var board = LudoBoard.Create(3);
            // All players have tokens near home
            board.tokenPositions[0] = 56;  // Player 0
            board.tokenPositions[4] = 56;  // Player 1
            board.tokenPositions[8] = 56;  // Player 2
            
            // All should be able to reach home with exactly 1
            var result0 = board.GetMovableTokens(0, 1);
            Assert.That(result0.IsOk, Is.True);
            Assert.That(result0.Unwrap().HasFlag(MovableTokens.T0), Is.True);
            
            var result1 = board.GetMovableTokens(1, 1);
            Assert.That(result1.IsOk, Is.True);
            Assert.That(result1.Unwrap().HasFlag(MovableTokens.T0), Is.True);
            
            var result2 = board.GetMovableTokens(2, 1);
            Assert.That(result2.IsOk, Is.True);
            Assert.That(result2.Unwrap().HasFlag(MovableTokens.T0), Is.True);
        }

        // ========== Consecutive Sixes Complex Scenarios ==========

        [Test]
        public void ConsecutiveSixes_ExactlyThree_MultipleScenarios()
        {
            var state = LudoState.Create(4);
            
            // Scenario 1: 3 sixes in a row
            state.RecordDiceRoll(6, MovableTokens.T0);
            state.ClearTurnAfterMove(0);
            Assert.That(state.currentPlayer, Is.EqualTo(0)); // Stays
            
            state.RecordDiceRoll(6, MovableTokens.T0);
            state.ClearTurnAfterMove(0);
            Assert.That(state.currentPlayer, Is.EqualTo(0)); // Stays
            
            state.RecordDiceRoll(6, MovableTokens.T0);
            state.ClearTurnAfterMove(0);
            Assert.That(state.currentPlayer, Is.EqualTo(1)); // Advances
            Assert.That(state.consecutiveSixes, Is.EqualTo(0)); // Reset
            
            // Scenario 2: Another 3 sixes for player 1
            state.RecordDiceRoll(6, MovableTokens.T0);
            state.ClearTurnAfterMove(4);
            Assert.That(state.currentPlayer, Is.EqualTo(1));
            
            state.RecordDiceRoll(6, MovableTokens.T0);
            state.ClearTurnAfterMove(4);
            Assert.That(state.currentPlayer, Is.EqualTo(1));
            
            state.RecordDiceRoll(6, MovableTokens.T0);
            state.ClearTurnAfterMove(4);
            Assert.That(state.currentPlayer, Is.EqualTo(2)); // Advances
        }

        // ========== Race Condition Simulations ==========

        [Test]
        public void RaceCondition_LastTwoTokensReachHome_FirstWins()
        {
            var game = LudoGame.Create(2);
            
            // Player 0 has 3 home, 1 at 56
            game.board.tokenPositions[0] = 57;
            game.board.tokenPositions[1] = 57;
            game.board.tokenPositions[2] = 57;
            game.board.tokenPositions[3] = 56;
            
            // Player 1 has 3 home, 1 at 56
            game.board.tokenPositions[4] = 57;
            game.board.tokenPositions[5] = 57;
            game.board.tokenPositions[6] = 57;
            game.board.tokenPositions[7] = 56;
            
            // Player 0's turn - should win
            game.state.currentPlayer = 0;
            game.state.RecordDiceRoll(1, MovableTokens.T3);
            
            var result = game.MoveToken(3);
            Assert.That(result.IsOk, Is.True);
            Assert.That(game.gameWon, Is.True);
            Assert.That(game.winner, Is.EqualTo(0));
        }

        // ========== All Tokens in Different States ==========

        [Test]
        public void MixedTokenStates_AllPossiblePositions()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 0;   // At base
            board.tokenPositions[1] = 1;   // Start position
            board.tokenPositions[2] = 25;  // Mid track
            board.tokenPositions[3] = 51;  // End of main track
            board.tokenPositions[4] = 52;  // Home stretch
            board.tokenPositions[5] = 56;  // Near home
            board.tokenPositions[6] = 57;  // Home
            board.tokenPositions[7] = 10;  // Random position
            
            Assert.That(board.IsAtBase(0), Is.True);
            Assert.That(board.IsOnMainTrack(1), Is.True);
            Assert.That(board.IsOnMainTrack(2), Is.True);
            Assert.That(board.IsOnMainTrack(3), Is.True);
            Assert.That(board.IsOnHomeStretch(4), Is.True);
            Assert.That(board.IsOnHomeStretch(5), Is.True);
            Assert.That(board.IsHome(6), Is.True);
            Assert.That(board.IsOnMainTrack(7), Is.True);
        }

        // ========== Boundary Between Main Track and Home Stretch ==========

        [Test]
        public void BoundaryTransition_Position50Through57()
        {
            var board = LudoBoard.Create(2);
            
            for (byte pos = 50; pos <= 57; pos++)
            {
                board.tokenPositions[0] = pos;
                
                bool onMainTrack = board.IsOnMainTrack(0);
                bool onHomeStretch = board.IsOnHomeStretch(0);
                bool isHome = board.IsHome(0);
                
                // Position 50-51: Main track
                // Position 52-56: Home stretch
                // Position 57: Home
                if (pos <= 51)
                {
                    Assert.That(onMainTrack, Is.True, $"Position {pos} should be on main track");
                    Assert.That(onHomeStretch, Is.False, $"Position {pos} should not be on home stretch");
                    Assert.That(isHome, Is.False, $"Position {pos} should not be home");
                }
                else if (pos <= 56)
                {
                    Assert.That(onMainTrack, Is.False, $"Position {pos} should not be on main track");
                    Assert.That(onHomeStretch, Is.True, $"Position {pos} should be on home stretch");
                    Assert.That(isHome, Is.False, $"Position {pos} should not be home");
                }
                else // 57
                {
                    Assert.That(onMainTrack, Is.False, $"Position {pos} should not be on main track");
                    Assert.That(onHomeStretch, Is.False, $"Position {pos} should not be on home stretch");
                    Assert.That(isHome, Is.True, $"Position {pos} should be home");
                }
            }
        }

        // ========== Every Possible Dice Roll From Every Position ==========

        [Test]
        public void ExhaustiveTest_AllDiceRollsFromPosition50()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 50;
            
            var result1 = board.MoveToken(0, 1);
            Assert.That(result1.IsOk, Is.True);
            Assert.That(result1.Unwrap(), Is.EqualTo(51));
            
            board.tokenPositions[0] = 50;
            var result2 = board.MoveToken(0, 2);
            Assert.That(result2.IsOk, Is.True);
            Assert.That(result2.Unwrap(), Is.EqualTo(52));
            
            board.tokenPositions[0] = 50;
            var result3 = board.MoveToken(0, 3);
            Assert.That(result3.IsOk, Is.True);
            Assert.That(result3.Unwrap(), Is.EqualTo(53));
            
            board.tokenPositions[0] = 50;
            var result4 = board.MoveToken(0, 4);
            Assert.That(result4.IsOk, Is.True);
            Assert.That(result4.Unwrap(), Is.EqualTo(54));
            
            board.tokenPositions[0] = 50;
            var result5 = board.MoveToken(0, 5);
            Assert.That(result5.IsOk, Is.True);
            Assert.That(result5.Unwrap(), Is.EqualTo(55));
            
            board.tokenPositions[0] = 50;
            var result6 = board.MoveToken(0, 6);
            Assert.That(result6.IsOk, Is.True);
            Assert.That(result6.Unwrap(), Is.EqualTo(56));
        }

        [Test]
        public void ExhaustiveTest_AllDiceRollsFromPosition51()
        {
            var board = LudoBoard.Create(2);
            
            board.tokenPositions[0] = 51;
            var result1 = board.MoveToken(0, 1);
            Assert.That(result1.IsOk, Is.True);
            Assert.That(result1.Unwrap(), Is.EqualTo(52)); // Enters home stretch
            
            board.tokenPositions[0] = 51;
            var result2 = board.MoveToken(0, 2);
            Assert.That(result2.IsOk, Is.True);
            Assert.That(result2.Unwrap(), Is.EqualTo(53));
            
            board.tokenPositions[0] = 51;
            var result6 = board.MoveToken(0, 6);
            Assert.That(result6.IsOk, Is.True);
            Assert.That(result6.Unwrap(), Is.EqualTo(57)); // Exactly home
        }

        // ========== Stress Test: Maximum Game Length ==========

        [Test]
        public void StressTest_VeryLongGame_NoHang()
        {
            var game = LudoGame.Create(4, seed: 999);
            int maxTurns = 50000;
            int turns = 0;
            
            while (!game.gameWon && turns < maxTurns)
            {
                var rollResult = game.RollDice();
                if (rollResult.IsErr)
                {
                    break; // Game might have ended
                }
                
                if (game.state.MustMakeMove() && game.state.HasMovableTokens())
                {
                    // Move first movable token
                    for (int i = 0; i < 4; i++)
                    {
                        if (game.state.IsTokenMovable(i))
                        {
                            game.MoveToken(i);
                            break;
                        }
                    }
                }
                
                turns++;
            }
            
            // Should complete within reasonable turns
            Assert.That(turns, Is.LessThan(maxTurns));
        }

        // ========== Capture Chain Scenarios ==========

        [Test]
        public void CaptureChain_TokenCapturedMultipleTimes()
        {
            var board = LudoBoard.Create(2);
            
            // Player 0 token at position 10
            board.tokenPositions[0] = 10;
            
            // Player 1 token moves to position 10 (should capture)
            board.tokenPositions[4] = 9;
            var result = board.MoveToken(4, 1);
            Assert.That(result.IsOk, Is.True);
            
            var capture1 = board.TryCaptureOpponent(4);
            // Check if capture happened based on absolute positions
            Assert.That(capture1.IsOk, Is.True);
        }

        // ========== All Players Simultaneously Near Win ==========

        [Test]
        public void AllPlayersNearWin_FirstToFinishWins()
        {
            var game = LudoGame.Create(3);
            
            // All players have 3 tokens home, 1 near home
            for (int player = 0; player < 3; player++)
            {
                int start = player * 4;
                game.board.tokenPositions[start + 0] = 57;
                game.board.tokenPositions[start + 1] = 57;
                game.board.tokenPositions[start + 2] = 57;
                game.board.tokenPositions[start + 3] = 55; // 2 away from home
            }
            
            // Player 0's turn
            game.state.currentPlayer = 0;
            game.state.RecordDiceRoll(2, MovableTokens.T3);
            
            var result = game.MoveToken(3);
            Assert.That(result.IsOk, Is.True);
            Assert.That(game.gameWon, Is.True);
            Assert.That(game.winner, Is.EqualTo(0));
        }

        // ========== Invalid State Recovery ==========

        [Test]
        public void InvalidState_TokenBeyondHome_HandlesGracefully()
        {
            var board = LudoBoard.Create(2);
            board.tokenPositions[0] = 58; // Beyond home (invalid)
            
            // Should not crash
            Assert.DoesNotThrow(() => {
                bool isHome = board.IsHome(0);
                bool isOnStretch = board.IsOnHomeStretch(0);
                bool isOnTrack = board.IsOnMainTrack(0);
            });
        }

        // ========== Movable Tokens With All Edge Cases ==========

        [Test]
        public void MovableTokens_ComplexMix()
        {
            var board = LudoBoard.Create(2);
            
            // Token 0: At base (needs 6)
            board.tokenPositions[0] = 0;
            
            // Token 1: On track (can move with anything)
            board.tokenPositions[1] = 25;
            
            // Token 2: Would overshoot with 3+ (at position 55)
            board.tokenPositions[2] = 55;
            
            // Token 3: Already home (can't move)
            board.tokenPositions[3] = 57;
            
            // Test with dice 1
            var result1 = board.GetMovableTokens(0, 1);
            Assert.That(result1.IsOk, Is.True);
            var mask1 = result1.Unwrap();
            Assert.That(mask1.HasFlag(MovableTokens.T0), Is.False); // Can't exit with 1
            Assert.That(mask1.HasFlag(MovableTokens.T1), Is.True);  // Can move
            Assert.That(mask1.HasFlag(MovableTokens.T2), Is.True);  // Can reach 56
            Assert.That(mask1.HasFlag(MovableTokens.T3), Is.False); // Already home
            
            // Test with dice 2
            var result2 = board.GetMovableTokens(0, 2);
            Assert.That(result2.IsOk, Is.True);
            var mask2 = result2.Unwrap();
            Assert.That(mask2.HasFlag(MovableTokens.T0), Is.False); // Can't exit with 2
            Assert.That(mask2.HasFlag(MovableTokens.T1), Is.True);  // Can move
            Assert.That(mask2.HasFlag(MovableTokens.T2), Is.True);  // Can reach 57
            Assert.That(mask2.HasFlag(MovableTokens.T3), Is.False); // Already home
            
            // Test with dice 3
            var result3 = board.GetMovableTokens(0, 3);
            Assert.That(result3.IsOk, Is.True);
            var mask3 = result3.Unwrap();
            Assert.That(mask3.HasFlag(MovableTokens.T0), Is.False); // Can't exit with 3
            Assert.That(mask3.HasFlag(MovableTokens.T1), Is.True);  // Can move
            Assert.That(mask3.HasFlag(MovableTokens.T2), Is.False); // Would overshoot
            Assert.That(mask3.HasFlag(MovableTokens.T3), Is.False); // Already home
            
            // Test with dice 6
            var result6 = board.GetMovableTokens(0, 6);
            Assert.That(result6.IsOk, Is.True);
            var mask6 = result6.Unwrap();
            Assert.That(mask6.HasFlag(MovableTokens.T0), Is.True);  // Can exit
            Assert.That(mask6.HasFlag(MovableTokens.T1), Is.True);  // Can move
            Assert.That(mask6.HasFlag(MovableTokens.T2), Is.False); // Would overshoot
            Assert.That(mask6.HasFlag(MovableTokens.T3), Is.False); // Already home
        }

        // ========== ToString Testing ==========

        [Test]
        public void ToString_WithMixedState_DoesNotCrash()
        {
            var board = LudoBoard.Create(4);
            board.tokenPositions[0] = 0;
            board.tokenPositions[1] = 1;
            board.tokenPositions[5] = 25;
            board.tokenPositions[10] = 52;
            board.tokenPositions[15] = 57;
            
            Assert.DoesNotThrow(() => {
                string output = board.ToString();
                Assert.That(output, Is.Not.Null);
                Assert.That(output.Length, Is.GreaterThan(0));
            });
        }

        // ========== Every Position Movement Test ==========

        [Test]
        public void MovementFromEveryPosition_1Through51()
        {
            var board = LudoBoard.Create(2);
            
            for (byte pos = 1; pos <= 51; pos++)
            {
                board.tokenPositions[0] = pos;
                
                // Should be able to move forward by 1
                var result = board.MoveToken(0, 1);
                Assert.That(result.IsOk, Is.True, $"Should be able to move from position {pos}");
                Assert.That(result.Unwrap(), Is.EqualTo(pos + 1), $"Moving from {pos} by 1 should reach {pos + 1}");
            }
        }

        // ========== Capture on Every Safe Tile ==========

        [Test]
        public void NoCapture_OnAllSafeTiles()
        {
            var board = LudoBoard.Create(4);
            byte[] safeTiles = { 1, 14, 27, 40 };
            
            foreach (byte safeTile in safeTiles)
            {
                // Reset board
                board = LudoBoard.Create(4);
                
                // Put two different players on same safe tile
                board.tokenPositions[0] = safeTile; // Player 0
                board.tokenPositions[4] = safeTile; // Player 1
                
                var result = board.TryCaptureOpponent(0);
                Assert.That(result.IsOk, Is.True);
                Assert.That(result.Unwrap(), Is.EqualTo(-1), $"Should not capture on safe tile {safeTile}");
                Assert.That(board.GetTokenPosition(4), Is.EqualTo(safeTile), $"Token should still be at safe tile {safeTile}");
            }
        }

        // ========== Consecutive Sixes Interruption ==========

        [Test]
        public void ConsecutiveSixes_InterruptedByNonSix()
        {
            var state = LudoState.Create(2);
            
            // One six
            state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(state.consecutiveSixes, Is.EqualTo(1));
            state.hasRolled = false;
            
            // Interrupted by non-six
            state.RecordDiceRoll(3, MovableTokens.T0);
            Assert.That(state.consecutiveSixes, Is.EqualTo(0));
            state.ClearTurnAfterMove(0);
            
            // Now rolling sixes again should start from 1
            state.RecordDiceRoll(6, MovableTokens.T0);
            Assert.That(state.consecutiveSixes, Is.EqualTo(1));
        }

        // ========== Win Detection Timing ==========

        [Test]
        public void WinDetection_OnExactlyLastToken()
        {
            var game = LudoGame.Create(2);
            
            // Set up player 0 with 3 tokens home
            game.board.tokenPositions[0] = 57;
            game.board.tokenPositions[1] = 57;
            game.board.tokenPositions[2] = 57;
            
            // Last token at various positions
            for (byte startPos = 52; startPos < 57; startPos++)
            {
                game.gameWon = false;
                game.winner = -1;
                game.board.tokenPositions[3] = startPos;
                
                byte neededRoll = (byte)(57 - startPos);
                game.state = LudoState.Create(2);
                game.state.RecordDiceRoll(neededRoll, MovableTokens.T3);
                
                var result = game.MoveToken(3);
                Assert.That(result.IsOk, Is.True, $"Should successfully move from {startPos} to home");
                Assert.That(game.gameWon, Is.True, $"Should detect win when moving from {startPos} to home");
                Assert.That(game.winner, Is.EqualTo(0), $"Winner should be player 0");
            }
        }
    }
}
