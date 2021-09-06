
namespace GrpcChat.Client.Applibs
{
    using System.Linq;
    using System.Reflection;
    using Autofac;
    using GrpcChat.Client.GrpcClient;
    using GrpcChat.Client.Model;
    using GrpcChat.Client.Model.Command;
    using GrpcChat.Service;
    using NLog;
    using NLog.Extensions.Logging;

    internal static class AutofacConfig
    {
        private static IContainer container;

        public static IContainer Container
        {
            get
            {
                if (container == null)
                {
                    Register();
                }

                return container;
            }
        }

        private static void Register()
        {
            var builder = new ContainerBuilder();
            var asm = Assembly.GetExecutingAssembly();

            // 指定處理client指令的handler
            builder.RegisterAssemblyTypes(asm)
                .Where(t => t.IsAssignableTo<IActionHandler>())
                .Named<IActionHandler>(t => t.Name.Replace("Handler", string.Empty).ToLower())
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            LogManager.Configuration = new NLogLoggingConfiguration(ConfigHelper.Config.GetSection("NLog"));
            builder.Register<Logger>(p => NLog.Web.NLogBuilder.ConfigureNLog(LogManager.Configuration).GetCurrentClassLogger())
                .As<ILogger>()
                .SingleInstance();

            builder.RegisterType<MemberService.MemberServiceClient>()
                .WithParameter("channel", GrpcChannelService.GrpcChannel)
                .SingleInstance();

            builder.RegisterType<BidirectionalService.BidirectionalServiceClient>()
                .WithParameter("channel", GrpcChannelService.GrpcChannel)
                .SingleInstance();

            builder.RegisterType<GrpcChatClient>()
                .As<IGrpcClient>()
                .SingleInstance();

            builder.RegisterType<GenerateMemberCommand>()
                .Named<ICommand>("1")
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<FindMemberCommand>()
                .Named<ICommand>("2")
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<GetAllMemberCommand>()
                .Named<ICommand>("3")
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<ChatCommand>()
                .Named<ICommand>("4")
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            builder.RegisterType<ChatStatus>()
                .As<IChatStatus>()
                .SingleInstance();

            container = builder.Build();
        }
    }
}
