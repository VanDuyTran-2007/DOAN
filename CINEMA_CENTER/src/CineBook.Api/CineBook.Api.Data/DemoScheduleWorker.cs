using CineBook.Services.Mock;
using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Data;

internal sealed class DemoScheduleWorker(IServiceScopeFactory scopes, ILogger<DemoScheduleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMinutes(5);
            try
            {
                await ExtendScheduleIfExpiredAsync(stoppingToken);
                interval = TimeSpan.FromMinutes(30);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Không cập nhật được lịch chiếu demo. Sẽ thử lại.");
            }
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ExtendScheduleIfExpiredAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CineBookDbContext>();
        if (!await db.Database.CanConnectAsync(ct) || (await db.Database.GetPendingMigrationsAsync(ct)).Any())
            return;

        var today = DemoDataSeeder.CinemaToday();
        var startOfDay = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), MockData.VnOffset).UtcDateTime;
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (!await db.Showtimes.AnyAsync(showtime => showtime.StartTimeUtc >= startOfDay && showtime.Id.StartsWith("st|"), ct)
            && await db.Movies.AnyAsync(movie => movie.Id == "m01", ct))
        {
            await DemoDataSeeder.AddShowtimesAsync(db, today, ct);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Đã bổ sung lịch chiếu demo cho 7 ngày từ {Day}.", today);
        }
        await transaction.CommitAsync(ct);
    }
}
