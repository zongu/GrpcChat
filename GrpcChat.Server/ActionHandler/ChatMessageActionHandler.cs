
namespace GrpcChat.Server.ActionHandler
{
    using System;
    using System.Text.Json;
    using GrpcChat.Server.Model;
    using GrpcChat.Server.Model.Service;
    using GrpcChat.Service;
    using Microsoft.Extensions.Logging;
    using NetCoreGrpc.Action;

    public class ChatMessageActionHandler : IActionHandler
    {
        private ILogger<ChatMessageActionHandler> logger;

        private IClientShip clientShip;

        public ChatMessageActionHandler(ILogger<ChatMessageActionHandler> logger, IClientShip clientShip)
        {
            this.logger = logger;
            this.clientShip = clientShip;
        }

        public bool Execute(ActionModel actionModel)
        {
            try
            {
                var action = ChatMessageAction.Parser.ParseFrom(actionModel.Content);

                if (action == null)
                {
                    throw new Exception("action is null");
                }

                this.clientShip.Brocast(action);
                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} Execute Exception Content:{actionModel.Content}");
                return false;
            }
        }
    }
}
