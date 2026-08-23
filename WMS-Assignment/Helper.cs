using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public class Helper(IWebHostEnvironment en,
                    IHttpContextAccessor ct)
{
    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);

    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0,hash,password)
            == PasswordVerificationResult.Success;
    }

    public void Login(string username, string password, bool rememberMe,string role)
    {
        List<Claim> claims = [
            new(ClaimTypes.Name,username),
            new(ClaimTypes.Role,role),
            ];

        ClaimsIdentity identity = new(claims, "Cookies");
        ClaimsPrincipal principal = new(identity);


        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void LogOut()
    {
        ct.HttpContext!.SignOutAsync();
    }
    

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 0; i <= 10; i++)
        {
            password += s[r.Next(s.Length)];

        }
        return password;
    }

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png|webp)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png|webp)$", RegexOptions.IgnoreCase);

        if(!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG,WEBP and PNG photo is allowed";
        }
        else if(f.Length > 1 * 1024 * 1024)
        {
            return "Photo size cannot more than 1MB";
        }

        return "";
   
    }

    public string SavePhoto(IFormFile f,string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }
    
}
