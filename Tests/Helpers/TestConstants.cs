namespace Ludo.Tests.Helpers
{
    /// <summary>
    /// Constants used across test files
    /// </summary>
    public static class TestConstants
    {
        public const int DefaultPlayerCount = 4;
        public const int TokensPerPlayer = 4;
        public const int MinDiceValue = 1;
        public const int MaxDiceValue = 6;
        public const int ExitDiceValue = 6;

        public const byte BasePosition = 0;
        public const byte StartPosition = 1;
        public const byte MainTrackEnd = 51;
        public const byte HomeStretchStart = 52;
        public const byte HomePosition = 57;

        public const int MaxSimulationTurns = 1000;
    }
}
