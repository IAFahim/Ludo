namespace Ludo
{
    public static class LudoExtensions
    {
        // LudoBoard extensions
        public static Result<byte, GameError> MoveToken(this ref LudoBoard board, int tokenIndex, byte diceRoll)
        {
            if (board.TryMoveToken(tokenIndex, diceRoll, out var newPosition, out var error))
            {
                return Result<byte, GameError>.Ok(newPosition);
            }
            return Result<byte, GameError>.Err(error);
        }

        public static Result<int, GameError> TryCaptureOpponent(this ref LudoBoard board, int movedTokenIndex)
        {
            if (board.TryCaptureOpponent(movedTokenIndex, out var capturedTokenIndex, out var error))
            {
                return Result<int, GameError>.Ok(capturedTokenIndex);
            }
            return Result<int, GameError>.Err(error);
        }

        public static Result<MovableTokens, GameError> GetMovableTokens(this ref LudoBoard board, int playerIndex, byte diceRoll)
        {
            if (board.TryGetMovableTokens(playerIndex, diceRoll, out var mask, out var error))
            {
                return Result<MovableTokens, GameError>.Ok(mask);
            }
            return Result<MovableTokens, GameError>.Err(error);
        }

        public static Result<bool, GameError> HasPlayerWon(this ref LudoBoard board, int playerIndex)
        {
            if (board.TryHasPlayerWon(playerIndex, out var hasWon, out var error))
            {
                return Result<bool, GameError>.Ok(hasWon);
            }
            return Result<bool, GameError>.Err(error);
        }

        // LudoGame extensions
        public static Result<Dice, GameError> RollDice(this LudoGame game)
        {
            if (game.TryRollDice(out var dice, out var error))
            {
                return Result<Dice, GameError>.Ok(dice);
            }
            return Result<Dice, GameError>.Err(error);
        }

        public static Result<MoveResult, GameError> MoveToken(this LudoGame game, int tokenLocalIndex)
        {
            if (game.TryMoveToken(tokenLocalIndex, out var result, out var error))
            {
                return Result<MoveResult, GameError>.Ok(result);
            }
            return Result<MoveResult, GameError>.Err(error);
        }
    }
}
