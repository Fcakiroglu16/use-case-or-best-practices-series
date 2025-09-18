namespace Todo.API.Middleware
{
    public class UserLoggingScopeMiddleware(RequestDelegate next, ILogger<UserLoggingScopeMiddleware> logger)
    {
        public async Task Invoke(HttpContext context)
        {
            var userId = context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst("sub")?.Value ?? context.User.Identity.Name
                : "anonymous";

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["UserId"] = userId!
            }))
            {
                await next(context);
            }
        }
    }
}