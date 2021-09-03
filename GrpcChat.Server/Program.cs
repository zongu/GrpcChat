
namespace GrpcChat.Server
{
    using Autofac.Extensions.DependencyInjection;
    using GrpcChat.Server.Applibs;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using NLog.Web;

    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseUrls(ConfigHelper.ServiceUrl)
                        .UseStartup<Startup>();
                })
                .UseNLog(); // setup nlog di
    }
}
