
namespace GrpcChat.Domain.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GrpcChat.Model;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;

    public class MemberRepository : IMemberRepository
    {
        private const string dbName = "GrpcChat";

        private const string collectionName = "Member";

        private MongoClient client { get; set; }
        private IMongoDatabase db { get; set; }
        private IMongoCollection<Member> collection { get; set; }

        static MemberRepository()
        {
            BsonClassMap.RegisterClassMap<Member>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.MapIdMember(p => p.MemberId);
            });
        }

        public MemberRepository(MongoClient mongoClient)
        {
            this.client = mongoClient;
            this.db = this.client.GetDatabase(dbName);
            this.collection = this.db.GetCollection<Member>(collectionName);
        }


        public (Exception exception, Member member) Find(string account)
        {
            try
            {
                var filter = Builders<Member>.Filter.Eq(p => p.Account, account);
                var result = this.collection.Find(filter).FirstOrDefault();
                return (null, result);
            }
            catch (Exception ex)
            {
                return (ex, null);
            }
        }

        public (Exception exception, IEnumerable<Member> members) GetAll()
        {
            try
            {
                var filter = Builders<Member>.Filter.Empty;
                var result = this.collection.Find(filter).ToList();
                return (null, result);
            }
            catch (Exception ex)
            {
                return (ex, null);
            }
        }

        public Exception Insert(Member member)
        {
            try
            {
                this.collection.InsertOne(member);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
