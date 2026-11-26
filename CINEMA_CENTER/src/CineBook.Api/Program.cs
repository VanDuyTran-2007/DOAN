using CineBook.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Program
{
	private static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		builder.Services.AddControllers();
		builder.Services.AddDbContext<CineBookDbContext>((DbContextOptionsBuilder options) =>
		{
			options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
		});
		WebApplication webApplication = builder.Build();
		webApplication.UseHttpsRedirection();
		webApplication.UseAuthorization();
		webApplication.MapControllers();
		webApplication.Run();
	}
}
