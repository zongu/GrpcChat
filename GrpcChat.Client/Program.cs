
namespace GrpcChat.Client
{
    using System;
    using Autofac;
    using GrpcChat.Client.Applibs;
    using GrpcChat.Client.GrpcClient;
    using GrpcChat.Client.Model.Command;
    using NLog;

    class Program
    {
        static void Main(string[] args)
        {
            using (var scope = AutofacConfig.Container.BeginLifetimeScope())
            {
                var logger = scope.Resolve<ILogger>();
                var grpcClient = scope.Resolve<IGrpcClient>();
                grpcClient.StartAsync();

                try
                {
                    var cmd = string.Empty;

                    while (cmd.ToLower() != "5")
                    {
                        Console.Clear();

                        switch (cmd)
                        {
                            case "1":
                            case "2":
                            case "3":
                            case "4":
                                var commander = scope.ResolveNamed<ICommand>(cmd);
                                commander.Execute();
                                break;
                            default:
                                break;
                        }

                        Console.WriteLine("1. Generate Member");
                        Console.WriteLine("2. Find Member");
                        Console.WriteLine("3. Get All Member");
                        Console.WriteLine("4. Let`s Chat");
                        Console.WriteLine("5. Exist");
                        cmd = Console.ReadLine();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            Console.Write("Finished");
            Console.Read();
        }
    }
}
