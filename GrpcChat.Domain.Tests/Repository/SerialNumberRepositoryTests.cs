

namespace GrpcChat.Domain.Tests.Repository
{
    using System.Linq;
    using GrpcChat.Domain.Repository;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using StackExchange.Redis;

    [TestClass]
    public class SerialNumberRepositoryTests
    {
        private ISerialNumberRepository repo;

        const string redisConn = @"localhost:6379";

        const string affixKey = "GrpcChat";

        const int dataBase = 0;

        [TestInitialize]
        public void Init()
        {
            var conn = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(redisConn));
            var redis = conn.GetDatabase(dataBase);
            this.repo = new SerialNumberRepository(conn, affixKey, dataBase);

            var keys = conn.GetServer(redisConn).Keys(dataBase, $"{affixKey}*", 10, CommandFlags.None).ToList();
            keys.ForEach(key => redis.KeyDelete(key));
        }

        [TestMethod]
        public void 取流水號測試()
        {
            var getResult = this.repo.GetSerialNumber();

            Assert.IsNull(getResult.exception);
            Assert.AreEqual(getResult.sn, 1);

            getResult = this.repo.GetSerialNumber();

            Assert.IsNull(getResult.exception);
            Assert.AreEqual(getResult.sn, 2);
        }
    }
}
