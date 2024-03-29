﻿namespace Mercury.Utils.Extensions;

using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

public static class SerializationExtensions
{
    public static string ToUTF8String(this byte[] bytes) => Encoding.UTF8.GetString(bytes);

    public static byte[] ToUTF8Bytes(this string str) => Encoding.UTF8.GetBytes(str);

    public static string AsJSON<T>(this T t) => JsonConvert.SerializeObject(t);

    public static Stream AsJSONStream<T>(this T t) => new MemoryStream(t.AsJSON().ToUTF8Bytes());

    public static T DeserializeJSON<T>(this string s) => JsonConvert.DeserializeObject<T>(s);

    public static async Task<T> ReadJSON<T>(this Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        byte[] bytes = ms.ToArray();
        string s = bytes.ToUTF8String();
        return JsonConvert.DeserializeObject<T>(s, new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
        });
    }

    public static Stream GZipCompress(this Stream input)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            input.CopyTo(gzip);
        }

        return new MemoryStream(output.ToArray());
    }

    public static Stream GZipDecompress(this Stream input)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            gzip.CopyTo(output);
        }

        return new MemoryStream(output.ToArray());
    }
}