using System;

namespace Ludo
{
    [Serializable]
    public struct LudoState
    {
        public byte currentPlayer;
        public byte playerCount;
        public sbyte moveableTokens;
        public byte consecutiveSixes;
        public byte lastDiceRoll;
        
        public LudoState(byte playerCount)
        {
            this.playerCount = playerCount;
            this.currentPlayer = 0;
            this.moveableTokens = -1; // -1 indicates no pending move
            this.consecutiveSixes = 0;
            this.lastDiceRoll = 0;
        }
    }

    public static class LudoStateImpl
    {
        private const byte MaxConsecutiveSixes = 3;
        private const byte DiceValueSix = 6;

        /// <summary>
        /// Handles dice roll and token movement in a single turn action.
        /// If tokenId is -1, only rolls the dice and calculates moveable tokens.
        /// If tokenId is valid, applies the move for that token.
        /// </summary>
        public static bool TryRoll(
            this ref LudoState ludoState,
            ref LudoBoard ludoBoard,
            sbyte tokenId,
            byte diceValue,
            out byte newPosition,
            out sbyte evictedTokenIndex,
            out LudoError ludoError
        )
        {
            newPosition = 0;
            evictedTokenIndex = -1;
            ludoError = default;

            // Validate dice roll
            if (!TryValidateDiceRoll(diceValue, out ludoError))
            {
                return false;
            }

            // Case 1: Rolling dice (tokenId = -1)
            if (tokenId < 0)
            {
                // Can't roll if there's a pending move
                if (ludoState.moveableTokens > 0)
                {
                    ludoError = LudoError.TurnMissing;
                    return false;
                }

                return TryRollDice(ref ludoState, ref ludoBoard, diceValue, out ludoError);
            }

            // Case 2: Applying a move (tokenId >= 0)
            return TryApplyMove(
                ref ludoState,
                ref ludoBoard,
                tokenId,
                out newPosition,
                out evictedTokenIndex,
                out ludoError
            );
        }

        /// <summary>
        /// Rolls the dice and calculates which tokens can move.
        /// </summary>
        private static bool TryRollDice(
            ref LudoState ludoState,
            ref LudoBoard ludoBoard,
            byte diceValue,
            out LudoError ludoError
        )
        {
            ludoError = default;
            ludoState.lastDiceRoll = diceValue;

            // Check for three consecutive sixes
            if (diceValue == DiceValueSix)
            {
                ludoState.consecutiveSixes++;
                
                if (ludoState.consecutiveSixes >= MaxConsecutiveSixes)
                {
                    // Lose turn, reset counter, move to next player
                    ludoState.consecutiveSixes = 0;
                    AdvanceToNextPlayer(ref ludoState);
                    ludoState.moveableTokens = -1;
                    return true;
                }
            }
            else
            {
                ludoState.consecutiveSixes = 0;
            }

            // Calculate moveable tokens
            if (!ludoBoard.TryGetMovableTokens(
                ludoState.currentPlayer,
                diceValue,
                out ludoState.moveableTokens,
                out ludoError))
            {
                return false;
            }

            // If no tokens can move, advance to next player
            if (ludoState.moveableTokens == 0)
            {
                AdvanceToNextPlayer(ref ludoState);
                ludoState.moveableTokens = -1;
            }

            return true;
        }

        /// <summary>
        /// Applies a move for the specified token using the last rolled dice value.
        /// </summary>
        private static bool TryApplyMove(
            ref LudoState ludoState,
            ref LudoBoard ludoBoard,
            sbyte tokenId,
            out byte newPosition,
            out sbyte evictedTokenIndex,
            out LudoError ludoError
        )
        {
            newPosition = 0;
            evictedTokenIndex = -1;

            // Validate that there's a pending move
            if (ludoState.moveableTokens <= 0)
            {
                ludoError = LudoError.TurnMissing;
                return false;
            }

            // Validate token ID is in valid range (0-3 for the current player)
            if (tokenId < 0 || tokenId >= 4)
            {
                ludoError = LudoError.InvalidTokenIndex;
                return false;
            }

            // Check if the specified token is actually moveable
            int tokenBit = 1 << tokenId;
            if ((ludoState.moveableTokens & tokenBit) == 0)
            {
                ludoError = LudoError.TokenNotMovable;
                return false;
            }

            // Calculate the actual token index for this player
            int playerTokenStartIndex = ludoState.currentPlayer * 4;
            int tokenIndex = playerTokenStartIndex + tokenId;

            // Handle getting out of base
            if (ludoBoard.IsAtBase(tokenIndex) && ludoState.lastDiceRoll == DiceValueSix)
            {
                if (!ludoBoard.TryGetOutOfBase(tokenIndex, ludoState.lastDiceRoll, out ludoError))
                {
                    return false;
                }
                newPosition = 1; // Start position
            }
            else
            {
                // Normal move
                if (!ludoBoard.TryMoveToken(
                    tokenIndex,
                    ludoState.lastDiceRoll,
                    out newPosition,
                    out evictedTokenIndex,
                    out ludoError))
                {
                    return false;
                }
            }

            // Move is successful, clear pending moves
            ludoState.moveableTokens = -1;

            // If didn't roll a six, advance to next player
            if (ludoState.lastDiceRoll != DiceValueSix)
            {
                AdvanceToNextPlayer(ref ludoState);
                ludoState.consecutiveSixes = 0;
            }
            // If rolled a six, player gets another turn (but don't reset consecutiveSixes)

            return true;
        }

        /// <summary>
        /// Standalone move application for external use (when dice is already rolled).
        /// </summary>
        public static bool TryApplyMove(
            this ref LudoState ludoState,
            ref LudoBoard ludoBoard,
            int tokenIndex,
            int diceValue,
            out byte newPosition,
            out sbyte evictedTokenIndex,
            out LudoError ludoError
        )
        {
            newPosition = 0;
            evictedTokenIndex = -1;

            if (!TryValidateDiceRoll((byte)diceValue, out ludoError))
            {
                return false;
            }

            // Validate token belongs to current player
            int expectedPlayerTokenStart = ludoState.currentPlayer * 4;
            int expectedPlayerTokenEnd = expectedPlayerTokenStart + 4;
            
            if (tokenIndex < expectedPlayerTokenStart || tokenIndex >= expectedPlayerTokenEnd)
            {
                ludoError = LudoError.InvalidTokenIndex;
                return false;
            }

            // Handle getting out of base
            if (ludoBoard.IsAtBase(tokenIndex) && diceValue == DiceValueSix)
            {
                if (!ludoBoard.TryGetOutOfBase(tokenIndex, diceValue, out ludoError))
                {
                    return false;
                }
                newPosition = 1;
            }
            else
            {
                // Normal move
                if (!ludoBoard.TryMoveToken(
                    tokenIndex,
                    diceValue,
                    out newPosition,
                    out evictedTokenIndex,
                    out ludoError))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Advances to the next player in turn order.
        /// </summary>
        private static void AdvanceToNextPlayer(ref LudoState ludoState)
        {
            ludoState.currentPlayer = (byte)((ludoState.currentPlayer + 1) % ludoState.playerCount);
        }

        /// <summary>
        /// Validates dice roll value.
        /// </summary>
        private static bool TryValidateDiceRoll(byte diceValue, out LudoError ludoError)
        {
            if (diceValue < 1 || diceValue > 6)
            {
                ludoError = LudoError.InvalidDiceRoll;
                return false;
            }
            ludoError = default;
            return true;
        }

        /// <summary>
        /// Checks if the current player has won the game.
        /// </summary>
        public static bool TryCheckWin(
            this ref LudoState ludoState,
            ref LudoBoard ludoBoard,
            out bool hasWon,
            out LudoError ludoError
        )
        {
            return ludoBoard.TryHasWon(ludoState.currentPlayer, out hasWon, out ludoError);
        }

        /// <summary>
        /// Resets the game state to initial conditions.
        /// </summary>
        public static void Reset(this ref LudoState ludoState)
        {
            ludoState.currentPlayer = 0;
            ludoState.moveableTokens = -1;
            ludoState.consecutiveSixes = 0;
            ludoState.lastDiceRoll = 0;
        }
    }
}