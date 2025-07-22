using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace GameCopier.Extensions
{
    public static class DispatcherQueueExtensions
    {
        public static async Task EnqueueAsync(this DispatcherQueue dispatcher, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            dispatcher.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            await tcs.Task;
        }
    }
}