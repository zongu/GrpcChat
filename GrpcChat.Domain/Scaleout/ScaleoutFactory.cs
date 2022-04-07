
namespace GrpcChat.Domain.Scaleout
{
    using System;
    using System.Text.Json;
    using GrpcChat.Service;
    using StackExchange.Redis;

    public static class ScaleoutFactory
    {
        private static string _affixKey;

        private static int _dataBase;

        private static ConnectionMultiplexer redisConn;

        private static ISubscriber redisSubscriber
        {
            get
            {
                if (redisConn == null)
                {
                    return null;
                }

                redisConn.GetDatabase(_dataBase);
                return redisConn.GetSubscriber();
            }
        }

        public static void Start(ConnectionMultiplexer conn, string affixKey, int dataBase, Action<ActionModel> callback)
        {
            if (redisConn != null)
            {
                return;
            }

            redisConn = conn;
            _affixKey = affixKey;
            _dataBase = dataBase;

            // subscribe
            {
                redisSubscriber.Subscribe($"{_affixKey}:Scaleout", (topic, message) =>
                {
                    var actionModel = JsonSerializer.Deserialize<ActionModel>(message.ToString());

                    if (actionModel != null)
                    {
                        callback(actionModel);
                    }
                });
            }
        }

        public static void Stop()
        {
            if (redisConn == null)
            {
                return;
            }

            redisConn.Close();
            redisConn.Dispose();
        }

        public static Exception Publish(ActionModel actionModel)
        {
            try
            {
                if (redisSubscriber == null)
                {
                    throw new Exception("RedisSubscriber is null");
                }

                redisSubscriber.Publish($"{_affixKey}:Scaleout", JsonSerializer.Serialize(actionModel));

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
