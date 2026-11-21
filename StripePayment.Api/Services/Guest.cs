using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace StripePayment.Api;

public static class Guest
{
    private const string Cookie = "stripe-store-guest";
    public static Guid? Read(HttpContext context, IDataProtectionProvider protection)
    {
        if (!context.Request.Cookies.TryGetValue(Cookie, out var value)) return null;
        try
        {
            var plaintext = protection.CreateProtector(Cookie).Unprotect(value);
            return Guid.TryParse(plaintext, out var id) ? id : null;
        }
        catch (CryptographicException) { return null; }
    }

    public static Guid GetOrCreate(HttpContext context, IDataProtectionProvider protection)
    {
        if (Read(context, protection) is { } existing) return existing;
        var id = Guid.NewGuid();
        context.Response.Cookies.Append(Cookie, protection.CreateProtector(Cookie).Protect(id.ToString()), new CookieOptions
        {
            HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax,
            Path = "/api", MaxAge = TimeSpan.FromDays(30), IsEssential = true
        });
        return id;
    }
}
