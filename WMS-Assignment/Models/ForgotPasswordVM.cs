using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models;

public class ForgotPasswordVM
{
    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;
}