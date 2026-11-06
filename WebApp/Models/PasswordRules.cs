namespace WebApp.Models;

// One place for the password rule, used when creating users and changing passwords
public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    // Returns an error message, or null if the password is acceptable
    public static string? Validate(string? password) =>
        string.IsNullOrEmpty(password) ? "Password is required."
        : password.Length < MinLength ? $"Password must be at least {MinLength} characters."
        : password.Length > MaxLength ? $"Password can be at most {MaxLength} characters."
        : null;
}
