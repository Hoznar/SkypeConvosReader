using System.Xml;
using System.Xml.Linq;
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class MessageViewModel {
    private readonly Message _message;
    public Contact? Contact { get; }
    public AvatarViewModel? Avatar { get; set; }
    public bool IsMine { get; set; }
    
    public string? Author => _message.Author;
    public string? DisplayName { get; }
    public DateTime Date => _message.Timestamp;

    public TimeSpan? CallDuration { get; }
    public string? Text { get; set; }
    

    public MessageViewModel(Message message, Contact? contact, Dictionary<string, Contact> contacts, bool authorIsUser) {
        _message = message;
        IsMine = authorIsUser;
        Contact = contact;
        DisplayName = contact == null ? message.FromDisplayName : contact.FullName;
        Avatar = new AvatarViewModel(contact, message.FromDisplayName);
        ParseBodyXML(message,contacts);
    }

    private void ParseBodyXML(Message message, Dictionary<string, Contact> contacts) {
        if (message.Type is 30 or 39) {
            ParseCallMessage(message);
        }
        else if (message.Type == 201) {
            Text = ParseAttachmentMessage(message, "image");
        }
        else if (message.Type is 253 or 255) {
            Text = ParseAttachmentMessage(message, "video");
        }
        else if (message.Type == 254) {
            Text = ParseAttachmentMessage(message, "file");
        }
        else if (message.Type == 61) {
            Text = ParseClassicMessage(message.BodyXml);
        }
        else if (message.Type == 2) {
            Text = $"{DisplayName} changed the conversation name to {ParseClassicMessage(message.BodyXml)}";
        }
        else if (message.Type == 13) {
            Text = $"{DisplayName} left the chat.";
        }
        else if (message.Type == 10) {
            Text = ParseAddedMembersMessage(message, contacts);
        }
        else {
            Text = message.BodyXml;
        }
    }

    private string ParseClassicMessage(string body) {
        try {
            var root = XElement.Parse($"<root>{body}</root>");
            return root.Value;
        }
        catch (XmlException) {
            return body;
        }
    }
    
    private string? ParseAttachmentMessage(Message message, string attachmentType) {
        try {
            var root = XElement.Parse($"<root>{message.BodyXml}</root>");
            var attachmentName = root
                .Descendants("OriginalName")
                .Select(x => (string?)x.Attribute("v"))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            
            return string.IsNullOrWhiteSpace(attachmentName)
                ? $"{message.FromDisplayName} sent a {attachmentType}."
                : $"{message.FromDisplayName} sent a {attachmentType} \"{attachmentName}\"";
        }
        catch (XmlException) {
            return $"{message.FromDisplayName} sent a {attachmentType}.";
        }
    }

    private void ParseCallMessage(Message message) {
        try {
            var root = XElement.Parse($"<root>{message.BodyXml}</root>");
            var partList = root.Element("partlist");
            if (partList == null) return;
            
            var type = (string?)partList.Attribute("type");
            var part = partList.Element("part");
            if (part == null) return;
            
            var durationElement = part.Element("duration");
            
            switch (type) {
                case "started":
                    Text = $"{DisplayName} started a call.";
                    break;

                case "ended":
                    if (durationElement == null) {
                        Text = $"{DisplayName} missed call.";
                    }
                    else if (int.TryParse(durationElement.Value, out int duration)) {
                        Text = $"{DisplayName} ended a call, that lasted for {FormatDuration(duration)}.";
                    }
                    break;
            }
        }
        catch (XmlException) {
        }
    }
    
    private string FormatDuration(int seconds) {
        var time = TimeSpan.FromSeconds(seconds);
        if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m {time.Seconds}s";
        if (time.TotalMinutes >= 1) return $"{time.Minutes}m {time.Seconds}s";
        return $"{time.Seconds}s";
    }
    
    private string ParseAddedMembersMessage(Message message, Dictionary<string, Contact> contacts) {
        var identities = message.Identities?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (identities == null || identities.Length == 0) return $"{DisplayName} added a new chat member.";

        var names = identities
            .Select(identity =>
                contacts.TryGetValue(identity, out var contact)
                    ? contact.FullName
                    : identity)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        if (names.Count == 0) return $"{DisplayName} added a new chat member.";
        return $"{DisplayName} added {string.Join(", ", names.Select(name => $"\"{name}\""))} to the conversation.";
    }
}