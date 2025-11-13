using System.Net.WebSockets;
using System.Text;

var client = new ClientWebSocket();

Console.WriteLine("WebSocket Client Example");
Console.WriteLine("========================\n");

try
{
    await client.ConnectAsync(new Uri("ws://localhost:5002/ws"), CancellationToken.None);
    Console.WriteLine("Connected to WebSocket server!");

    var receiveTask = Task.Run(async () =>
    {
        var buffer = new byte[1024 * 4];
        while (client.State == WebSocketState.Open)
        {
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");
            }
        }
    });

    // Send messages
    var messages = new[]
    {
        "Hello from WebSocket client!",
        "This is a second message.",
        "WebSockets are cool!"
    };

    foreach (var message in messages)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        Console.WriteLine($"Sent: {message}");
        await Task.Delay(1000);
    }

    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();

    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
    await receiveTask;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    client.Dispose();
}
