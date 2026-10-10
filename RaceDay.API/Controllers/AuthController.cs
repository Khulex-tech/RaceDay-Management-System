using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Handles user registration, authentication, logout, and identity checks.
    /// </summary>
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public AuthController(RaceDayDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Registers a new user account with role Organiser or Participant.
        /// </summary>
        /// <param name="dto">Registration details including name, email, password, and role.</param>
        /// <response code="201">User account successfully created.</response>
        /// <response code="400">Validation failed (invalid role or malformed input).</response>
        /// <response code="409">Conflict, email is already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            // Only the two system roles are allowed
            if (dto.Role != Roles.Organiser && dto.Role != Roles.Participant)
            {
                return BadRequest(new { message = "Role must be Organiser or Participant." });
            }

            string email = dto.Email.Trim().ToLower();

            // Each email can only be registered once
            bool emailTaken = await _db.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
            {
                return Conflict(new { message = "This email is already registered." });
            }

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                role = user.Role
            });
        }

        /// <summary>
        /// Authenticates user credentials and starts a server-side session.
        /// </summary>
        /// <param name="dto">Login credentials (email and password).</param>
        /// <response code="200">Login successful, session cookie set.</response>
        /// <response code="401">Unauthorized, invalid email or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            string email = dto.Email.Trim().ToLower();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Same message for wrong email or wrong password so we don't reveal which one failed
            if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // Save the user in the server-side session
            HttpContext.Session.SetInt32(SessionUserId, user.UserId);
            HttpContext.Session.SetString(SessionRole, user.Role);

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FirstName + " " + user.LastName,
                role = user.Role
            });
        }

        /// <summary>
        /// Ends the current user session.
        /// </summary>
        /// <response code="200">Session successfully ended.</response>
        /// <response code="401">Unauthorized, no active session found.</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            HttpContext.Session.Clear();
            return Ok(new { message = "Session ended." });
        }

        /// <summary>
        /// Returns the identity and role details stored in the active session.
        /// </summary>
        /// <response code="200">Active session user details returned.</response>
        /// <response code="401">Unauthorized, no active session found.</response>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            var user = await _db.Users.FindAsync(CurrentUserId);
            if (user == null)
            {
                // The user was removed after logging in, so end the session
                HttpContext.Session.Clear();
                return NotLoggedIn();
            }

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FirstName + " " + user.LastName,
                email = user.Email,
                role = user.Role
            });
        }
    }
}
