namespace SkypeConvosReader.Models;

public class Conversation {
    public int Id { get; set; }
    public string? Identity { get; set; }
    public int Type { get; set; }
    public string? DisplayName { get; set; }
    public DateTime LastMessageDate { get; set; }
    public DateTime FirstMessageDate { get; set; }
    
    public int MessageCount { get; set; }
}