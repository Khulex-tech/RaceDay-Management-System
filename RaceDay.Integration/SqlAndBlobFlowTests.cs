using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API;
using RaceDay.Web;
using RaceDay.Web.Services;

namespace RaceDay.Integration
{
    /*
     * Runs against a real SQL Server and Azurite when the connection strings are set.
     * GitHub Actions sets RACEDAY_REQUIRE_SQL so a missing database fails the build.
     * A normal local test run without those variables does not pretend the database was checked.
     */
    public class SqlAndBlobFlowTests
    {
        [Fact]
        public async Task FreshDatabase_EnforcesRoles_AndStoresTheBannerInBlobStorage()
        {
            string? connection = Environment.GetEnvironmentVariable("RACEDAY_SQL_CONNECTION");
            string? blob = Environment.GetEnvironmentVariable("RACEDAY_BLOB_CONNECTION");
            bool required = Environment.GetEnvironmentVariable("RACEDAY_REQUIRE_SQL") == "true";
            if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(blob))
            {
                if (required)
                {
                    Assert.Fail("RACEDAY_SQL_CONNECTION and RACEDAY_BLOB_CONNECTION must be set.");
                }

                return;
            }

            await using var api = new WebApplicationFactory<ApiEntryPoint>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", connection);
                builder.UseSetting("Database:ApplyMigrations", "true");
                builder.UseSetting("UseHttpsRedirection", "false");
                builder.UseSetting("AzureBlob:ConnectionString", blob);
                builder.UseSetting("AzureBlob:ContainerName", "raceday-images");
            });

            using var thabo = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
            using var ayesha = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
            using var sipho = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });

            Assert.Equal(HttpStatusCode.OK, (await Login(thabo, "thabo.mokoena@raceday.co.za")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Login(ayesha, "ayesha.patel@raceday.co.za")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Login(sipho, "sipho.ndlovu@gmail.com")).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await thabo.GetAsync("/api/dashboard/organiser")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await sipho.GetAsync("/api/dashboard/organiser")).StatusCode);

            // Event 3 is the Durban walk owned by Thabo. Ayesha must not delete it.
            Assert.Equal(HttpStatusCode.Forbidden, (await ayesha.DeleteAsync("/api/events/3")).StatusCode);

            string results = await sipho.GetStringAsync("/api/results/my-results");
            Assert.Contains("Soweto Marathon", results);
            Assert.Contains("47", results);

            using var upload = new MultipartFormDataContent();
            var file = new ByteArrayContent(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4 });
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            upload.Add(file, "file", "banner.png");
            var uploaded = await thabo.PostAsync("/api/events/3/banner", upload);
            string uploadedBody = await uploaded.Content.ReadAsStringAsync();
            Assert.True(uploaded.StatusCode == HttpStatusCode.OK, uploadedBody);

            var match = Regex.Match(uploadedBody, "\"bannerImageUrl\":\"([^\"]+)\"");
            Assert.True(match.Success, uploadedBody);
            string url = match.Groups[1].Value.Replace("\\u0026", "&");
            Assert.Contains("raceday-images", url);

            var service = new BlobServiceClient(blob);
            var container = service.GetBlobContainerClient("raceday-images");
            string blobName = url.Split('/').Last();
            var downloaded = await container.GetBlobClient(blobName).DownloadContentAsync();
            Assert.True(downloaded.Value.Content.ToMemory().Length > 0);

            using var anonymous = new HttpClient();
            var publicBlob = await anonymous.GetAsync(url);
            Assert.False(publicBlob.IsSuccessStatusCode, "A private container must not serve the banner anonymously.");

            var streamed = await thabo.GetAsync("/api/events/3/banner");
            Assert.Equal(HttpStatusCode.OK, streamed.StatusCode);
            Assert.True((await streamed.Content.ReadAsByteArrayAsync()).Length > 0);

            var handler = new NonDisposingHandler(api.Server.CreateHandler());
            await using var web = new WebApplicationFactory<WebEntryPoint>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddHttpClient<IRaceDayApi, RaceDayApiClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => handler);
                });
            });

            using var browser = web.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });
            string loginPage = await browser.GetStringAsync("/Account/Login");
            var token = Regex.Match(loginPage, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(token.Success);
            var loggedIn = await browser.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "sipho.ndlovu@gmail.com",
                ["Password"] = "RaceDay@2026",
                ["__RequestVerificationToken"] = token.Groups[1].Value
            }));
            Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);

            string myResults = await browser.GetStringAsync("/MyResults");
            Assert.Contains("Soweto Marathon", myResults);
            Assert.Contains("47th recorded. 3 results on file.", myResults);
            Assert.DoesNotContain("out of", myResults);
        }

        private static Task<HttpResponseMessage> Login(HttpClient client, string email)
        {
            return client.PostAsJsonAsync("/api/auth/login", new { email, password = "RaceDay@2026" });
        }

        private sealed class NonDisposingHandler : DelegatingHandler
        {
            public NonDisposingHandler(HttpMessageHandler inner) : base(inner)
            {
            }

            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}
