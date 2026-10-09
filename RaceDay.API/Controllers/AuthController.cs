using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    // Handles register, login, logout and "who am I" (section 2 of the endpoint plan)
    [Route("api/auth")]
    public class AuthController : BaseApiController
    {
        private readonly RaceDayDbContext _db;

        public AuthController(RaceDayDbContext db)
        {
            _db = db;
        }

        // POST /api/auth/register
        [HttpPost("register")]
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

        // POST /api/auth/login
        [HttpPost("login")]
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

        // POST /api/auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            if (CurrentUserId == null)
            {
                return NotLoggedIn();
            }

            HttpContext.Session.Clear();
            return Ok(new { message = "Session ended." });
        }

        // GET /api/auth/me
        [HttpGet("me")]
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
