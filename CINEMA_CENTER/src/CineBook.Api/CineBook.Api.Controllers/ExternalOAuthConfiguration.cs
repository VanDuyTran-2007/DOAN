using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace CineBook.Api.Controllers;

internal static class ExternalOAuthConfiguration
{
    internal static void AddCineBookProviders(this AuthenticationBuilder auth, IConfiguration configuration)
    {
        var google = configuration.GetSection("Authentication:Google");
        if (!string.IsNullOrWhiteSpace(google["ClientId"]) && !string.IsNullOrWhiteSpace(google["ClientSecret"]))
        {
            auth.AddGoogle(options =>
            {
                options.ClientId = google["ClientId"]!;
                options.ClientSecret = google["ClientSecret"]!;
                Configure(options);
                options.Events.OnCreatingTicket = context =>
                {
                    var emailVerified = (context.User.TryGetProperty("verified_email", out var verified) ||
                        context.User.TryGetProperty("email_verified", out verified)) &&
                        verified.ValueKind == JsonValueKind.True;
                    if (!emailVerified)
                    {
                        foreach (var email in context.Identity!.FindAll(ClaimTypes.Email).ToArray())
                            context.Identity.RemoveClaim(email);
                    }
                    return Task.CompletedTask;
                };
            });
        }

        var facebook = configuration.GetSection("Authentication:Facebook");
        if (!string.IsNullOrWhiteSpace(facebook["AppId"]) && !string.IsNullOrWhiteSpace(facebook["AppSecret"]))
        {
            auth.AddFacebook(options =>
            {
                options.AppId = facebook["AppId"]!;
                options.AppSecret = facebook["AppSecret"]!;
                options.Scope.Add("email");
                options.Fields.Add("email");
                Configure(options);
            });
        }
    }

    private static void Configure(OAuthOptions options)
    {
        options.SaveTokens = false;
        options.RemoteAuthenticationTimeout = TimeSpan.FromMinutes(5);
        options.Events.OnTicketReceived = context => CompleteAsync(context.HttpContext, context.Properties,
            context.Principal, context.HandleResponse);
        options.Events.OnRemoteFailure = context => CompleteAsync(context.HttpContext, context.Properties,
            null, context.HandleResponse);
        options.Events.OnAccessDenied = context => CompleteAsync(context.HttpContext, context.Properties,
            null, context.HandleResponse);
    }

    private static Task CompleteAsync(HttpContext context, AuthenticationProperties? properties,
        ClaimsPrincipal? principal, Action handle)
    {
        handle();
        var flows = context.RequestServices.GetRequiredService<ExternalAuthFlows>();
        var callback = flows.BrowserCallback(properties, principal);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        if (callback is not null)
        {
            context.Response.Redirect(callback);
            return Task.CompletedTask;
        }
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Yêu cầu đã hết hạn. Hãy mở lại CineBook và thử đăng nhập.");
    }
}
