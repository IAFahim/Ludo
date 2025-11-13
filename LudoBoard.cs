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
    public bool IsAtBase(int absToken) => TokenPositions[absToken] == BasePosition;

    public bool IsOnMainTrack(int absToken)
    {
        byte pos = TokenPositions[absToken];
        return pos is >= StartPosition and <= MainTrackLength;
    }

    public bool IsOnHomeStretch(int absToken)
    {
        byte pos = TokenPositions[absToken];
        return pos is >= HomeStretchStart and < HomePosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsHome(int absToken) => TokenPositions[absToken] == HomePosition;

    public bool IsOnSafeTile(int absToken)
    {
        if (IsOnHomeStretch(absToken)) return true;
        if (!IsOnMainTrack(absToken)) return false;
        int a = GetAbsolutePosition(absToken);
        return a is 1 or 14 or 27 or 40;
    }

    public bool TryGetMovableTokens(int playerIndex, byte dice, out MovableTokens mask, out GameError error)
    {
        mask = MovableTokens.None; error = GameError.None;

        if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
            { error = GameError.InvalidPlayerIndex; return false; }

        if (!LudoUtil.IsValidDiceRoll(dice))
            { error = GameError.InvalidDiceRoll; return false; }

        int start = LudoUtil.GetPlayerTokenStart(playerIndex);
        for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
        {
            int absIdx = start + i;
            if (CanMoveToken(absIdx, dice))
                mask |= (MovableTokens)(1 << i);
        }
        return true;
    }

    public bool TryMoveToken(int absTokenIndex, byte dice, out byte newPos, out GameError error)
    {
        newPos = 0; error = GameError.None;

        if (!LudoUtil.IsValidTokenIndex(absTokenIndex, TokenPositions.Length))
            { error = GameError.InvalidTokenIndex; return false; }

        if (!LudoUtil.IsValidDiceRoll(dice))
            { error = GameError.InvalidDiceRoll; return false; }

        if (IsHome(absTokenIndex))
            { error = GameError.TokenAlreadyHome; return false; }

        if (IsAtBase(absTokenIndex))
        {
            if (dice != ExitDiceValue)
                { error = GameError.CannotLeaveBaseWithoutSix; return false; }

            TokenPositions[absTokenIndex] = StartPosition;
            newPos = StartPosition;
            return true;
        }

        // On track or in stretch
        if (!TryCalculateNewPosition(absTokenIndex, dice, out var p, out error))
            return false;

        TokenPositions[absTokenIndex] = p;
        newPos = p;
        return true;
    }

    public bool TryCaptureOpponent(int movedAbsToken, out int capturedAbsToken)
    {
        capturedAbsToken = -1;

        if (!IsOnMainTrack(movedAbsToken) || IsOnSafeTile(movedAbsToken))
            return true;

        int landingAbs = GetAbsolutePosition(movedAbsToken);
        int oppCount = 0;
        int oppIdx = -1;

        for (int i = 0; i < TokenPositions.Length; i++)
        {
            if (!IsOnMainTrack(i)) continue;
            if (LudoUtil.IsSamePlayer(i, movedAbsToken)) continue;
            if (GetAbsolutePosition(i) != landingAbs) continue;

            oppCount++;
            if (oppCount == 1) oppIdx = i;
            else return true; // blockade: no capture
        }

        if (oppCount == 1)
        {
            TokenPositions[oppIdx] = 0; // send back to base
            capturedAbsToken = oppIdx;
        }
        return true;
    }

    public bool TryHasPlayerWon(int playerIndex, out bool hasWon, out GameError error)
    {
        hasWon = false; error = GameError.None;
        if (!LudoUtil.IsValidPlayerIndex(playerIndex, PlayerCount))
            { error = GameError.InvalidPlayerIndex; return false; }

        int start = LudoUtil.GetPlayerTokenStart(playerIndex);
        for (int i = 0; i < LudoUtil.TokensPerPlayer; i++)
            if (!IsHome(start + i)) return true;

        hasWon = true;
        return true;
    }

    // ---- internals
    private bool CanMoveToken(int absTokenIndex, byte dice)
    {
        if (IsHome(absTokenIndex)) return false;
        if (IsAtBase(absTokenIndex)) return dice == 6;
        return TryCalculateNewPosition(absTokenIndex, dice, out _, out _);
    }

    private bool TryCalculateNewPosition(int absTokenIndex, byte dice, out byte newPos, out GameError error)
    {
        newPos = 0; error = GameError.None;
        byte cur = TokenPositions[absTokenIndex];

        // main track
        if (cur is >= 1 and <= 51)
        {
            int target = cur + dice;
            if (target <= 51)
            {
                newPos = (byte)target;
                return true;
            }
            // cross into home stretch
            int intoStretch = target - 51;
            int stretchPos = 52 + intoStretch - 1;
            if (stretchPos > 57) { error = GameError.WouldOvershootHome; return false; }
            newPos = (byte)stretchPos;
            return true;
        }

        // already in stretch
        if (cur is >= 52 and < 57)
        {
            int target = cur + dice;
            if (target > 57) { error = GameError.WouldOvershootHome; return false; }
            newPos = (byte)target;
            return true;
        }

        error = GameError.TokenNotMovable;
        return false;
    }

    private int GetAbsolutePosition(int absToken)
    {
        if (!IsOnMainTrack(absToken)) return -1;
        int player = LudoUtil.GetPlayerFromToken(absToken);
        int rel = TokenPositions[absToken]; // 1..51
        int offset = (PlayerCount == 2) ? player * 2 * QuarterTrack : player * QuarterTrack;
        return (rel - 1 + offset) % 52 + 1;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("Players: ").Append(PlayerCount).Append(" | ");
        for (int p = 0; p < PlayerCount; p++)
        {
            if (p > 0) sb.Append(" || ");
            sb.Append("P").Append(p).Append(": ");
            int start = LudoUtil.GetPlayerTokenStart(p);
            for (int t = 0; t < LudoUtil.TokensPerPlayer; t++)
            {
                if (t > 0) sb.Append(", ");
                int idx = start + t;
                byte pos = TokenPositions[idx];

                if (pos == 0) sb.Append("Base");
                else if (pos == 57) sb.Append("Home✨");
                else if (pos >= 52) sb.Append("Stretch-").Append(pos - 52 + 1);
                else
                {
                    sb.Append(pos).Append("@");
                    int a = GetAbsolutePosition(idx);
                    sb.Append(a);
                    if (IsOnSafeTile(idx)) sb.Append("⭐");
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

    public void RecordDiceRoll(byte value, MovableTokens mask)
    {
        LastDiceRoll = value;
        MovableTokensMask = mask;
        ConsecutiveSixes = value == 6 ? (byte)(ConsecutiveSixes + 1) : (byte)0;
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
    public int CapturedAbsToken { get; init; }
    public bool DidCapture => CapturedAbsToken >= 0;

    public static MoveResult Create(byte pos, int captured = -1) =>
        new() { NewPosition = pos, CapturedAbsToken = captured };
}

public readonly struct DiceRollResult
{
    public Dice Dice { get; init; }
    public MovableTokens Movable { get; init; }
    public bool ForfeitedForTripleSix { get; init; }
    public int TurnId { get; init; }
    public int Player { get; init; }

    public static DiceRollResult Create(Dice dice, MovableTokens movable, bool forfeited, int turnId, int player) =>
        new() { Dice = dice, Movable = movable, ForfeitedForTripleSix = forfeited, TurnId = turnId, Player = player };
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
    public bool TryRollDice(out DiceRollResult result, out GameError error)
    {
        result = default;
        error = GameError.None;

        if (GameWon) { error = GameError.GameAlreadyWon; return false; }
        if (!_state.CanRollDice()) { error = GameError.NoTurnAvailable; return false; }

        byte roll = (byte)_rng.Next(LudoUtil.MinDiceValue, LudoUtil.MaxDiceValue + 1);
        if (!_board.TryGetMovableTokens(_state.CurrentPlayer, roll, out var mask, out error))
            return false;

        // Tentatively record to update consecutive sixes.
        _state.RecordDiceRoll(roll, mask);

        // --- Triple-six rule: forfeit immediately, no movement permitted ---
        if (_state.ConsecutiveSixes >= LudoUtil.MaxConsecutiveSixes) // i.e., == 3
        {
            // We expose the roll (clients can animate), but movement is disallowed.
            result = DiceRollResult.Create(Dice.FromByte(roll), MovableTokens.None, forfeited: true, _state.TurnId, _state.CurrentPlayer);

            // Advance to next player right now and clear state.
            _state.AdvanceTurn();
            return true;
        }

        // If no moves, pass the turn automatically
        if (mask == MovableTokens.None)
        {
            result = DiceRollResult.Create(Dice.FromByte(roll), mask, forfeited: false, _state.TurnId, _state.CurrentPlayer);
            _state.AdvanceTurn();
            return true;
        }

        // Normal case: moves available; client should now send MoveToken command
        result = DiceRollResult.Create(Dice.FromByte(roll), mask, forfeited: false, _state.TurnId, _state.CurrentPlayer);
        return true;
    }

    /// <summary>
    /// SERVER: Move one of the current player's movable tokens (local index 0..3).
    /// - Applies capture, win detection, turn/extra-turn logic.
    /// </summary>
    public bool TryMoveToken(int tokenLocalIndex, out MoveResult result, out GameError error)
    {
        result = default; error = GameError.None;

        if (GameWon) { error = GameError.GameAlreadyWon; return false; }
        if (!_state.MustMove) { error = GameError.NoTurnAvailable; return false; }
        if ((uint)tokenLocalIndex >= LudoUtil.TokensPerPlayer) { error = GameError.InvalidTokenIndex; return false; }
        if ((_state.MovableTokensMask & (MovableTokens)(1 << tokenLocalIndex)) == 0)
            { error = GameError.TokenNotMovable; return false; }

        int abs = LudoUtil.GetPlayerTokenStart(_state.CurrentPlayer) + tokenLocalIndex;

        if (!_board.TryMoveToken(abs, _state.LastDiceRoll, out var newPos, out error))
            return false;

        _board.TryCaptureOpponent(abs, out var capturedAbs);

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

        result = MoveResult.Create(newPos, capturedAbs);
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
    public static LudoGame FromSnapshot(GameSnapshot snap)
    {
        var g = new LudoGame(snap.PlayerCount, seed: 0);
        g._board.TokenPositions = (byte[])snap.Tokens.Clone();
        g._state = new LudoState
        {
            PlayerCount = (byte)snap.PlayerCount,
            CurrentPlayer = (byte)snap.CurrentPlayer,
            ConsecutiveSixes = (byte)snap.ConsecutiveSixes,
            LastDiceRoll = (byte)snap.LastDiceRoll,
            MovableTokensMask = snap.MovableTokensMask,
            TurnId = snap.TurnId,
            Version = snap.Version
        };
        g.GameWon = snap.GameWon;
        g.Winner = snap.Winner;
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
    public int CapturedAbsToken { get; init; } // -1 if none
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
                else if (game.TryRollDice(out var dr, out var err))
                {
                    yield return new DiceRolledEvent
                    {
                        Player = dr.Player,
                        TurnId = dr.TurnId,
                        Dice = dr.Dice,
                        Movable = dr.Movable,
                        ForfeitedForTripleSix = dr.ForfeitedForTripleSix,
                        Snapshot = game.GetSnapshot()
                    };
                }
                else
                {
                    yield return new ErrorEvent { Error = err, Message = err.ToString(), Snapshot = game.GetSnapshot() };
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

                if (game.TryMoveToken(m.TokenLocalIndex, out var mv, out var err))
                {
                    // Determine extra turn: if current player didn’t advance, TurnId won’t change.
                    // But we advanced on server; check by comparing snapshot TurnId vs provided ExpectTurnId.
                    var snap = game.GetSnapshot();
                    bool extraTurn = snap.TurnId == m.ExpectTurnId; // same turn -> still same player
                    yield return new TokenMovedEvent
                    {
                        Player = snap.CurrentPlayer, // note: if extraTurn false, this is next player
                        TurnId = snap.TurnId,
                        TokenLocalIndex = m.TokenLocalIndex,
                        NewPosition = mv.NewPosition,
                        CapturedAbsToken = mv.CapturedAbsToken,
                        ExtraTurn = extraTurn,
                        GameWon = snap.GameWon,
                        Winner = snap.Winner,
                        Snapshot = snap
                    };

                    // If turn actually advanced, emit a TurnAdvancedEvent as well (handy for UI).
                    if (!extraTurn)
                    {
                        yield return new TurnAdvancedEvent
                        {
                            PreviousPlayer = (snap.CurrentPlayer + snap.PlayerCount - 1) % snap.PlayerCount,
                            NextPlayer = snap.CurrentPlayer,
                            TurnId = snap.TurnId,
                            Snapshot = snap
                        };
                    }
                }
                else
                {
                    yield return new ErrorEvent { Error = err, Message = err.ToString(), Snapshot = game.GetSnapshot() };
                }
                break;
            }
            default:
                yield return new ErrorEvent { Error = GameError.InvalidCommandForTurn, Message = "Unknown command.", Snapshot = game.GetSnapshot() };
                break;
        }
    }
}
