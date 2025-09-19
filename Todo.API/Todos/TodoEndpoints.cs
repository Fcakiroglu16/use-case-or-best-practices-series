using System.Text.Json;

namespace Todo.API.Todos;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("/api/todos")
            .WithTags("Todos")
            .WithOpenApi();

        // GET all todos
        todos.MapGet("/", async (ITodoRepository repo) =>
            await repo.GetAllAsync());

        // GET todo by id
        todos.MapGet("/{id:guid}", async (Guid id, ITodoRepository repo) =>
        {
            var todo = await repo.GetByIdAsync(id);
            return todo is null ? Results.NotFound() : Results.Ok(todo);
        });

        // POST create a new todo
        todos.MapPost("/", async (TodoItem todo, ITodoRepository repo, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TodoEndpoints");
            logger.LogInformation("Creating a new todo item");


            Guid userId = Guid.NewGuid();


            logger.LogInformation($"A new todo item was created for user ${userId}");

            logger.LogInformation("A new todo item was created for user {UserId}", userId);

            logger.LogInformation("A new todo created todo ={todo}", JsonSerializer.Serialize(todo));

            logger.LogInformation(
                "New todo created with Id {TodoId} for user {UserId}. {todo}",
                todo.Id,
                userId,
                JsonSerializer.Serialize(todo)
            );


            var identityNumber = "12345678901";

            logger.LogInformation("A new todo item was created for Identity number: {identityNumber}", identityNumber);

            logger.LogInformation(
                $"A new todo item was created for Identity number without log attributes: {identityNumber}");

            var created = await repo.CreateAsync(todo);
            return Results.Created($"/api/todos/{created.Id}", created);
        });

        // PUT update a todo
        todos.MapPut("/{id:guid}", async (Guid id, TodoItem todo, ITodoRepository repo) =>
        {
            var updated = await repo.UpdateAsync(id, todo);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        // DELETE a todo
        todos.MapDelete("/{id:guid}", async (Guid id, ITodoRepository repo) =>
        {
            var deleted = await repo.DeleteAsync(id);
            return deleted ? Results.NoContent() : Results.NotFound();
        });
    }
}