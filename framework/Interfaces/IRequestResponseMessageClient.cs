namespace Mercury.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using static Fundamentals.Types;

    public interface IRequestResponseMessageClient<TMessagePayload>
    {
        IObservable<RequestResponseMessage<TMessagePayload>> CreateWatermarkObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default);

        Task SendRequestResponseMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default);
    }
}