using System.Windows;
using SkypeConvosReader.Models;
using SkypeConvosReader.ViewModels;

namespace SkypeConvosReader.Windows;

public partial class ContactInfoWindow : Window {
    
    public ContactInfoWindow(Contact contact) {
        InitializeComponent();
        DataContext = new ContactInfoViewModel(contact);
    }
}