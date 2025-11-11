using System;

namespace Ludo
{
    [Serializable]
    public struct LudoState
    {
        public byte playerCount;
        public sbyte moveableTokens;
        public byte consecutiveSixes;
        public byte lastDiceRoll;
        
        public static LudoState Default()
        {
            return new LudoState
            {
                moveableTokens = -1
            };
        }
    }

    public static class LudoStateImpl
    {
        
    }
}