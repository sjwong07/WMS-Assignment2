using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WMS_Assignment.Controllers;

public class HomeController(DB Db) : Controller
{
    

    public async Task<IActionResult> Index()
    {
       


        return View();
    }

    public IActionResult About()
    {
        return View();
    }


    public IActionResult Contact()
    {
        return View();
    }
}