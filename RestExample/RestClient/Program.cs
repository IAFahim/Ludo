using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

Console.WriteLine("REST Client Example");
Console.WriteLine("===================\n");

// POST: Create messages
Console.WriteLine("Creating messages...");
var message1 = new Message { Content = "Hello from REST client!" };
var message2 = new Message { Content = "This is a second message." };

var response1 = await client.PostAsJsonAsync("/api/messages", message1);
var created1 = await response1.Content.ReadFromJsonAsync<Message>();
Console.WriteLine($"Created: {created1?.Id} - {created1?.Content}");

var response2 = await client.PostAsJsonAsync("/api/messages", message2);
var created2 = await response2.Content.ReadFromJsonAsync<Message>();
Console.WriteLine($"Created: {created2?.Id} - {created2?.Content}");

// GET: Retrieve all messages
Console.WriteLine("\nRetrieving all messages...");
var messages = await client.GetFromJsonAsync<List<Message>>("/api/messages");
foreach (var msg in messages ?? new List<Message>())
{
    Console.WriteLine($"  {msg.Id}: {msg.Content} (at {msg.Timestamp:HH:mm:ss})");
}

// GET: Retrieve single message
Console.WriteLine("\nRetrieving message by ID...");
var singleMessage = await client.GetFromJsonAsync<Message>($"/api/messages/{created1?.Id}");
Console.WriteLine($"  Found: {singleMessage?.Content}");

// PUT: Update message
Console.WriteLine("\nUpdating message...");
var updateMessage = new Message { Content = "Updated message content!" };
await client.PutAsJsonAsync($"/api/messages/{created1?.Id}", updateMessage);
var updated = await client.GetFromJsonAsync<Message>($"/api/messages/{created1?.Id}");
Console.WriteLine($"  Updated to: {updated?.Content}");

// DELETE: Remove message
Console.WriteLine("\nDeleting message...");
await client.DeleteAsync($"/api/messages/{created2?.Id}`");
Console.WriteLine($"  Deleted message {created2?.Id}");

// GET: Verify deletion
Console.WriteLine("\nFinal message list:");
messages = await client.GetFromJsonAsync<List<Message>>("/api/messages");
foreach (var msg in messages ?? new List<Message>())
{
    Console.WriteLine($"  {msg.Id}: {msg.Content}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
