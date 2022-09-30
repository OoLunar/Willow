using System;
using System.Threading;
using System.Threading.Tasks;

namespace OoLunar.Willow.Models.Actions
{
    public interface IAction
    {
        Ulid CorrelationId { get; init; }
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
