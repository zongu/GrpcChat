
namespace GrpcChat.Client.Model.Command
{
    using System;
    using GrpcChat.Domain.Model;
    using GrpcChat.Model;
    using GrpcChat.Service;
    using NLog;

    public class GenerateMemberCommand : ICommand
    {
        private ILogger logger;

        private MemberService.MemberServiceClient svc;

        public GenerateMemberCommand(ILogger logger, MemberService.MemberServiceClient svc)
        {
            this.logger = logger;
            this.svc = svc;
        }

        public bool Execute()
        {
            this.logger.Trace($"{this.GetType().Name} Execut");

            try
            {
                Console.Write("Account:");
                var account = Console.ReadLine();
                Console.WriteLine("NickName:");
                var nickName = Console.ReadLine();

                var insertResult = this.svc.Insert(new Member()
                {
                    MemberId = Guid.NewGuid().ToString(),
                    Account = account,
                    NickName = nickName,
                    CreateDateTimeStamp = TimeStampHelper.ToUtcTimeStamp(DateTime.Now)
                });

                Console.WriteLine("Generate Member Success");

                return true;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} Execute Exception");
                return false;
            }
        }
    }
}
