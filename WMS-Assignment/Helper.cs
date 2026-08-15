using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

public class Helper(IWebHostEnvironment en,
                    IHttpContextAccessor ct)
{
    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);

    }

    public bool VerifyPassword(string hash,string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
            == PasswordVerificationResult.Success;
    }

   /* public void SignOut()
    {
        ct.HttpContext!.SignOutAsync();
    }
   */



}