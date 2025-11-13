var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var messages = new List<Message>();

app.MapGet("/api/messages", () => Results.Ok(messages));

app.MapGet("/api/messages/{id}", (int id) =>
{
    var message = messages.FirstOrDefault(m => m.Id == id);
    return message != null ? Results.Ok(message) : Results.NotFound();
});

app.MapPost("/api/messages", (Message message) =>
{
    message.Id = messages.Count > 0 ? messages.Max(m => m.Id) + 1 : 1;
    message.Timestamp = DateTime.UtcNow;
    messages.Add(message);
    return Results.Created($"/api/messages/{message.Id}", message);
});

app.MapPut("/api/messages/{id}", (int id, Message updatedMessage) =>
{
    var message = messages.FirstOrDefault(m => m.Id == id);
    if (message == null) return Results.NotFound();
    
    message.Content = updatedMessage.Content;
    message.Timestamp = DateTime.UtcNow;
    return Results.Ok(message);
});

app.MapDelete("/api/messages/{id}", (int id) =>
{
    var message = messages.FirstOrDefault(m => m.Id == id);
    if (message == null) return Results.NotFound();
    
    messages.Remove(message);
    return Results.NoContent();
});

Console.WriteLine("REST Server running on http://localhost:5000");
app.Run("http://localhost:5000");

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
