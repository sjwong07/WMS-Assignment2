using System.Runtime.CompilerServices;

namespace WMS_Assignment;

public static class Extensions
{
    public static bool IsAjax(this HttpRequest request)
    {
        return request.Headers.XRequestedWith == "XMLHttpRequest";
    }
}
