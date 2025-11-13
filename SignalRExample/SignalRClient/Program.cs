using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5001/chatHub")
    .Build();

connection.On<string, string>("ReceiveMessage", (user, message) =>
{
    Console.WriteLine($"[{user}]: {message}");
});

connection.On<string>("ReceiveBroadcast", (message) =>
{
    Console.WriteLine($"[Broadcast]: {message}");
});

Console.WriteLine("SignalR Client Example");
Console.WriteLine("======================\n");

try
{
    await connection.StartAsync();
    Console.WriteLine("Connected to SignalR hub!");

    // Send messages
    await connection.InvokeAsync("SendMessage", "Client1", "Hello from SignalR client!");
    await Task.Delay(500);

    await connection.InvokeAsync("SendMessage", "Client1", "This is a real-time message.");
    await Task.Delay(500);

    await connection.InvokeAsync("BroadcastMessage", "Broadcasting to all other clients!");
    await Task.Delay(500);

    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();

    await connection.StopAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    await connection.DisposeAsync();
}
