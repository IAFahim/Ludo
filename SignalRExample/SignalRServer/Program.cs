using SignalRServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.MapHub<ChatHub>("/chatHub");

Console.WriteLine("SignalR Server running on http://localhost:5001");
Console.WriteLine("Hub endpoint: http://localhost:5001/chatHub");
app.Run("http://localhost:5001");

public partial class Program { }
