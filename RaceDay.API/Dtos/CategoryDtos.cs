using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos
{
    // Body for POST /api/events/{eventId}/categories and PUT /api/categories/{id}
    public class CategoryRequestDto
    {
        [Required, StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        [Range(0, 99999999.99, ErrorMessage = "Entry fee cannot be negative.")]
        public decimal EntryFee { get; set; }

        [Range(0, 120)]
        public int MinAge { get; set; }

        [Range(0, 120)]
        public int MaxAge { get; set; }
    }
}
