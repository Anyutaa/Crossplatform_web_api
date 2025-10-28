using Microsoft.EntityFrameworkCore;
using Crossplatform_2_smirnova.Models;
using BCrypt.Net;

namespace Crossplatform_2_smirnova.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Таблицы
        public DbSet<User> Users { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingRoom> BookingRooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User → Room (1 ко многим)
            modelBuilder.Entity<Room>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Booking (1 ко многим)
            modelBuilder.Entity<Booking>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь многие-ко-многим через BookingRoom
            modelBuilder.Entity<BookingRoom>()
                .HasKey(br => new { br.BookingId, br.RoomId });

            modelBuilder.Entity<BookingRoom>()
                .HasOne(br => br.Booking)
                .WithMany(b => b.BookingRooms)
                .HasForeignKey(br => br.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookingRoom>()
                .HasOne(br => br.Room)
                .WithMany(r => r.BookingRooms)
                .HasForeignKey(br => br.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Уникальный Email у пользователей
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Хранение enum BookingStatus как строки
            modelBuilder.Entity<Booking>()
                .Property(b => b.Status)
                .HasConversion<string>();

            // --- Добавление начального администратора ---

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 3,
                Email = "admin@example.com",
                Name = "Admin",
                PasswordHash = "$2a$11$65jcjY9zzNv6j5f55Oh52.yStCAEt4WZaUdqynjYGNKJW1mHYoPqq", // статический хэш
                Role = UserRole.Admin,
                Status = UserStatus.Active
            });
        }
    }
}
