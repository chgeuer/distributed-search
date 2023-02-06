namespace Mercury.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using static Fundamentals.Types;

public interface IRequestResponseMessageClient<TMessagePayload>
{
    Task SendRequestResponseMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken = default);

    IObservable<RequestResponseMessage<TMessagePayload>> CreateObervable(CancellationToken cancellationToken = default);
}