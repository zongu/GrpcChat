
namespace GrpcChat.Client.Applibs
{
    using System.IO;
    using Microsoft.Extensions.Configuration;

    internal static class ConfigHelper
    {
        private static IConfiguration _config;

        public static IConfiguration Config
        {
            get
            {
                if (_config == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables();

                    _config = builder.Build();
                }

                return _config;
            }
        }

        /// <summary>
        /// gRPC service url
        /// </summary>
        public static readonly string GrpcServiceUrl = Config["GrpcServiceUrl"];
    }
}
