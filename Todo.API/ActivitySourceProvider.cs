using System.Diagnostics;

namespace Todo.API
{
    public class ActivitySourceProvider
    {
        public static ActivitySource Source = new ActivitySource("Todo.API");
    }
}