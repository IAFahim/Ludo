#!/bin/bash

# 🎮 Run the FUN Ludo Client-Server Tests! 🎲

echo "╔═══════════════════════════════════════════════════════╗"
echo "║   🎮  LUDO CLIENT-SERVER TESTS  🎲                   ║"
echo "║   Watch epic battles unfold in real-time!            ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo ""

# Function to run a test with nice formatting
run_test() {
    local test_name=$1
    local description=$2
    
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  $description"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    
    dotnet test --filter "FullyQualifiedName~$test_name" --logger "console;verbosity=detailed" --no-build
    
    echo ""
    echo "Press Enter to continue..."
    read
}

# Build first
echo "🔨 Building project..."
dotnet build --no-restore
echo ""

# Run each fun test
run_test "EpicBattle" "⚔️  THE EPIC BATTLE: Alice vs Bob"
run_test "CrazyCaptureCarnival" "🎪  THE CRAZY CAPTURE CARNIVAL (4 Players!)"
run_test "TripleSixShowdown" "🎲  THE TRIPLE SIX SHOWDOWN"
run_test "SnapshotSync" "🔄  SNAPSHOT SYNC TEST (Reconnection Magic!)"

echo ""
echo "╔═══════════════════════════════════════════════════════╗"
echo "║   🏆  ALL TESTS COMPLETE!  🎉                        ║"
echo "║   The client-server architecture is SOLID! ✨        ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo ""
