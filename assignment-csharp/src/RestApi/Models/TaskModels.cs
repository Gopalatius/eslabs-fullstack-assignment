namespace RestApi.Models;

public record TaskItem(Guid Id, string Title, bool Done, DateTimeOffset CreatedAt);

public record CreateTaskRequest(string? Title);
