namespace Mercury.Messaging
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Mercury.Interfaces;
    using static Mercury.Fundamentals.Types;

    internal class ServiceBusMessagingClient<TMessagePayload> : IRequestResponseMessageClient<TMessagePayload>
    {
        public ServiceBusMessagingClient(IDistributedSearchConfiguration demoCredential)
        {
            throw new NotImplementedException();
        }

        IObservable<RequestResponseMessage<TMessagePayload>> IRequestResponseMessageClient<TMessagePayload>.CreateWatermarkObervable(SeekPosition startingPosition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IRequestResponseMessageClient<TMessagePayload>.SendRequestResponseMessage(TMessagePayload messagePayload, string requestId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}