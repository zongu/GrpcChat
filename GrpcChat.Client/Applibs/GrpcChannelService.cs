
namespace GrpcChat.Client.Applibs
{
    using System;
    using Grpc.Net.Client;

    internal static class GrpcChannelService
    {
        private static Lazy<GrpcChannel> lazyGrpcChannel;

        public static GrpcChannel GrpcChannel
        {
            get
            {
                if (lazyGrpcChannel == null)
                {
                    lazyGrpcChannel = new Lazy<GrpcChannel>(() =>
                    {
                        return GrpcChannel.ForAddress(ConfigHelper.GrpcServiceUrl);
                    });
                }

                return lazyGrpcChannel.Value;
            }
        }
    }
}
