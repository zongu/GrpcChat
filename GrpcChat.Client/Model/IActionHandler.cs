
namespace GrpcChat.Client.Model
{
    using GrpcChat.Service;

    public interface IActionHandler
    {
        bool Execute(ActionModel actionModel);
    }
}
