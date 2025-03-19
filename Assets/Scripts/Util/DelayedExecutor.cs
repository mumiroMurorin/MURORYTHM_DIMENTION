using System;
using System.Threading;
using System.Threading.Tasks;

public class DelayedExecutor
{
    public static async Task ExecuteAfterDelay(float delaySeconds, Action action, CancellationToken token = default)
    {
        if (action == null) return;

        try
        {
            int delayMilliseconds = (int)(delaySeconds * 1000);
            await Task.Delay(delayMilliseconds, token);

            if (!token.IsCancellationRequested)
            {
                action.Invoke();
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}
