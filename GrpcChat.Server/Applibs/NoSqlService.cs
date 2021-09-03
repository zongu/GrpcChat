﻿
namespace GrpcChat.Server.Applibs
{
    using System;
    using Microsoft.Extensions.Logging;
    using MongoDB.Driver;
    using StackExchange.Redis;

    internal static class NoSqlService
    {
        private static ILogger logger = new LoggerFactory().CreateLogger("NoSqlService");

        private static Lazy<ConnectionMultiplexer> lazyRedisConnections;

        public static ConnectionMultiplexer RedisConnections
        {
            get
            {
                if (lazyRedisConnections == null)
                {
                    var options = ConfigurationOptions.Parse(ConfigHelper.RedisConn);
                    options.AbortOnConnectFail = false;

                    var muxer = ConnectionMultiplexer.Connect(options);
                    muxer.ConnectionFailed += (sender, e) =>
                    {
                        logger.LogError("redis failed: " + EndPointCollection.ToString(e.EndPoint) + "/" + e.ConnectionType);
                    };
                    muxer.ConnectionRestored += (sender, e) =>
                    {
                        logger.LogError("redis restored: " + EndPointCollection.ToString(e.EndPoint) + "/" + e.ConnectionType);
                    };

                    return muxer;
                }

                return lazyRedisConnections.Value;
            }
        }

        public static string RedisAffixKey
        {
            get
            {
                return ConfigHelper.RedisAffixKey;
            }
        }

        public static int RedisDataBase
        {
            get
            {
                return ConfigHelper.RedisDataBase;
            }
        }

        private static Lazy<MongoClient> lazyMongoConnetion;

        public static MongoClient MongoConnetion
        {
            get
            {
                if (lazyMongoConnetion == null)
                {
                    lazyMongoConnetion = new Lazy<MongoClient>(() =>
                    {
                        return new MongoClient(ConfigHelper.MongoDBConn);
                    });
                }

                return lazyMongoConnetion.Value;
            }
        }
    }
}
