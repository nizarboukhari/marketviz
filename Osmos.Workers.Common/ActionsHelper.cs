using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Osmos.Workers.Common
{
    public static class ActionsHelper
    {
        public static void SmoothRun(Action action, string errorMessage = null)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            try
            {
                action();
            }
            catch (Exception e)
            {
                string message = errorMessage ?? e.Message;
                Console.WriteLine($"error in {action.Method.Name}: {message}");
                Console.WriteLine($"{e.StackTrace}");
            }
        }

        public static async Task SmoothRunAsync(Func<Task> function, string errorMessage = null)
        {
            if (function == null) throw new ArgumentNullException(nameof(function));

            try
            {
                await function();
            }
            catch (Exception e)
            {
                string message = errorMessage ?? e.Message;
                Console.WriteLine($"error in {function.Method.Name}: {message}");
                Console.WriteLine($"{e.StackTrace}");
            }
        }

        public static async Task LoopAsync(Func<Task> function, int ? delay = null) {
            do
            {
                await SmoothRunAsync(function);
                if (delay != null) {
                    await Task.Delay((int)delay);
                    //Console.WriteLine($"waiting for loop delay of {delay}");
                }
            } while (true);
        }

        public static void Loop(Action action, int? delay = null) {
            do
            {
                SmoothRun(action);
                if (delay != null)
                {
                    Task.Delay((int)delay).Wait();
                    //Console.WriteLine($"waiting for loop delay of {delay}");
                }
            } while (true);
        }

        public static async Task LoopAsync(IEnumerable<Func<Task>> functions, int? delay = null)
        {
            async Task function()
            {
                await Task.WhenAll(functions.Select(f => Task.Run(async () =>
                {
                    await f();
                })));
            }
            await LoopAsync(function);
        }
    }
}
