namespace PEngineV.Models;

public record GuestbookPageViewModel(
    IEnumerable<GuestbookEntryViewModel> Entries,
    bool IsLoggedIn,
    bool IsOwner);
