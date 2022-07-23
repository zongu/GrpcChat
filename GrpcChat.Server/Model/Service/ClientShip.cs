
namespace GrpcChat.Server.Model.Service
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Google.Protobuf;
    using Grpc.Core;
    using GrpcChat.Domain.Repository;
    using GrpcChat.Domain.Scaleout;
    using GrpcChat.Service;
    using Microsoft.Extensions.Logging;

    public class ClientShip : IClientShip
    {
        private ConcurrentDictionary<string, IServerStreamWriter<ActionModel>> dic;

        private ILogger<ClientShip> logger;

        private ISerialNumberRepository repo;

        public ClientShip(ILogger<ClientShip> logger, ISerialNumberRepository repo)
        {
            this.dic = new ConcurrentDictionary<string, IServerStreamWriter<ActionModel>>();
            this.logger = logger;
            this.repo = repo;
        }

        public void Brocast(IMessage action)
        {
            try
            {
                var snResult = this.repo.GetSerialNumber();

                if (snResult.exception != null)
                {
                    throw snResult.exception;
                }

                var publishResult = ScaleoutFactory.Publish(new ActionModel()
                {
                    Action = action.GetType().Name,
                    SerialNumber = snResult.sn,
                    Content = action.ToByteString()
                });

                if (publishResult != null)
                {
                    throw publishResult;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} BroadCast Exception");
            }
        }

        public void BrocastScaleOut(ActionModel actionModel)
        {
            try
            {
                var tasks = this.dic.Select(obj =>
                {
                    return Task.Run(() =>
                    {
                        lock (obj.Key)
                        {
                            try
                            {
                                obj.Value.WriteAsync(actionModel);
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError(ex, $"{this.GetType().Name} Broadcast exception id:{obj.Key}, content:{actionModel.Content}");
                            }
                        }
                    });
                }).ToArray();

                Task.WaitAll(tasks);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} BrocastScaleOut Exception");
            }
        }

        public void Send(string id, IMessage action)
        {
            try
            {
                var snResult = this.repo.GetSerialNumber();

                if (snResult.exception != null)
                {
                    throw snResult.exception;
                }

                if (this.dic.TryGetValue(id, out var client))
                {
                    lock (id)
                    {
                        try
                        {
                            client.WriteAsync(new ActionModel()
                            {
                                Action = action.GetType().Name,
                                SerialNumber = snResult.sn,
                                Content = action.ToByteString()
                            });
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, $"{this.GetType().Name} Send exception id:{id}, content:{JsonSerializer.Serialize(action)}");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{this.GetType().Name} Send Exception");
            }
        }

        public bool TryAdd(string id, IServerStreamWriter<ActionModel> caller)
        {
            return this.dic.TryAdd(id, caller);
        }

        public bool TryRemove(string id)
        {
            return this.dic.TryRemove(id, out var client);
        }
    }
}
