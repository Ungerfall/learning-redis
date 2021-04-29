using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedisApp.Utils;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;

namespace RedisApp
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();
			services.AddSingleton<IRedisKeyResolver, RedisKeyResolver>();
			services.AddStackExchangeRedisCache(options =>
			{
				options.Configuration = Configuration.GetConnectionString("Redis");
			});
			services.AddSession();
			services.AddSingleton<IConnectionMultiplexer>(collection => ConnectionMultiplexer.Connect(Configuration.GetConnectionString("Redis")));
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IRedisKeyResolver keyResolver, IDistributedCache cache, IConnectionMultiplexer redis)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.UseSession();

			app.Use(async (context, next) =>
			{
				var tempSessionKey = Keys.LastRequestTime + "Temp";

				await context.Session.LoadAsync();
				context.Session.SetString(Keys.LastRequestTime, context.Session.GetString(tempSessionKey) ?? string.Empty);
				context.Session.SetString(tempSessionKey, DateTime.Now.ToString());
				await context.Session.CommitAsync();

				await next.Invoke();
			});

			app.Use(async (context, next) =>
			{
				await next.Invoke();

				await cache.SetStringAsync(
					keyResolver.GetKeyWithPrefix(Keys.LastRequestTime),
					DateTime.Now.ToString(),
					new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(1) });
			});

			app.Use(async (context, next) =>
			{
				await next.Invoke();

				var db = redis.GetDatabase();
				await db.StringIncrementAsync(new RedisKey("Counter"), flags: CommandFlags.FireAndForget);
			});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
			});
		}
	}
}
