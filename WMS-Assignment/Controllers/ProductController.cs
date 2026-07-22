using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS_Assignment.Models;

namespace WMS_Assignment.Controllers;


public class ProductController(DB db) : Controller
{
    public IActionResult Menu()
    {
        return View();

    }

   
    public IActionResult AdminMenu()
    {
        return View();
    }

    

    public IActionResult Create()
    {
        return View();
    }

    public IActionResult Detail()
    {
        return View();
    }

    public IActionResult Update()
    {
        return View();
    }

    public IActionResult Delete()
    {
        return View();
    }



}