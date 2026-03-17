using Microsoft.AspNetCore.Diagnostics;
using RestApi.Models;
using RestApi.Services;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var maxTitleLength = builder.Configuration.GetValue("Validation:TaskTitleMaxLength", 200);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is not null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        var problem = Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Unexpected server error",
            detail: app.Environment.IsDevelopment() ? exception?.Message : null,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = context.TraceIdentifier
            });

        await problem.ExecuteAsync(context);
    });
});

if (allowedOrigins.Length > 0)
{
    app.UseCors("AppCors");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => app.Environment.IsDevelopment()
        ? Results.Redirect("/swagger")
        : Results.Ok(new
        {
            service = "rest-api",
            status = "ok"
        }))
    .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "rest-api",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/tasks", (ITaskStore store) => Results.Ok(store.List()))
    .WithName("ListTasks")
    .WithOpenApi();

app.MapPost("/api/tasks", (CreateTaskRequest request, ITaskStore store, ILogger<Program> logger) =>
{
    var title = request.Title?.Trim();

    if (string.IsNullOrWhiteSpace(title))
    {
        return ValidationError("The title field is required.");
    }

    if (title.Length > maxTitleLength)
    {
        return ValidationError($"The title field must be {maxTitleLength} characters or fewer.");
    }

    var task = store.Create(title);
    logger.LogInformation("Created task {TaskId} with title {Title}", task.Id, task.Title);
    return Results.Created($"/api/tasks/{task.Id}", task);
})
.WithName("CreateTask")
.WithOpenApi();

app.Run();

static IResult ValidationError(string message) =>
    Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["title"] = [message]
    });

public partial class Program;
