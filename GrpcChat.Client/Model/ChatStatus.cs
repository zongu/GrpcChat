
namespace GrpcChat.Client.Model
{
    public class ChatStatus : IChatStatus
    {
        private bool online = false;

        public bool Online 
        {
            get 
            {
                return this.online;
            }
            set
            {
                this.online = value;
            }
        }
    }
}
