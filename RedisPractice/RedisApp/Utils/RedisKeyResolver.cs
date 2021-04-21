using Microsoft.Extensions.Configuration;

namespace RedisApp.Utils
{
	public class RedisKeyResolver : IRedisKeyResolver
	{
		private IConfiguration _config { get; }

		public RedisKeyResolver(IConfiguration config)
		{
			_config = config;
		}

		public string GetKeyWithPrefix(string key)
		{
			return $"{_config["RedisNamespace"]}:{key}";
		}
	}
}
