using Microsoft.AspNetCore.SignalR;

namespace SignalRServer;

public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        Console.WriteLine($"Received: {user}: {message}");
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task BroadcastMessage(string message)
    {
        Console.WriteLine($"Broadcasting: {message}");
        await Clients.Others.SendAsync("ReceiveBroadcast", message);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}
