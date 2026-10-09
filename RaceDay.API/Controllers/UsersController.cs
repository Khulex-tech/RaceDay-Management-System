using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /*
     * User profile endpoints (section 3 of the endpoint plan).
     * The profile is always taken from the session, never from the URL,
     * so a user can only ever see or change their own profile.
     */
    [Route("api/users")]
    public class UsersController : BaseApiController
    {
        private readonly RaceDayDbContext _db;
        private readonly IWebHostEnvironment _env;

        public UsersController(RaceDayDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET /api/users/profile
        [HttpGet("profile")]
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

        // PUT /api/users/profile
        [HttpPut("profile")]
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

        // PUT /api/users/profile/password
        [HttpPut("profile/password")]
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

        // POST /api/users/profile/picture
        [HttpPost("profile/picture")]
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

        // GET /api/users/{id} - limited public view, Organisers only
        [HttpGet("{id:int}")]
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
