using System;
using System.Threading.Tasks;

namespace OoLunar.Willow.Models.Actions
{
    public interface IAction
    {
        Ulid CorrelationId { get; init; }
        Task Execute();
    }
}
