using NUnit.Framework;
using Ludo;

[TestFixture]
public class LudoBoardTests
{
    // Helper method to manually set token positions for specific test scenarios.
    private void SetTokenPosition(ref LudoBoard board, int tokenIndex, byte position)
    {
        board.tokenPositions[tokenIndex] = position;
    }

    #region Constructor Tests

    [Test]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Constructor_WithValidPlayerCount_InitializesCorrectly(int playerCount)
    {
        var board = new LudoBoard(playerCount);

        Assert.That(board.playerCount, Is.EqualTo(playerCount));
        Assert.That(board.tokenPositions.Length, Is.EqualTo(playerCount * 4));
        Assert.That(board.tokenPositions.All(p => p == 0), Is.True, "All tokens should start at base (position 0).");
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

    #region Game Logic Tests - HasWon

    [Test]
    public void HasWon_WhenAllPlayerTokensAreHome_ReturnsOkTrue()
    {
        var board = new LudoBoard(2);
        for (int i = 0; i < 4; i++)
        {
            SetTokenPosition(ref board, i, 59);
        }
        
        var result = board.HasWon(0);
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Is.True);
    }

    [Test]
    public void HasWon_WhenNotAllTokensAreHome_ReturnsOkFalse()
    {
        var board = new LudoBoard(2);
        for (int i = 0; i < 3; i++)
        {
            SetTokenPosition(ref board, i, 59);
        }
        SetTokenPosition(ref board, 3, 58);
        
        var result = board.HasWon(0);
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Is.False);
    }

    [Test]
    public void HasWon_WithInvalidPlayerIndex_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.HasWon(-1);
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.InvalidPlayerIndex));
        
        var result2 = board.HasWon(2);
        Assert.That(result2.IsErr, Is.True);
        Assert.That(result2.UnwrapErr(), Is.EqualTo(LudoError.InvalidPlayerIndex));
    }

    #endregion

    #region Game Logic Tests - GetOutOfBase

    [Test]
    public void GetOutOfBase_WithTokenAtBase_ReturnsOkAndMovesTokenToStart()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetOutOfBase(0);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(1));
    }

    [Test]
    public void GetOutOfBase_WithInvalidTokenIndex_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetOutOfBase(-1);
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.InvalidTokenIndex));
        
        var result2 = board.GetOutOfBase(8);
        Assert.That(result2.IsErr, Is.True);
        Assert.That(result2.UnwrapErr(), Is.EqualTo(LudoError.InvalidTokenIndex));
    }

    [Test]
    public void GetOutOfBase_WhenTokenNotAtBase_ReturnsError()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10);
        
        var result = board.GetOutOfBase(0);
        
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.TokenNotAtBase));
    }

    [Test]
    public void GetOutOfBase_WhenStartIsBlocked_ReturnsError()
    {
        var board = new LudoBoard(2);
        // Player 1's tokens blocking player 0's start (absolute position 1)
        // Player 1's offset is 26. To be at abs 1, relative = (1-1-26+52)%52+1 = 27
        SetTokenPosition(ref board, 4, 27);
        SetTokenPosition(ref board, 5, 27);
        
        var result = board.GetOutOfBase(0);
        
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.PathBlocked));
        Assert.That(board.tokenPositions[0], Is.EqualTo(0), "Token should remain at base");
    }

    #endregion

    #region Game Logic Tests - MoveToken

    [Test]
    public void MoveToken_SimpleMoveOnMainTrack_ReturnsOkAndMoves()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10);
        
        var result = board.MoveToken(0, 5);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Is.EqualTo(15));
        Assert.That(board.tokenPositions[0], Is.EqualTo(15));
    }

    [Test]
    public void MoveToken_WithInvalidTokenIndex_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.MoveToken(-1, 5);
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.InvalidTokenIndex));
        
        var result2 = board.MoveToken(8, 5);
        Assert.That(result2.IsErr, Is.True);
        Assert.That(result2.UnwrapErr(), Is.EqualTo(LudoError.InvalidTokenIndex));
    }

    [Test]
    public void MoveToken_WithInvalidSteps_ReturnsError()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10);
        
        var result = board.MoveToken(0, 0);
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.InvalidDiceRoll));
        
        var result2 = board.MoveToken(0, -1);
        Assert.That(result2.IsErr, Is.True);
        Assert.That(result2.UnwrapErr(), Is.EqualTo(LudoError.InvalidDiceRoll));
    }

    [Test]
    public void MoveToken_WhenTokenAlreadyHome_ReturnsError()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 59);
        
        var result = board.MoveToken(0, 3);
        
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.TokenAlreadyHome));
    }

    [Test]
    public void MoveToken_EntersHomeStretchForPlayer0()
    {
        var board = new LudoBoard(4);
        SetTokenPosition(ref board, 0, 51);
        
        var result = board.MoveToken(0, 4); // Passes home entry tile 52
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(55)); // 53 + 3 - 1
    }

    [Test]
    public void MoveToken_MoveWithinHomeStretch()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 54);
        
        var result = board.MoveToken(0, 3);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(57));
    }

    [Test]
    public void MoveToken_OvershootsHome_ReturnsError()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 57);
        
        var result = board.MoveToken(0, 4); // Would end up at 61, which is > 59
        
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.WouldOvershootHome));
        Assert.That(board.tokenPositions[0], Is.EqualTo(57), "Token should not move");
    }

    [Test]
    public void MoveToken_ExactlyReachesHome_ReturnsOk()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 57);
        
        var result = board.MoveToken(0, 2); // Exactly reaches 59
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(59));
    }

    [Test]
    public void MoveToken_CapturesOpponent()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10); // Player 0, token 0
        // Player 1's token 4 needs to be at absolute position 15.
        // Player 1 offset is 26. Relative pos = (15 - 1 - 26 + 52) % 52 + 1 = 41
        SetTokenPosition(ref board, 4, 41); // Player 1, token 4

        var result = board.MoveToken(0, 5); // Player 0 moves to 15, captures Player 1's token

        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(15));
        Assert.That(board.tokenPositions[4], Is.EqualTo(0), "Opponent should be captured");
    }

    [Test]
    public void MoveToken_DoesNotCaptureOnSafeTile()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10); // Player 0, token 0
        // Player 1's token 4 needs to be at absolute safe position 14.
        // Relative pos = (14 - 1 - 26 + 52) % 52 + 1 = 40
        SetTokenPosition(ref board, 4, 40); // Player 1, token 4

        var result = board.MoveToken(0, 4); // Player 0 moves to safe tile 14

        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(14));
        Assert.That(board.tokenPositions[4], Is.EqualTo(40), "Should not capture on safe tile");
    }

    [Test]
    public void MoveToken_PathBlocked_ReturnsError()
    {
        var board = new LudoBoard(4);
        SetTokenPosition(ref board, 0, 10);
        // Player 1 has two tokens blocking absolute position 12
        // Player 1 offset is 13. To be at abs 12, relative pos is (12-1-13+52)%52+1 = 51
        SetTokenPosition(ref board, 4, 51);
        SetTokenPosition(ref board, 5, 51);

        var result = board.MoveToken(0, 3); // Try to move to pos 13, passing blocked 12

        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.PathBlocked));
        Assert.That(board.tokenPositions[0], Is.EqualTo(10), "Token should not move");
    }

    [Test]
    public void MoveToken_FromBase_WithRoll6_ReturnsOk()
    {
        var board = new LudoBoard(2);
        
        var result = board.MoveToken(0, 6);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(1));
    }

    [Test]
    public void MoveToken_FromBase_WithoutRoll6_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.MoveToken(0, 5);
        
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.TokenNotMovable));
        Assert.That(board.tokenPositions[0], Is.EqualTo(0), "Token should remain at base");
    }

    #endregion

    #region GetMovableTokens Tests

    [Test]
    public void GetMovableTokens_Roll6_CanMoveFromBase()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetMovableTokens(0, 6);
        
        Assert.That(result.IsOk, Is.True);
        var movable = result.Unwrap();
        Assert.That(movable, Contains.Item(0));
        Assert.That(movable, Contains.Item(1));
        Assert.That(movable, Contains.Item(2));
        Assert.That(movable, Contains.Item(3));
    }

    [Test]
    public void GetMovableTokens_RollNot6_CannotMoveFromBase()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetMovableTokens(0, 5);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Is.Empty);
    }

    [Test]
    public void GetMovableTokens_TokenAtHome_IsNotMovable()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 59); // Token 0 is home
        
        var result = board.GetMovableTokens(0, 3);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Does.Not.Contain(0));
    }

    [Test]
    public void GetMovableTokens_PathIsBlocked_TokenIsNotMovable()
    {
        var board = new LudoBoard(4);
        SetTokenPosition(ref board, 0, 10);
        // Player 1 has two tokens blocking absolute position 12
        SetTokenPosition(ref board, 4, 51);
        SetTokenPosition(ref board, 5, 51);

        var result = board.GetMovableTokens(0, 3);

        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Does.Not.Contain(0));
    }
    
    [Test]
    public void GetMovableTokens_HomeStretchMoveOvershoots_TokenIsNotMovable()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 58);
        
        var result = board.GetMovableTokens(0, 3); // Would overshoot home
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Is.Empty);
    }

    [Test]
    public void GetMovableTokens_WithInvalidPlayerIndex_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetMovableTokens(-1, 6);
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.InvalidPlayerIndex));
        
        var result2 = board.GetMovableTokens(2, 6);
        Assert.That(result2.IsErr, Is.True);
        Assert.That(result2.UnwrapErr(), Is.EqualTo(LudoError.InvalidPlayerIndex));
    }

    [Test]
    public void GetMovableTokens_WithInvalidDiceRoll_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetMovableTokens(0, 0);
        Assert.That(result.IsErr, Is.True);
        Assert.That(result.UnwrapErr(), Is.EqualTo(LudoError.InvalidDiceRoll));
        
        var result2 = board.GetMovableTokens(0, 7);
        Assert.That(result2.IsErr, Is.True);
        Assert.That(result2.UnwrapErr(), Is.EqualTo(LudoError.InvalidDiceRoll));
    }

    [Test]
    public void GetMovableTokens_MixedTokenStates_ReturnsOnlyMovable()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 0);  // At base
        SetTokenPosition(ref board, 1, 10); // On track
        SetTokenPosition(ref board, 2, 59); // Home
        SetTokenPosition(ref board, 3, 55); // Home stretch
        
        var result = board.GetMovableTokens(0, 3);
        
        Assert.That(result.IsOk, Is.True);
        var movable = result.Unwrap();
        Assert.That(movable, Does.Not.Contain(0)); // Base without 6
        Assert.That(movable, Contains.Item(1));     // Can move on track
        Assert.That(movable, Does.Not.Contain(2)); // Already home
        Assert.That(movable, Contains.Item(3));     // Can move in home stretch
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Test]
    public void MoveToken_WrapsAroundTrackCorrectly()
    {
        var board = new LudoBoard(4);
        // Player 1's token near their home entry
        SetTokenPosition(ref board, 4, 10);
        
        var result = board.MoveToken(4, 5);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[4], Is.EqualTo(15));
    }

    [Test]
    public void MoveToken_MultipleCaptures_OnSameTile()
    {
        var board = new LudoBoard(4);
        SetTokenPosition(ref board, 0, 10);
        // Multiple opponent tokens on same tile
        SetTokenPosition(ref board, 4, 41); // Player 1 at abs 15
        SetTokenPosition(ref board, 8, 28); // Player 2 at abs 15 (offset 39, (15-1-39+52)%52+1=28)
        
        var result = board.MoveToken(0, 5);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(15));
    }

    [Test]
    public void MoveToken_DoesNotCaptureOwnTokens()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10);
        SetTokenPosition(ref board, 1, 15); // Own token at target
        
        var result = board.MoveToken(0, 5);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(15));
        Assert.That(board.tokenPositions[1], Is.EqualTo(15), "Own token should not be captured");
    }

    [Test]
    public void MoveToken_BlockedByOwnTokens_DoesNotPreventMovement()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10);
        SetTokenPosition(ref board, 1, 12); // Own token
        SetTokenPosition(ref board, 2, 12); // Own token - 2 tokens but same player
        
        var result = board.MoveToken(0, 3);
        
        // Own tokens don't block - only opponent tokens do
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(13));
    }

    [Test]
    public void GetTokenPosition_ValidIndex_ReturnsOk()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 15);
        
        var result = board.GetTokenPosition(0);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Is.EqualTo(15));
    }

    [Test]
    public void GetTokenPosition_InvalidIndex_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetTokenPosition(-1);
        Assert.That(result.IsErr, Is.True);
        
        var result2 = board.GetTokenPosition(8);
        Assert.That(result2.IsErr, Is.True);
    }

    [Test]
    public void SetTokenPosition_ValidIndex_ReturnsOk()
    {
        var board = new LudoBoard(2);
        
        var result = board.SetTokenPosition(0, 25);
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(board.tokenPositions[0], Is.EqualTo(25));
    }

    [Test]
    public void SetTokenPosition_InvalidIndex_ReturnsError()
    {
        var board = new LudoBoard(2);
        
        var result = board.SetTokenPosition(-1, 25);
        Assert.That(result.IsErr, Is.True);
        
        var result2 = board.SetTokenPosition(8, 25);
        Assert.That(result2.IsErr, Is.True);
    }

    #endregion

    #region Result Type Tests

    [Test]
    public void Result_Map_TransformsOkValue()
    {
        var board = new LudoBoard(2);
        SetTokenPosition(ref board, 0, 10);
        
        var result = board.MoveToken(0, 5)
            .Map(pos => $"Moved to position {pos}");
        
        Assert.That(result.IsOk, Is.True);
        Assert.That(result.Unwrap(), Is.EqualTo("Moved to position 15"));
    }

    [Test]
    public void Result_AndThen_ChainsOperations()
    {
        var board = new LudoBoard(2);
        
        var result = board.GetMovableTokens(0, 6)
            .AndThen(tokens => tokens.Count > 0 
                ? Result<int, LudoError>.Ok(tokens[0])
                : Result<int, LudoError>.Err(LudoError.TokenNotMovable))
            .AndThen(tokenIndex => board.MoveToken(tokenIndex, 6));
        
        Assert.That(result.IsOk, Is.True);
    }

    [Test]
    public void Result_UnwrapOr_ProvidesDefault()
    {
        var board = new LudoBoard(2);
        
        var position = board.MoveToken(0, 5).UnwrapOr(0);
        
        Assert.That(position, Is.EqualTo(0)); // Error case, returns default
    }

    [Test]
    public void Result_TryGetValue_ProvidesErrorInfo()
    {
        var board = new LudoBoard(2);
        
        var result = board.MoveToken(0, 5);
        
        if (result.TryGetValue(out var value, out var error))
        {
            Assert.Fail("Should have failed");
        }
        else
        {
            Assert.That(error, Is.EqualTo(LudoError.TokenNotMovable));
        }
    }

    #endregion
}