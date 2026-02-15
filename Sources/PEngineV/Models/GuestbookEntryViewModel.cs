namespace PEngineV.Models;

public record GuestbookEntryViewModel(
    int Id,
    string Name,
    string Message,
    DateTime CreatedAt);
