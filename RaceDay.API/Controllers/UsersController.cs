using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles user profile viewing, profile editing, password changes, and profile picture uploads.
    /// </summary>
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : BaseApiController
    {
        private readonly RaceDayDbContext _db;
        private readonly IWebHostEnvironment _env;

        public UsersController(RaceDayDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /// <summary>
        /// Returns the profile details of the currently authenticated user.
        /// </summary>
        /// <response code="200">User profile returned.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        [HttpGet("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return NotLoggedIn();
            }

            return Ok(ToProfile(user));
        }

        /// <summary>
        /// Updates the profile details of the currently authenticated user.
        /// </summary>
        /// <param name="dto">Updated personal profile details.</param>
        /// <response code="200">User profile updated.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        [HttpPut("profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return NotLoggedIn();
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = dto.PhoneNumber;

            await _db.SaveChangesAsync();
            return Ok(ToProfile(user));
        }

        /// <summary>
        /// Updates the password of the currently authenticated user.
        /// </summary>
        /// <param name="dto">Current password and new password details.</param>
        /// <response code="200">Password successfully updated.</response>
        /// <response code="401">Unauthorized, current password incorrect or no session.</response>
        [HttpPut("profile/password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return NotLoggedIn();
            }

            // The current password must be correct before we allow a change
            if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            {
                return Unauthorized(new { message = "Current password is incorrect." });
            }

            user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Password updated." });
        }

        /// <summary>
        /// Uploads a profile picture for the logged-in user.
        /// </summary>
        /// <param name="file">Image file (JPEG/PNG, up to 5 MB).</param>
        /// <response code="200">Profile picture uploaded and URL returned.</response>
        /// <response code="400">Bad request, invalid file format or size.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        [HttpPost("profile/picture")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UploadProfilePicture(IFormFile? file)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                return NotLoggedIn();
            }

            string? error = ImageUploadHelper.Validate(file);
            if (error != null)
            {
                return BadRequest(new { message = error });
            }

            user.ProfilePictureUrl = await ImageUploadHelper.SaveAsync(file!, _env.WebRootPath);
            await _db.SaveChangesAsync();

            return Ok(new { profilePictureUrl = user.ProfilePictureUrl });
        }

        /// <summary>
        /// Returns limited public view of a user (Organisers only).
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <response code="200">User details returned.</response>
        /// <response code="401">Unauthorized, no active session.</response>
        /// <response code="403">Forbidden, caller is not an Organiser.</response>
        /// <response code="404">Not Found, user does not exist.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUser(int id)
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            if (CurrentRole != Roles.Organiser)
            {
                return NoAccess("Only Organisers can view other users.");
            }

            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FirstName + " " + user.LastName,
                role = user.Role
            });
        }

        // Builds the profile response (never includes the password hash)
        private static object ToProfile(User user)
        {
            return new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                profilePictureUrl = user.ProfilePictureUrl,
                role = user.Role,
                createdAt = user.CreatedAt
            };
        }
    }
}
