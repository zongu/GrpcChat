using Autofac;
using Autofac.Extensions.DependencyInjection;
using GrpcChat.Domain.Repository;
using GrpcChat.Domain.Scaleout;
using GrpcChat.Server.Applibs;
using GrpcChat.Server.Command;
using GrpcChat.Server.Model;
using GrpcChat.Server.Model.Service;
using NLog.Extensions.Logging;
using NLog.Web;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// nlog
builder.Host.UseNLog();

builder.WebHost.ConfigureKestrel(options =>
{
    // http/2 listen specify port with tls
    options.ListenAnyIP(ConfigHelper.ServicePort, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// grpc
builder.Services.AddGrpc();

// listen url
builder.WebHost.UseUrls(ConfigHelper.ServiceUrl);

// autofac
{
    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

    builder.Host.ConfigureContainer<ContainerBuilder>(_builder =>
    {
        var asm = Assembly.GetExecutingAssembly();

        // 指定處理client指令的handler
        _builder.RegisterAssemblyTypes(asm)
            .Where(t => t.IsAssignableTo<IActionHandler>())
            .Named<IActionHandler>(t => t.Name.Replace("Handler", string.Empty).ToLower())
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
            .SingleInstance();

        _builder.RegisterType<MemberRepository>()
            .WithParameter("mongoClient", NoSqlService.MongoConnetion)
            .As<IMemberRepository>()
            .SingleInstance();

        _builder.RegisterType<SerialNumberRepository>()
            .WithParameter("conn", NoSqlService.RedisConnections)
            .WithParameter("affixKey", NoSqlService.RedisAffixKey)
            .WithParameter("dataBase", NoSqlService.RedisDataBase)
            .As<ISerialNumberRepository>()
            .SingleInstance();

        _builder.RegisterType<ClientShip>()
            .As<IClientShip>()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
            .SingleInstance();
    });
}

var app = builder.Build();
// generate service provider
ConfigHelper.ServiceProvider = app.Services;

// Configure the HTTP request pipeline.
app.MapGrpcService<MemberCommand>();
app.MapGrpcService<BidirectionalCommand>();

// nlog認appsetting設定
NLog.LogManager.Configuration = new NLogLoggingConfiguration(ConfigHelper.Config.GetSection("NLog"));

// scaleOut
{
    var clientShip = ConfigHelper.ServiceProvider.GetRequiredService<IClientShip>();

    ScaleoutFactory.Start(
        NoSqlService.RedisConnections,
        NoSqlService.RedisAffixKey,
        NoSqlService.RedisDataBase,
        clientShip.BrocastScaleOut);
}

app.Run();
