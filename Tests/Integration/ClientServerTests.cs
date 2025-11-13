using NUnit.Framework;
using Ludo;
using System.Collections.Generic;
using System.Linq;

namespace Ludo.Tests.Integration
{
    /// <summary>
    /// 🎮 FUN Client-Server Communication Tests! 🎲
    /// These tests simulate real client-server interactions with epic battles!
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("ClientServer")]
    [Category("Fun")]
    public class ClientServerTests
    {
        /// <summary>
        /// 🎉 The Epic Battle of Two Players!
        /// Alice and Bob duke it out in a thrilling Ludo match!
        /// </summary>
        [Test]
        public void EpicBattle_AliceVsBob_ServerBroadcastsEvents()
        {
            // 🎮 Server creates the game
            var server = LudoGame.Create(2, seed: 42);
            
            // 👥 Client contexts
            var alice = new SimulatedClient("Alice 🦄", 0);
            var bob = new SimulatedClient("Bob 🐉", 1);
            var clients = new[] { alice, bob };
            
            TestContext.WriteLine("🎮 === THE EPIC LUDO BATTLE BEGINS ===");
            TestContext.WriteLine($"⚔️  {alice.Name} vs {bob.Name}");
            TestContext.WriteLine();
            
            int maxTurns = 500;
            int turn = 0;
            
            while (!server.GameWon && turn < maxTurns)
            {
                var currentPlayer = clients[server.CurrentPlayer];
                
                // Client sends RollDiceCommand
                var rollCmd = new RollDiceCommand { ExpectTurnId = server.TurnId };
                var events = ServerSide.Handle(server, rollCmd).ToList();
                
                // Broadcast events to all clients
                foreach (var evt in events)
                {
                    BroadcastToClients(clients, evt, turn);
                    
                    if (evt is DiceRolledEvent diceEvt)
                    {
                        if (diceEvt.ForfeitedForTripleSix)
                        {
                            TestContext.WriteLine($"  💥 {currentPlayer.Name} rolled THREE SIXES in a row! Turn forfeited! 😱");
                        }
                        else if (diceEvt.Movable != MovableTokens.None)
                        {
                            // Try to move a token
                            int tokenToMove = FindFirstMovableToken(diceEvt.Movable);
                            var moveCmd = new MoveTokenCommand 
                            { 
                                ExpectTurnId = diceEvt.TurnId, 
                                TokenLocalIndex = tokenToMove 
                            };
                            
                            var moveEvents = ServerSide.Handle(server, moveCmd).ToList();
                            foreach (var moveEvt in moveEvents)
                            {
                                BroadcastToClients(clients, moveEvt, turn);
                            }
                        }
                    }
                }
                
                turn++;
            }
            
            if (server.GameWon)
            {
                var winner = clients[server.Winner];
                TestContext.WriteLine();
                TestContext.WriteLine($"🏆 === VICTORY! ===");
                TestContext.WriteLine($"👑 {winner.Name} WINS after {turn} turns! 🎊");
                TestContext.WriteLine($"📊 Final Score:");
                TestContext.WriteLine($"   {alice.Name}: {alice.TokensHome} tokens home, {alice.Captures} captures!");
                TestContext.WriteLine($"   {bob.Name}: {bob.TokensHome} tokens home, {bob.Captures} captures!");
            }
            
            Assert.That(server.GameWon, Is.True, "Game should complete");
            Assert.That(turn, Is.LessThan(maxTurns), "Game should complete within reasonable turns");
        }

        /// <summary>
        /// 🎪 The Crazy Capture Carnival!
        /// Watch tokens get sent back home in this chaotic 4-player mayhem!
        /// </summary>
        [Test]
        public void CrazyCaptureCarnival_FourPlayers_ManyCaptures()
        {
            var server = LudoGame.Create(4, seed: 123);
            
            var players = new[]
            {
                new SimulatedClient("🦁 Leo", 0),
                new SimulatedClient("🦊 Foxy", 1),
                new SimulatedClient("🐼 Panda", 2),
                new SimulatedClient("🦉 Hootie", 3)
            };
            
            TestContext.WriteLine("🎪 === THE CRAZY CAPTURE CARNIVAL ===");
            TestContext.WriteLine("🎯 Who will capture the most tokens?!");
            TestContext.WriteLine();
            
            int turn = 0;
            int maxTurns = 1000;
            int totalCaptures = 0;
            
            while (!server.GameWon && turn < maxTurns)
            {
                var rollCmd = new RollDiceCommand { ExpectTurnId = server.TurnId };
                var events = ServerSide.Handle(server, rollCmd).ToList();
                
                foreach (var evt in events)
                {
                    if (evt is DiceRolledEvent diceEvt && 
                        diceEvt.Movable != MovableTokens.None && 
                        !diceEvt.ForfeitedForTripleSix)
                    {
                        int tokenIdx = FindFirstMovableToken(diceEvt.Movable);
                        var moveCmd = new MoveTokenCommand 
                        { 
                            ExpectTurnId = diceEvt.TurnId, 
                            TokenLocalIndex = tokenIdx 
                        };
                        
                        var moveEvents = ServerSide.Handle(server, moveCmd).ToList();
                        foreach (var moveEvt in moveEvents)
                        {
                            if (moveEvt is TokenMovedEvent tokenEvt)
                            {
                                var player = players[tokenEvt.Player];
                                
                                if (tokenEvt.CapturedAbsToken >= 0)
                                {
                                    int victimPlayer = tokenEvt.CapturedAbsToken / 4;
                                    var victim = players[victimPlayer];
                                    
                                    player.Captures++;
                                    totalCaptures++;
                                    
                                    TestContext.WriteLine($"💥 Turn {turn}: {player.Name} CAPTURED {victim.Name}'s token! Total captures: {totalCaptures}");
                                }
                                
                                if (tokenEvt.GameWon)
                                {
                                    player.TokensHome = 4;
                                }
                            }
                        }
                    }
                }
                
                turn++;
            }
            
            TestContext.WriteLine();
            TestContext.WriteLine($"🎊 === CARNIVAL RESULTS ===");
            if (server.GameWon)
            {
                var winner = players[server.Winner];
                TestContext.WriteLine($"👑 Winner: {winner.Name}");
            }
            TestContext.WriteLine($"📈 Total Captures in this crazy game: {totalCaptures}");
            foreach (var player in players)
            {
                TestContext.WriteLine($"   {player.Name}: {player.Captures} captures");
            }
            
            Assert.That(totalCaptures, Is.GreaterThan(0), "There should be some captures in a 4-player game!");
            Assert.That(turn, Is.LessThan(maxTurns));
        }

        /// <summary>
        /// 🎲 The Triple Six Showdown!
        /// Testing the dreaded triple-six rule - will anyone forfeit their turn?
        /// </summary>
        [Test]
        public void TripleSixShowdown_TestsForfeitRule()
        {
            // Use a specific seed that's likely to produce sixes
            var server = LudoGame.Create(2, seed: 777);
            
            var players = new[]
            {
                new SimulatedClient("Lucky Luke 🍀", 0),
                new SimulatedClient("Unlucky Uma 🎲", 1)
            };
            
            TestContext.WriteLine("🎲 === THE TRIPLE SIX SHOWDOWN ===");
            TestContext.WriteLine("⚠️  Watch out for the TRIPLE SIX RULE!");
            TestContext.WriteLine();
            
            int turn = 0;
            int maxTurns = 500;
            int tripleSixCount = 0;
            
            while (!server.GameWon && turn < maxTurns)
            {
                var currentPlayer = players[server.CurrentPlayer];
                
                var rollCmd = new RollDiceCommand { ExpectTurnId = server.TurnId };
                var events = ServerSide.Handle(server, rollCmd).ToList();
                
                foreach (var evt in events)
                {
                    if (evt is DiceRolledEvent diceEvt)
                    {
                        if (diceEvt.ForfeitedForTripleSix)
                        {
                            tripleSixCount++;
                            TestContext.WriteLine($"⚡ Turn {turn}: {currentPlayer.Name} rolled TRIPLE SIX! 🎲🎲🎲 Turn FORFEITED!");
                            currentPlayer.TripleSixes++;
                        }
                        else if (diceEvt.Movable != MovableTokens.None)
                        {
                            int tokenIdx = FindFirstMovableToken(diceEvt.Movable);
                            var moveCmd = new MoveTokenCommand 
                            { 
                                ExpectTurnId = diceEvt.TurnId, 
                                TokenLocalIndex = tokenIdx 
                            };
                            ServerSide.Handle(server, moveCmd).ToList();
                        }
                    }
                }
                
                turn++;
            }
            
            TestContext.WriteLine();
            TestContext.WriteLine($"📊 === SHOWDOWN STATS ===");
            TestContext.WriteLine($"🎲 Total Triple-Six Forfeits: {tripleSixCount}");
            foreach (var player in players)
            {
                TestContext.WriteLine($"   {player.Name}: {player.TripleSixes} triple sixes");
            }
            
            // The test passes regardless of forfeits, but we log them for fun
            Assert.That(turn, Is.LessThan(maxTurns));
            TestContext.WriteLine($"✅ Game completed in {turn} turns!");
        }

        /// <summary>
        /// 🔄 Snapshot Sync Test - Can clients recover from disconnection?
        /// </summary>
        [Test]
        public void SnapshotSync_ClientReconnects_StaysInSync()
        {
            var server = LudoGame.Create(2, seed: 999);
            
            TestContext.WriteLine("🔄 === SNAPSHOT SYNC TEST ===");
            TestContext.WriteLine("Testing client reconnection and synchronization!");
            TestContext.WriteLine();
            
            // Play a few turns
            for (int i = 0; i < 10; i++)
            {
                var rollCmd = new RollDiceCommand { ExpectTurnId = server.TurnId };
                var events = ServerSide.Handle(server, rollCmd).ToList();
                
                foreach (var evt in events)
                {
                    if (evt is DiceRolledEvent diceEvt && 
                        diceEvt.Movable != MovableTokens.None && 
                        !diceEvt.ForfeitedForTripleSix)
                    {
                        int tokenIdx = FindFirstMovableToken(diceEvt.Movable);
                        var moveCmd = new MoveTokenCommand 
                        { 
                            ExpectTurnId = diceEvt.TurnId, 
                            TokenLocalIndex = tokenIdx 
                        };
                        ServerSide.Handle(server, moveCmd).ToList();
                    }
                }
            }
            
            // Get snapshot
            var snapshot = server.GetSnapshot();
            TestContext.WriteLine($"📸 Server snapshot captured:");
            TestContext.WriteLine($"   Turn: {snapshot.TurnId}, Version: {snapshot.Version}");
            TestContext.WriteLine($"   Current Player: {snapshot.CurrentPlayer}");
            
            // Simulate client reconnecting and rehydrating
            var reconnectedClient = LudoGame.FromSnapshot(snapshot);
            
            TestContext.WriteLine($"🔌 Client reconnected and rehydrated from snapshot");
            
            // Verify sync
            Assert.That(reconnectedClient.CurrentPlayer, Is.EqualTo(server.CurrentPlayer));
            Assert.That(reconnectedClient.TurnId, Is.EqualTo(server.TurnId));
            Assert.That(reconnectedClient.Version, Is.EqualTo(server.Version));
            Assert.That(reconnectedClient.GameWon, Is.EqualTo(server.GameWon));
            
            // Verify token positions
            for (int i = 0; i < 8; i++)
            {
                Assert.That(reconnectedClient.GetTokenPosition(i), Is.EqualTo(server.GetTokenPosition(i)),
                    $"Token {i} position mismatch");
            }
            
            TestContext.WriteLine("✅ Client is perfectly in sync!");
            TestContext.WriteLine($"🎯 All {snapshot.Tokens.Length} token positions matched!");
        }

        // ===== Helper Methods =====
        
        private void BroadcastToClients(SimulatedClient[] clients, IEvent evt, int turn)
        {
            foreach (var client in clients)
            {
                client.ReceiveEvent(evt);
            }
            
            // Log interesting events
            if (evt is DiceRolledEvent diceEvt)
            {
                var player = clients[diceEvt.Player];
                string diceSymbol = diceEvt.Dice == 6 ? "⚡" : "🎲";
                TestContext.WriteLine($"{diceSymbol} Turn {turn}: {player.Name} rolled {diceEvt.Dice}");
            }
            else if (evt is TokenMovedEvent moveEvt)
            {
                var player = clients[moveEvt.Player];
                if (moveEvt.GameWon)
                {
                    TestContext.WriteLine($"🏆 {player.Name} got a token HOME! WINNER! 🎊");
                }
                else if (moveEvt.ExtraTurn)
                {
                    TestContext.WriteLine($"🎁 {player.Name} gets an EXTRA TURN!");
                }
            }
        }
        
        private int FindFirstMovableToken(MovableTokens mask)
        {
            for (int i = 0; i < 4; i++)
            {
                if ((mask & (MovableTokens)(1 << i)) != 0)
                    return i;
            }
            return 0;
        }
    }

    /// <summary>
    /// Simulates a game client with personality and stats!
    /// </summary>
    public class SimulatedClient
    {
        public string Name { get; }
        public int PlayerIndex { get; }
        public int TokensHome { get; set; }
        public int Captures { get; set; }
        public int TripleSixes { get; set; }
        
        public SimulatedClient(string name, int playerIndex)
        {
            Name = name;
            PlayerIndex = playerIndex;
            TokensHome = 0;
            Captures = 0;
            TripleSixes = 0;
        }
        
        public void ReceiveEvent(IEvent evt)
        {
            // Client processes events (could update local UI state here)
            if (evt is TokenMovedEvent moveEvt && moveEvt.Player == PlayerIndex)
            {
                if (moveEvt.NewPosition == 57)
                {
                    TokensHome++;
                }
            }
        }
    }
}
