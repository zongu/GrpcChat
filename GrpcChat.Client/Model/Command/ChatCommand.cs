
namespace GrpcChat.Client.Model.Command
{
    using System;
    using GrpcChat.Client.GrpcClient;
    using GrpcChat.Domain.Model;
    using GrpcChat.Service;
    using NetCoreGrpc.Action;
    using NetCoreGrpc.Model;
    using NLog;

    public class ChatCommand : ICommand
    {
        private ILogger logger;

        private MemberService.MemberServiceClient svc;

        private IChatStatus chatStatus;

        private IGrpcClient grpcClient;

        public ChatCommand(ILogger logger, MemberService.MemberServiceClient svc, IChatStatus chatStatus, IGrpcClient grpcClient)
        {
            this.logger = logger;
            this.svc = svc;
            this.chatStatus = chatStatus;
            this.grpcClient = grpcClient;
        }

        public bool Execute()
        {
            try
            {
                Console.Write("Account:");
                var account = Console.ReadLine();

                var findResult = this.svc.Find(new FindRequest()
                {
                    Account = account
                });

                if (findResult.Member == null)
                {
                    Console.WriteLine($"{account} not exist");
                    return true;
                }

                this.chatStatus.Online = true;
                var cmd = string.Empty;

                while (cmd.ToLower() != "exit")
                {
                    if (!string.IsNullOrEmpty(cmd))
                    {
                        this.grpcClient.SendAction(new ChatMessageAction()
                        {
                            ChatMessage = new ChatMessage()
                            {
                                NickName = findResult.Member.NickName,
                                Message = cmd,
                                CreateDateTimeStamp = TimeStampHelper.ToUtcTimeStamp(DateTime.Now)
                            }
                        });
                    }

                    cmd = Console.ReadLine();
                }

                this.chatStatus.Online = true;
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
