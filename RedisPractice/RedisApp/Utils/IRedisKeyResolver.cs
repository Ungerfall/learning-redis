using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RedisApp.Utils
{
	public interface IRedisKeyResolver
	{
		string GetKeyWithPrefix(string key);
	}
}
