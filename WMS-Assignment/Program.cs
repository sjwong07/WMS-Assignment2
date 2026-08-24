global using WMS_Assignment.Models;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// 1. Session and HttpContext
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// 2. Localization Services
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

// 3. Controllers and Views
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

// Configure Authentication with Cookie
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
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "WMSRestaurantAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();
// Seed Roles and Default Admin Account Automatically on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    var hp = scope.ServiceProvider.GetRequiredService<Helper>();

    // 1. Ensure Roles exist
    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { Id = "Member", Description = "Member" },
            new Role { Id = "Admin", Description = "Admin" }
        );
        db.SaveChanges();
    }

    // 2. Find existing admin or create a new one
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
    }
    else
    {
        // Force refresh hash and unlock on every startup
        admin.Hash = hp.HashPassword("Admin1234@");
        admin.RoleId = "Admin";
        admin.FailedLogin = 0;
        admin.LockoutEnd = null;
    }

    db.SaveChanges();
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

// Ensure Cookie provider is first in line
locOptions.RequestCultureProviders.Clear();
locOptions.RequestCultureProviders.Add(new CookieRequestCultureProvider());

app.UseRequestLocalization(locOptions);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();