using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDay.API.Controllers;
using RaceDay.API.Dtos;
using RaceDay.API.Helpers;
using RaceDay.API.Models;
using RaceDay.Tests.TestHelpers;
using Xunit;

namespace RaceDay.Tests.Controllers
{
    /*
     * Unit tests for AuthController covering registration, login, logout, and identity endpoints.
     */
    public class AuthControllerTests
    {
        [Fact]
        public async Task Register_ValidDto_Returns201CreatedAndSavesUser()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Register_ValidDto_Returns201CreatedAndSavesUser));
            var controller = new AuthController(db).WithSession();

            var dto = new RegisterDto
            {
                FirstName = "Thabo",
                LastName = "Mokoena",
                Email = "thabo@raceday.co.za",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                PhoneNumber = "0821234567",
                Role = Roles.Participant
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, statusResult.StatusCode);

            var userInDb = db.Users.FirstOrDefault(u => u.Email == "thabo@raceday.co.za");
            Assert.NotNull(userInDb);
            Assert.Equal("Thabo", userInDb.FirstName);
            Assert.Equal(Roles.Participant, userInDb.Role);
            Assert.True(PasswordHelper.VerifyPassword("Password123!", userInDb.PasswordHash));
        }

        [Fact]
        public async Task Register_OrganiserRole_Returns400AndDoesNotCreateOrganiser()
        {
            var db = TestDbContextFactory.CreateDbContext(nameof(Register_OrganiserRole_Returns400AndDoesNotCreateOrganiser));
            var controller = new AuthController(db).WithSession();
            var dto = new RegisterDto
            {
                FirstName = "Untrusted",
                LastName = "Organiser",
                Email = "untrusted@raceday.co.za",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = Roles.Organiser
            };

            var result = await controller.Register(dto);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.DoesNotContain(db.Users, user => user.Email == dto.Email);
        }

        [Fact]
        public async Task RegisterOrganiser_MissingInviteCode_Returns401()
        {
            var db = TestDbContextFactory.CreateDbContext(nameof(RegisterOrganiser_MissingInviteCode_Returns401));
            var controller = new AuthController(db).WithSession();
            var dto = new OrganiserRegistrationDto
            {
                FirstName = "Trusted",
                LastName = "Organiser",
                Email = "trusted@raceday.co.za",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var result = await controller.RegisterOrganiser(dto, null);

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public async Task RegisterOrganiser_ValidInviteCode_CreatesOrganiser()
        {
            var db = TestDbContextFactory.CreateDbContext(nameof(RegisterOrganiser_ValidInviteCode_CreatesOrganiser));
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:OrganiserInviteCode"] = "test-invite-code"
                })
                .Build();
            var controller = new AuthController(db, configuration).WithSession();
            var dto = new OrganiserRegistrationDto
            {
                FirstName = "Trusted",
                LastName = "Organiser",
                Email = "trusted@raceday.co.za",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var result = await controller.RegisterOrganiser(dto, "test-invite-code");

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(Roles.Organiser, db.Users.Single(user => user.Email == dto.Email).Role);
        }

        [Fact]
        public async Task Register_InvalidRole_Returns400BadRequest()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Register_InvalidRole_Returns400BadRequest));
            var controller = new AuthController(db).WithSession();

            var dto = new RegisterDto
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@raceday.co.za",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = "SuperAdmin"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409Conflict()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Register_DuplicateEmail_Returns409Conflict));
            db.Users.Add(new User
            {
                FirstName = "Existing",
                LastName = "User",
                Email = "existing@raceday.co.za",
                PasswordHash = PasswordHelper.HashPassword("Password123!"),
                Role = Roles.Participant
            });
            await db.SaveChangesAsync();

            var controller = new AuthController(db).WithSession();
            var dto = new RegisterDto
            {
                FirstName = "New",
                LastName = "User",
                Email = "existing@raceday.co.za",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = Roles.Participant
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var conflict = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflict.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_Returns200OKAndSetsSession()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Login_ValidCredentials_Returns200OKAndSetsSession));
            var user = new User
            {
                FirstName = "Sipho",
                LastName = "Dlamini",
                Email = "sipho@raceday.co.za",
                PasswordHash = PasswordHelper.HashPassword("Secret123!"),
                Role = Roles.Participant
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var controller = new AuthController(db).WithSession();
            var dto = new LoginDto
            {
                Email = "sipho@raceday.co.za",
                Password = "Secret123!"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Login_InvalidCredentials_Returns401Unauthorized()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Login_InvalidCredentials_Returns401Unauthorized));
            db.Users.Add(new User
            {
                FirstName = "Sipho",
                LastName = "Dlamini",
                Email = "sipho@raceday.co.za",
                PasswordHash = PasswordHelper.HashPassword("Secret123!"),
                Role = Roles.Participant
            });
            await db.SaveChangesAsync();

            var controller = new AuthController(db).WithSession();
            var dto = new LoginDto
            {
                Email = "sipho@raceday.co.za",
                Password = "WrongPassword"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public void Logout_Authenticated_ClearsSessionAndReturns200()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Logout_Authenticated_ClearsSessionAndReturns200));
            var controller = new AuthController(db).WithSession(userId: 1, role: Roles.Organiser);

            // Act
            var result = controller.Logout();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public void Logout_Unauthenticated_Returns401Unauthorized()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Logout_Unauthenticated_Returns401Unauthorized));
            var controller = new AuthController(db).WithSession();

            // Act
            var result = controller.Logout();

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }

        [Fact]
        public async Task Me_AuthenticatedUser_Returns200WithUserData()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Me_AuthenticatedUser_Returns200WithUserData));
            var user = new User
            {
                FirstName = "Lerato",
                LastName = "Khumalo",
                Email = "lerato@raceday.co.za",
                PasswordHash = PasswordHelper.HashPassword("Secret123!"),
                Role = Roles.Participant
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var controller = new AuthController(db).WithSession(userId: user.UserId, role: Roles.Participant);

            // Act
            var result = await controller.Me();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Me_Unauthenticated_Returns401Unauthorized()
        {
            // Arrange
            var db = TestDbContextFactory.CreateDbContext(nameof(Me_Unauthenticated_Returns401Unauthorized));
            var controller = new AuthController(db).WithSession();

            // Act
            var result = await controller.Me();

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorized.StatusCode);
        }
    }
}
