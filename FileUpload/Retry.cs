namespace FileUpload;

/// <summary>
/// See https://stackoverflow.com/questions/1563191/cleanest-way-to-write-retry-logic
/// </summary>
public static class Retry
{
    /// <summary>
    /// Try an operation and on failure wait and try again.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="retryInterval">Doubles on each retry.</param>
    /// <param name="maxAttemptCount"></param>
    /// <returns></returns>
    /// <exception cref="AggregateException"></exception>
    public static async Task DoAsync(Func<Task> action, TimeSpan retryInterval, int maxAttemptCount = 3)
    {
        var exceptions = new List<Exception>();

        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                if (attempted > 0)
                {
                    await Task.Delay(retryInterval * (attempted + 1));
                }
                await action();
                return;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }
        throw new AggregateException(exceptions);
    }
}