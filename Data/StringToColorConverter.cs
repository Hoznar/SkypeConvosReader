using System.Windows.Media;

namespace SkypeConvosReader.Data;

public static class StringToColorConverter {
    private static readonly Color[] AvatarColors = {
        Colors.CornflowerBlue,
        Colors.MediumPurple,
        Colors.SteelBlue,
        Colors.Teal,
        Colors.OliveDrab,
        Colors.Coral,
        Colors.IndianRed,
        Colors.SlateBlue
    };

    public static Color GetAvatarColor(string? name) {
        if (string.IsNullOrEmpty(name)) return AvatarColors[0];
        int hash = StringComparer.OrdinalIgnoreCase.GetHashCode(name);
        int index = (hash & 0x7fffffff) % AvatarColors.Length;
        return AvatarColors[index];
    }
}