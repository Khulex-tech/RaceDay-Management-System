using System.Text;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Tests.TestHelpers
{
    /*
     * In-memory session implementation for unit testing controller actions.
     * Provides backing store for GetInt32, GetString, SetInt32, SetString and Clear.
     */
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new();

        public bool IsAvailable => true;
        public string Id => "TestSessionId";
        public IEnumerable<string> Keys => _sessionStorage.Keys;

        public void Set(string key, byte[] value)
        {
            _sessionStorage[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _sessionStorage.TryGetValue(key, out value!);
        }

        public void Remove(string key)
        {
            _sessionStorage.Remove(key);
        }

        public void Clear()
        {
            _sessionStorage.Clear();
        }

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
