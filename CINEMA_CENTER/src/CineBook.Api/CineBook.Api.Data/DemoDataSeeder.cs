using CineBook.Services.Mock;
using Microsoft.EntityFrameworkCore;
using MovieStatus = CineBook.Models.MovieStatus;
using SeatType = CineBook.Models.SeatType;

namespace CineBook.Api.Data;

internal static class DemoDataSeeder
{
    private const int AuditoriumCount = 3;
    private const int TotalColumns = 15;
    private const int ScheduleDays = 7;
    private static readonly int[] ShowtimeSlots = [0, 2, 4, 6];
    private static readonly int[] CoupleSeatColumns = [1, 3, 5, 9, 11, 13];

    internal static async Task SeedAsync(CineBookDbContext db, CancellationToken ct)
    {
        var brands = MockData.Brands.Select(brand => new Brand
        {
            Id = brand.Id, Name = brand.Name, LogoUrl = brand.LogoUrl, ThemeColorHex = brand.ThemeColorHex
        });
        var cinemas = MockData.Cinemas.Select(cinema => new Cinema
        {
            Id = cinema.Id, BrandId = cinema.BrandId, Name = cinema.Name, Address = cinema.Address,
            City = cinema.City, District = cinema.District, Latitude = cinema.Latitude, Longitude = cinema.Longitude
        });
        var movies = MockData.Movies.Select(movie => new Movie
        {
            Id = movie.Id, Title = movie.Title, OriginalTitle = movie.OriginalTitle, PosterUrl = movie.PosterUrl,
            BackdropUrl = movie.BackdropUrl, DurationMinutes = movie.DurationMinutes,
            AgeRating = movie.AgeRating.ToString(), ReleaseDate = DateOnly.FromDateTime(movie.ReleaseDate),
            Rating = movie.Rating, Status = movie.Status.ToString(), IsHot = movie.IsHot,
            Description = movie.Description, Director = movie.Director
        });
        var combos = MockData.Combos.Select(combo => new Combo
        {
            Id = combo.Id, Name = combo.Name, Description = combo.Description, Price = combo.Price, ImageUrl = combo.ImageUrl
        });
        var auditoriums = MockData.Cinemas.SelectMany(cinema =>
            Enumerable.Range(1, AuditoriumCount).Select(room => new Auditorium
            {
                Id = $"{cinema.Id}-r{room}", CinemaId = cinema.Id, Name = $"Phòng {room}", TotalColumns = TotalColumns
            })).ToArray();
        await AddMissingAsync(db.Brands, brands, brand => brand.Id, ct);
        await AddMissingAsync(db.Cinemas, cinemas, cinema => cinema.Id, ct);
        await AddMissingAsync(db.Movies, movies, movie => movie.Id, ct);
        await AddMissingAsync(db.Combos, combos, combo => combo.Id, ct);
        await AddMissingAsync(db.Auditoriums, auditoriums, room => room.Id, ct);
        await AddMissingAsync(db.Seats, auditoriums.SelectMany(BuildSeats), seat => seat.Id, ct);
        await AddShowtimesAsync(db, CinemaToday(), ct);
        await db.SaveChangesAsync(ct);
    }

    internal static DateOnly CinemaToday() => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(MockData.VnOffset).Date);

    internal static Task AddShowtimesAsync(CineBookDbContext db, DateOnly today, CancellationToken ct) =>
        AddMissingAsync(db.Showtimes, BuildShowtimes(today), showtime => showtime.Id, ct);

    private static IEnumerable<Seat> BuildSeats(Auditorium room)
    {
        for (char row = 'A'; row <= 'J'; row++)
        {
            var type = row switch
            {
                'J' => SeatType.Couple,
                >= 'F' and <= 'H' => SeatType.Vip,
                _ => SeatType.Standard
            };
            var columns = row switch
            {
                'A' => Enumerable.Range(2, TotalColumns - 2).Where(column => column != 8),
                'J' => CoupleSeatColumns,
                _ => Enumerable.Range(1, TotalColumns).Where(column => column != 8)
            };
            foreach (var (column, index) in columns.Select((column, index) => (column, index)))
            {
                yield return new Seat
                {
                    Id = $"{room.Id}-{row}{index + 1}", AuditoriumId = room.Id, Row = row.ToString(),
                    Number = index + 1, Type = type.ToString(), GridColumn = column,
                    ColumnSpan = type == SeatType.Couple ? 2 : 1, IsAvailable = true
                };
            }
        }
    }

    private static IEnumerable<Showtime> BuildShowtimes(DateOnly today)
    {
        foreach (var cinema in MockData.Cinemas)
        {
            var movies = MockData.Movies.Where(movie => movie.Status == MovieStatus.NowShowing &&
                MockData.IsScreening(movie.Id, cinema.Id)).ToArray();
            for (var day = today; day < today.AddDays(ScheduleDays); day = day.AddDays(1))
            {
                // Khoảng cách giữa các khung giờ đủ cho cả phim dài nhất trong bộ dữ liệu demo.
                foreach (var (slot, index) in ShowtimeSlots.Select((slot, index) => (slot, index)))
                {
                    for (int room = 0; room < Math.Min(AuditoriumCount, movies.Length); room++)
                    {
                        var movie = movies[(day.DayNumber + index * AuditoriumCount + room) % movies.Length];
                        var showtime = MockData.BuildShowtime(movie.Id, cinema.Id, day.ToDateTime(TimeOnly.MinValue), slot);
                        var startUtc = showtime.StartTime.UtcDateTime;
                        yield return new Showtime
                        {
                            Id = showtime.Id, MovieId = movie.Id, AuditoriumId = $"{cinema.Id}-r{room + 1}",
                            StartTimeUtc = startUtc, EndTimeUtc = startUtc.AddMinutes(movie.DurationMinutes),
                            Format = showtime.Format.ToString(), Language = showtime.Language.ToString(), BasePrice = showtime.BasePrice
                        };
                    }
                }
            }
        }
    }

    private static async Task AddMissingAsync<TEntity>(
        DbSet<TEntity> table, IEnumerable<TEntity> source, Func<TEntity, string> key, CancellationToken ct)
        where TEntity : class
    {
        var rows = source.ToArray();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keys = table.Select(row => EF.Property<string>(row, "Id"));
        foreach (var ids in rows.Select(key).Chunk(500))
        {
            known.UnionWith(await keys.Where(id => ids.Contains(id)).ToListAsync(ct));
        }
        table.AddRange(rows.Where(row => known.Add(key(row))));
    }
}
