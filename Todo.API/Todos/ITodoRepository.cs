namespace Todo.API.Todos;

public interface ITodoRepository
{
    Task<List<TodoItem>> GetAllAsync();
    Task<TodoItem?> GetByIdAsync(Guid id);
    Task<TodoItem> CreateAsync(TodoItem todo);
    Task<TodoItem?> UpdateAsync(Guid id, TodoItem todo);
    Task<bool> DeleteAsync(Guid id);
}