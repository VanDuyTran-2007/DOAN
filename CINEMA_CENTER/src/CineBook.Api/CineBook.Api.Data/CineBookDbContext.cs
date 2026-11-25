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
	}
}
