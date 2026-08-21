using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WMS_Assignment.Controllers;

public class MemberController(DB db):Controller
{
    public IActionResult Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = db.Users.FirstOrDefault(u => u.Id == userId);
        return View(user);
    }
}
