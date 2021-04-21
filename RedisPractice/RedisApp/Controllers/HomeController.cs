using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
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

		public HomeController(ILogger<HomeController> logger, IRedisKeyResolver keyResolver, IDistributedCache cache)
		{
			_logger = logger;
			_keyResolver = keyResolver;
			_cache = cache;
		}

		public IActionResult Index()
		{
			return View();
		}

		public async Task<IActionResult> Info()
		{
			var appInfoViewModel = new AppInfoViewModel();

			await HttpContext.Session.LoadAsync();
			appInfoViewModel.CurrentUserLastRequestTime = HttpContext.Session.GetString(Keys.LastRequestTime);
			await HttpContext.Session.CommitAsync();

			appInfoViewModel.LastRequestTime = await _cache.GetStringAsync(_keyResolver.GetKeyWithPrefix(Keys.LastRequestTime));

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
			var parts = viewModel.Command.Split(' ');
			var mainCommand = parts.First();
			var args = parts.Skip(1).Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();

			using (var redis = ConnectionMultiplexer.Connect("workshop.redis.cache.windows.net:6379,password=tjHDfvqlUNEqeD7L022WMb+VuQbSKyDSpkNY6ROn7gQ=,ssl=False,abortConnect=False"))
			{
				var db = redis.GetDatabase();
				var redisResult = await db.ExecuteAsync(mainCommand, args);

				var redisResultType =  redisResult.GetType();

				viewModel.Result = redisResult.Type == ResultType.MultiBulk
					? string.Join(System.Environment.NewLine, (string[])redisResult)
					: redisResult.ToString();
			}

			return View(viewModel);
		}
	}
}
