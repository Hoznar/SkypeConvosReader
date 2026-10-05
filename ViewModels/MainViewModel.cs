using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class MainViewModel : INotifyPropertyChanged {

    private SkypeDatabase? _database;
    private bool IsDatabaseLoaded => _database != null;
    
    public event EventHandler? MessagesLoaded;
    
    private CancellationTokenSource? _searchDelayCts;
    
    public AccountViewModel? CurrentAccount { get; set; }
    public ObservableCollection<ConversationViewModel> Conversations { get; } = new();
    public ICollectionView ConversationsView { get; }

    public ObservableCollection<MessageViewModel> Messages { get; } = new();
    public ICollectionView MessagesView { get; }

    public Dictionary<string, Contact> Contacts { get; private set; } = new();

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

    private bool _orderMessagesAscending = false;
    public bool OrderMessagesAscending {
        get => _orderMessagesAscending;
        set {
            if (_orderMessagesAscending == value) return;
            _orderMessagesAscending = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OrderMessagesText));
            LoadMessages(_selectedConversation);
        }
    }

    public string OrderMessagesText => OrderMessagesAscending ? "Newest" : "Oldest";

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
            InitializeDefaultFilters();
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
            DelayMessageSearch();
        }
    }

    private DateTime? _messageDateFrom;
    public DateTime? MessageDateFrom {
        get => _messageDateFrom;
        set {
            if (_messageDateFrom == value) return;
            _messageDateFrom = value;
            OnPropertyChanged();
            LoadMessages(_selectedConversation);
        }
    }

    private DateTime? _messageDateTo;
    public DateTime? MessageDateTo {
        get => _messageDateTo;
        set {
            if (_messageDateTo == value) return;
            _messageDateTo = value;
            OnPropertyChanged();
            LoadMessages(_selectedConversation);
        }
    }
    
    public MainViewModel() {
        Conversations = new ObservableCollection<ConversationViewModel>();
        Messages = new ObservableCollection<MessageViewModel>();
        
        MessagesView = CollectionViewSource.GetDefaultView(Messages);
        MessagesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(MessageViewModel.MessageDate)));
        
        ConversationsView = CollectionViewSource.GetDefaultView(Conversations);
        ConversationsView.Filter = FilterConversation;
        ApplySorting();
    }
    
    /* Sets up account, fetches all conversations, sets up contacts, constructs viewModels. */
    public bool LoadDatabase(SkypeDatabase database) {
        _database = database;
        
        var accounts = _database.GetAccounts();
        if (accounts.Count == 0) {
            _database = null;
            return false;
        }

        var account  = accounts.First();
        var conversations = _database.GetConversations();

        Contacts = _database.GetContacts()
            .Where(c => !string.IsNullOrWhiteSpace(c.SkypeName))
            .ToDictionary(c => c.SkypeName!);
        
        if (account.SkypeName != null && Contacts.TryGetValue(account.SkypeName, out var contact)) {
            account.Contact = contact;
        }
        else if (account.SkypeName != null && account.Contact != null) {
            Contacts[account.SkypeName] = account.Contact;
        }

        CurrentAccount = new AccountViewModel(account);
        
        Conversations.Clear();
        Messages.Clear();
        foreach (var conversation in conversations) {
            Conversations.Add(new ConversationViewModel(_database, conversation, Contacts));
        }

        OnPropertyChanged(nameof(CurrentAccount));
        OnPropertyChanged(nameof(Contacts));
        OnPropertyChanged(nameof(IsDatabaseLoaded));

        ConversationsView.Refresh();
        return true;
    }
    
    /* Unused method from when working with static database. */
    /* Constructs the MainViewModel. Fetches all contacts, sets up account, creates ConversationViewModel, and sets up Filtering for conversation list. */
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
    
    /* Needed for up-to-date display of properties. */
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

    /* Applies ascending or descending order of conversations. */
    private void ApplySorting() {
        using (ConversationsView.DeferRefresh()) {
            ConversationsView.SortDescriptions.Clear();
            ConversationsView.SortDescriptions.Add(
                new SortDescription(GetSortProperty(), SortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
        }
    }

    /* Sets by what property the conversation list gets ordered by. */
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

    /* Gets messages with current filters from the skype database, constructs a ViewModel with needed data for display */
    private void LoadMessages(ConversationViewModel? conversation) {
        if (_database == null) return;
        Messages.Clear();
        _currentOffset = 0;
        _hasMoreMessages = true;
        
        if (conversation != null) {
            var messages = _database.GetMessages(conversation.Conversation.Id, PageSize, _currentOffset, !_orderMessagesAscending, MessageDateFrom, MessageDateTo, MessageSearch);
            if (messages.Count < PageSize) _hasMoreMessages = false;
            messages.Reverse();

            foreach (var message in messages) {
                var contact = GetContact(message.Author);
                Messages.Add(new MessageViewModel(message, contact, Contacts, CurrentAccount.SkypeName == message.Author));
            }
            MessagesLoaded?.Invoke(this, EventArgs.Empty);
        }
    }

    /* Implements paging. Loads more messages (with current filters) when user scrolls to the end of ListView */
    public bool LoadMoreMessages() {
        if (_database == null || _selectedConversation == null || _isLoading || !_hasMoreMessages) return false;
        _isLoading = true;

        try {
            _currentOffset += PageSize;

            var messages = _database.GetMessages(_selectedConversation.Conversation.Id, PageSize, _currentOffset, !_orderMessagesAscending, MessageDateFrom, MessageDateTo, MessageSearch);
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

    /* Reset the default filters, needed so the messages won't load multiple times when setting the default filter values */
    private void InitializeDefaultFilters() {
        _messageDateFrom = _selectedConversation?.FirstMessageDate;
        _messageDateTo = _selectedConversation?.LastMessageDate;
        _messageSearch = "";
        OnPropertyChanged(nameof(MessageDateFrom));
        OnPropertyChanged(nameof(MessageDateTo));
        OnPropertyChanged(nameof(MessageSearch));
    }

    /* Small delay before applying message filter, to not reload on every newly typed character */
    private async void DelayMessageSearch() {
        _searchDelayCts?.Cancel();
        _searchDelayCts?.Dispose();

        _searchDelayCts = new CancellationTokenSource();
        var token = _searchDelayCts.Token;

        try {
            await Task.Delay(500, token);
            if (token.IsCancellationRequested) return;
            LoadMessages(SelectedConversation);
        }
        catch (TaskCanceledException) {
        }
    }
}