namespace Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using static Fundamentals.Types;

    public interface IMessageClient<TMessagePayload>
    {
        IObservable<Message<TMessagePayload>> CreateObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default);

        Task SendMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default);

        Task SendMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default);
    }
}
