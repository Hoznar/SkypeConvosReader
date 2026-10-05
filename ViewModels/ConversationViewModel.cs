using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class ConversationViewModel {
    private Dictionary<string, Contact> _contacts;
    public Conversation Conversation { get; set; }
    public List<Contact> Participants { get; set; }
    public AvatarViewModel Avatar { get; set; }
    
    public string? DisplayName => Conversation.DisplayName;
    public string Description { get; set; }

    public string DisplayNameShort => string.IsNullOrWhiteSpace(Conversation.DisplayName)
        ? "Unnamed conversation"
        : Conversation.DisplayName.Length > 40
            ? Conversation.DisplayName[..37] + "..."
            : Conversation.DisplayName;
    
    public int MessageCount => Conversation.MessageCount;

    public DateTime? LastMessageDate => Conversation.LastMessageDate;
    public DateTime? FirstMessageDate => Conversation.FirstMessageDate;
    
    public IEnumerable<string?> ParticipantNames => Participants
        .Select(p => p.FullName ?? p.SkypeName)
        .Where(name => !string.IsNullOrWhiteSpace(name));
    
    public string MessageCountAsString {
        get {
            var count = Conversation.MessageCount;
            return count == 1 ? $"{count} message" : $"{count} messages";
        }
    }
    

    public ConversationViewModel(SkypeDatabase _database, Conversation conversation, Dictionary<string, Contact> contacts) {
        Conversation = conversation;
        _contacts = contacts;

        Contact? contact = null;
        
        Participants = _database.GetParticipantsNames(conversation.Id)
            .Where(identity => _contacts.ContainsKey(identity))
            .Select(identity => _contacts[identity])
            .ToList();
        
        if (conversation.Type == 1) {
            contact = Participants.FirstOrDefault(x => x.SkypeName == conversation.Identity);
            if (contact != null && !string.IsNullOrWhiteSpace(contact.Mood)) {
                Description = contact.Mood;
            }
            else {
                Description = "Offline";
            }
        }
        else {
            Description = $"{Participants.Count} people";
        }

        Avatar = new AvatarViewModel(contact, conversation.DisplayName);
    }
}