
namespace GrpcChat.Server.ActionHandler
{
    using System;
    using GrpcChat.Server.Model;
    using GrpcChat.Server.Model.Service;
    using GrpcChat.Service;
    using Microsoft.Extensions.Logging;
    using NetCoreGrpc.Action;
    using Newtonsoft.Json;

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
                var action = JsonConvert.DeserializeObject<ChatMessageAction>(actionModel.Content);
                this.clientShip.Boradcast(action);
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
