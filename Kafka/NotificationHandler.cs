using System.Collections.Concurrent;

namespace UserMicroService.Kafka;

public class NotificationHandler
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingRequests = new();

    public Task<bool> WaitForNotificationAsync(string correlationId, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[correlationId] = tcs;

        // Remove after timeout
        _ = Task.Delay(timeout).ContinueWith(_ =>
        {
            _pendingRequests.TryRemove(correlationId, out TaskCompletionSource<bool> _);
            tcs.TrySetResult(false); // timeout
        });

        return tcs.Task;
    }

    public void HandleNotification(NotificationBatch batch)
    {
        foreach (var correlationId in batch.CorrelationIds)
        {
            if (_pendingRequests.TryRemove(correlationId, out var tcs))
            {
                tcs.TrySetResult(true); // complete the waiting HTTP request
            }
        }
    }
}
