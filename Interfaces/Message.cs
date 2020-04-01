namespace Interfaces
{
    using System.Collections.Generic;

    public class Message<T>
    {
#pragma warning disable SA1401 // Fields should be private
        public readonly long Offset;

        public readonly T Value;

        public readonly IDictionary<string, object> Properties;
#pragma warning restore SA1401 // Fields should be private

        public Message(long offset, T value, IDictionary<string, object> properties)
        {
            (this.Offset, this.Value, this.Properties) = (offset, value, properties);
        }
    }
}
