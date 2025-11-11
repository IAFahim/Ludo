namespace Ludo
{
    public enum LudoError : byte
    {
        None = 0,
        InvalidTokenIndex = 1,
        TokenNotMovable = 2,
        InvalidDiceRoll = 3,
        TokenAlreadyHome = 4,
        TokenNotAtBase = 5,
        WouldOvershootHome = 6,
        InvalidPlayerIndex = 7,
        InvalidPositionValue = 8,
        TurnMissing = 9,
    }
    
}