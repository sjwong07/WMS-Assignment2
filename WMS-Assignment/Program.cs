global using WMS_Assignment.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddScoped<Helper>();

builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\Restaurant.mdf;
");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<Microsoft.AspNetCore.Builder.RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new System.Globalization.CultureInfo("en"),
        new System.Globalization.CultureInfo("ms"),
        new System.Globalization.CultureInfo("zh")
    };

    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

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
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.Name = "WMSRestaurantAuth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Seed Roles Automatically on App Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();

    if (!db.Roles.Any())
    {
        db.Roles.AddRange(
            new Role { Id = "Member", Description = "Member" },
            new Role { Id = "Admin", Description = "Admin" }
        );
        db.SaveChanges();
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();