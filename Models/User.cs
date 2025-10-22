using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crossplatform_2_smirnova.Models
{
    public enum UserRole
    {
        Admin,
        User
    }

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; } = UserRole.User;

        public bool IsActive { get; set; } = true;   // Для блокировки
        public bool IsArchived { get; set; } = false; // Для архивации

        // Бизнес-логика
        public void Archive()
        {
            IsArchived = true;
            IsActive = false; // Архивированный пользователь не может быть активным
        }

        public void RestoreFromArchive()
        {
            IsArchived = false;
            IsActive = true;
        }

        public bool CanEditBooking(Booking booking)
        {
            // Архивированный пользователь не может редактировать брони
            return !IsArchived && (Role == UserRole.Admin || booking.UserId == Id);
        }

        public bool CanManageRoom(Room room)
        {
            // Архивированный пользователь не может управлять комнатами
            return !IsArchived && (Role == UserRole.Admin || room.OwnerId == Id);
        }
    }
}
