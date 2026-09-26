using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class ContactInfoViewModel {
    private Contact _contact { get; set; }
    public AvatarViewModel Avatar { get; }
    
    public string FullName { get; }
    public string SkypeName { get; }
    public string Mood { get; }
    public string About { get; }

    public string Gender { get; }
    public string Birthday { get; }

    public string Country { get; }
    public string Province { get; }
    public string City { get; }

    public string Phone { get; }
    public string Website { get; }

    public ContactInfoViewModel(Contact contact) {
        _contact = contact;
        Avatar = new AvatarViewModel(contact, contact.FullName);
        
        FullName = Display(contact.FullName);
        SkypeName = Display(contact.SkypeName);
        Mood = Display(contact.Mood);
        About = Display(contact.About);

        Gender = DisplayGender(contact.Gender);
        Birthday = Display(contact.Birthday.ToString("dd/MM/yyyy"));

        Country = DisplayCountryFlag(contact.Country);
        Province = Display(contact.Province);
        City = Display(contact.City);

        Phone = Display(contact.Phone);
        Website = Display(contact.Website);
    }
    
    private static string Display(string? value) {
        return string.IsNullOrWhiteSpace(value) ? "Not specified" : value;
    }
    
    private static string DisplayCountryFlag(string? value) {
        return value == null ? "Not specified" : string.Concat(value.ToUpper().Select(x => char.ConvertFromUtf32(x + 0x1F1A5)));
    }
    
    private static string DisplayGender(int? value) {
        if (value != null) {
            if (value == 1) return "Male";
            if (value == 2) return "Female";
        }
        return "Not specified";
    }
}