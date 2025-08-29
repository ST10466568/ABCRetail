using System.Text.Json.Serialization;

namespace ABCRetail.Models
{
    public class InventoryQueueMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty; // "add", "remove", "update", "low_stock_alert"

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "normal"; // "low", "normal", "high", "urgent"

        [JsonPropertyName("status")]
        public string Status { get; set; } = "pending"; // "pending", "processing", "completed", "failed"
    }

    public enum InventoryAction
    {
        Add,
        Remove,
        Update,
        LowStockAlert,
        OutOfStockAlert,
        RestockRequest,
        InventoryAudit
    }

    public enum MessagePriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum MessageStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}
