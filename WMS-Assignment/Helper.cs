using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public class Helper(IWebHostEnvironment en, IHttpContextAccessor ct)
{
    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password) == PasswordVerificationResult.Success;
    }

    public void Login(string username, string password, bool rememberMe)
    {
        List<Claim> claims = [
            new(ClaimTypes.Name, username)
        ];

        ClaimsIdentity identity = new(claims, "CookieAuth");
        ClaimsPrincipal principal = new(identity);

        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        ct.HttpContext!.SignInAsync("CookieAuth", principal, properties);
    }

    public void LogOut()
    {
        ct.HttpContext!.SignOutAsync("CookieAuth");
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";
        Random r = new();

        for (int i = 0; i < 10; i++)
        {
            password += s[r.Next(s.Length)];
        }
        return password;
    }

    public string ValidatePhoto(IFormFile? f)
    {
        if (f == null || f.Length == 0)
            return ""; // No photo uploaded, nothing to validate

        var reType = new Regex(@"^image\/(jpeg|png|webp)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png|webp)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG, WEBP and PNG photo is allowed";
        }
        else if (f.Length > 1 * 1024 * 1024)
        {
            return "Photo size cannot be more than 1MB";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var folderPath = Path.Combine(en.WebRootPath, folder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(folderPath, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.SaveAsJpeg(path);

        return file;
    }

    public void DeletePhoto(string fileName, string folder)
    {
        if (string.IsNullOrEmpty(fileName)) return;

        var path = Path.Combine(en.WebRootPath, folder, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}