using WebApp.Models;

namespace WebApp.Tests;

public class PasswordRulesTests
{
    [Fact]
    [Trait("Category", "Positive")]
    public void Accepts_password_at_minimum_length()
    {
        Assert.Null(PasswordRules.Validate(new string('x', PasswordRules.MinLength)));
        Assert.Null(PasswordRules.Validate(UserSeeder.DemoPassword));
    }

    [Fact]
    [Trait("Category", "Negative")]
    public void Rejects_missing_short_and_overlong_passwords()
    {
        Assert.Equal("Password is required.", PasswordRules.Validate(null));
        Assert.Equal("Password is required.", PasswordRules.Validate(""));
        Assert.Equal($"Password must be at least {PasswordRules.MinLength} characters.",
            PasswordRules.Validate(new string('x', PasswordRules.MinLength - 1)));
        Assert.NotNull(PasswordRules.Validate(new string('x', PasswordRules.MaxLength + 1)));
    }
}
