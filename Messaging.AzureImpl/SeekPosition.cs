namespace Messaging.AzureImpl
{
    using Azure.Messaging.EventHubs.Consumer;

    public class SeekPosition
    {
        private bool fromTail;
        private long fromPosition;

        public static SeekPosition Tail { get; } = new SeekPosition { fromTail = true, fromPosition = 0 };

        public static SeekPosition FromPosition(long position) => new SeekPosition { fromTail = false, fromPosition = position };

        private SeekPosition()
        {
        }

        internal EventPosition AsEventPosition() 
            => this.fromTail
                ? EventPosition.Latest
                : EventPosition.FromOffset(offset: this.fromPosition, isInclusive: false);
    }
}