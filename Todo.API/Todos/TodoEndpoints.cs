using MassTransit;
using Shared.Bus;

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
        todos.MapPost("/",
            async (TodoItem todo, ITodoRepository repo, ILoggerFactory loggerFactory,
                IPublishEndpoint publishEndpoint) =>
            {
                var logger = loggerFactory.CreateLogger("TodoEndpoints");
                logger.LogInformation("Creating a new todo item");

                await publishEndpoint.Publish(new ResizeImageCommand("image url", 1000, 400));


                var created = await repo.CreateAsync(todo);
                return Results.Created($"/api/todos/{created.Id}", created);
            });
        todos.MapPost("/send-batch-message",
            async ( ILoggerFactory loggerFactory,
                IPublishEndpoint publishEndpoint) =>
            {
                var logger = loggerFactory.CreateLogger("TodoEndpoints");


                Enumerable.Range(0, 1000).ToList().ForEach(async x =>
                {
                    await publishEndpoint.Publish(new ResizeImageCommand("image url", 1000, 400));

                });
                //logging
                logger.LogInformation("Sending batch message");
                return Results.Ok();






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