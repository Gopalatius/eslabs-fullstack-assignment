using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WebSocketApp.Tests;

public sealed class WebSocketTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebSocketTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WebSocket_EchoesMessage()
    {
        var webSocket = _factory.Server.CreateWebSocketClient();
        using var socket = await webSocket.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        var request = Encoding.UTF8.GetBytes("""
            {"type":"echo","message":"hello from test"}
            """);

        await socket.SendAsync(request, WebSocketMessageType.Text, true, CancellationToken.None);

        var buffer = new byte[4096];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        var payload = JsonSerializer.Deserialize<EchoResponse>(Encoding.UTF8.GetString(buffer, 0, result.Count));

        Assert.NotNull(payload);
        Assert.Equal("echo", payload!.Type);
        Assert.Equal("hello from test", payload.Message);
        Assert.False(string.IsNullOrWhiteSpace(payload.ClientId));
    }

    [Fact]
    public async Task WebSocket_InvalidPayloadReturnsError()
    {
        var webSocket = _factory.Server.CreateWebSocketClient();
        using var socket = await webSocket.ConnectAsync(new Uri("ws://localhost/ws"), CancellationToken.None);

        var request = Encoding.UTF8.GetBytes("""
            {"type":"unknown","message":""}
            """);

        await socket.SendAsync(request, WebSocketMessageType.Text, true, CancellationToken.None);

        var buffer = new byte[4096];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        var payload = JsonSerializer.Deserialize<EchoResponse>(Encoding.UTF8.GetString(buffer, 0, result.Count));

        Assert.NotNull(payload);
        Assert.Equal("error", payload!.Type);
        Assert.False(string.IsNullOrWhiteSpace(payload.ClientId));
    }

    private sealed record EchoResponse(string Type, string Message, string ClientId, DateTimeOffset Timestamp);
}
