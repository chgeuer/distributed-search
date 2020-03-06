namespace Interfaces
{
    using Newtonsoft.Json;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public static class SerializationExtensions
    {
        public static string ToUTF8String(this byte[] bytes) => Encoding.UTF8.GetString(bytes);

        public static byte[] ToUTF8Bytes(this string str) => Encoding.UTF8.GetBytes(str);

        public static Stream AsJSONStream<T>(this T t) => new MemoryStream(JsonConvert.SerializeObject(t).ToUTF8Bytes());

        public static async Task<T> ReadJSON<T>(this Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            byte[] bytes = ms.ToArray();
            string s = bytes.ToUTF8String();
            return JsonConvert.DeserializeObject<T>(s);
        }
    }
}