using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;
using SkypeConvosReader.ViewModels;
using SkypeConvosReader.Windows;

namespace SkypeConvosReader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private MainViewModel _viewModel;
    private SkypeDatabase? _database;
    private ContactInfoWindow? _contactInfoWindow;
    
    private ScrollViewer? _messageScrollViewer;
    private bool isMessageScrollEnabled = false;
    
    private MessageViewModel? _scrollAnchor;
    private double _scrollAnchorOffset;
    
    public MainWindow() {
        InitializeComponent();
        
        _viewModel = new MainViewModel();
        _viewModel.MessagesLoaded += OnMessagesLoaded;
        DataContext = _viewModel;
    }
    
    private void LoadDatabase_Click(object sender, RoutedEventArgs e) {
        var dialog = new OpenFileDialog {
            Title = "Select Skype database",
            Filter = "Skype database (main.db)|main.db|SQLite database (*.db)|*.db",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        try {
            var database = new SkypeDatabase(dialog.FileName);

            if (!_viewModel.LoadDatabase(database)) {
                MessageBox.Show("No account was found in this database.", "Invalid database", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Title = $"{_viewModel.CurrentAccount?.FullName} - {_viewModel.Conversations.Count} conversations";
        }
        catch (Exception ex) {
            MessageBox.Show($"Could not load the database.\n\n{ex.Message}", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowContactInfo(Contact contact) {
        if (_contactInfoWindow == null) {
            _contactInfoWindow = new ContactInfoWindow(contact);
            _contactInfoWindow.Closed += (s, e) => {
                _contactInfoWindow = null;
            };
            _contactInfoWindow.Show();
        }
        else {
            _contactInfoWindow.DataContext = new ContactInfoViewModel(contact);
            _contactInfoWindow.Activate();
        }
    }

    private void Contact_Click(object sender, RoutedEventArgs e) {
        if (sender is not Button button) return;

        switch (button.Tag) {
            case MessageViewModel message:
                if (message.Contact != null) ShowContactInfo(message.Contact);
                break;

            case ConversationViewModel conversation:
                if (conversation.Participants.FirstOrDefault() != null) ShowContactInfo(conversation.Participants.First());
                break;

            case AccountViewModel account:
                if (account.Contact != null) ShowContactInfo(account.Contact);
                break;
        }
    }

    private void OnMessagesLoaded(object? sender, EventArgs e) {
        Dispatcher.BeginInvoke(new Action(ScrollMessagesToBottom), DispatcherPriority.ContextIdle);
    }
    
    private void SortDirectionButton_Click(object? sender, RoutedEventArgs e) {
        if (DataContext is MainViewModel mainViewModel) {
            mainViewModel.SortAscending = !mainViewModel.SortAscending;
        }
    }
    
    private void OrderMessagesByButton_Click(object? sender, RoutedEventArgs e) {
        if (DataContext is MainViewModel mainViewModel) {
            mainViewModel.OrderMessagesAscending = !mainViewModel.OrderMessagesAscending;
        }
    }
    
    private void MessagesListBox_Loaded(object sender, RoutedEventArgs e) {
        _messageScrollViewer = FindVisualChild<ScrollViewer>(MessagesListBox);
        
        if (_messageScrollViewer != null) {
            _messageScrollViewer.ScrollChanged += MessageScrollViewer_ScrollChanged;
        }
        
        MessagesListBox.Loaded -= MessagesListBox_Loaded;
    }
    
    private void ScrollMessagesToBottom() {
        if (_messageScrollViewer == null) return;
        _messageScrollViewer.ScrollToEnd();
        isMessageScrollEnabled = true;
    }

    private void MessageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
        if (!isMessageScrollEnabled || _messageScrollViewer == null) return;
        if (_messageScrollViewer.VerticalOffset <= 0) {
            
            isMessageScrollEnabled = false;
            SaveScrollPosition();
            
            if (DataContext is MainViewModel viewModel) {
                bool loaded = viewModel.LoadMoreMessages();
                if (loaded) {
                    Dispatcher.BeginInvoke(new Action(RestoreScrollPosition), DispatcherPriority.ContextIdle);
                }
                else {
                    isMessageScrollEnabled = true;
                }
            }
        }
    }

    private void SaveScrollPosition() {
        var item = GetFirstVisibleMessage();
        if (item == null) return;

        _scrollAnchor = item.DataContext as MessageViewModel;
        Point position = item.TranslatePoint(new Point(0, 0), _messageScrollViewer!);
        _scrollAnchorOffset = position.Y;
    }
    
    private void RestoreScrollPosition() {
        if (_scrollAnchor == null || _messageScrollViewer == null) return;

        var container = MessagesListBox.ItemContainerGenerator.ContainerFromItem(_scrollAnchor) as ListBoxItem;
        if (container == null) return;

        Point position = container.TranslatePoint(new Point(0, 0), _messageScrollViewer);

        double difference = position.Y - _scrollAnchorOffset;

        _messageScrollViewer.ScrollToVerticalOffset(_messageScrollViewer.VerticalOffset + difference);

        _scrollAnchor = null;
        isMessageScrollEnabled = true;
    }

    private ListBoxItem? GetFirstVisibleMessage() {
        if (_messageScrollViewer == null) return null;

        for (int i = 0; i < MessagesListBox.Items.Count; i++) {
            var container = MessagesListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
            if (container == null) continue;
           
            Point position = container.TranslatePoint(new Point(0, 0), _messageScrollViewer);
            if (position.Y >= 0) {
                return container;
            }
        }
        return null;
    }
    
    private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++) {
            var child = VisualTreeHelper.GetChild(obj, i);

            if (child is T result) return result;

            var descendant = FindVisualChild<T>(child);

            if (descendant != null) return descendant;
        }
        return null;
    }
}