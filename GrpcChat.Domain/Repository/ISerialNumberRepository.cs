
namespace GrpcChat.Domain.Repository
{
    using System;

    public interface ISerialNumberRepository
    {
        (Exception exception, long sn) GetSerialNumber();
    }
}
