
namespace GrpcChat.Server.Command
{
    using System;
    using System.Threading.Tasks;
    using Google.Protobuf.WellKnownTypes;
    using Grpc.Core;
    using GrpcChat.Domain.Repository;
    using GrpcChat.Model;
    using GrpcChat.Service;
    using Microsoft.Extensions.Logging;

    public class MemberCommand : MemberService.MemberServiceBase
    {
        private ILogger<MemberCommand> logger;

        private IMemberRepository repo;

        public MemberCommand(ILogger<MemberCommand> logger, IMemberRepository repo)
        {
            this.logger = logger;
            this.repo = repo;
        }

        public override async Task<FindResponse> Find(FindRequest request, ServerCallContext context)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var findResult = this.repo.Find(request.Account);

                    if (findResult.exception != null)
                    {
                        throw findResult.exception;
                    }

                    return new FindResponse()
                    {
                        Member = findResult.member
                    };
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"{this.GetType().Name} Find Exception");
                    throw;
                }
            });
        }

        public override async Task<GetAllResponse> GetAll(Empty request, ServerCallContext context)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var getResult = this.repo.GetAll();

                    if (getResult.exception != null)
                    {
                        throw getResult.exception;
                    }

                    var result = new GetAllResponse();
                    result.Members.Add(getResult.members);

                    return result;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"{this.GetType().Name} GetAll Exception");
                    throw;
                }
            });
        }

        public override async Task<Member> Insert(Member request, ServerCallContext context)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var insertResult = this.repo.Insert(request);

                    if (insertResult != null)
                    {
                        throw insertResult;
                    }

                    return request;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"{this.GetType().Name} Insert Exception");
                    throw;
                }
            });
        }
    }
}
