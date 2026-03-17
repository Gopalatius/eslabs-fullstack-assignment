using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var keepAliveSeconds = builder.Configuration.GetValue("WebSocket:KeepAliveSeconds", 30);
var maxMessageBytes = builder.Configuration.GetValue("WebSocket:MaxMessageBytes", 8192);

builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var app = builder.Build();
var connections = new ConcurrentDictionary<string, WebSocket>();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(keepAliveSeconds)
};

if (allowedOrigins.Length > 0)
{
    foreach (var origin in allowedOrigins)
    {
        webSocketOptions.AllowedOrigins.Add(origin);
    }

    app.UseCors("AppCors");
}

app.UseWebSockets(webSocketOptions);
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "websocket-app",
    timestamp = DateTimeOffset.UtcNow
}));

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Expected a WebSocket upgrade request."
        });
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = Guid.NewGuid().ToString("D");
    connections[clientId] = socket;
    app.Logger.LogInformation("WebSocket client connected: {ClientId}. Active connections: {ConnectionCount}", clientId, connections.Count);

    try
    {
        await HandleSocketAsync(socket, clientId, app.Logger, maxMessageBytes, context.RequestAborted);
    }
    finally
    {
        connections.TryRemove(clientId, out _);
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        app.Logger.LogInformation("WebSocket client disconnected: {ClientId}. Active connections: {ConnectionCount}", clientId, connections.Count);
    }
});

app.Run();

static async Task HandleSocketAsync(
    WebSocket socket,
    string clientId,
    ILogger logger,
    int maxMessageBytes,
    CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
    {
        string? message;
        try
        {
            message = await ReceiveTextMessageAsync(socket, maxMessageBytes, cancellationToken);
        }
        catch (MessageTooLargeException)
        {
            await SendJsonAsync(socket, new ErrorResponse("error", $"Payload exceeds {maxMessageBytes} bytes.", clientId), cancellationToken);
            await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Payload too large", cancellationToken);
            break;
        }
        catch (WebSocketException)
        {
            await SendJsonAsync(socket, new ErrorResponse("error", "Only text messages are supported.", clientId), cancellationToken);
            await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Only text messages are supported", cancellationToken);
            break;
        }
        catch (OperationCanceledException)
        {
            break;
        }

        if (message is null)
        {
            break;
        }

        logger.LogInformation("Received message from {ClientId}: {Message}", clientId, message);

        EchoRequest? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EchoRequest>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            await SendJsonAsync(socket, new ErrorResponse("error", "Invalid JSON payload.", clientId), cancellationToken);
            continue;
        }

        if (payload?.Type != "echo" || string.IsNullOrWhiteSpace(payload.Message))
        {
            await SendJsonAsync(socket, new ErrorResponse("error", "Expected {\"type\":\"echo\",\"message\":\"...\"}.", clientId), cancellationToken);
            continue;
        }

        await SendJsonAsync(socket, new EchoResponse("echo", payload.Message.Trim(), clientId, DateTimeOffset.UtcNow), cancellationToken);
    }
}

static async Task<string?> ReceiveTextMessageAsync(WebSocket socket, int maxMessageBytes, CancellationToken cancellationToken)
{
    var buffer = new byte[1024];
    using var stream = new MemoryStream();

    while (true)
    {
        var result = await socket.ReceiveAsync(buffer, cancellationToken);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            return null;
        }

        if (result.MessageType != WebSocketMessageType.Text)
        {
            throw new WebSocketException("Only text messages are supported.");
        }

        if (stream.Length + result.Count > maxMessageBytes)
        {
            throw new MessageTooLargeException();
        }

        stream.Write(buffer, 0, result.Count);

        if (result.EndOfMessage)
        {
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}

static async Task SendJsonAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
{
    if (socket.State != WebSocketState.Open)
    {
        return;
    }

    var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
    await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
}

public partial class Program;

internal sealed record EchoRequest(string? Type, string? Message);

internal sealed record EchoResponse(string Type, string Message, string ClientId, DateTimeOffset Timestamp);

internal sealed record ErrorResponse(string Type, string Message, string ClientId);

internal sealed class MessageTooLargeException : Exception;
