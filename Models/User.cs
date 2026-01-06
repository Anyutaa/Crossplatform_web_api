using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crossplatform_2_smirnova.Models
{
    public enum UserRole
    {
        Admin,
        User
    }

    public enum UserStatus
    {
        Active,
        Blocked,
        Archived
    }

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public long? TelegramId { get; set; }

        public string? TelegramUsername { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        [Required]
        public UserStatus Status { get; set; } = UserStatus.Active;


        public void Archive()
        {
            Status = UserStatus.Archived;
        }

        public void RestoreFromArchive()
        {
            Status = UserStatus.Active;
        }

        public void Block()
        {
            Status = UserStatus.Blocked;
        }

        public void Unblock()
        {
            Status = UserStatus.Active;
        }

        public bool CanEditBooking(Booking booking)
        {
            // Только активные пользователи могут редактировать брони
            return Status == UserStatus.Active &&
                   (Role == UserRole.Admin || booking.UserId == Id);
        }

        public bool CanManageRoom(Room room)
        {
            // Только активные пользователи могут управлять комнатами
            return Status == UserStatus.Active &&
                   (Role == UserRole.Admin || room.OwnerId == Id);
        }
    }
}
