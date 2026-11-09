using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using RaceDay.Web.Services;

namespace RaceDay.Web.Tests
{
    public class ApiCookieIsolationTests
    {
        [Fact]
        public async Task TwoMvcSessions_SendOnlyTheirOwnApiCookie()
        {
            var handler = new RecordingHandler();
            using var http = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
            var accessor = new HttpContextAccessor();
            var client = new RaceDayApiClient(http, accessor);

            var contextA = new DefaultHttpContext { Session = new MemorySession() };
            var contextB = new DefaultHttpContext { Session = new MemorySession() };

            accessor.HttpContext = contextA;
            var loginA = await client.LoginAsync("a@test.co.za", "password1");
            accessor.HttpContext = contextB;
            var loginB = await client.LoginAsync("b@test.co.za", "password1");

            Assert.True(loginA.Success);
            Assert.True(loginB.Success);
            Assert.Equal("RaceDay.Session=user-a", contextA.Session.GetString(SessionKeys.ApiCookie));
            Assert.Equal("RaceDay.Session=user-b", contextB.Session.GetString(SessionKeys.ApiCookie));

            accessor.HttpContext = contextA;
            await client.GetProfileAsync();
            accessor.HttpContext = contextB;
            await client.GetProfileAsync();

            var profileCalls = handler.Calls.Where(call => call.Path == "/api/users/profile").ToList();
            Assert.Equal(2, profileCalls.Count);
            Assert.Equal("RaceDay.Session=user-a", profileCalls[0].Cookie);
            Assert.Equal("RaceDay.Session=user-b", profileCalls[1].Cookie);
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            public List<(string Path, string? Cookie)> Calls { get; } = new();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = request.RequestUri!.AbsolutePath;
                string? cookie = request.Headers.TryGetValues("Cookie", out var values) ? string.Join(";", values) : null;
                Calls.Add((path, cookie));

                if (path.EndsWith("/api/auth/login", StringComparison.Ordinal))
                {
                    string body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                    string token = body.Contains("a@test", StringComparison.Ordinal) ? "user-a" : "user-b";
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"userId\":1,\"fullName\":\"Test User\",\"role\":\"Participant\"}", Encoding.UTF8, "application/json")
                    };
                    response.Headers.TryAddWithoutValidation("Set-Cookie", $"RaceDay.Session={token}; path=/; httponly");
                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"userId\":1,\"firstName\":\"Test\",\"lastName\":\"User\",\"email\":\"a@test.co.za\",\"phoneNumber\":null,\"profilePictureUrl\":null,\"role\":\"Participant\"}", Encoding.UTF8, "application/json")
                };
            }
        }

        private sealed class MemorySession : ISession
        {
            private readonly Dictionary<string, byte[]> _store = new();
            public bool IsAvailable => true;
            public string Id { get; } = Guid.NewGuid().ToString();
            public IEnumerable<string> Keys => _store.Keys;
            public void Clear() => _store.Clear();
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Remove(string key) => _store.Remove(key);
            public void Set(string key, byte[] value) => _store[key] = value;
            public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
        }
    }
}
