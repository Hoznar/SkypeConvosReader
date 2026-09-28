using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class AccountViewModel {
    private Account _account;
    
    public string? SkypeName => _account.SkypeName;
    public string? FullName => _account.FullName;
    public Contact? Contact => _account.Contact;
    
    public AvatarViewModel Avatar { get; set; }
    
    public AccountViewModel(Account account) {
        _account = account;
        Avatar = new AvatarViewModel(account.Contact, account.FullName);
    }
}