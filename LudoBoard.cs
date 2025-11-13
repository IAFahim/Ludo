using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ludo;

/// <summary>Stable error codes for client/server.</summary>
public enum GameError : byte
{
    None = 0,
    InvalidTokenIndex,
    InvalidPlayerIndex,
    InvalidDiceRoll,
    InvalidCommandForTurn,
    TokenNotMovable,
    TokenAlreadyHome,
    TokenNotAtBase,
    WouldOvershootHome,
    NoTurnAvailable,
    GameAlreadyWon,
    CannotLeaveBaseWithoutSix
}

[Flags]
public enum MovableTokens : byte
{
    None = 0,
    T0 = 1 << 0,
    T1 = 1 << 1,
    T2 = 1 << 2,
    T3 = 1 << 3
}

public readonly record struct Dice(byte Value)
{
    public override string ToString() => Value.ToString();
    public static implicit operator byte(Dice d) => d.Value;
    public static Dice FromByte(byte b) => new(b);
}

public static class LudoUtil
{
    public const byte MinDiceValue = 1;
    public const byte MaxDiceValue = 6;
    public const byte TokensPerPlayer = 4;
    public const byte MaxConsecutiveSixes = 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidDiceRoll(int diceRoll) =>
        diceRoll is >= MinDiceValue and <= MaxDiceValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidPlayerIndex(int playerIndex, int playerCount) =>
        (uint)playerIndex < (uint)playerCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidTokenIndex(int tokenIndex, int totalTokens) =>
        (uint)tokenIndex < (uint)totalTokens;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPlayerFromToken(int tokenIndex) =>
        tokenIndex / TokensPerPlayer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPlayerTokenStart(int playerIndex) =>
        playerIndex * TokensPerPlayer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSamePlayer(int tokenIndex1, int tokenIndex2) =>
        GetPlayerFromToken(tokenIndex1) == GetPlayerFromToken(tokenIndex2);
}

// ---------------------------
// Pure board (positions only)
// ---------------------------
[Serializable]
public struct LudoBoard
{
    private const byte BasePosition = 0;
    private const byte StartPosition = 1;
    private const byte MainTrackLength = 51;
    private const byte HomeStretchStart = 52;
    private const byte HomeStretchLength = 5;
    private const byte HomePosition = HomeStretchStart + HomeStretchLength; // 57

    private const byte AbsoluteTrackLength = 52;
    private const byte ExitDiceValue = 6;
    private const byte QuarterTrack = AbsoluteTrackLength / 4; // 13

    public byte[] TokenPositions; // length = playerCount * 4

    public int PlayerCount => TokenPositions.Length / LudoUtil.TokensPerPlayer;

    public static LudoBoard Create(int playerCount)
    {
        if (playerCount is < 2 or > 4)
            throw new ArgumentOutOfRangeException(nameof(playerCount), "Player count must be 2-4.");
        return new LudoBoard { TokenPositions = new byte[playerCount * LudoUtil.TokensPerPlayer] };
    }

    public byte GetTokenPosition(int absoluteTokenIndex) =>
        LudoUtil.IsValidTokenIndex(absoluteTokenIndex, TokenPositions.Length)
            ? TokenPositions[absoluteTokenIndex]
            : (byte)0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAtBase(int absoluteTokenIndex) => TokenPositions[absoluteTokenIndex] == BasePosition;

    public bool IsOnMainTrack(int absoluteTokenIndex)
    {
        byte position = TokenPositions[absoluteTokenIndex];
        return position is >= StartPosition and <= MainTrackLength;
    }

    public bool IsOnHomeStretch(int absoluteTokenIndex)
    {
        byte position = TokenPositions[absoluteTokenIndex];
        return position is >= HomeStretchStart and < HomePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsHome(int absoluteTokenIndex) => TokenPositions[absoluteTokenIndex] == HomePosition;

    public bool IsOnSafeTile(int absoluteTokenIndex)
    {
        if (IsOnHomeStretch(absoluteTokenIndex)) return true;
        if (!IsOnMainTrack(absoluteTokenIndex)) return false;
        int absolutePosition = GetAbsolutePosition(absoluteTokenIndex);
        return absolutePosition is 1 or 14 or 27 or 40;
    }

    public bool TryGetMovableTokens(int playerIndex, byte diceValue, out MovableTokens movableTokensMask, out GameError error)
    {
        movableTokensMask = MovableTokens.None; error = GameError.None;

        if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
            { error = GameError.InvalidPlayerIndex; return false; }

        if (!LudoUtil.IsValidDiceRoll(diceValue))
            { error = GameError.InvalidDiceRoll; return false; }

        int startTokenIndex = LudoUtil.GetPlayerTokenStart(playerIndex);
        for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
        {
            int absoluteTokenIndex = startTokenIndex + i;
            if (CanMoveToken(absoluteTokenIndex, diceValue))
                movableTokensMask |= (MovableTokens)(1 << i);
        }
        return true;
    }

    public bool TryMoveToken(int absoluteTokenIndex, byte diceValue, out byte newPosition, out GameError error)
    {
        newPosition = 0; error = GameError.None;

        if (!LudoUtil.IsValidTokenIndex(absoluteTokenIndex, TokenPositions.Length))
            { error = GameError.InvalidTokenIndex; return false; }

        if (!LudoUtil.IsValidDiceRoll(diceValue))
            { error = GameError.InvalidDiceRoll; return false; }

        if (IsHome(absoluteTokenIndex))
            { error = GameError.TokenAlreadyHome; return false; }

        if (IsAtBase(absoluteTokenIndex))
        {
            if (diceValue != ExitDiceValue)
                { error = GameError.CannotLeaveBaseWithoutSix; return false; }

            TokenPositions[absoluteTokenIndex] = StartPosition;
            newPosition = StartPosition;
            return true;
        }

        // On track or in stretch
        if (!TryCalculateNewPosition(absoluteTokenIndex, diceValue, out var position, out error))
            return false;

        TokenPositions[absoluteTokenIndex] = position;
        newPosition = position;
        return true;
    }

    public bool TryCaptureOpponent(int movedAbsoluteTokenIndex, out int capturedAbsoluteTokenIndex)
    {
        capturedAbsoluteTokenIndex = -1;

        if (!IsOnMainTrack(movedAbsoluteTokenIndex) || IsOnSafeTile(movedAbsoluteTokenIndex))
            return true;

        int landingAbsolutePosition = GetAbsolutePosition(movedAbsoluteTokenIndex);
        int opponentCount = 0;
        int opponentTokenIndex = -1;

        for (int i = 0; i < TokenPositions.Length; i++)
        {
            if (!IsOnMainTrack(i)) continue;
            if (LudoUtil.IsSamePlayer(i, movedAbsoluteTokenIndex)) continue;
            if (GetAbsolutePosition(i) != landingAbsolutePosition) continue;

            opponentCount++;
            if (opponentCount == 1) opponentTokenIndex = i;
            else return true; // blockade: no capture
        }

        if (opponentCount == 1)
        {
            TokenPositions[opponentTokenIndex] = 0; // send back to base
            capturedAbsoluteTokenIndex = opponentTokenIndex;
        }
        return true;
    }

    public bool TryHasPlayerWon(int playerIndex, out bool hasWon, out GameError error)
    {
        hasWon = false; error = GameError.None;
        if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
            { error = GameError.InvalidPlayerIndex; return false; }

        int startTokenIndex = LudoUtil.GetPlayerTokenStart(playerIndex);
        for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
            if (!IsHome(startTokenIndex + i)) return true;

        hasWon = true;
        return true;
    }

    // ---- internals
    private bool CanMoveToken(int absoluteTokenIndex, byte diceValue)
    {
        if (IsHome(absoluteTokenIndex)) return false;
        if (IsAtBase(absoluteTokenIndex)) return diceValue == 6;
        return TryCalculateNewPosition(absoluteTokenIndex, diceValue, out _, out _);
    }

    private bool TryCalculateNewPosition(int absoluteTokenIndex, byte diceValue, out byte newPosition, out GameError error)
    {
        newPosition = 0; error = GameError.None;
        byte currentPosition = TokenPositions[absoluteTokenIndex];

        // main track
        if (currentPosition is >= 1 and <= 51)
        {
            int targetPosition = currentPosition + diceValue;
            if (targetPosition <= 51)
            {
                newPosition = (byte)targetPosition;
                return true;
            }
            // cross into home stretch
            int intoStretch = targetPosition - 51;
            int stretchPosition = 52 + intoStretch - 1;
            if (stretchPosition > 57) { error = GameError.WouldOvershootHome; return false; }
            newPosition = (byte)stretchPosition;
            return true;
        }

        // already in stretch
        if (currentPosition is >= 52 and < 57)
        {
            int targetPosition = currentPosition + diceValue;
            if (targetPosition > 57) { error = GameError.WouldOvershootHome; return false; }
            newPosition = (byte)targetPosition;
            return true;
        }

        error = GameError.TokenNotMovable;
        return false;
    }

    private int GetAbsolutePosition(int absoluteTokenIndex)
    {
        if (!IsOnMainTrack(absoluteTokenIndex)) return -1;
        int playerIndex = LudoUtil.GetPlayerFromToken(absoluteTokenIndex);
        int relativePosition = TokenPositions[absoluteTokenIndex]; // 1..51
        int offset = (PlayerCount == 2) ? playerIndex * 2 * QuarterTrack : playerIndex * QuarterTrack;
        return (relativePosition - 1 + offset) % 52 + 1;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("Players: ").Append(PlayerCount).Append(" | ");
        for (int playerIndex = 0; playerIndex < PlayerCount; playerIndex++)
        {
            if (playerIndex > 0) sb.Append(" || ");
            sb.Append("P").Append(playerIndex).Append(": ");
            int startTokenIndex = LudoUtil.GetPlayerTokenStart(playerIndex);
            for (int tokenIndex = 0; tokenIndex < LudoUtil.TokensPerPlayer; tokenIndex++)
            {
                if (tokenIndex > 0) sb.Append(", ");
                int absoluteTokenIndex = startTokenIndex + tokenIndex;
                byte position = TokenPositions[absoluteTokenIndex];

                if (position == 0) sb.Append("Base");
                else if (position == 57) sb.Append("Home✨");
                else if (position >= 52) sb.Append("Stretch-").Append(position - 52 + 1);
                else
                {
                    sb.Append(position).Append("@");
                    int absolutePosition = GetAbsolutePosition(absoluteTokenIndex);
                    sb.Append(absolutePosition);
                    if (IsOnSafeTile(absoluteTokenIndex)) sb.Append("⭐");
                }
            }
        }
        return sb.ToString();
    }
}

// ---------------------------
// State & results
// ---------------------------
[Serializable]
public struct LudoState
{
    public byte CurrentPlayer;
    public byte PlayerCount;
    public byte ConsecutiveSixes;
    public byte LastDiceRoll;
    public MovableTokens MovableTokensMask;
    public int TurnId;
    public long Version;

    public bool HasRolled => LastDiceRoll != 0;
    public bool MustMove => MovableTokensMask != MovableTokens.None;

    public static LudoState Create(byte playerCount)
    {
        if (playerCount is < 2 or > 4)
            throw new ArgumentOutOfRangeException(nameof(playerCount), "Player count must be 2-4.");
        return new LudoState
        {
            PlayerCount = playerCount,
            CurrentPlayer = 0,
            ConsecutiveSixes = 0,
            LastDiceRoll = 0,
            MovableTokensMask = MovableTokens.None,
            TurnId = 0,
            Version = 0
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanRollDice() => !HasRolled && !MustMove;

    public void RecordDiceRoll(byte diceValue, MovableTokens movableTokensMask)
    {
        LastDiceRoll = diceValue;
        MovableTokensMask = movableTokensMask;
        ConsecutiveSixes = diceValue == 6 ? (byte)(ConsecutiveSixes + 1) : (byte)0;
        Version++;
    }

    public void ClearAfterMoveOrExtraRoll()
    {
        // keeps CurrentPlayer, ConsecutiveSixes as-is (for extra turn when != 3)
        LastDiceRoll = 0;
        MovableTokensMask = MovableTokens.None;
        Version++;
    }

    public void AdvanceTurn()
    {
        CurrentPlayer = (byte)((CurrentPlayer + 1) % PlayerCount);
        ConsecutiveSixes = 0;
        LastDiceRoll = 0;
        MovableTokensMask = MovableTokens.None;
        TurnId++;
        Version++;
    }
}

public readonly struct MoveResult
{
    public byte NewPosition { get; init; }
    public int CapturedAbsoluteTokenIndex { get; init; }
    public bool DidCapture => CapturedAbsoluteTokenIndex >= 0;

    public static MoveResult Create(byte newPosition, int capturedAbsoluteTokenIndex = -1) =>
        new() { NewPosition = newPosition, CapturedAbsoluteTokenIndex = capturedAbsoluteTokenIndex };
}

public readonly struct DiceRollResult
{
    public Dice Dice { get; init; }
    public MovableTokens Movable { get; init; }
    public bool ForfeitedForTripleSix { get; init; }
    public int TurnId { get; init; }
    public int Player { get; init; }

    public static DiceRollResult Create(Dice diceValue, MovableTokens movableTokens, bool forfeited, int turnId, int playerIndex) =>
        new() { Dice = diceValue, Movable = movableTokens, ForfeitedForTripleSix = forfeited, TurnId = turnId, Player = playerIndex };
}

// ---------------------------
// Public Game API
// ---------------------------
[Serializable]
public sealed class LudoGame
{
    private LudoBoard _board;
    private LudoState _state;
    private readonly Random _rng;

    public bool GameWon { get; private set; }
    public int Winner { get; private set; }

    public int PlayerCount => _board.PlayerCount;
    public int CurrentPlayer => _state.CurrentPlayer;
    public int TurnId => _state.TurnId;
    public long Version => _state.Version;

    private LudoGame(int playerCount, int? seed = null)
    {
        _board = LudoBoard.Create(playerCount);
        _state = LudoState.Create((byte)playerCount);
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        GameWon = false;
        Winner = -1;
    }

    public static LudoGame Create(int playerCount, int? seed = null) => new(playerCount, seed);

    /// <summary>
    /// SERVER: Roll dice for the current player.
    /// - Computes movable tokens and sets state.
    /// - If this is the third consecutive six, the turn is immediately forfeited (no move allowed) and we auto-advance to next player.
    /// </summary>
    public bool TryRollDice(out DiceRollResult diceRollResult, out GameError error)
    {
        diceRollResult = default;
        error = GameError.None;

        if (GameWon) { error = GameError.GameAlreadyWon; return false; }
        if (!_state.CanRollDice()) { error = GameError.NoTurnAvailable; return false; }

        byte diceRoll = (byte)_rng.Next(LudoUtil.MinDiceValue, LudoUtil.MaxDiceValue + 1);
        if (!_board.TryGetMovableTokens(_state.CurrentPlayer, diceRoll, out var movableTokensMask, out error))
            return false;

        // Tentatively record to update consecutive sixes.
        _state.RecordDiceRoll(diceRoll, movableTokensMask);

        // --- Triple-six rule: forfeit immediately, no movement permitted ---
        if (_state.ConsecutiveSixes >= LudoUtil.MaxConsecutiveSixes) // i.e., == 3
        {
            // We expose the roll (clients can animate), but movement is disallowed.
            diceRollResult = DiceRollResult.Create(Dice.FromByte(diceRoll), MovableTokens.None, forfeited: true, _state.TurnId, _state.CurrentPlayer);

            // Advance to next player right now and clear state.
            _state.AdvanceTurn();
            return true;
        }

        // If no moves, pass the turn automatically
        if (movableTokensMask == MovableTokens.None)
        {
            diceRollResult = DiceRollResult.Create(Dice.FromByte(diceRoll), movableTokensMask, forfeited: false, _state.TurnId, _state.CurrentPlayer);
            _state.AdvanceTurn();
            return true;
        }

        // Normal case: moves available; client should now send MoveToken command
        diceRollResult = DiceRollResult.Create(Dice.FromByte(diceRoll), movableTokensMask, forfeited: false, _state.TurnId, _state.CurrentPlayer);
        return true;
    }

    /// <summary>
    /// SERVER: Move one of the current player's movable tokens (local index 0..3).
    /// - Applies capture, win detection, turn/extra-turn logic.
    /// </summary>
    public bool TryMoveToken(int tokenLocalIndex, out MoveResult moveResult, out GameError error)
    {
        moveResult = default; error = GameError.None;

        if (GameWon) { error = GameError.GameAlreadyWon; return false; }
        if (!_state.MustMove) { error = GameError.NoTurnAvailable; return false; }
        if ((uint)tokenLocalIndex >= LudoUtil.TokensPerPlayer) { error = GameError.InvalidTokenIndex; return false; }
        if ((_state.MovableTokensMask & (MovableTokens)(1 << tokenLocalIndex)) == 0)
            { error = GameError.TokenNotMovable; return false; }

        int absoluteTokenIndex = LudoUtil.GetPlayerTokenStart(_state.CurrentPlayer) + tokenLocalIndex;

        if (!_board.TryMoveToken(absoluteTokenIndex, _state.LastDiceRoll, out var newPosition, out error))
            return false;

        _board.TryCaptureOpponent(absoluteTokenIndex, out var capturedAbsoluteTokenIndex);

        if (_board.TryHasPlayerWon(_state.CurrentPlayer, out var won, out _) && won)
        {
            GameWon = true;
            Winner = _state.CurrentPlayer;
        }

        // Extra turn on six UNLESS this would have been the 3rd six (which we never allow here)
        bool extraTurn = _state.LastDiceRoll == 6 && _state.ConsecutiveSixes < LudoUtil.MaxConsecutiveSixes;

        if (extraTurn)
        {
            // Keep the same player; allow them to roll again; keep consecutive sixes count.
            _state.ClearAfterMoveOrExtraRoll();
        }
        else
        {
            _state.AdvanceTurn();
        }

        moveResult = MoveResult.Create(newPosition, capturedAbsoluteTokenIndex);
        return true;
    }

    // --------- Serialization & snapshots (for client/server) ---------

    /// <summary>A compact, serializable view of the entire game for sync.</summary>
    public GameSnapshot GetSnapshot() =>
        new()
        {
            PlayerCount = PlayerCount,
            CurrentPlayer = CurrentPlayer,
            ConsecutiveSixes = _state.ConsecutiveSixes,
            LastDiceRoll = _state.LastDiceRoll,
            MovableTokensMask = _state.MovableTokensMask,
            Tokens = (byte[])_board.TokenPositions.Clone(),
            GameWon = GameWon,
            Winner = Winner,
            TurnId = _state.TurnId,
            Version = _state.Version
        };

    /// <summary>Rehydrate game from a snapshot (e.g., for client to mirror server state).</summary>
    public static LudoGame FromSnapshot(GameSnapshot snapshot)
    {
        var g = new LudoGame(snapshot.PlayerCount, seed: 0);
        g._board.TokenPositions = (byte[])snapshot.Tokens.Clone();
        g._state = new LudoState
        {
            PlayerCount = (byte)snapshot.PlayerCount,
            CurrentPlayer = (byte)snapshot.CurrentPlayer,
            ConsecutiveSixes = (byte)snapshot.ConsecutiveSixes,
            LastDiceRoll = (byte)snapshot.LastDiceRoll,
            MovableTokensMask = snapshot.MovableTokensMask,
            TurnId = snapshot.TurnId,
            Version = snapshot.Version
        };
        g.GameWon = snapshot.GameWon;
        g.Winner = snapshot.Winner;
        return g;
    }

    // ---- Narrow read-only helpers ----
    public byte GetTokenPosition(int absoluteTokenIndex) => _board.GetTokenPosition(absoluteTokenIndex);
    public bool IsTokenHome(int absoluteTokenIndex) => _board.IsHome(absoluteTokenIndex);
    public bool IsTokenAtBase(int absoluteTokenIndex) => _board.IsAtBase(absoluteTokenIndex);
    public string Describe() => _board.ToString();
}

public static class Wire
{
    static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ToJson<T>(T obj) => JsonSerializer.Serialize(obj, _opts);
    public static T? FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, _opts);
}

/// <summary>Immutable snapshot for syncing state to clients.</summary>
public sealed class GameSnapshot
{
    public required int PlayerCount { get; init; }
    public required int CurrentPlayer { get; init; }
    public required int ConsecutiveSixes { get; init; }
    public required int LastDiceRoll { get; init; }
    public required MovableTokens MovableTokensMask { get; init; }
    public required byte[] Tokens { get; init; } = Array.Empty<byte>();
    public required bool GameWon { get; init; }
    public required int Winner { get; init; }
    public required int TurnId { get; init; }
    public required long Version { get; init; }
}

// -------- Commands clients send to server --------
public interface ICommand { public string Type { get; } }

public sealed class RollDiceCommand : ICommand
{
    public string Type => nameof(RollDiceCommand);
    public int ExpectTurnId { get; init; } // optimistic control; server can ignore if mismatch
}

public sealed class MoveTokenCommand : ICommand
{
    public string Type => nameof(MoveTokenCommand);
    public int ExpectTurnId { get; init; }
    public int TokenLocalIndex { get; init; } // 0..3
}

// -------- Events server broadcasts to clients ----
public interface IEvent { public string Type { get; } }

public sealed class DiceRolledEvent : IEvent
{
    public string Type => nameof(DiceRolledEvent);
    public int Player { get; init; }
    public int TurnId { get; init; }
    public byte Dice { get; init; }
    public MovableTokens Movable { get; init; }
    public bool ForfeitedForTripleSix { get; init; }
    public GameSnapshot Snapshot { get; init; } = null!;
}

public sealed class TokenMovedEvent : IEvent
{
    public string Type => nameof(TokenMovedEvent);
    public int Player { get; init; }
    public int TurnId { get; init; }
    public int TokenLocalIndex { get; init; }
    public byte NewPosition { get; init; }
    public int CapturedAbsoluteTokenIndex { get; init; } // -1 if none
    public bool ExtraTurn { get; init; }
    public bool GameWon { get; init; }
    public int Winner { get; init; }
    public GameSnapshot Snapshot { get; init; } = null!;
}

public sealed class TurnAdvancedEvent : IEvent
{
    public string Type => nameof(TurnAdvancedEvent);
    public int PreviousPlayer { get; init; }
    public int NextPlayer { get; init; }
    public int TurnId { get; init; }
    public GameSnapshot Snapshot { get; init; } = null!;
}

public sealed class ErrorEvent : IEvent
{
    public string Type => nameof(ErrorEvent);
    public GameError Error { get; init; }
    public string Message { get; init; } = string.Empty;
    public GameSnapshot Snapshot { get; init; } = null!;
}

public static class ServerSide
{
    /// <summary>Handle a command and emit one or more events (to broadcast to all clients).</summary>
    public static IEnumerable<IEvent> Handle(LudoGame game, ICommand cmd)
    {
        switch (cmd)
        {
            case RollDiceCommand r:
            {
                if (r.ExpectTurnId != game.TurnId)
                    yield return new ErrorEvent { Error = GameError.InvalidCommandForTurn, Message = "Turn ID mismatch.", Snapshot = game.GetSnapshot() };
                else if (game.TryRollDice(out var diceRollResult, out var error))
                {
                    yield return new DiceRolledEvent
                    {
                        Player = diceRollResult.Player,
                        TurnId = diceRollResult.TurnId,
                        Dice = diceRollResult.Dice,
                        Movable = diceRollResult.Movable,
                        ForfeitedForTripleSix = diceRollResult.ForfeitedForTripleSix,
                        Snapshot = game.GetSnapshot()
                    };
                }
                else
                {
                    yield return new ErrorEvent { Error = error, Message = error.ToString(), Snapshot = game.GetSnapshot() };
                }
                break;
            }
            case MoveTokenCommand m:
            {
                if (m.ExpectTurnId != game.TurnId)
                {
                    yield return new ErrorEvent { Error = GameError.InvalidCommandForTurn, Message = "Turn ID mismatch.", Snapshot = game.GetSnapshot() };
                    yield break;
                }

                if (game.TryMoveToken(m.TokenLocalIndex, out var moveResult, out var error))
                {
                    // Determine extra turn: if current player didn’t advance, TurnId won’t change.
                    // But we advanced on server; check by comparing snapshot TurnId vs provided ExpectTurnId.
                    var snapshot = game.GetSnapshot();
                    bool extraTurn = snapshot.TurnId == m.ExpectTurnId; // same turn -> still same player
                    yield return new TokenMovedEvent
                    {
                        Player = snapshot.CurrentPlayer, // note: if extraTurn false, this is next player
                        TurnId = snapshot.TurnId,
                        TokenLocalIndex = m.TokenLocalIndex,
                        NewPosition = moveResult.NewPosition,
                        CapturedAbsoluteTokenIndex = moveResult.CapturedAbsoluteTokenIndex,
                        ExtraTurn = extraTurn,
                        GameWon = snapshot.GameWon,
                        Winner = snapshot.Winner,
                        Snapshot = snapshot
                    };

                    // If turn actually advanced, emit a TurnAdvancedEvent as well (handy for UI).
                    if (!extraTurn)
                    {
                        yield return new TurnAdvancedEvent
                        {
                            PreviousPlayer = (snapshot.CurrentPlayer + snapshot.PlayerCount - 1) % snapshot.PlayerCount,
                            NextPlayer = snapshot.CurrentPlayer,
                            TurnId = snapshot.TurnId,
                            Snapshot = snapshot
                        };
                    }
                }
                else
                {
                    yield return new ErrorEvent { Error = error, Message = error.ToString(), Snapshot = game.GetSnapshot() };
                }
                break;
            }
            default:
                yield return new ErrorEvent { Error = GameError.InvalidCommandForTurn, Message = "Unknown command.", Snapshot = game.GetSnapshot() };
                break;
        }
    }
}
