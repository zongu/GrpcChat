
namespace GrpcChat.Client.Model.Command
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using GrpcChat.Service;
    using NLog;

    public class GetAllMemberCommand : ICommand
    {
        private ILogger logger;

        private MemberService.MemberServiceClient svc;

        public GetAllMemberCommand(ILogger logger, MemberService.MemberServiceClient svc)
        {
            this.logger = logger;
            this.svc = svc;
        }

        public bool Execute()
        {
            this.logger.Trace($"{this.GetType().Name} Execute");

            try
            {
                var getResult = this.svc.GetAll(new Google.Protobuf.WellKnownTypes.Empty());
                Console.WriteLine(string.Join("\r\n", getResult.Members.Select(m => JsonSerializer.Serialize(m))));
                Console.Read();
                Console.Clear();
                return true;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} Execute");
                return false;
            }
        }
    }
}
