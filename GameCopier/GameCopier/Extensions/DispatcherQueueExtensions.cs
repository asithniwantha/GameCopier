using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

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