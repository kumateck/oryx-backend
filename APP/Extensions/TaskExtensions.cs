namespace APP.Extensions;

public static class TaskExtensions
{
    public static async Task<List<T>> WhenAll<T>(this IEnumerable<Task<T>> tasks)
    {
        return (await Task.WhenAll(tasks)).ToList();
    }
}
