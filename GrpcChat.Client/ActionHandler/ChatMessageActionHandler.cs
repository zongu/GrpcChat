
namespace GrpcChat.Client.ActionHandler
{
    using System;
    using System.Text.Json;
    using GrpcChat.Client.Model;
    using GrpcChat.Domain.Model;
    using GrpcChat.Service;
    using NetCoreGrpc.Action;
    using NLog;

    public class ChatMessageActionHandler : IActionHandler
    {
        private ILogger logger;

        private IChatStatus chatStatus;

        public ChatMessageActionHandler(ILogger logger, IChatStatus chatStatus)
        {
            this.logger = logger;
            this.chatStatus = chatStatus;
        }

        public bool Execute(ActionModel actionModel)
        {
            try
            {
                if (chatStatus.Online)
                {
                    var action = JsonSerializer.Deserialize<ChatMessageAction>(actionModel.Content);
                    Console.WriteLine(string.Empty);
                    Console.WriteLine(
                        $"{action.ChatMessage.NickName} " +
                        $"{TimeStampHelper.ToLocalDateTime(action.ChatMessage.CreateDateTimeStamp).ToString("hh:mm:ss")}:" +
                        $"{action.ChatMessage.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, $"{this.GetType().Name} Execute Exception Content:{actionModel.Content}");
                return false;
            }
        }
    }
}
