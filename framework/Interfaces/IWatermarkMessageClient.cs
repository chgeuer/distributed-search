namespace Mercury.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;
using static Fundamentals.Types;

public interface IWatermarkMessageClient<TMessagePayload>
{
    IObservable<WatermarkMessage<TMessagePayload>> CreateWatermarkObervable(SeekPosition startingPosition, CancellationToken cancellationToken = default);

    Task<Watermark> SendWatermarkMessage(TMessagePayload messagePayload, CancellationToken cancellationToken = default);
}