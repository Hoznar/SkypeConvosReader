using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SkypeConvosReader.Controls;

public partial class Avatar : UserControl {
public Avatar() {
        InitializeComponent();
        UpdateAvatar();
    }

    public ImageSource? AvatarImage {
        get => (ImageSource?)GetValue(AvatarImageProperty);
        set => SetValue(AvatarImageProperty, value);
    }

    public char AvatarLetter {
        get => (char)GetValue(AvatarLetterProperty);
        set => SetValue(AvatarLetterProperty, value);
    }

    public Brush AvatarColor {
        get => (Brush)GetValue(AvatarColorProperty);
        set => SetValue(AvatarColorProperty, value);
    }
    
    public bool HasAvatar {
        get => (bool)GetValue(HasAvatarProperty);
        set => SetValue(HasAvatarProperty, value);
    }
    
    public static readonly DependencyProperty AvatarImageProperty =
        DependencyProperty.Register(nameof(AvatarImage), typeof(ImageSource), typeof(Avatar), new PropertyMetadata(null, OnAvatarPropertyChanged));
    
    public static readonly DependencyProperty AvatarLetterProperty =
        DependencyProperty.Register(nameof(AvatarLetter), typeof(char), typeof(Avatar), new PropertyMetadata('?', OnAvatarPropertyChanged));
    
    public static readonly DependencyProperty AvatarColorProperty =
        DependencyProperty.Register(nameof(AvatarColor), typeof(Brush), typeof(Avatar), new PropertyMetadata(Brushes.Gray));
    
    public static readonly DependencyProperty HasAvatarProperty =
        DependencyProperty.Register(nameof(HasAvatar), typeof(bool), typeof(Avatar), new PropertyMetadata(false, OnAvatarPropertyChanged));
    
    private static void OnAvatarPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is Avatar avatar) avatar.UpdateAvatar();
    }
    
    private void UpdateAvatar() {
        if (AvatarImageControl == null || AvatarLetterControl == null) return;

        if (HasAvatar && AvatarImage != null) {
            AvatarImageControl.Visibility = Visibility.Visible;
            AvatarLetterControl.Visibility = Visibility.Collapsed;
        }
        else {
            AvatarImageControl.Visibility = Visibility.Collapsed;
            AvatarLetterControl.Visibility = Visibility.Visible;
        }
    }
}