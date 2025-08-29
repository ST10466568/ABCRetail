namespace ABCRetail.Services;

public interface IAzureQueueService
{
    Task<bool> EnqueueMessageAsync(string queueName, string message);
    Task<string?> DequeueMessageAsync(string queueName);
    Task<List<string>> PeekMessagesAsync(string queueName, int maxMessages = 10);
    Task<int> GetQueueLengthAsync(string queueName);
}


