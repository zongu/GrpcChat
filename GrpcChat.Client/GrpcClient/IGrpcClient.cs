
namespace GrpcChat.Client.GrpcClient
{
    using System.Threading.Tasks;
    using Grpc.Core;

    public interface IGrpcClient
    {
        Task EndAsync();

        Task SendAction(object action);

        Task StartAsync();
    }
}
