using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;

namespace SignalRExample.Tests;

[TestFixture]
public class SignalRServerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private string _hubUrl = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _hubUrl = $"{_factory.Server.BaseAddress}chatHub";
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Connection_CanConnect_Successfully()
    {
        var connection = CreateConnection();
        
        await connection.StartAsync();
        
        connection.State.Should().Be(HubConnectionState.Connected);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task Connection_CanDisconnect_Successfully()
    {
        var connection = CreateConnection();
        await connection.StartAsync();
        
        await connection.StopAsync();
        
        connection.State.Should().Be(HubConnectionState.Disconnected);
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_SingleClient_ReceivesOwnMessage()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<(string user, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add((user, message));
        });
        
        await connection.StartAsync();
        await connection.InvokeAsync("SendMessage", "TestUser", "Hello World");
        await Task.Delay(500);
        
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].user.Should().Be("TestUser");
        receivedMessages[0].message.Should().Be("Hello World");
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_MultipleClients_AllReceive()
    {
        var connection1 = CreateConnection();
        var connection2 = CreateConnection();
        var connection3 = CreateConnection();
        
        var received1 = new List<(string user, string message)>();
        var received2 = new List<(string user, string message)>();
        var received3 = new List<(string user, string message)>();
        
        connection1.On<string, string>("ReceiveMessage", (user, message) => received1.Add((user, message)));
        connection2.On<string, string>("ReceiveMessage", (user, message) => received2.Add((user, message)));
        connection3.On<string, string>("ReceiveMessage", (user, message) => received3.Add((user, message)));
        
        await connection1.StartAsync();
        await connection2.StartAsync();
        await connection3.StartAsync();
        
        await connection1.InvokeAsync("SendMessage", "User1", "Message from User1");
        await Task.Delay(500);
        
        received1.Should().HaveCount(1);
        received2.Should().HaveCount(1);
        received3.Should().HaveCount(1);
        
        received1[0].Should().Be(("User1", "Message from User1"));
        received2[0].Should().Be(("User1", "Message from User1"));
        received3[0].Should().Be(("User1", "Message from User1"));
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection3.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
        await connection3.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_MultipleMessages_AllReceived()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<(string user, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add((user, message));
        });
        
        await connection.StartAsync();
        
        await connection.InvokeAsync("SendMessage", "User1", "Message 1");
        await connection.InvokeAsync("SendMessage", "User2", "Message 2");
        await connection.InvokeAsync("SendMessage", "User3", "Message 3");
        await Task.Delay(500);
        
        receivedMessages.Should().HaveCount(3);
        receivedMessages[0].Should().Be(("User1", "Message 1"));
        receivedMessages[1].Should().Be(("User2", "Message 2"));
        receivedMessages[2].Should().Be(("User3", "Message 3"));
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task BroadcastMessage_MultipleClients_OnlyOthersReceive()
    {
        var connection1 = CreateConnection();
        var connection2 = CreateConnection();
        var connection3 = CreateConnection();
        
        var received1 = new List<string>();
        var received2 = new List<string>();
        var received3 = new List<string>();
        
        connection1.On<string>("ReceiveBroadcast", message => received1.Add(message));
        connection2.On<string>("ReceiveBroadcast", message => received2.Add(message));
        connection3.On<string>("ReceiveBroadcast", message => received3.Add(message));
        
        await connection1.StartAsync();
        await connection2.StartAsync();
        await connection3.StartAsync();
        
        await connection1.InvokeAsync("BroadcastMessage", "Hello from connection1");
        await Task.Delay(500);
        
        received1.Should().BeEmpty();
        received2.Should().HaveCount(1);
        received3.Should().HaveCount(1);
        received2[0].Should().Be("Hello from connection1");
        received3[0].Should().Be("Hello from connection1");
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection3.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
        await connection3.DisposeAsync();
    }

    [Test]
    public async Task BroadcastMessage_SingleClient_ReceivesNothing()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<string>();
        
        connection.On<string>("ReceiveBroadcast", message => receivedMessages.Add(message));
        
        await connection.StartAsync();
        await connection.InvokeAsync("BroadcastMessage", "Test message");
        await Task.Delay(500);
        
        receivedMessages.Should().BeEmpty();
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_WithEmptyUser_SendsSuccessfully()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<(string user, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add((user, message));
        });
        
        await connection.StartAsync();
        await connection.InvokeAsync("SendMessage", "", "Message with empty user");
        await Task.Delay(500);
        
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].user.Should().BeEmpty();
        receivedMessages[0].message.Should().Be("Message with empty user");
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_WithEmptyMessage_SendsSuccessfully()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<(string user, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add((user, message));
        });
        
        await connection.StartAsync();
        await connection.InvokeAsync("SendMessage", "TestUser", "");
        await Task.Delay(500);
        
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].message.Should().BeEmpty();
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_WithSpecialCharacters_PreservesContent()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<(string user, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add((user, message));
        });
        
        await connection.StartAsync();
        var specialMessage = "Hello! @#$%^&*() 你好 🎉 \n\t";
        await connection.InvokeAsync("SendMessage", "User", specialMessage);
        await Task.Delay(500);
        
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].message.Should().Be(specialMessage);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_WithLongContent_SendsSuccessfully()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<(string user, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add((user, message));
        });
        
        await connection.StartAsync();
        var longMessage = new string('A', 10000);
        await connection.InvokeAsync("SendMessage", "User", longMessage);
        await Task.Delay(500);
        
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].message.Should().HaveLength(10000);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task MultipleConnections_SequentialMessages_AllReceived()
    {
        var connection1 = CreateConnection();
        var connection2 = CreateConnection();
        
        var received1 = new List<(string user, string message)>();
        var received2 = new List<(string user, string message)>();
        
        connection1.On<string, string>("ReceiveMessage", (user, message) => received1.Add((user, message)));
        connection2.On<string, string>("ReceiveMessage", (user, message) => received2.Add((user, message)));
        
        await connection1.StartAsync();
        await connection2.StartAsync();
        
        await connection1.InvokeAsync("SendMessage", "User1", "From 1");
        await connection2.InvokeAsync("SendMessage", "User2", "From 2");
        await connection1.InvokeAsync("SendMessage", "User1", "Again from 1");
        await Task.Delay(500);
        
        received1.Should().HaveCount(3);
        received2.Should().HaveCount(3);
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Test]
    public async Task Connection_Reconnect_AfterDisconnect()
    {
        var connection = CreateConnection();
        
        await connection.StartAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
        
        await connection.StopAsync();
        connection.State.Should().Be(HubConnectionState.Disconnected);
        
        await connection.StartAsync();
        connection.State.Should().Be(HubConnectionState.Connected);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task SendMessage_RapidFire_AllReceived()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<(string user, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add((user, message));
        });
        
        await connection.StartAsync();
        
        var tasks = Enumerable.Range(0, 20)
            .Select(i => connection.InvokeAsync("SendMessage", $"User{i}", $"Message {i}"))
            .ToArray();
        
        await Task.WhenAll(tasks);
        await Task.Delay(1000);
        
        receivedMessages.Should().HaveCount(20);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Test]
    public async Task BroadcastMessage_MultipleBroadcasts_AllReceived()
    {
        var connection1 = CreateConnection();
        var connection2 = CreateConnection();
        
        var received2 = new List<string>();
        
        connection2.On<string>("ReceiveBroadcast", message => received2.Add(message));
        
        await connection1.StartAsync();
        await connection2.StartAsync();
        
        await connection1.InvokeAsync("BroadcastMessage", "Broadcast 1");
        await connection1.InvokeAsync("BroadcastMessage", "Broadcast 2");
        await connection1.InvokeAsync("BroadcastMessage", "Broadcast 3");
        await Task.Delay(500);
        
        received2.Should().HaveCount(3);
        received2[0].Should().Be("Broadcast 1");
        received2[1].Should().Be("Broadcast 2");
        received2[2].Should().Be("Broadcast 3");
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Test]
    public async Task MixedMessages_SendAndBroadcast_BothWork()
    {
        var connection1 = CreateConnection();
        var connection2 = CreateConnection();
        
        var received1Send = new List<(string user, string message)>();
        var received2Send = new List<(string user, string message)>();
        var received2Broadcast = new List<string>();
        
        connection1.On<string, string>("ReceiveMessage", (user, message) => received1Send.Add((user, message)));
        connection2.On<string, string>("ReceiveMessage", (user, message) => received2Send.Add((user, message)));
        connection2.On<string>("ReceiveBroadcast", message => received2Broadcast.Add(message));
        
        await connection1.StartAsync();
        await connection2.StartAsync();
        
        await connection1.InvokeAsync("SendMessage", "User1", "Regular message");
        await connection1.InvokeAsync("BroadcastMessage", "Broadcast message");
        await Task.Delay(500);
        
        received1Send.Should().HaveCount(1);
        received2Send.Should().HaveCount(1);
        received2Broadcast.Should().HaveCount(1);
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Test]
    public async Task LateJoiner_MissesEarlierMessages()
    {
        var connection1 = CreateConnection();
        var received1 = new List<(string user, string message)>();
        
        connection1.On<string, string>("ReceiveMessage", (user, message) => received1.Add((user, message)));
        
        await connection1.StartAsync();
        await connection1.InvokeAsync("SendMessage", "User1", "Early message");
        await Task.Delay(500);
        
        var connection2 = CreateConnection();
        var received2 = new List<(string user, string message)>();
        connection2.On<string, string>("ReceiveMessage", (user, message) => received2.Add((user, message)));
        await connection2.StartAsync();
        
        await connection1.InvokeAsync("SendMessage", "User1", "Late message");
        await Task.Delay(500);
        
        received1.Should().HaveCount(2);
        received2.Should().HaveCount(1);
        received2[0].message.Should().Be("Late message");
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Test]
    public async Task DisconnectedClient_DoesNotReceive()
    {
        var connection1 = CreateConnection();
        var connection2 = CreateConnection();
        
        var received1 = new List<(string user, string message)>();
        var received2 = new List<(string user, string message)>();
        
        connection1.On<string, string>("ReceiveMessage", (user, message) => received1.Add((user, message)));
        connection2.On<string, string>("ReceiveMessage", (user, message) => received2.Add((user, message)));
        
        await connection1.StartAsync();
        await connection2.StartAsync();
        
        await connection1.InvokeAsync("SendMessage", "User1", "Before disconnect");
        await Task.Delay(500);
        
        await connection2.StopAsync();
        
        await connection1.InvokeAsync("SendMessage", "User1", "After disconnect");
        await Task.Delay(500);
        
        received1.Should().HaveCount(2);
        received2.Should().HaveCount(1);
        received2[0].message.Should().Be("Before disconnect");
        
        await connection1.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Test]
    public async Task ConcurrentSends_FromMultipleClients_AllReceived()
    {
        var connections = Enumerable.Range(0, 5)
            .Select(_ => CreateConnection())
            .ToArray();
        
        var allReceived = new List<(string user, string message)>();
        var lockObj = new object();
        
        foreach (var conn in connections)
        {
            conn.On<string, string>("ReceiveMessage", (user, message) =>
            {
                lock (lockObj)
                {
                    allReceived.Add((user, message));
                }
            });
        }
        
        await Task.WhenAll(connections.Select(c => c.StartAsync()));
        
        var sendTasks = connections.Select((conn, i) =>
            conn.InvokeAsync("SendMessage", $"User{i}", $"Message from {i}"))
            .ToArray();
        
        await Task.WhenAll(sendTasks);
        await Task.Delay(1000);
        
        allReceived.Should().HaveCount(25);
        
        await Task.WhenAll(connections.Select(c => c.StopAsync()));
        await Task.WhenAll(connections.Select(c => c.DisposeAsync().AsTask()));
    }

    [Test]
    public async Task MessageOrdering_PreservedForSingleClient()
    {
        var connection = CreateConnection();
        var receivedMessages = new List<string>();
        
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            receivedMessages.Add(message);
        });
        
        await connection.StartAsync();
        
        for (int i = 0; i < 10; i++)
        {
            await connection.InvokeAsync("SendMessage", "User", $"Message {i}");
        }
        
        await Task.Delay(500);
        
        receivedMessages.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            receivedMessages[i].Should().Be($"Message {i}");
        }
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    private HubConnection CreateConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();
    }
}
