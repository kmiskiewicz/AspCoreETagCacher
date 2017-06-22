using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspCoreETagCacher.Attribiutes;
using AspCoreETagCacher.Middleware;
using AspCoreETagCacher.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SampleWebApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {


            ///////////////////////////////////////////////////////////////////////////////////
            //Cache config start
            var redisConnection = "your redis connection"; //for example your_host.redis.cache.windows.net,defaultDatabase=2,password=your_password",
            services.AddSingleton<IDistributedCache>(factory =>
            {
                var cache = new RedisCache(new RedisCacheOptions
                {
                    Configuration = redisConnection
                });

                return cache;
            });
            services.AddSingleton<ICacheRedisService, RedisCacheService>();
            services.AddSingleton<IMemoryCache>(factory =>
            {
                var cache = new MemoryCache(new MemoryCacheOptions());
                return cache;
            });
            services.AddSingleton<ICacheMemoryService, MemoryCacheService>();
          //  services.AddTransient<CacheAttribute>();
            //Cache config end

            ///////////////////////////////////////////////////////////////////////////////////
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            ///////ADD E TAG MIDDLEWARE/////////////////////////////////////////////////////////////
            //Purpose of CacheMiddleware is to intercept request, 
            //and provide custom stream to which further middlewares could write (and read).
            //When the middleware invocation process will return to CacheMiddleware,
            //this stream's content will be copied to original response stream. 
            app.UseMiddleware<CacheETagMiddleware>();
            ////////////////////////////////////////////////////////////////////////////////
            app.UseMvc();
        }
    }
}
