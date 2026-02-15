namespace PEngineV.Models;

public record RegisterViewModel(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword);
