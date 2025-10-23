using NUnit.Framework;
using Ludo;
using System;
using System.Collections.Generic;
using System.Linq;

[TestFixture]
public class LudoBoardTests
{
    // Helper method to manually set token positions for specific test scenarios.
    private void SetTokenPosition(ref LudoBoard board, int tokenIndex, byte position)
    {
        var positions = board.TokenPositions;
        positions[tokenIndex] = position;
        // In C# 12 we could use ref reassignment, but this is more compatible.
        // This is a bit of a workaround because LudoBoard is a struct.
        // A better approach might be a method within LudoBoard for testing.
        // For this example, we'll create a new board with the modified state.
        var newBoard = new LudoBoard(board.PlayerCount);
        newBoard.TokenPositions = positions;
        board = newBoard;
    }

    #region Constructor Tests

    [Test]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Constructor_WithValidPlayerCount_InitializesCorrectly(int playerCount)
    {
        var board = new LudoBoard(playerCount);

        Assert.That(board.PlayerCount, Is.EqualTo(playerCount));
        Assert.That(board.TokenPositions.Length, Is.EqualTo(playerCount * 4));
        Assert.That(board.TokenPositions.All(p => p == 0), Is.True, "All tokens should start at base (position 0).");
    }

    #endregion

    #region State Checking Tests

    [Test]
    public void IsAtBase_WhenTokenIsAtPosition0_ReturnsTrue()
    {
        var board = new LudoBoard(2);
        Assert.That(board.IsAtBase(0), Is.True);
    }

    [Test]
    public void IsOnMainTrack_WhenTokenPositionIsBetween1And52_ReturnsTrue()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 1);
        Assert.That(board.IsOnMainTrack(0), Is.True);
        SetTokenPosition(ref board, 0, 52);
        Assert.That(board.IsOnMainTrack(0), Is.True);
    }
    
    [Test]
    public void IsOnHomeStretch_WhenTokenPositionIsBetween53And58_ReturnsTrue()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 53);
        Assert.That(board.IsOnHomeStretch(0), Is.True);
        SetTokenPosition(ref board, 0, 58);
        Assert.That(board.IsOnHomeStretch(0), Is.True);
    }

    [Test]
    public void IsHome_WhenTokenPositionIs59_ReturnsTrue()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 59); // 53 (HomeStretchStart) + 6 (StepsToHome)
        Assert.That(board.IsHome(0), Is.True);
    }

    #endregion

    #region Safe Tile Tests

    [Test]
    public void IsOnSafeTile_WhenOnAbsoluteSafeTile_ReturnsTrue()
    {
        var board = new LudoBoard(4);
        // Player 0 on absolute tile 14
        SetTokenPosition(ref board, 0, 14);
        Assert.That(board.IsOnSafeTile(0), Is.True);

        // Player 1 on absolute tile 27 (their relative position is 14)
        SetTokenPosition(ref board, 4, 14);
        Assert.That(board.IsOnSafeTile(4), Is.True);
    }

    [Test]
    public void IsOnSafeTile_WhenOnHomeStretch_ReturnsTrue()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 54);
        Assert.That(board.IsOnSafeTile(0), Is.True);
    }

    [Test]
    public void IsOnSafeTile_WhenOnNonSafeTile_ReturnsFalse()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 5);
        Assert.That(board.IsOnSafeTile(0), Is.False);
    }

    #endregion

    #region Game Logic Tests

    [Test]
    public void HasWon_WhenAllPlayerTokensAreHome_ReturnsTrue()
    {
        var board = new LudoBoard(2);
        for (int i = 0; i < 4; i++)
        {
            SetTokenPosition(ref board, i, 59);
        }
        Assert.That(board.HasWon(0), Is.True);
    }

    [Test]
    public void HasWon_WhenNotAllTokensAreHome_ReturnsFalse()
    {
        var board = new LudoBoard(2);
        for (int i = 0; i < 3; i++)
        {
            SetTokenPosition(ref board, i, 59);
        }
        SetTokenPosition(ref board, 3, 58);
        Assert.That(board.HasWon(0), Is.False);
    }

    [Test]
    public void GetOutOfBase_WithTokenAtBase_MovesTokenToStart()
    {
        var board = new LudoBoard(2);
        board.GetOutOfBase(0);
        Assert.That(board.TokenPositions[0], Is.EqualTo(1));
    }

    [Test]
    public void GetOutOfBase_WhenLandingOnOpponent_CapturesOpponent()
    {
        var board = new LudoBoard(2);
        // Player 1 (token 4) is at their start, which is absolute position 27.
        // Player 0's start is absolute position 1.
        // Let's place Player 1's token at Player 0's start.
        SetTokenPosition(ref board, 4, 27); // Player 1's token at their relative pos 27 (abs 52)
        SetTokenPosition(ref board, 5, 1);  // Player 1's token at their relative pos 1 (abs 27)
        
        // Now, let's place Player 0's token at a position that will land on Player 1's token
        // For this test, we need to adjust the scenario.
        // Player 1, token 4, at relative position 40. Absolute position is (40-1+26)%52+1 = 13.
        // Player 0, token 0, gets out of base. Lands on relative position 1. Absolute position is 1.
        // Let's place Player 1's token on Player 0's start tile.
        // Player 1's start is abs 27. To get to abs 1, they need to move 27 tiles. So relative pos is 27.
        SetTokenPosition(ref board, 4, 27);

        // Player 0 (token 0) gets out of base.
        board.GetOutOfBase(0);

        Assert.That(board.TokenPositions[0], Is.EqualTo(1)); // Player 0 moved
        Assert.That(board.TokenPositions[4], Is.EqualTo(0)); // Player 1 captured
    }

    [Test]
    public void MoveToken_SimpleMoveOnMainTrack()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10);
        board.MoveToken(0, 5);
        Assert.That(board.TokenPositions[0], Is.EqualTo(15));
    }

    [Test]
    public void MoveToken_WrapsAroundBoard()
    {
        var board = new LudoBoard(4);
        SetTokenPosition(ref board, 0, 51+6);
        board.MoveToken(0, 4);
        Assert.That(board.TokenPositions[0], Is.EqualTo(3)); // 51 -> 52 -> 1 -> 2 -> 3
    }

    [Test]
    public void MoveToken_EntersHomeStretchForPlayer0()
    {
        var board = new LudoBoard(4);
        SetTokenPosition(ref board, 0, 51);
        board.MoveToken(0, 4); // Passes home entry tile 52
        // 1 step to 52, 3 steps into home stretch
        Assert.That(board.TokenPositions[0], Is.EqualTo(53 + 3 - 1)); // 55
    }

    [Test]
    public void MoveToken_EntersHomeStretchForPlayer1_4Players()
    {
        var board = new LudoBoard(4);
        // Player 1's home entry is at absolute tile 13.
        // To be at relative position 12, their absolute position is (12-1+13)%52+1 = 25
        SetTokenPosition(ref board, 4, 12);
        board.MoveToken(4, 3); // Passes home entry tile 13
        // 1 step to relative 13, 2 steps into home stretch
        Assert.That(board.TokenPositions[4], Is.EqualTo(53 + 2 - 1)); // 54
    }

    [Test]
    public void MoveToken_MoveWithinHomeStretch()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 54);
        board.MoveToken(0, 3);
        Assert.That(board.TokenPositions[0], Is.EqualTo(57));
    }

    [Test]
    public void MoveToken_OvershootsHome_DoesNotMove()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 57);
        board.MoveToken(0, 4); // Would end up at 61, which is > 59
        Assert.That(board.TokenPositions[0], Is.EqualTo(57));
    }

    [Test]
    public void MoveToken_CapturesOpponent()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10); // Player 0, token 0
        // Player 1's token 4 needs to be at absolute position 15.
        // Player 1 offset is 26. Relative pos = (15 - 1 - 26 + 52) % 52 + 1 = 41
        SetTokenPosition(ref board, 4, 41); // Player 1, token 4

        board.MoveToken(0, 5); // Player 0 moves to 15, captures Player 1's token

        Assert.That(board.TokenPositions[0], Is.EqualTo(15));
        Assert.That(board.TokenPositions[4], Is.EqualTo(0)); // Captured
    }

    [Test]
    public void MoveToken_DoesNotCaptureOnSafeTile()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10); // Player 0, token 0
        // Player 1's token 4 needs to be at absolute safe position 14.
        // Relative pos = (14 - 1 - 26 + 52) % 52 + 1 = 40
        SetTokenPosition(ref board, 4, 40); // Player 1, token 4

        board.MoveToken(0, 4); // Player 0 moves to safe tile 14

        Assert.That(board.TokenPositions[0], Is.EqualTo(14));
        Assert.That(board.TokenPositions[4], Is.EqualTo(40)); // Not captured
    }

    #endregion

    #region GetMovableTokens Tests

    [Test]
    public void GetMovableTokens_Roll6_CanMoveFromBase()
    {
        var board = new LudoBoard(2);
        var movable = board.GetMovableTokens(0, 6);
        Assert.That(movable, Contains.Item(0));
        Assert.That(movable, Contains.Item(1));
        Assert.That(movable, Contains.Item(2));
        Assert.That(movable, Contains.Item(3));
    }

    [Test]
    public void GetMovableTokens_RollNot6_CannotMoveFromBase()
    {
        var board = new LudoBoard(2);
        var movable = board.GetMovableTokens(0, 5);
        Assert.That(movable, Is.Empty);
    }

    [Test]
    public void GetMovableTokens_TokenAtHome_IsNotMovable()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 59); // Token 0 is home
        var movable = board.GetMovableTokens(0, 3);
        Assert.That(movable, Does.Not.Contain(0));
    }

    [Test]
    public void GetMovableTokens_PathIsBlocked_TokenIsNotMovable()
    {
        var board = new LudoBoard(4);
        // Player 0 has a token at relative position 10
        SetTokenPosition(ref board, 0, 10);
        // Player 1 has two tokens blocking absolute position 12
        // Player 1 offset is 13. To be at abs 12, relative pos is (12-1-13+52)%52+1 = 51
        SetTokenPosition(ref board, 4, 51);
        SetTokenPosition(ref board, 5, 51);

        var movable = board.GetMovableTokens(0, 3); // Try to move 3 steps to pos 13, passing blocked 12

        Assert.That(movable, Does.Not.Contain(0));
    }
    
    [Test]
    public void GetMovableTokens_HomeStretchMoveOvershoots_TokenIsNotMovable()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 58);
        var movable = board.GetMovableTokens(0, 3); // Would overshoot home
        Assert.That(movable, Is.Empty);
    }

    #endregion
}