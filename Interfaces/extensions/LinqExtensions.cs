namespace Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class LinqExtensions
    {
        public static async Task ForeachAwaiting<T>(this IEnumerable<T> values, Func<T, Task> action)
        {
            foreach (var value in values)
            {
                await action(value);
            }
        }

        public static void Foreach<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (var value in values)
            {
                action(value);
            }
        }
    }
}