
namespace GrpcChat.Client.Applibs
{
    using System.Linq;
    using System.Reflection;
    using Autofac;
    using GrpcChat.Client.Model;

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

            //builder.RegisterType<Logfactory>

            container = builder.Build();
        }
    }
}
