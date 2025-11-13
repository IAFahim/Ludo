using System.Net;
using System.Net.WebSockets;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace WebSocketExample.Tests;

[TestFixture]
public class WebSocketServerTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();
    }

    [Test]
    public async Task Connection_CanConnect_Successfully()
    {
        var client = await CreateAndConnectClient();
        
        client.State.Should().Be(WebSocketState.Open);
        
        await CloseClient(client);
    }

    [Test]
    public async Task Connection_CanClose_Successfully()
    {
        var client = await CreateAndConnectClient();
        
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
        
        client.State.Should().Be(WebSocketState.Closed);
        client.Dispose();
    }

    [Test]
    public async Task SendMessage_ReceivesEcho_Successfully()
    {
        var client = await CreateAndConnectClient();
        
        await SendMessage(client, "Hello WebSocket");
        var response = await ReceiveMessage(client);
        
        response.Should().Be("Echo: Hello WebSocket");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_EmptyString_ReceivesEcho()
    {
        var client = await CreateAndConnectClient();
        
        await SendMessage(client, "");
        var response = await ReceiveMessage(client);
        
        response.Should().Be("Echo: ");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_WithSpecialCharacters_PreservesContent()
    {
        var client = await CreateAndConnectClient();
        var specialMessage = "Hello! @#$%^&*() 你好 🎉";
        
        await SendMessage(client, specialMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {specialMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_LongContent_ReceivesEcho()
    {
        var client = await CreateAndConnectClient();
        var longMessage = new string('A', 3000);
        
        await SendMessage(client, longMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {longMessage}");
        response.Should().HaveLength(3006);
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMultipleMessages_AllEchoed_InOrder()
    {
        var client = await CreateAndConnectClient();
        
        var messages = new[] { "Message 1", "Message 2", "Message 3" };
        
        foreach (var msg in messages)
        {
            await SendMessage(client, msg);
        }
        
        var responses = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            responses.Add(await ReceiveMessage(client));
        }
        
        responses.Should().HaveCount(3);
        responses[0].Should().Be("Echo: Message 1");
        responses[1].Should().Be("Echo: Message 2");
        responses[2].Should().Be("Echo: Message 3");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_RapidFire_AllEchoed()
    {
        var client = await CreateAndConnectClient();
        
        var sendTasks = Enumerable.Range(0, 10)
            .Select(i => SendMessage(client, $"Message {i}"))
            .ToArray();
        
        await Task.WhenAll(sendTasks);
        
        var responses = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            responses.Add(await ReceiveMessage(client));
        }
        
        responses.Should().HaveCount(10);
        responses.Should().AllSatisfy(r => r.Should().StartWith("Echo: Message "));
        
        await CloseClient(client);
    }

    [Test]
    public async Task MultipleClients_EachReceivesOwnEcho()
    {
        var client1 = await CreateAndConnectClient();
        var client2 = await CreateAndConnectClient();
        
        await SendMessage(client1, "From Client 1");
        await SendMessage(client2, "From Client 2");
        
        var response1 = await ReceiveMessage(client1);
        var response2 = await ReceiveMessage(client2);
        
        response1.Should().Be("Echo: From Client 1");
        response2.Should().Be("Echo: From Client 2");
        
        await CloseClient(client1);
        await CloseClient(client2);
    }

    [Test]
    public async Task SendMessage_AfterReconnect_StillWorks()
    {
        var client = await CreateAndConnectClient();
        
        await SendMessage(client, "First message");
        var response1 = await ReceiveMessage(client);
        response1.Should().Be("Echo: First message");
        
        await CloseClient(client);
        
        client = await CreateAndConnectClient();
        
        await SendMessage(client, "Second message");
        var response2 = await ReceiveMessage(client);
        response2.Should().Be("Echo: Second message");
        
        await CloseClient(client);
    }

    [Test]
    public async Task Connection_MultipleSequential_AllSucceed()
    {
        for (int i = 0; i < 5; i++)
        {
            var client = await CreateAndConnectClient();
            client.State.Should().Be(WebSocketState.Open);
            await CloseClient(client);
        }
    }

    [Test]
    public async Task SendMessage_WithNewlines_PreservesFormat()
    {
        var client = await CreateAndConnectClient();
        var multilineMessage = "Line 1\nLine 2\nLine 3";
        
        await SendMessage(client, multilineMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {multilineMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_WithTabs_PreservesFormat()
    {
        var client = await CreateAndConnectClient();
        var tabbedMessage = "Column1\tColumn2\tColumn3";
        
        await SendMessage(client, tabbedMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {tabbedMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_JsonPayload_EchoedCorrectly()
    {
        var client = await CreateAndConnectClient();
        var jsonMessage = "{\"name\":\"John\",\"age\":30,\"city\":\"New York\"}";
        
        await SendMessage(client, jsonMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {jsonMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_XmlPayload_EchoedCorrectly()
    {
        var client = await CreateAndConnectClient();
        var xmlMessage = "<root><item>Value</item></root>";
        
        await SendMessage(client, xmlMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {xmlMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task ConcurrentClients_AllWorkIndependently()
    {
        var clients = await Task.WhenAll(
            Enumerable.Range(0, 5)
                .Select(_ => CreateAndConnectClient())
        );
        
        var sendTasks = clients.Select((client, i) =>
            SendMessage(client, $"Message from client {i}")
        ).ToArray();
        
        await Task.WhenAll(sendTasks);
        
        for (int i = 0; i < clients.Length; i++)
        {
            var response = await ReceiveMessage(clients[i]);
            response.Should().Be($"Echo: Message from client {i}");
        }
        
        await Task.WhenAll(clients.Select(CloseClient));
    }

    [Test]
    public async Task LongRunningConnection_StaysOpen()
    {
        var client = await CreateAndConnectClient();
        
        client.State.Should().Be(WebSocketState.Open);
        
        await Task.Delay(2000);
        
        client.State.Should().Be(WebSocketState.Open);
        
        await SendMessage(client, "Still alive");
        var response = await ReceiveMessage(client);
        response.Should().Be("Echo: Still alive");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_Interleaved_WithDelays()
    {
        var client = await CreateAndConnectClient();
        
        await SendMessage(client, "Message 1");
        await Task.Delay(100);
        await SendMessage(client, "Message 2");
        await Task.Delay(100);
        await SendMessage(client, "Message 3");
        
        var response1 = await ReceiveMessage(client);
        var response2 = await ReceiveMessage(client);
        var response3 = await ReceiveMessage(client);
        
        response1.Should().Be("Echo: Message 1");
        response2.Should().Be("Echo: Message 2");
        response3.Should().Be("Echo: Message 3");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_VeryLongContent_HandlesCorrectly()
    {
        var client = await CreateAndConnectClient();
        var veryLongMessage = new string('B', 3500);
        
        await SendMessage(client, veryLongMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().StartWith("Echo: ");
        response.Should().HaveLength(3506);
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_UnicodeCharacters_PreservesEncoding()
    {
        var client = await CreateAndConnectClient();
        var unicodeMessage = "Hello 世界 مرحبا привет שלום";
        
        await SendMessage(client, unicodeMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {unicodeMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_Numbers_EchoedCorrectly()
    {
        var client = await CreateAndConnectClient();
        
        await SendMessage(client, "12345");
        var response = await ReceiveMessage(client);
        
        response.Should().Be("Echo: 12345");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_MixedContent_EchoedCorrectly()
    {
        var client = await CreateAndConnectClient();
        var mixedMessage = "Text123!@#$%ABC你好🎉";
        
        await SendMessage(client, mixedMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {mixedMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task MultipleMessages_BackToBack_AllProcessed()
    {
        var client = await CreateAndConnectClient();
        
        for (int i = 0; i < 20; i++)
        {
            await SendMessage(client, $"Message {i}");
            var response = await ReceiveMessage(client);
            response.Should().Be($"Echo: Message {i}");
        }
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_WithQuotes_PreservesContent()
    {
        var client = await CreateAndConnectClient();
        var quotedMessage = "He said \"Hello\" to me";
        
        await SendMessage(client, quotedMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {quotedMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task SendMessage_WithBackslashes_PreservesContent()
    {
        var client = await CreateAndConnectClient();
        var pathMessage = "C:\\Users\\Test\\File.txt";
        
        await SendMessage(client, pathMessage);
        var response = await ReceiveMessage(client);
        
        response.Should().Be($"Echo: {pathMessage}");
        
        await CloseClient(client);
    }

    [Test]
    public async Task Connection_CloseWithReason_CompletesCleanly()
    {
        var client = await CreateAndConnectClient();
        
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test finished successfully", CancellationToken.None);
        
        client.State.Should().Be(WebSocketState.Closed);
        client.CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
        
        client.Dispose();
    }

    [Test]
    public async Task MultipleClients_ConcurrentSends_AllReceiveEchoes()
    {
        var client1 = await CreateAndConnectClient();
        var client2 = await CreateAndConnectClient();
        var client3 = await CreateAndConnectClient();
        
        var send1 = SendMessage(client1, "From 1");
        var send2 = SendMessage(client2, "From 2");
        var send3 = SendMessage(client3, "From 3");
        
        await Task.WhenAll(send1, send2, send3);
        
        var response1 = await ReceiveMessage(client1);
        var response2 = await ReceiveMessage(client2);
        var response3 = await ReceiveMessage(client3);
        
        response1.Should().Be("Echo: From 1");
        response2.Should().Be("Echo: From 2");
        response3.Should().Be("Echo: From 3");
        
        await CloseClient(client1);
        await CloseClient(client2);
        await CloseClient(client3);
    }

    [Test]
    public async Task SendMessage_AfterOtherClientDisconnects_StillWorks()
    {
        var client1 = await CreateAndConnectClient();
        var client2 = await CreateAndConnectClient();
        
        await SendMessage(client1, "Before disconnect");
        await ReceiveMessage(client1);
        
        await CloseClient(client2);
        
        await SendMessage(client1, "After disconnect");
        var response = await ReceiveMessage(client1);
        response.Should().Be("Echo: After disconnect");
        
        await CloseClient(client1);
    }

    private async Task<WebSocket> CreateAndConnectClient()
    {
        var wsClient = _factory.Server.CreateWebSocketClient();
        return await wsClient.ConnectAsync(new Uri(_factory.Server.BaseAddress, "ws"), CancellationToken.None);
    }

    private async Task SendMessage(WebSocket client, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task<string> ReceiveMessage(WebSocket client, int timeoutMs = 5000)
    {
        var buffer = new byte[1024 * 64];
        var cts = new CancellationTokenSource(timeoutMs);
        
        var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
        
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    private async Task CloseClient(WebSocket client)
    {
        if (client.State == WebSocketState.Open)
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
        }
        client.Dispose();
    }
}
