namespace PEngineV.Models;

public record PostWriteViewModel(
    int? Id,
    string Title,
    string Content,
    string? Password);
