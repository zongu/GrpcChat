
namespace GrpcChat.Client.GrpcClient
{
    using System.Threading.Tasks;

    public interface IGrpcClient
    {
        Task StartAsync();
    }
}
