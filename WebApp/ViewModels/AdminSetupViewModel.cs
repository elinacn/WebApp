using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public class AdminSetupViewModel
{
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
