
namespace GrpcChat.Server.Model.Service
{
    using Grpc.Core;
    using GrpcChat.Service;

    public interface IClientShip
    {
        void Brocast(object action);

        void BrocastScaleOut(ActionModel actionModel);

        void Send(string id, object action);

        bool TryAdd(string id, IServerStreamWriter<ActionModel> caller);

        bool TryRemove(string id);
    }
}
