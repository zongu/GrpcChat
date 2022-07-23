
namespace GrpcChat.Client.GrpcClient
{
    using Autofac.Features.Indexed;
    using Google.Protobuf;
    using Grpc.Core;
    using GrpcChat.Client.Model;
    using GrpcChat.Service;
    using NLog;
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    

    public class GrpcChatClient : IGrpcClient
    {
        private ILogger logger;

        private BidirectionalService.BidirectionalServiceClient grpcClient;

        private IIndex<string, IActionHandler> handlerSets;

        private AsyncDuplexStreamingCall<ActionModel, ActionModel> streamCall;

        private bool finished = false;

        public GrpcChatClient(
            ILogger logger,
            BidirectionalService.BidirectionalServiceClient grpcClient,
            IIndex<string, IActionHandler> handlerSets)
        {
            this.logger = logger;
            this.grpcClient = grpcClient;
            this.handlerSets = handlerSets;
        }

        public async Task EndAsync()
        {
            if (this.streamCall != null)
            {
                try
                {
                    await this.streamCall.RequestStream.CompleteAsync();
                    this.streamCall.Dispose();
                    this.streamCall = null;
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, $"{this.GetType().Name} EndAsync Exception");
                }
            }

            this.finished = true;
        }

        public async Task SendAction(IMessage action) 
        {
            if (this.streamCall != null)
            {
                var request = new ActionModel()
                {
                    Action = action.GetType().Name,
                    Content = action.ToByteString()
                };

                this.logger.Trace($"{this.GetType().Name} Request:{request.Content}");

                try
                {
                    await this.streamCall.RequestStream.WriteAsync(request);
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, $"{this.GetType().Name} SendAction Exception");
                }
            }
        }

        public async Task StartAsync()
        {
            while (!finished)
            {
                try
                {
                    var headers = new Metadata { new Metadata.Entry("id", Guid.NewGuid().ToString()) };
                    this.streamCall = this.grpcClient.ActionAsync(new CallOptions(headers));

                    await foreach (var action in this.streamCall.ResponseStream.ReadAllAsync())
                    {
                        if (this.handlerSets.TryGetValue(action.Action.ToLower(), out var handler))
                        {
                            this.logger.Trace($"{this.GetType().Name} Call Content:{action.Content}");
                            handler.Execute(action);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, $"{this.GetType().Name} StartAsync Exception");
                    this.streamCall = null;
                    SpinWait.SpinUntil(() => false, 10000);
                }
            }
        }
    }
}
