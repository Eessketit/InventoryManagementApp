using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementApp.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public string Name { get; set; } = "N/A";

        [Required]
        public string Address { get; set; } = "N/A";

        public UserStatus Status { get; set; } = UserStatus.Unverified;

        public DateTime RegisteredAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? EmailConfirmationToken { get; set; }
    }
}