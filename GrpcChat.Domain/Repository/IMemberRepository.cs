
namespace GrpcChat.Domain.Repository
{
    using System;
    using System.Collections.Generic;
    using GrpcChat.Model;

    public interface IMemberRepository
    {
        (Exception exception, Member member) Find(string account);

        (Exception exception, IEnumerable<Member> members) GetAll();

        Exception Insert(Member member);
    }
}
