using Service.Models;
using StackExchange.Redis;
using System.Text.Json;
using User = Service.Models.User;

namespace Service.Data
{
    public class Cache : ICache
    {
        private readonly UserContext _context;
        private readonly IConnectionMultiplexer _redis;
       
        public Cache(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public void AddUser(Models.User user)
        {
            if (user == null)
            {
                throw new ArgumentOutOfRangeException(nameof(user));
            }
            var cache = _redis.GetDatabase();
            var serializedUser = JsonSerializer.Serialize(user);
           //cache.StringSet(user.Id, serializedUser);
        }

        public Models.User? DeleteUser(string id)
        {
            var cache = _redis.GetDatabase();
            var user = cache.StringGet(id);
            if (user.IsNullOrEmpty)
            {
                return null;
            }
            cache.KeyDelete(id);
            return JsonSerializer.Deserialize<User>(user);
        }

        public IEnumerable<User> GetAllUsers()
        {
            var cache = _redis.GetDatabase();

            var userKeysCache = cache.Multiplexer.GetServer(_redis.GetEndPoints().First()).Keys(pattern: "User:*");

            var Users = new List<User>();

            foreach (var key in userKeysCache)
            {
                var UserJson = cache.StringGet(key);
                if (!UserJson.IsNullOrEmpty)
                {
                    var User = JsonSerializer.Deserialize<User>(UserJson);
                    Users.Add(User);
                }
            }

            return Users;
        }

        public User? GetUserById(string id)
        {
            var db = _redis.GetDatabase();
            var userJson = db.StringGet($"User:{id}");
            if (userJson.IsNullOrEmpty)
            {
                return null;
            }
            return JsonSerializer.Deserialize<Models.User>(userJson);
        }

        public User? UpdateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var db = _redis.GetDatabase();
            var userJson = JsonSerializer.Serialize(user);
            db.StringSet($"User:{user.Id}", userJson);
            return user;
        }

        Models.User? ICache.DeleteUser(int id)
        {
            var db = _redis.GetDatabase();
            var userJson = db.StringGet($"User:{id}");
            if (userJson.IsNullOrEmpty)
            {
                return null;
            }
            db.KeyDelete($"User:{id}");
            return JsonSerializer.Deserialize<User>(userJson);
        }

        IEnumerable<Models.User> ICache.GetAllUsers()
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var userKeys = server.Keys(pattern: "User:*");

            var users = new List<Models.User>();
            foreach (var key in userKeys)
            {
                var userJson = db.StringGet(key);
                if (!userJson.IsNullOrEmpty)
                {
                    var user = JsonSerializer.Deserialize<Models.User>(userJson);
                    users.Add(user);
                }
            }

            return users;
        }

        User? ICache.GetUserById(int id)
        {
            var db = _redis.GetDatabase();
            var userJson = db.StringGet($"User:{id}");
            if (userJson.IsNullOrEmpty)
            {
                return null;
            }
            return JsonSerializer.Deserialize<Models.User>(userJson);
        }
    }
}
