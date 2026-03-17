using System.Collections.Concurrent;
using RestApi.Models;

namespace RestApi.Services;

public interface ITaskStore
{
    IReadOnlyList<TaskItem> List();
    TaskItem Create(string title);
}

public sealed class InMemoryTaskStore : ITaskStore
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _tasks = new();

    public InMemoryTaskStore()
    {
        var seededTask = new TaskItem(
            Guid.NewGuid(),
            "Review API deployment checklist",
            false,
            DateTimeOffset.UtcNow);

        _tasks.TryAdd(seededTask.Id, seededTask);
    }

    public IReadOnlyList<TaskItem> List() =>
        _tasks.Values
            .OrderBy(task => task.CreatedAt)
            .ToArray();

    public TaskItem Create(string title)
    {
        var task = new TaskItem(
            Guid.NewGuid(),
            title,
            false,
            DateTimeOffset.UtcNow);

        _tasks.TryAdd(task.Id, task);
        return task;
    }
}
