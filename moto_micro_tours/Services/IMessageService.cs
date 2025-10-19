using moto_micro_tours.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace moto_micro_tours.Services;

public interface IMessageService
{
    Task<Message> CreateMessageAsync(Message message);
}
