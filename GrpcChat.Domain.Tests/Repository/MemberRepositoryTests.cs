
namespace GrpcChat.Domain.Tests.Repository
{
    using System;
    using System.Linq;
    using GrpcChat.Domain.Model;
    using GrpcChat.Domain.Repository;
    using GrpcChat.Model;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MongoDB.Driver;

    [TestClass]
    public class MemberRepositoryTests
    {
        const string mongoConn = @"mongodb://localhost:27017";

        private IMemberRepository repo;

        [TestInitialize]
        public void Init()
        {
            var client = new MongoClient(mongoConn);
            var db = client.GetDatabase("GrpcChat");
            db.DropCollection("Member");

            this.repo = new MemberRepository(client);
        }

        [TestMethod]
        public void 新增資料測試()
        {
            var insertResult = this.repo.Insert(new Member()
            {
                MemberId = Guid.NewGuid().ToString(),
                Account = "TEST001",
                NickName = "NickName001",
                CreateDateTimeStamp = TimeStampHelper.ToUtcTimeStamp(DateTime.Now)
            });

            Assert.IsNull(insertResult);
        }

        [TestMethod]
        public void 查找指定ID資料測試()
        {
            var insertResult = this.repo.Insert(new Member()
            {
                MemberId = Guid.NewGuid().ToString(),
                Account = "TEST001",
                NickName = "NickName001",
                CreateDateTimeStamp = TimeStampHelper.ToUtcTimeStamp(DateTime.Now)
            });

            Assert.IsNull(insertResult);

            var findResult = this.repo.Find("TEST002");

            Assert.IsNull(findResult.exception);
            Assert.IsNull(findResult.member);

            findResult = this.repo.Find("TEST001");

            Assert.IsNull(findResult.exception);
            Assert.IsNotNull(findResult.member);
            Assert.AreEqual(findResult.member.NickName, "NickName001");
        }

        [TestMethod]
        public void 取得所有資料測試()
        {
            var insertResult = this.repo.Insert(new Member()
            {
                MemberId = Guid.NewGuid().ToString(),
                Account = "TEST001",
                NickName = "NickName001",
                CreateDateTimeStamp = TimeStampHelper.ToUtcTimeStamp(DateTime.Now)
            });

            Assert.IsNull(insertResult);

            var getResult = this.repo.GetAll();

            Assert.IsNull(getResult.exception);
            Assert.IsNotNull(getResult.members);
            Assert.AreEqual(getResult.members.Count(), 1);

            insertResult = this.repo.Insert(new Member()
            {
                MemberId = Guid.NewGuid().ToString(),
                Account = "TEST002",
                NickName = "NickName002",
                CreateDateTimeStamp = TimeStampHelper.ToUtcTimeStamp(DateTime.Now)
            });

            Assert.IsNull(insertResult);

            getResult = this.repo.GetAll();

            Assert.IsNull(getResult.exception);
            Assert.IsNotNull(getResult.members);
            Assert.AreEqual(getResult.members.Count(), 2);
        }
    }
}
