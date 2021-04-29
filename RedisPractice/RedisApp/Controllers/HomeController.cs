using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RedisApp.Models;
using RedisApp.Utils;

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
		public IActionResult Execute(CommandViewModel viewModel)
		{
			return View(viewModel);
		}
	}
}
