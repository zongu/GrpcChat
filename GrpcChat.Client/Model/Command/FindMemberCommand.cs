
namespace GrpcChat.Client.Model.Command
{
    using System;
    using System.Text.Json;
    using GrpcChat.Service;
    using NLog;

    public class FindMemberCommand : ICommand
    {
        private ILogger logger;

        private MemberService.MemberServiceClient svc;

        public FindMemberCommand(ILogger logger, MemberService.MemberServiceClient svc)
        {
            this.logger = logger;
            this.svc = svc;
        }

        public bool Execute()
        {
            this.logger.Trace($"{this.GetType().Name} Execute");

            try
            {
                Console.Write("Account:");
                var account = Console.ReadLine();

                var findResult = this.svc.Find(new FindRequest()
                {
                    Account = account
                });

                Console.WriteLine(JsonSerializer.Serialize(findResult));
                Console.Read();
                Console.Clear();

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
