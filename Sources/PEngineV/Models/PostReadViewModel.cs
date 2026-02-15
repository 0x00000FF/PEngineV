namespace PEngineV.Models;

public record PostReadViewModel(
    PostViewModel Post,
    IEnumerable<CommentViewModel> Comments,
    bool IsLoggedIn,
    bool IsOwner);
