using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;

namespace ABCRetail.Services;

public class AzureQueueService : IAzureQueueService
{
    private readonly QueueServiceClient? _queueServiceClient;
    private readonly bool _isAzureConnected;

    public AzureQueueService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        var queueSasUrl = configuration["AzureStorage:QueueSasUrl"];
        
        // Check if we have a valid SAS URL or connection string
        if (!string.IsNullOrEmpty(queueSasUrl))
        {
            try
            {
                // Use the full SAS URL for queue storage
                _queueServiceClient = new QueueServiceClient(new Uri(queueSasUrl));
                _isAzureConnected = true;
                Console.WriteLine("✅ Azure Queue Service connected successfully using SAS URL");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure Queue Service SAS URL connection failed: {ex.Message}");
                _isAzureConnected = false;
            }
        }
        else if (!string.IsNullOrEmpty(connectionString) && 
            !connectionString.Contains("YOUR_ACCOUNT_NAME") && 
            !connectionString.Contains("YOUR_ACCOUNT_KEY"))
        {
            try
            {
                _queueServiceClient = new QueueServiceClient(connectionString);
                _isAzureConnected = true;
                Console.WriteLine("✅ Azure Queue Service connected successfully using connection string");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Azure Queue Service connection string failed: {ex.Message}");
                _isAzureConnected = false;
            }
        }
        else
        {
            Console.WriteLine("⚠️ Azure Queue Service: Missing SAS URL or valid connection string");
            _isAzureConnected = false;
        }
    }

    public async Task<bool> EnqueueMessageAsync(string queueName, string message)
    {
        if (!_isAzureConnected || _queueServiceClient == null)
            return false;

        try
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message)));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> DequeueMessageAsync(string queueName)
    {
        if (!_isAzureConnected || _queueServiceClient == null)
            return null;

        try
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            var response = await queueClient.ReceiveMessageAsync();
            
            if (response.Value != null)
            {
                var message = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(response.Value.MessageText));
                await queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                return message;
            }
        }
        catch
        {
            // Log error if needed
        }

        return null;
    }

    public async Task<List<string>> PeekMessagesAsync(string queueName, int maxMessages = 10)
    {
        var messages = new List<string>();
        
        if (!_isAzureConnected || _queueServiceClient == null)
            return messages;

        try
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            var response = await queueClient.PeekMessagesAsync(maxMessages);
            
            foreach (var message in response.Value)
            {
                var decodedMessage = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
                messages.Add(decodedMessage);
            }
        }
        catch
        {
            // Log error if needed
        }

        return messages;
    }

    public async Task<int> GetQueueLengthAsync(string queueName)
    {
        if (!_isAzureConnected || _queueServiceClient == null)
            return 0;

        try
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            var properties = await queueClient.GetPropertiesAsync();
            return properties.Value.ApproximateMessagesCount;
        }
        catch
        {
            return 0;
        }
    }
}
