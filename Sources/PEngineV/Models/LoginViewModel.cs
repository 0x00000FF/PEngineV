namespace PEngineV.Models;

public record LoginViewModel(
    string Username,
    string Password,
    bool RequiresTwoFactor = false,
    string? TwoFactorCode = null,
    string? ErrorMessage = null);
