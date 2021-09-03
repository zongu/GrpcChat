
namespace GrpcChat.Server.Command
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac.Features.Indexed;
    using Grpc.Core;
    using GrpcChat.Server.Model;
    using GrpcChat.Server.Model.Service;
    using GrpcChat.Service;
    using Microsoft.Extensions.Logging;

    public class BidirectionalCommand : BidirectionalService.BidirectionalServiceBase
    {
        private ILogger<BidirectionalCommand> logger;

        private IClientShip clientShip;

        private IIndex<string, IActionHandler> handlerSets;

        public BidirectionalCommand(ILogger<BidirectionalCommand> logger, IClientShip clientShip, IIndex<string, IActionHandler> handlerSets)
        {
            this.logger = logger;
            this.clientShip = clientShip;
            this.handlerSets = handlerSets;
        }

        public override async Task ActionAsync(IAsyncStreamReader<ActionModel> requestStream, IServerStreamWriter<ActionModel> responseStream, ServerCallContext context)
        {
            var id = context.RequestHeaders.FirstOrDefault(p => p.Key == "id")?.Value ?? Guid.NewGuid().ToString();
            this.clientShip.TryAdd(id, responseStream);

            await Task.Run(async () =>
            {
                try
                {
                    await foreach (var action in requestStream.ReadAllAsync())
                    {
                        if (this.handlerSets.TryGetValue(action.Action.ToLower(), out var handler))
                        {
                            handler.Execute(action);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"{this.GetType().Name} ActionAsync Exception");
                    // client 斷線
                    this.clientShip.TryRemove(id);
                }
            });

            // client 結束通訊
            this.clientShip.TryRemove(id);
        }
    }
}
