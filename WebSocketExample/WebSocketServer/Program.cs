using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("WebSocket client connected");
        await HandleWebSocket(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

Console.WriteLine("WebSocket Server running on http://localhost:5002");
Console.WriteLine("WebSocket endpoint: ws://localhost:5002/ws");
app.Run("http://localhost:5002");

async Task HandleWebSocket(WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    
    try
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Console.WriteLine("WebSocket client disconnected");
            }
            else if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");
                
                var response = $"Echo: {message}";
                var responseBytes = Encoding.UTF8.GetBytes(response);
                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseBytes), 
                    WebSocketMessageType.Text, 
                    true, 
                    CancellationToken.None);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

public partial class Program { }
