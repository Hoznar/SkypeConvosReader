using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkypeConvosReader.Data;
using SkypeConvosReader.Models;

namespace SkypeConvosReader.ViewModels;

public class AvatarViewModel {
    public ImageSource? Image { get; set; }
    public char Letter { get; set; }
    public Brush Color { get; set; }
    
    public bool HasImage => Image != null;

    public AvatarViewModel(Contact? contact, string? fallbackName) {
        var name = contact?.FullName ?? fallbackName;
        Letter = string.IsNullOrWhiteSpace(name) ? '?' : name.Trim()[0];
        Color = new SolidColorBrush(StringToColorConverter.GetAvatarColor(name));

        if (contact?.AvatarImage is not null) {
            Image = ConvertToImage(contact.AvatarImage);
        }
    }
    
    private ImageSource? ConvertToImage(byte[] imageData) {
        try {
            if (imageData.Length <= 1) return null;
            using var stream = new MemoryStream(imageData, 1, imageData.Length - 1);

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch (NotSupportedException) {
            return null;
        }
    }
}