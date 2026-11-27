using Microsoft.EntityFrameworkCore;

namespace CineBook.Api.Data;

public class CineBookDbContext : DbContext
{
	public DbSet<Movie> Movies => Set<Movie>();

	public DbSet<Brand> Brands => Set<Brand>();

	public DbSet<Cinema> Cinemas => Set<Cinema>();

	public DbSet<Auditorium> Auditoriums => Set<Auditorium>();

	public DbSet<Showtime> Showtimes => Set<Showtime>();

	public DbSet<Seat> Seats => Set<Seat>();

	public DbSet<SeatHold> SeatHolds => Set<SeatHold>();

	public DbSet<SeatHoldSeat> SeatHoldSeats => Set<SeatHoldSeat>();

	public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();

	public DbSet<User> Users => Set<User>();

	public DbSet<Combo> Combos => Set<Combo>();

	public DbSet<Booking> Bookings => Set<Booking>();

	public DbSet<BookingCombo> BookingCombos => Set<BookingCombo>();
	public DbSet<Voucher> Vouchers => Set<Voucher>();
	public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

	public CineBookDbContext(DbContextOptions<CineBookDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<SeatHoldSeat>().HasKey((SeatHoldSeat x) => new { x.HoldId, x.SeatId });
		modelBuilder.Entity<SeatHoldSeat>().HasOne<SeatHold>().WithMany()
			.HasForeignKey((SeatHoldSeat x) => x.HoldId);
		modelBuilder.Entity<BookingSeat>().HasKey((BookingSeat x) => new { x.BookingId, x.SeatId });
		modelBuilder.Entity<BookingSeat>().HasOne<Booking>().WithMany()
			.HasForeignKey((BookingSeat x) => x.BookingId);
		modelBuilder.Entity<BookingCombo>().HasKey((BookingCombo x) => new { x.BookingId, x.ComboId });
		modelBuilder.Entity<BookingCombo>().HasOne<Booking>().WithMany()
			.HasForeignKey((BookingCombo x) => x.BookingId);

		var user = modelBuilder.Entity<User>();
		user.Property(x => x.Email).HasMaxLength(254);
		user.Property(x => x.Phone).HasMaxLength(32);
		user.Property(x => x.SecondaryEmail1).HasMaxLength(254);
		user.Property(x => x.SecondaryEmail2).HasMaxLength(254);
		user.Property(x => x.City).HasMaxLength(100);
		user.Property(x => x.District).HasMaxLength(100);
		user.Property(x => x.Version).IsRowVersion();
		user.HasIndex(x => x.Email).IsUnique();
		user.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] <> N''");

		var external = modelBuilder.Entity<ExternalLogin>();
		external.HasKey(x => new { x.Provider, x.Subject });
		external.Property(x => x.Provider).HasMaxLength(16);
		external.Property(x => x.Subject).HasMaxLength(255).UseCollation("Latin1_General_100_BIN2");
		external.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
		external.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

		foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
		{
			// Giữ cả khóa ghép dưới giới hạn kích thước chỉ mục của SQL Server.
			if (property.ClrType == typeof(string) && property.Name.EndsWith("Id", StringComparison.Ordinal))
				property.SetMaxLength(128);
			if (property.ClrType == typeof(decimal))
			{
				property.SetPrecision(18);
				property.SetScale(2);
			}
		}
	}
}
