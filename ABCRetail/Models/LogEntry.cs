namespace ABCRetail.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public Dictionary<string, string>? Properties { get; set; }
}


