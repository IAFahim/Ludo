public static class Program
{
    public static void Main()
    {
        static int FirstMovable(MovableTokens mask)
        {
            for (int i = 0; i < 4; i++)
                if (((int)mask & (1 << i)) != 0)
                    return i;
            return -1;
        }

        var game = LudoGame.Create(playerCount: 2, seed: 12);

// 1) Roll
        if (!game.TryRollDice(out var dice, out var err))
        {
            Console.WriteLine($"Roll failed: {err}");
            return;
        }

        Console.WriteLine(
            $"P{game.CurrentPlayer} rolled {dice} (mustMove={game.state.MustMakeMove()}, mask={game.state.movableTokensMask})");

// 2) If move required, move the first allowed token
        if (game.state.MustMakeMove() && game.state.HasMovableTokens())
        {
            int localIndex = FirstMovable(game.state.movableTokensMask);
            if (!game.TryMoveToken(localIndex, out var move, out err))
            {
                Console.WriteLine($"Move failed: {err}");
            }
            else
            {
                Console.WriteLine($"Moved token {localIndex}: newPos={move.NewPosition}, captured={move.DidCapture}");
            }
        }

// 3) Print board
        Console.WriteLine(game.board.ToString());
    }
}