global using WMS_Assignment.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Ensure authentication cookies remain valid across application restarts and rebuilds
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "Keys");
if (!Directory.Exists(keysFolder))
{
    Directory.CreateDirectory(keysFolder);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("WMS_Assignment_App");

// 1. Session and HttpContext
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// 2. Localization Services
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

// 3. Controllers, Views, and Localization Support
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// 4. Authorization and Dependency Injection
builder.Services.AddAuthorization();
builder.Services.AddScoped<Helper>();

// 5. Database Connection
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\Restaurant.mdf;
");

// 6. Configure Authentication with Cookie & Persistent Login Support
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "CookieAuth";
    options.DefaultSignInScheme = "CookieAuth";
    options.DefaultChallengeScheme = "CookieAuth";
})
.AddCookie("CookieAuth", options =>
{
    options.LoginPath = "/Security/Login";
    options.LogoutPath = "/Security/Logout";
    options.AccessDeniedPath = "/Security/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Extended span to support Remember Me cookies
    options.SlidingExpiration = true;
    options.Cookie.Name = "WMSRestaurantAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// 7. Seed Roles and Default Admin Account Automatically on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    var hp = scope.ServiceProvider.GetRequiredService<Helper>();

    var superadmin = db.SuperAdmins.FirstOrDefault(
        sa => sa.Username.ToLower() == "superadmin123"
    );

    if (superadmin == null)
    {
        superadmin = new SuperAdmin
        {
            Id = Guid.NewGuid().ToString(),
            Username = "SuperAdmin123",
            Name = "SuperAdminDemo",
            Email = "SuperAdminDemo123@gmail.com",
            FirstName = "SuperAdmin",
            LastName = "Demo",
            RoleId = "SuperAdmin",
            Hash = hp.HashPassword("SuperAdmin1234@"),
            CreatedDate = DateTime.Now,
            FailedLogin = 0,
            LockoutEnd = null
        };
        db.SuperAdmins.Add(superadmin);
        db.SaveChanges();
    }

    var testMember = db.Members.FirstOrDefault(u => u.Username.ToLower() == "jennie12");
    if (testMember == null)
    {
        testMember = new Member
        {
            Id = Guid.NewGuid().ToString(),
            Username = "jennie12",
            Name = "jenniekim",
            Email = "jenniekim123@gmail.com",
            FirstName = "jennie",
            LastName = "kim",
            RoleId = "Member",
            Hash = hp.HashPassword("JennieKim1234@"),
            CreatedDate = DateTime.Now,
            FailedLogin = 0,
            LockoutEnd = null
        };
        db.Members.Add(testMember);
        db.SaveChanges();
    }

    // Find existing admin or create a new one
    var admin = db.Admins.FirstOrDefault(u => u.Username.ToLower() == "admin123")
                ?? db.Users.OfType<Admin>().FirstOrDefault(u => u.Username.ToLower() == "admin123");

    if (admin == null)
    {
        admin = new Admin
        {
            Id = Guid.NewGuid().ToString(),
            Username = "admin123",
            Name = "System Admin",
            Email = "admin123@gmail.com",
            FirstName = "System",
            LastName = "Admin",
            RoleId = "Admin",
            Hash = hp.HashPassword("Admin1234@"),
            CreatedDate = DateTime.Now,
            FailedLogin = 0,
            LockoutEnd = null
        };
        db.Admins.Add(admin);
        db.SaveChanges();
    }
}

// 8. Configure Supported Languages (Cultures)
var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("zh-CN"),
    new CultureInfo("ms-MY")
};

var locOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

locOptions.RequestCultureProviders.Clear();
locOptions.RequestCultureProviders.Add(new CookieRequestCultureProvider());

app.UseRequestLocalization(locOptions);

// 9. Pipeline Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();

app.Run();