namespace Interfaces
{
    public interface IMessageEnrichableWithPayloadAddress
    {
        IMessageEnrichableWithPayloadAddress SetPayloadAddress(string address);

        string GetPayloadAddress();
    }
}