using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WMS_Assignment.Controllers;

public class MemberController(DB db):Controller
{
    public IActionResult Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = db.Users.FirstOrDefault(u => u.Id == userId);

       
       

        if (user == null)
            return RedirectToAction("Login", "Home");

        var vm = new ProfileVM
        {
            UserId = userId,
            Username = user.Username,
            Email = user.Email,
            
        };

        return View(vm);
    }
}
