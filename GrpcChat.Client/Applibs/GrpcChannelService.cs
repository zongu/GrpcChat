
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
                        var httpHandler = new HttpClientHandler();
                        // Return `true` to allow certificates that are untrusted/invalid
                        httpHandler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                        return GrpcChannel.ForAddress(ConfigHelper.GrpcServiceUrl, new GrpcChannelOptions { HttpHandler = httpHandler });
                    });
                }

                return lazyGrpcChannel.Value;
            }
        }
    }
}
