
namespace GrpcChat.Client.GrpcClient
{
    using System.Threading.Tasks;
    using Google.Protobuf;

    public interface IGrpcClient
    {
        Task EndAsync();

        Task SendAction(IMessage action);

        Task StartAsync();
    }
}
