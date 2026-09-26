namespace SkypeConvosReader.Models;

public class Contact {
    public int Id { get; set; } 
    public string? SkypeName { get; set; }
    public string? FullName { get; set; }
    public byte[]? AvatarImage { get; set; }
    
    public DateTime Birthday { get; set; }
    public int? Gender { get; set; }
    public string? Country { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    
    public string? Phone { get; set; }
    public string? Website { get; set; }
    
    public string? About { get; set; }
    public string? Mood { get; set; }
}