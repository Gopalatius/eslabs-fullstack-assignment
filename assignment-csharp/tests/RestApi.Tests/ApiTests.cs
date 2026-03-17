using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RestApi.Tests;

public sealed class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(_ => { }).CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_ReturnsSeededTask()
    {
        var tasks = await _client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks");

        Assert.NotNull(tasks);
        Assert.NotEmpty(tasks!);
    }

    [Fact]
    public async Task PostTasks_CreatesTask()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Prepare callbot demo notes"
        });

        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<TaskResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("Prepare callbot demo notes", created!.Title);
        Assert.False(created.Done);
    }

    [Fact]
    public async Task PostTasks_RejectsEmptyTitle()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new
        {
            title = "   "
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record TaskResponse(Guid Id, string Title, bool Done, DateTimeOffset CreatedAt);
}
