using System;

namespace Ludo
{
    /// <summary>
    /// Compact, testable Ludo board rules engine with centralized validation.
    /// Relative positions:
    ///   0   = Base
    ///   1..51 = Main track (player-relative)
    ///   52..56 = Home stretch (5 tiles)
    ///   57  = Home (win for that token)
    /// Absolute loop (capturing / safe squares): 52 tiles.
    /// </summary>
    [Serializable]
    public struct LudoBoard
    {
        // Relative positions (player-centric)
        private const byte BasePosition = 0;
        private const byte StartPosition = 1;
        private const byte RelativeMainMax = 51;
        private const byte HomeStretchStartPosition = 52;
        public const byte StepsToHome = 5;
        private const byte HomePosition = HomeStretchStartPosition + StepsToHome; // 57

        // Absolute loop (global)
        private const byte AbsoluteTrackLength = 52;
        private const byte ExitFromBaseAtRoll = 6;
        private const byte TokensPerPlayer = 4;
        private const byte PlayerTrackOffset = AbsoluteTrackLength / 4; // 13

        // Absolute safe tiles (global)
        public static readonly byte[] SafeAbsoluteTiles = new byte[] { 1, 14, 27, 40 };

        public byte[] tokenPositions;
        public int PlayerCount => tokenPositions.Length / 4;

        // =========================
        // Centralized Validation
        // =========================

        private bool TryValidateTokenIndex(int tokenIndex, out LudoError error)
        {
            if (tokenIndex < 0 || tokenIndex >= tokenPositions.Length)
            {
                error = LudoError.InvalidTokenIndex;
                return false;
            }
            error = default;
            return true;
        }

        private bool TryValidatePlayerIndex(int playerIndex, out LudoError error)
        {
            if (playerIndex < 0 || playerIndex >= PlayerCount)
            {
                error = LudoError.InvalidPlayerIndex;
                return false;
            }
            error = default;
            return true;
        }

        private bool TryValidateDiceRoll(int diceRoll, out LudoError error)
        {
            if (diceRoll < 1 || diceRoll > 6)
            {
                error = LudoError.InvalidDiceRoll;
                return false;
            }
            error = default;
            return true;
        }

        private bool TryValidatePositionValue(byte position, out LudoError error)
        {
            if (position == BasePosition ||
                position >= StartPosition && position <= RelativeMainMax ||
                position >= HomeStretchStartPosition && position < HomePosition ||
                position == HomePosition)
            {
                error = default;
                return true;
            }
            error = LudoError.InvalidPositionValue;
            return false;
        }

        // =========================
        // Constructor
        // =========================

        public LudoBoard(int playerCount)
        {
            tokenPositions = new byte[Math.Max(0, playerCount) * TokensPerPlayer];
        }

        // =========================
        // Public API - Token Operations
        // =========================

        public bool TryGetTokenPosition(int tokenIndex, out byte position, out LudoError error)
        {
            position = 0;
            if (!TryValidateTokenIndex(tokenIndex, out error)) return false;

            position = tokenPositions[tokenIndex];
            return true;
        }

        public bool TrySetTokenPosition(int tokenIndex, byte position, out LudoError error)
        {
            if (!TryValidateTokenIndex(tokenIndex, out error)) return false;
            if (!TryValidatePositionValue(position, out error)) return false;

            tokenPositions[tokenIndex] = position;
            return true;
        }

        public bool TryMoveToken(int tokenIndex, int steps, out byte newPosition, out sbyte evictedTokenIndex, out LudoError error)
        {
            newPosition = 0;
            evictedTokenIndex = -1;

            if (!TryValidateTokenIndex(tokenIndex, out error)) return false;
            if (!TryValidateDiceRoll(steps, out error)) return false;

            if (IsHome(tokenIndex))
            {
                error = LudoError.TokenAlreadyHome;
                return false;
            }

            if (!TryComputeNewPosition(tokenIndex, steps, out newPosition, out error))
            {
                return false;
            }

            // Execute move
            tokenPositions[tokenIndex] = newPosition;

            // Handle capture logic
            if (IsOnMainTrack(tokenIndex) && !IsOnSafeTile(tokenIndex))
            {
                evictedTokenIndex = TryCaptureSingleOpponent(tokenIndex);
            }

            return true;
        }

        public bool TryGetOutOfBase(int tokenIndex, int diceRoll, out LudoError error)
        {
            if (!TryValidateTokenIndex(tokenIndex, out error)) return false;
            if (!TryValidateDiceRoll(diceRoll, out error)) return false;

            if (diceRoll != ExitFromBaseAtRoll)
            {
                error = LudoError.InvalidDiceRoll;
                return false;
            }

            if (!IsAtBase(tokenIndex))
            {
                error = LudoError.TokenNotAtBase;
                return false;
            }

            tokenPositions[tokenIndex] = StartPosition;
            return true;
        }

        // =========================
        // Public API - Game State Queries
        // =========================

        public bool TryGetMovableTokens(int playerIndex, int diceRoll, out sbyte movableTokens, out LudoError error)
        {
            movableTokens = 0;

            if (!TryValidatePlayerIndex(playerIndex, out error)) return false;
            if (!TryValidateDiceRoll(diceRoll, out error)) return false;

            int playerTokenStartIndex = playerIndex * TokensPerPlayer;
            for (int i = 0; i < TokensPerPlayer; i++)
            {
                int tokenIndex = playerTokenStartIndex + i;
                if (TryComputeNewPosition(tokenIndex, diceRoll, out _, out _))
                {
                    movableTokens |= (sbyte)(1 << i);
                }
            }

            return true;
        }

        public bool TryHasWon(int playerIndex, out bool hasWon, out LudoError error)
        {
            hasWon = false;

            if (!TryValidatePlayerIndex(playerIndex, out error)) return false;

            int start = playerIndex * TokensPerPlayer;
            for (int i = 0; i < TokensPerPlayer; i++)
            {
                if (!IsHome(start + i))
                {
                    return true; // hasWon = false already set
                }
            }

            hasWon = true;
            return true;
        }


        public bool IsAtBase(int tokenIndex) => tokenPositions[tokenIndex] == BasePosition;

        public bool IsOnMainTrack(int tokenIndex)
        {
            byte p = tokenPositions[tokenIndex];
            return p >= StartPosition && p <= RelativeMainMax;
        }

        public bool IsOnHomeStretch(int tokenIndex)
        {
            byte p = tokenPositions[tokenIndex];
            return p >= HomeStretchStartPosition && p < HomePosition;
        }

        public bool IsHome(int tokenIndex) => tokenPositions[tokenIndex] == HomePosition;

        public bool IsOnSafeTile(int tokenIndex)
        {
            if (IsOnHomeStretch(tokenIndex)) return true;
            if (!IsOnMainTrack(tokenIndex)) return false;

            int absolutePosition = GetAbsolutePosition(tokenIndex);
            return IsSafeAbsoluteTile((byte)absolutePosition);
        }

        // =========================
        // Private Helpers
        // =========================

        private bool TryComputeNewPosition(int tokenIndex, int steps, out byte newPosition, out LudoError error)
        {
            newPosition = 0;
            byte currentPosition = tokenPositions[tokenIndex];

            if (IsHome(tokenIndex))
            {
                error = LudoError.TokenAlreadyHome;
                return false;
            }

            // Token at base
            if (IsAtBase(tokenIndex))
            {
                if (steps != ExitFromBaseAtRoll)
                {
                    error = LudoError.TokenNotMovable;
                    return false;
                }
                newPosition = StartPosition;
                error = default;
                return true;
            }

            // Token on main track
            if (IsOnMainTrack(tokenIndex))
            {
                int relativeTarget = currentPosition + steps;

                if (relativeTarget <= RelativeMainMax)
                {
                    newPosition = (byte)relativeTarget;
                    error = default;
                    return true;
                }

                // Entering home stretch
                int stepsIntoHome = relativeTarget - RelativeMainMax;
                int target = HomeStretchStartPosition + stepsIntoHome - 1;

                if (target > HomePosition)
                {
                    error = LudoError.WouldOvershootHome;
                    return false;
                }

                newPosition = (byte)target;
                error = default;
                return true;
            }

            // Token on home stretch
            if (IsOnHomeStretch(tokenIndex))
            {
                int target = currentPosition + steps;
                if (target > HomePosition)
                {
                    error = LudoError.WouldOvershootHome;
                    return false;
                }

                newPosition = (byte)target;
                error = default;
                return true;
            }

            error = LudoError.TokenNotMovable;
            return false;
        }

        private sbyte TryCaptureSingleOpponent(int movedTokenIndex)
        {
            if (!IsOnMainTrack(movedTokenIndex)) return -1;

            int moverPlayer = movedTokenIndex / TokensPerPlayer;
            int landingAbs = GetAbsolutePosition(movedTokenIndex);

            int foundOpponentIndex = -1;
            int opponentsHere = 0;

            for (int i = 0; i < tokenPositions.Length; i++)
            {
                if (!IsOnMainTrack(i)) continue;
                if ((i / TokensPerPlayer) == moverPlayer) continue;
                if (GetAbsolutePosition(i) != landingAbs) continue;

                opponentsHere++;
                if (opponentsHere == 1) foundOpponentIndex = i;
                if (opponentsHere > 1) return -1; // Multiple opponents = no capture
            }

            if (opponentsHere == 1)
            {
                tokenPositions[foundOpponentIndex] = BasePosition;
                return (sbyte)foundOpponentIndex;
            }

            return -1;
        }

        private int GetAbsolutePosition(int tokenIndex)
        {
            if (!IsOnMainTrack(tokenIndex)) return -1;

            int playerIndex = tokenIndex / TokensPerPlayer;
            int relativePosition = tokenPositions[tokenIndex];
            int playerOffset = GetPlayerTrackOffset(playerIndex);

            return (relativePosition - 1 + playerOffset) % AbsoluteTrackLength + 1;
        }

        private int GetPlayerTrackOffset(int playerIndex)
        {
            if (PlayerCount == 2) return playerIndex * 2 * PlayerTrackOffset;
            return playerIndex * PlayerTrackOffset;
        }

        private static bool IsSafeAbsoluteTile(byte absolute)
        {
            foreach (var t in SafeAbsoluteTiles)
            {
                if (t == absolute) return true;
            }
            return false;
        }

        // =========================
        // Display
        // =========================

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append('P').Append(PlayerCount).Append(" | ");

            for (int p = 0; p < PlayerCount; p++)
            {
                if (p > 0) sb.Append(" || ");
                sb.Append('p').Append(p).Append(':');

                int start = p * TokensPerPlayer;
                for (int t = 0; t < TokensPerPlayer; t++)
                {
                    if (t > 0) sb.Append(',');
                    int idx = start + t;
                    byte pos = tokenPositions[idx];

                    if (IsAtBase(idx))
                    {
                        sb.Append('B');
                        continue;
                    }

                    if (IsHome(idx))
                    {
                        sb.Append('H');
                        continue;
                    }

                    if (IsOnHomeStretch(idx))
                    {
                        int step = pos - HomeStretchStartPosition + 1;
                        sb.Append('S').Append(step).Append('✨');
                        continue;
                    }

                    // On main track
                    int abs = GetAbsolutePosition(idx);
                    sb.Append(pos).Append('@').Append(abs);
                    if (IsOnSafeTile(idx)) sb.Append('⭐');
                }
            }

            return sb.ToString();
        }
    }
}