namespace Todo.API.Todos;

public class InMemoryTodoRepository : ITodoRepository
{
    private readonly List<TodoItem> _todos = [];

    public Task<List<TodoItem>> GetAllAsync()
    {
        return Task.FromResult(_todos.ToList());
    }

    public Task<TodoItem?> GetByIdAsync(Guid id)
    {
        string name = "pay";
        Console.WriteLine(name);
        return Task.FromResult(_todos.FirstOrDefault(t => t.Id == id));
    }

    public Task<TodoItem> CreateAsync(TodoItem todo)
    {
        _todos.Add(todo);
        return Task.FromResult(todo);
    }

    public Task<TodoItem?> UpdateAsync(Guid id, TodoItem todo)
    {
        var existing = _todos.FirstOrDefault(t => t.Id == id);
        if (existing == null) return Task.FromResult<TodoItem?>(null);

        existing.Title = todo.Title;
        existing.Description = todo.Description;
        existing.IsCompleted = todo.IsCompleted;

        return Task.FromResult<TodoItem?>(existing);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var existing = _todos.FirstOrDefault(t => t.Id == id);
        if (existing == null) return Task.FromResult(false);

        _todos.Remove(existing);
        return Task.FromResult(true);
    }
}