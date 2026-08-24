namespace WMS_Assignment.Models;

public static class Lang
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["en-US"] = new Dictionary<string, string>
        {
            ["Home"] = "Home",
            ["Menu"] = "Menu",
            ["Cart"] = "Cart",
            ["Profile"] = "Profile",
            ["Logout"] = "Logout",
            ["Login"] = "Login"
        },
        ["zh-CN"] = new Dictionary<string, string>
        {
            ["Home"] = "首页",
            ["Menu"] = "菜单",
            ["Cart"] = "购物车",
            ["Profile"] = "个人资料",
            ["Logout"] = "退出登录",
            ["Login"] = "登录"
        },
        ["ms-MY"] = new Dictionary<string, string>
        {
            ["Home"] = "Laman Utama",
            ["Menu"] = "Menu",
            ["Cart"] = "Troli",
            ["Profile"] = "Profil",
            ["Logout"] = "Log Keluar",
            ["Login"] = "Log Masuk"
        }
    };

    public static string T(string key)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;

        if (Translations.TryGetValue(culture, out var dict) && dict.TryGetValue(key, out var val))
        {
            return val;
        }

        if (Translations["en-US"].TryGetValue(key, out var defaultVal))
        {
            return defaultVal;
        }

        return key;
    }
}