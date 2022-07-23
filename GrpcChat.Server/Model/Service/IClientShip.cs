
namespace GrpcChat.Server.Model.Service
{
    using Google.Protobuf;
    using Grpc.Core;
    using GrpcChat.Service;

    public interface IClientShip
    {
        void Brocast(IMessage action);

        void BrocastScaleOut(ActionModel actionModel);

        void Send(string id, IMessage action);

        bool TryAdd(string id, IServerStreamWriter<ActionModel> caller);

        bool TryRemove(string id);
    }
}
