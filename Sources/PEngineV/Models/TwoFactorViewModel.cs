namespace PEngineV.Models;

public record TwoFactorSetupViewModel(
    string QrCodeUri,
    string Secret,
    string VerificationCode);

public record TwoFactorVerifyViewModel(
    string Code);
