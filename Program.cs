using Ludo;

var board = new LudoBoard(numberOfPlayers: 4);

// 1) Safely get movable tokens (no exceptions to catch)
var maybeMovables = board.GetMovableTokens(playerIndex: 0, diceRoll: 6);
if (!maybeMovables.TryGetValue(out var movables, out var err))
{
    Console.WriteLine($"Nope: {err}");
}
else
{
    // 2) Chain: pick first token -> try to move -> map to printable state
    Result<string, LudoError> summary =
        (movables.Count > 0
            ? Result<int,LudoError>.Ok(movables[0])
            : Result<int,LudoError>.Err(LudoError.TokenNotMovable))
        .AndThen(idx => board.MoveToken(idx, steps: 6))
        .Map(_ => board.ToString());                         // pass-through error

    Console.WriteLine(summary.UnwrapOrElse(e => $"Move failed: {e}"));
}