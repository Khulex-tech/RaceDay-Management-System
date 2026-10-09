using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Dtos
{
    // Body for POST /api/enrolments/{enrolmentId}/result and PUT /api/results/{id}
    public class ResultRequestDto
    {
        // Finish time in hh:mm:ss format, e.g. "03:41:18"
        [Required]
        public string FinishTime { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Position must be greater than 0.")]
        public int Position { get; set; }
    }
}
