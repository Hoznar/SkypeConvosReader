namespace SkypeConvosReader.Models;

public class Account {
    public int Id { get; set; }
    public string? SkypeName { get; set; }
    public string? FullName { get; set; }
    public Contact? Contact { get; set; }
}