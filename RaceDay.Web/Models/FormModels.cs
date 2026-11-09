using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    public class RegisterForm
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } = "Participant";

        [Display(Name = "Organiser invite code")]
        public string? InviteCode { get; set; }

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginForm
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class EventForm
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date and start time")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(14).AddHours(6);

        [Required, StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(0.01, 9999.99, ErrorMessage = "Distance must be greater than 0.")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";
    }

    public class CategoryForm
    {
        [Required, StringLength(100)]
        [Display(Name = "Category name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        [Range(0, 99999999.99, ErrorMessage = "Entry fee cannot be negative.")]
        [Display(Name = "Entry fee (R)")]
        public decimal EntryFee { get; set; }

        [Range(0, 120)]
        [Display(Name = "Minimum age")]
        public int MinAge { get; set; }

        [Range(0, 120)]
        [Display(Name = "Maximum age")]
        public int MaxAge { get; set; } = 59;
    }

    public class EnterEventForm
    {
        [Required]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Choose a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
    }

    public class ProfileForm
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }

    public class PasswordForm
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare(nameof(NewPassword), ErrorMessage = "New passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ResultForm
    {
        [Required]
        public int EnrolmentId { get; set; }

        public int? ResultId { get; set; }

        [Required]
        [RegularExpression(@"^\d{2}:\d{2}:\d{2}$", ErrorMessage = "Use hh:mm:ss, for example 03:41:18.")]
        [Display(Name = "Finish time")]
        public string FinishTime { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Position must be 1 or more.")]
        public int Position { get; set; }
    }

    public class ProfilePage
    {
        public ProfileForm Details { get; set; } = new();
        public PasswordForm Password { get; set; } = new();
    }

    public class HomeFilter
    {
        public string? EventType { get; set; }
        public string? Search { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IncludePast { get; set; }
        public int Page { get; set; } = 1;
    }
}
