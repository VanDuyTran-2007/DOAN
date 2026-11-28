using CineBook.Api.Controllers;
using CineBook.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class Program
{
	private static async Task Main(string[] args)
	{
		const string initDatabaseOption = "--init-db";
		bool initDatabase = args.Contains(initDatabaseOption);
		WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			Args = args.Where(arg => arg != initDatabaseOption).ToArray(),
			ContentRootPath = AppContext.BaseDirectory
		});
		if (initDatabase)
		{
			builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
		}
		builder.Services.AddControllers();
		builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
		builder.Services.AddSingleton<AccountPasswords>();
		builder.Services.AddSingleton<ExternalAuthFlows>();
		var authentication = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
		{
			options.Cookie.Name = "CineBook.Session";
			options.Cookie.SameSite = SameSiteMode.Strict;
			options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
			options.ExpireTimeSpan = TimeSpan.FromHours(8);
			options.Events.OnValidatePrincipal = async context =>
			{
				var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
				var version = context.Principal?.FindFirstValue("session_version");
				var db = context.HttpContext.RequestServices.GetRequiredService<CineBookDbContext>();
				if (Guid.TryParseExact(version, "N", out var sessionVersion) &&
				    await db.Users.AnyAsync(user => user.Id == id && !user.IsLocked && user.SessionVersion == sessionVersion,
				        context.HttpContext.RequestAborted))
					return;
				context.RejectPrincipal();
				await context.HttpContext.SignOutAsync();
			};
		});
		authentication.AddCineBookProviders(builder.Configuration);
		builder.Services.AddAuthorization();
		builder.Services.AddDbContext<CineBookDbContext>((provider, options) =>
		{
			options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
			if (initDatabase)
			{
				options.UseAsyncSeeding(async (context, _, ct) =>
				{
					var db = (CineBookDbContext)context;
					await provider.GetRequiredService<AccountPasswords>().UpgradeStoredPlaintextAsync(db, ct);
					await DemoDataSeeder.SeedAsync(db, ct);
				});
			}
		});
		if (!initDatabase)
			builder.Services.AddHostedService<DemoScheduleWorker>();
		WebApplication webApplication = builder.Build();
		if (initDatabase)
		{
			await using var initializationApp = webApplication;
			await using var scope = initializationApp.Services.CreateAsyncScope();
			var db = scope.ServiceProvider.GetRequiredService<CineBookDbContext>();
			await db.Database.MigrateAsync();
			Console.WriteLine("Đã cập nhật schema và dữ liệu demo CineBook.");
			return;
		}
		if (builder.Configuration.GetValue<bool>("CineBook:UseLoopbackProxy"))
		{
			// Nhận scheme HTTPS từ proxy chạy trên cùng máy.
			webApplication.UseForwardedHeaders(new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedProto
			});
		}
		webApplication.UseHttpsRedirection();
		webApplication.UseAuthentication();
		webApplication.Use(async (context, next) =>
		{
			if (context.User.HasClaim("password_setup", "true") &&
			    context.GetEndpoint()?.Metadata.GetMetadata<AllowDuringPasswordSetupAttribute>() is null)
			{
				context.Response.StatusCode = StatusCodes.Status403Forbidden;
				await context.Response.WriteAsJsonAsync(new { detail = "Hãy đặt mật khẩu trước khi tiếp tục." });
				return;
			}
			await next(context);
		});
		webApplication.UseAuthorization();
		webApplication.MapControllers();
		await webApplication.RunAsync();
	}
}
