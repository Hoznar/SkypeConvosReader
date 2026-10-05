using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class MessageViewModel {
    
    // Dependencies
    private readonly Message _message;
    public Contact? Contact { get; }
    
    // Triggers
    public bool IsSystemMessage { get; set; }
    public bool IsMine { get; set; }
    public bool IsAttachment { get; set; }

    // View
    public string? Author => _message.Author;
    public AvatarViewModel? Avatar { get; set; }
    public string? DisplayName { get; }
    public DateTime Date => _message.Timestamp;
    public DateTime MessageDate => Date.Date;
    public string? Text { get; set; }
    public TimeSpan? CallDuration { get; }

    public string? FileIconPath { get; private set; }


    public MessageViewModel(Message message, Contact? contact, Dictionary<string, Contact> contacts, bool authorIsUser) {
        _message = message;
        IsMine = authorIsUser;
        Contact = contact;
        DisplayName = contact == null ? message.FromDisplayName : contact.FullName;
        Avatar = new AvatarViewModel(contact, message.FromDisplayName);
        ParseBodyXML(message,contacts);
    }

    // Converts body_xml property of messages into a format ready for display.
    private void ParseBodyXML(Message message, Dictionary<string, Contact> contacts) {
        if (message.Type is 30 or 39) {
            ParseCallMessage(message);
            IsSystemMessage = true;
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
        else if (message.Type == 63) {
            Text = ParseContactMessage(message, contacts);
        }
        else if (message.Type == 2) {
            Text = $"{DisplayName} changed the conversation name to {ParseClassicMessage(message.BodyXml)}";
            IsSystemMessage = true;
        }
        else if (message.Type == 13) {
            Text = $"{DisplayName} left the chat.";
            IsSystemMessage = true;
        }
        else if (message.Type == 10) {
            Text = ParseAddedMembersMessage(message, contacts);
            IsSystemMessage = true;
        }
        else {
            Text = message.BodyXml;
        }
    }

    // Removes xml tags from text
    private string ParseClassicMessage(string body) {
        try {
            var root = XElement.Parse($"<root>{body}</root>");
            return root.Value;
        }
        catch (XmlException) {
            return body;
        }
    }

    private string ParseContactMessage(Message message, Dictionary<string, Contact> contacts) {
        IsAttachment = true;
        FileIconPath =  "/Assets/contact-book.png";
        
        try {
            var root = XElement.Parse($"<root>{message.BodyXml}</root>");
            var contact = root.Descendants("c").FirstOrDefault();

            if (contact == null) return $"Sent a contact.";

            var skypeName = (string?)contact.Attribute("s");
            var displayName = (string?)contact.Attribute("f");

            return string.IsNullOrWhiteSpace(displayName)
                ? $"Shared a contact."
                : $"Shared a contact \"{displayName}\".";
        }
        catch (XmlException) {
            return $"Shared a contact.";
        }
    }
    
    // Sets an icon for the chat bubble attachment based on xml tag
    private string? ParseAttachmentMessage(Message message, string attachmentType) {
        IsAttachment = true;
        FileIconPath = $"/Assets/file-{attachmentType}.png";
        
        try {
            var root = XElement.Parse($"<root>{message.BodyXml}</root>");
            var attachmentName = root
                .Descendants("OriginalName")
                .Select(x => (string?)x.Attribute("v"))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            return string.IsNullOrWhiteSpace(attachmentName)
                ? $"Attached a {attachmentType}."
                : $"Attached a {attachmentType} \"{attachmentName}\"";
        }
        catch (XmlException) {
            return $"Attached a {attachmentType}.";
        }
    }

    // Displays who started or ended a call. Displays length of the call if duration property is present.
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
                        Text = $"{DisplayName} missed a call.";
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