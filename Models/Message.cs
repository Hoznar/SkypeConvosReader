namespace SkypeConvosReader.Models;

public class Message {
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string? Author { get; set; }
    public string? FromDisplayName { get; set; }
    public DateTime Timestamp  { get; set; }
    public int Type { get; set; }
    public string? BodyXml { get; set; }
    public string? Identities { get; set; }
}

public enum MessageType {
    Text, Image, Video, File, Audio, Contact, Unknown
}