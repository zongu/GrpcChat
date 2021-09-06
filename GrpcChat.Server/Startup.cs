
namespace GrpcChat.Server
{
    using System.Reflection;
    using Autofac;
    using GrpcChat.Domain.Repository;
    using GrpcChat.Server.Applibs;
    using GrpcChat.Server.Command;
    using GrpcChat.Server.Model;
    using GrpcChat.Server.Model.Service;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using NLog;
    using NLog.Extensions.Logging;

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                // 繫結gRPC service
                endpoints.MapGrpcService<MemberCommand>();
                endpoints.MapGrpcService<BidirectionalCommand>();
            });

            // nlog認appsetting設定
            NLog.Config.LoggingConfiguration nlogConfig = new NLogLoggingConfiguration(ConfigHelper.Config.GetSection("NLog"));
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            var asm = Assembly.GetExecutingAssembly();

            // 指定處理client指令的handler
            builder.RegisterAssemblyTypes(asm)
                .Where(t => t.IsAssignableTo<IActionHandler>())
                .Named<IActionHandler>(t => t.Name.Replace("Handler", string.Empty).ToLower())
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<MemberRepository>()
                .WithParameter("mongoClient", NoSqlService.MongoConnetion)
                .As<IMemberRepository>()
                .SingleInstance();

            builder.RegisterType<SerialNumberRepository>()
                .WithParameter("conn", NoSqlService.RedisConnections)
                .WithParameter("affixKey", NoSqlService.RedisAffixKey)
                .WithParameter("dataBase", NoSqlService.RedisDataBase)
                .As<ISerialNumberRepository>()
                .SingleInstance();

            builder.RegisterType<ClientShip>()
                .As<IClientShip>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();
        }
    }
}
