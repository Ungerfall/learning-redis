using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RedisApp.Models;
using RedisApp.Utils;
using StackExchange.Redis;

namespace RedisApp.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IRedisKeyResolver _keyResolver;
		private readonly IDistributedCache _cache;
		private readonly IConnectionMultiplexer _redis;

		public HomeController(ILogger<HomeController> logger, IRedisKeyResolver keyResolver, IDistributedCache cache, IConnectionMultiplexer redis)
		{
			_logger = logger;
			_keyResolver = keyResolver;
			_cache = cache;
			_redis = redis;
		}

		public IActionResult Index()
		{
			return View();
		}

		public async Task<IActionResult> Info()
		{
			var appInfoViewModel = new AppInfoViewModel();

			appInfoViewModel.LastRequestTime = await _cache.GetStringAsync(_keyResolver.GetKeyWithPrefix(Keys.LastRequestTime));

			await HttpContext.Session.LoadAsync();
			appInfoViewModel.CurrentUserLastRequestTime = HttpContext.Session.GetString(Keys.LastRequestTime);

			var redisValue = await _redis.GetDatabase().StringGetAsync(new RedisKey("Counter"));
			appInfoViewModel.RedisCounter = redisValue.ToString();

			return View(appInfoViewModel);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}


		public IActionResult Execute()
		{
			return View(new CommandViewModel());
		}


		[HttpPost]
		public async Task<IActionResult> Execute(CommandViewModel viewModel)
		{
			var commandParts = viewModel.Command.Split(' ');
			var mainCommand = commandParts.FirstOrDefault();

			if (!string.IsNullOrWhiteSpace(mainCommand))
			{
				var args = commandParts.Skip(1).Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();

				var redisResult = await _redis.GetDatabase().ExecuteAsync(mainCommand, args);
				var redisResultType = redisResult.GetType();

				viewModel.Result = redisResult.Type == ResultType.MultiBulk
					? string.Join(System.Environment.NewLine, (string[])redisResult)
					: redisResult.ToString();
			}

			return View(viewModel);
		}
	}
}
