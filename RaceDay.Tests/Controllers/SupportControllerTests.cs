using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests.Controllers
{
    public class SupportControllerTests
    {
        [Fact]
        public async Task Health_InMemoryDatabase_ReturnsHealthyResponse()
        {
            var db = TestDbContextFactory.CreateDbContext(nameof(Health_InMemoryDatabase_ReturnsHealthyResponse));
            var controller = new SupportController(db);

            var result = await controller.Health();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.NotNull(okResult.Value);
        }
    }
}