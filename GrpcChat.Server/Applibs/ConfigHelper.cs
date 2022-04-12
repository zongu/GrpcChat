
namespace GrpcChat.Server.Applibs
{
    using System;
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

        public static IServiceProvider ServiceProvider;

        /// <summary>
        /// 服務PORT
        /// </summary>
        public static readonly int ServicePort = 8085;

        /// <summary>
        /// 服務位址
        /// </summary>
        public static readonly string ServiceUrl = $"http://*:{ServicePort}";

        /// <summary>
        /// mongodb連線字串
        /// </summary>
        public static readonly string MongoDBConn = Config["MongoDBConn"];

        /// <summary>
        /// Redis前贅詞
        /// </summary>
        public static readonly string RedisAffixKey = Config["RedisAffixKey"];

        /// <summary>
        /// REDIS DB
        /// </summary>
        public static readonly int RedisDataBase = Convert.ToInt32(Config["RedisDataBase"]);

        /// <summary>
        /// Redis連線字串
        /// </summary>
        public static readonly string RedisConn = Config["Redis"];
    }
}
