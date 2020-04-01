namespace Interfaces
{
    public class SeekPosition
    {
        public static SeekPosition FromPosition(long position) => new SeekPosition { FromTail = false, Offset = position };

        public static readonly SeekPosition Tail = new SeekPosition { FromTail = true, Offset = 0 };

        public bool FromTail { get; private set; }

        public long Offset { get; private set; }

        private SeekPosition()
        {
        }
    }
}
