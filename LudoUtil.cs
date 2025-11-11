using System;
using System.Runtime.CompilerServices;

namespace Ludo
{
    /// <summary>
    /// Utility helpers for Ludo game validation and common operations
    /// </summary>
    public static class LudoUtil
    {
        public const byte MinDiceValue = 1;
        public const byte MaxDiceValue = 6;
        
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidDiceRoll(int diceRoll) =>
            diceRoll >= MinDiceValue && diceRoll <= MaxDiceValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClampDiceRoll(int diceRoll) =>
            (byte)Math.Max(MinDiceValue, Math.Min(MaxDiceValue, diceRoll));
    }
}