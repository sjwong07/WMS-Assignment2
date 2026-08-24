using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WMS_Assignment.Models;

public class UpdateProfileVM
{
    [Required(ErrorMessage = "Username is required")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public string? CurrentPhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}