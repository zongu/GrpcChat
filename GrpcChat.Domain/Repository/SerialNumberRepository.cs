
namespace GrpcChat.Domain.Repository
{
    using System;
    using StackExchange.Redis;

    public class SerialNumberRepository : ISerialNumberRepository
    {

        private readonly ConnectionMultiplexer conn;

        private readonly string affixKey;

        private readonly int dataBase;

        public SerialNumberRepository(ConnectionMultiplexer conn, string affixKey, int dataBase)
        {
            this.conn = conn;
            this.affixKey = affixKey;
            this.dataBase = dataBase;
        }

        public (Exception exception, long sn) GetSerialNumber()
        {
            try
            {
                var sn = this.UseConnection(redis =>
                {
                    var keys = new RedisKey[] { $"{this.affixKey}:SerialNumber" };
                    string script =
                    @"
                        return redis.call('INCRBY', KEYS[1], 1)
                    ";

                    return (long)redis.ScriptEvaluate(script, keys);
                });

                return (null, sn);
            }
            catch (Exception ex)
            {
                return (ex, -1);
            }
        }

        private T UseConnection<T>(Func<IDatabase, T> func)
        {
            var redis = conn.GetDatabase(dataBase);
            return func(redis);
        }
    }
}
