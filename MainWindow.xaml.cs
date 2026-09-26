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
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;
using SkypeConvosReader.ViewModels;
using SkypeConvosReader.Windows;

namespace SkypeConvosReader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private SkypeDatabase? _database;
    private ContactInfoWindow? _contactInfoWindow;
    
    private ScrollViewer? _messageScrollViewer;
    private bool isMessageScrollEnabled = false;
    
    private MessageViewModel? _scrollAnchor;
    private double _scrollAnchorOffset;
    
    public MainWindow() {
        InitializeComponent();
        LoadDatabase();
    }

    private void LoadDatabase() {
        _database = new SkypeDatabase(@"C:\Users\7drim\Desktop\main.db");
        var accounts =  _database.GetAccounts();
        
        if (accounts.Count == 0) {
            MessageBox.Show("No accounts found");
            return;
        }
        
        var currentAccount = accounts.FirstOrDefault();
        var conversations = _database.GetConversations();
        Title = $"{currentAccount.FullName} - {conversations.Count} conversations";
        
        var viewModel = new MainViewModel(_database, currentAccount, conversations);
        viewModel.MessagesLoaded += OnMessagesLoaded;
        DataContext = viewModel;
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
        if (sender is Button button && 
            button.DataContext is MessageViewModel message && 
            message.Contact != null) {
            ShowContactInfo(message.Contact);
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