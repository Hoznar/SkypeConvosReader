using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class MainViewModel : INotifyPropertyChanged {

    private SkypeDatabase _database;
    public event EventHandler? MessagesLoaded;
    
    public AccountViewModel? CurrentAccount { get; set; }
    public ObservableCollection<ConversationViewModel> Conversations { get; set; }
    public ICollectionView ConversationsView { get; }
    
    public ObservableCollection<MessageViewModel> Messages { get; set; }
    public ICollectionView MessagesView { get; }

    public Dictionary<string, Contact> Contacts { get; set; }

    private string _conversationSearch = "";
    public string ConversationSearch {
        get => _conversationSearch;
        set {
            if (_conversationSearch == value) return;
            _conversationSearch = value;
            OnPropertyChanged();
            ConversationsView.Refresh();
        }
    }
    
    private ConversationSort _conversationSort = ConversationSort.LastMessage;
    public ConversationSort ConversationSort {
        get => _conversationSort;
        set {
            if (_conversationSort == value) return;
            _conversationSort = value;
            OnPropertyChanged();
            ApplySorting();
        }
    }

    private bool _sortAscending = false;
    public bool SortAscending {
        get => _sortAscending;
        set {
            if (_sortAscending == value) return;
            _sortAscending = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SortDirectionIcon));
            ApplySorting();
        }
    }
    
    public string SortDirectionIcon => SortAscending ? "↑" : "↓";

    private const int PageSize = 100;
    private int _currentOffset;
    private bool _isLoading;
    private bool _hasMoreMessages;

    public int MessageCount => Messages.Count;

    private ConversationViewModel? _selectedConversation;
    public ConversationViewModel? SelectedConversation {
        get => _selectedConversation;
        set {
            if (_selectedConversation == value) {
                return;
            }
            _selectedConversation = value;
            LoadMessages(_selectedConversation);
            
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsConversationSelected));
            OnPropertyChanged(nameof(IsConversationNotSelected));
            OnPropertyChanged(nameof(MessageCount));
        }
    }
    
    public bool IsConversationSelected => SelectedConversation != null;
    public bool IsConversationNotSelected => SelectedConversation == null;
    
    private string _messageSearch = "";
    public string MessageSearch {
        get => _messageSearch;
        set {
            if (_messageSearch == value) return;
            _messageSearch = value;
            OnPropertyChanged();

            // later: perform search
        }
    }

    private DateTime? _messageDateFrom;
    public DateTime? MessageDateFrom {
        get => _messageDateFrom;
        set {
            if (_messageDateFrom == value) return;
            _messageDateFrom = value;
            OnPropertyChanged();
        }
    }

    private DateTime? _messageDateTo;
    public DateTime? MessageDateTo {
        get => _messageDateTo;
        set {
            if (_messageDateTo == value) return;
            _messageDateTo = value;
            OnPropertyChanged();
        }
    }
    
    
    public MainViewModel(SkypeDatabase database, Account account, List<Conversation> conversations) {
        _database = database;
        Conversations = new ObservableCollection<ConversationViewModel>();
        Messages = new ObservableCollection<MessageViewModel>();
        
        Contacts = _database.GetContacts()
            .Where(c => !string.IsNullOrEmpty(c.SkypeName))
            .ToDictionary(c => c.SkypeName!);

        if (account.SkypeName != null && Contacts.TryGetValue(account.SkypeName, out var contact)) {
            account.Contact = contact;
        }
        else if (account.SkypeName != null && account.Contact != null) {
            Contacts[account.SkypeName] = account.Contact;
        }
        CurrentAccount = new AccountViewModel(account);
        
        foreach (var conversation in conversations) {
            Conversations.Add(new ConversationViewModel(_database, conversation, Contacts));
        }
        
        MessagesView = CollectionViewSource.GetDefaultView(Messages);
        MessagesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MessageViewModel.MessageDate)));
        
        ConversationsView = CollectionViewSource.GetDefaultView(Conversations);
        ConversationsView.Filter = FilterConversation;
        ApplySorting();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool FilterConversation(Object obj) {
        if (obj is not ConversationViewModel conversation) return false;
        if (string.IsNullOrWhiteSpace(ConversationSearch)) return true;

        string search = ConversationSearch.Trim();
        
        if (conversation.DisplayName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true) return true;
        
        return conversation.ParticipantNames.Any(name => name.Contains(search, StringComparison.CurrentCultureIgnoreCase));
    }

    private void ApplySorting() {
        using (ConversationsView.DeferRefresh()) {
            ConversationsView.SortDescriptions.Clear();
            ConversationsView.SortDescriptions.Add(
                new SortDescription(GetSortProperty(), SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
        }
    }

    private string GetSortProperty() {
        switch (ConversationSort) {
            case ConversationSort.LastMessage:
                return nameof(ConversationViewModel.LastMessageDate);
            case ConversationSort.Name:
                return nameof(ConversationViewModel.DisplayName);
            case ConversationSort.MessageCount:
                return nameof(ConversationViewModel.MessageCount);
            case ConversationSort.FirstMessage:
                return nameof(ConversationViewModel.FirstMessageDate);
            default:
                return nameof(ConversationViewModel.LastMessageDate);
        }
    }

    private void LoadMessages(ConversationViewModel? conversation) {
        Messages.Clear();
        _currentOffset = 0;
        _hasMoreMessages = true;
        
        if (conversation != null) {
            var messages = _database.GetMessages(conversation.Conversation.Id, PageSize, _currentOffset);
            if (messages.Count < PageSize) _hasMoreMessages = false;
            messages.Reverse();

            foreach (var message in messages) {
                var contact = GetContact(message.Author);
                Messages.Add(new MessageViewModel(message, contact, Contacts, CurrentAccount.SkypeName == message.Author));
            }
            MessagesLoaded?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool LoadMoreMessages() {
        if (_selectedConversation == null || _isLoading || !_hasMoreMessages) return false;
        _isLoading = true;

        try {
            _currentOffset += PageSize;

            var messages = _database.GetMessages(_selectedConversation.Conversation.Id, PageSize, _currentOffset);
            if (messages.Count < PageSize) _hasMoreMessages = false;

            foreach (var message in messages) {
                var contact = GetContact(message.Author);
                Messages.Insert(0, new MessageViewModel(message, contact, Contacts, CurrentAccount.SkypeName == message.Author));
            }
            return messages.Count > 0;
        }
        finally {
            _isLoading = false;
        } 
    }

    private Contact? GetContact(string? skypeName) {
        if (skypeName == null) return null;
        Contacts.TryGetValue(skypeName, out var contact);
        return contact;
    }
}