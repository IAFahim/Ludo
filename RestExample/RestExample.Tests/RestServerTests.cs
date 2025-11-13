using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace RestExample.Tests;

[TestFixture]
public class RestServerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [SetUp]
    public async Task SetUp()
    {
        var messages = await _client.GetFromJsonAsync<List<Message>>("/api/messages");
        if (messages != null)
        {
            foreach (var msg in messages)
            {
                await _client.DeleteAsync($"/api/messages/{msg.Id}");
            }
        }
    }

    [Test]
    public async Task GetMessages_WhenEmpty_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/messages");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
        messages.Should().NotBeNull();
        messages.Should().BeEmpty();
    }

    [Test]
    public async Task PostMessage_WithValidData_CreatesMessage()
    {
        var newMessage = new Message { Content = "Test message" };
        
        var response = await _client.PostAsJsonAsync("/api/messages", newMessage);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Message>();
        created.Should().NotBeNull();
        created!.Id.Should().BeGreaterThan(0);
        created.Content.Should().Be("Test message");
        created.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task PostMessage_ReturnsCreatedLocation()
    {
        var newMessage = new Message { Content = "Test message" };
        
        var response = await _client.PostAsJsonAsync("/api/messages", newMessage);
        
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/messages/");
    }

    [Test]
    public async Task PostMultipleMessages_AssignsUniqueIds()
    {
        var message1 = new Message { Content = "First message" };
        var message2 = new Message { Content = "Second message" };
        var message3 = new Message { Content = "Third message" };
        
        var response1 = await _client.PostAsJsonAsync("/api/messages", message1);
        var response2 = await _client.PostAsJsonAsync("/api/messages", message2);
        var response3 = await _client.PostAsJsonAsync("/api/messages", message3);
        
        var created1 = await response1.Content.ReadFromJsonAsync<Message>();
        var created2 = await response2.Content.ReadFromJsonAsync<Message>();
        var created3 = await response3.Content.ReadFromJsonAsync<Message>();
        
        created1!.Id.Should().NotBe(created2!.Id);
        created2.Id.Should().NotBe(created3!.Id);
        created1.Id.Should().NotBe(created3.Id);
    }

    [Test]
    public async Task GetMessages_AfterCreating_ReturnsAllMessages()
    {
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Message 1" });
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Message 2" });
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Message 3" });
        
        var response = await _client.GetAsync("/api/messages");
        var messages = await response.Content.ReadFromJsonAsync<List<Message>>();
        
        messages.Should().NotBeNull();
        messages.Should().HaveCount(3);
        messages!.Select(m => m.Content).Should().Contain(new[] { "Message 1", "Message 2", "Message 3" });
    }

    [Test]
    public async Task GetMessageById_WithValidId_ReturnsMessage()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Find me!" });
        var created = await createResponse.Content.ReadFromJsonAsync<Message>();
        
        var response = await _client.GetAsync($"/api/messages/{created!.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var message = await response.Content.ReadFromJsonAsync<Message>();
        message.Should().NotBeNull();
        message!.Id.Should().Be(created.Id);
        message.Content.Should().Be("Find me!");
    }

    [Test]
    public async Task GetMessageById_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/messages/99999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task PutMessage_WithValidId_UpdatesMessage()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Original" });
        var created = await createResponse.Content.ReadFromJsonAsync<Message>();
        var originalTimestamp = created!.Timestamp;
        
        await Task.Delay(100);
        
        var updateMessage = new Message { Content = "Updated content" };
        var response = await _client.PutAsJsonAsync($"/api/messages/{created.Id}", updateMessage);
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Message>();
        updated.Should().NotBeNull();
        updated!.Content.Should().Be("Updated content");
        updated.Timestamp.Should().BeAfter(originalTimestamp);
    }

    [Test]
    public async Task PutMessage_WithInvalidId_ReturnsNotFound()
    {
        var updateMessage = new Message { Content = "Updated" };
        
        var response = await _client.PutAsJsonAsync("/api/messages/99999", updateMessage);
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task PutMessage_PreservesId()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Original" });
        var created = await createResponse.Content.ReadFromJsonAsync<Message>();
        var originalId = created!.Id;
        
        var updateMessage = new Message { Content = "Updated" };
        await _client.PutAsJsonAsync($"/api/messages/{originalId}", updateMessage);
        
        var getResponse = await _client.GetAsync($"/api/messages/{originalId}");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<Message>();
        retrieved!.Id.Should().Be(originalId);
    }

    [Test]
    public async Task DeleteMessage_WithValidId_RemovesMessage()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Delete me" });
        var created = await createResponse.Content.ReadFromJsonAsync<Message>();
        
        var deleteResponse = await _client.DeleteAsync($"/api/messages/{created!.Id}");
        
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var getResponse = await _client.GetAsync($"/api/messages/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteMessage_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/messages/99999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteMessage_RemovesFromList()
    {
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Keep 1" });
        var deleteResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Delete" });
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Keep 2" });
        
        var toDelete = await deleteResponse.Content.ReadFromJsonAsync<Message>();
        await _client.DeleteAsync($"/api/messages/{toDelete!.Id}");
        
        var messages = await _client.GetFromJsonAsync<List<Message>>("/api/messages");
        messages.Should().HaveCount(2);
        messages.Should().NotContain(m => m.Id == toDelete.Id);
    }

    [Test]
    public async Task CrudOperations_CompleteFlow_WorksCorrectly()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Initial" });
        var created = await createResponse.Content.ReadFromJsonAsync<Message>();
        created.Should().NotBeNull();
        
        var getResponse = await _client.GetAsync($"/api/messages/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        await _client.PutAsJsonAsync($"/api/messages/{created.Id}", new Message { Content = "Modified" });
        var updated = await _client.GetFromJsonAsync<Message>($"/api/messages/{created.Id}");
        updated!.Content.Should().Be("Modified");
        
        var deleteResponse = await _client.DeleteAsync($"/api/messages/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var finalGet = await _client.GetAsync($"/api/messages/{created.Id}");
        finalGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task PostMessage_WithEmptyContent_CreatesMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "" });
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Message>();
        created!.Content.Should().BeEmpty();
    }

    [Test]
    public async Task PostMessage_WithLongContent_CreatesMessage()
    {
        var longContent = new string('A', 10000);
        var response = await _client.PostAsJsonAsync("/api/messages", new Message { Content = longContent });
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Message>();
        created!.Content.Should().HaveLength(10000);
    }

    [Test]
    public async Task PostMessage_WithSpecialCharacters_PreservesContent()
    {
        var specialContent = "Hello! @#$%^&*() 你好 🎉 \n\t\r";
        var response = await _client.PostAsJsonAsync("/api/messages", new Message { Content = specialContent });
        
        var created = await response.Content.ReadFromJsonAsync<Message>();
        created!.Content.Should().Be(specialContent);
    }

    [Test]
    public async Task GetMessages_ReturnsMessagesInOrder()
    {
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "First" });
        await Task.Delay(50);
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Second" });
        await Task.Delay(50);
        await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Third" });
        
        var messages = await _client.GetFromJsonAsync<List<Message>>("/api/messages");
        
        messages.Should().HaveCount(3);
        messages![0].Content.Should().Be("First");
        messages[1].Content.Should().Be("Second");
        messages[2].Content.Should().Be("Third");
    }

    [Test]
    public async Task ConcurrentPosts_MayEncounterRaceConditions()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(i => _client.PostAsJsonAsync("/api/messages", new Message { Content = $"Message {i}" }))
            .ToArray();
        
        var responses = await Task.WhenAll(tasks);
        
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        successCount.Should().BeGreaterThan(0, "At least some concurrent requests should succeed");
        
        var messages = await _client.GetFromJsonAsync<List<Message>>("/api/messages");
        messages.Should().NotBeNull();
    }

    [Test]
    public async Task UpdateMessage_MultipleUpdates_LastOneWins()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "V1" });
        var created = await createResponse.Content.ReadFromJsonAsync<Message>();
        
        await _client.PutAsJsonAsync($"/api/messages/{created!.Id}", new Message { Content = "V2" });
        await _client.PutAsJsonAsync($"/api/messages/{created.Id}", new Message { Content = "V3" });
        await _client.PutAsJsonAsync($"/api/messages/{created.Id}", new Message { Content = "V4" });
        
        var final = await _client.GetFromJsonAsync<Message>($"/api/messages/{created.Id}");
        final!.Content.Should().Be("V4");
    }

    [Test]
    public async Task DeleteMessage_TwiceInRow_SecondReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Delete twice" });
        var created = await createResponse.Content.ReadFromJsonAsync<Message>();
        
        var firstDelete = await _client.DeleteAsync($"/api/messages/{created!.Id}");
        firstDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        var secondDelete = await _client.DeleteAsync($"/api/messages/{created.Id}");
        secondDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task MessageTimestamp_IsUtc()
    {
        var response = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "UTC test" });
        var created = await response.Content.ReadFromJsonAsync<Message>();
        
        created!.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public async Task GetMessages_AfterMixedOperations_ReturnsCorrectState()
    {
        var r1 = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Keep 1" });
        var r2 = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Delete" });
        var r3 = await _client.PostAsJsonAsync("/api/messages", new Message { Content = "Keep 2" });
        
        var m1 = await r1.Content.ReadFromJsonAsync<Message>();
        var m2 = await r2.Content.ReadFromJsonAsync<Message>();
        
        await _client.PutAsJsonAsync($"/api/messages/{m1!.Id}", new Message { Content = "Keep 1 Updated" });
        await _client.DeleteAsync($"/api/messages/{m2!.Id}");
        
        var messages = await _client.GetFromJsonAsync<List<Message>>("/api/messages");
        messages.Should().HaveCount(2);
        messages!.Should().Contain(m => m.Content == "Keep 1 Updated");
        messages.Should().Contain(m => m.Content == "Keep 2");
    }
}

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
