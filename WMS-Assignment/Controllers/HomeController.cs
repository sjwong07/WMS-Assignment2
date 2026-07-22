using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WMS_Assignment.Controllers;

public class HomeController(DB Db) : Controller
{


    public IActionResult Index()
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

    public IActionResult Product()
    {
        return View();
    }

    public IActionResult Receipt()
    {
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    public IActionResult Cart()
    {
        return View();
    }
}