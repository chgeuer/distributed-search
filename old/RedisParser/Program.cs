namespace RedisParser
{
    using Pidgin;
    using Sprache;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using static Pidgin.Parser;
    using static Pidgin.Parser<char>;

    internal class Program
    {
        private static void Main()
        {
            // Parser<char, long> sequencedParser = Map((l, bar, x) => l, Long(10), String("bar"), Char('x')).Between(Char('$'), CRLF);
            // var w = Map((l, s) => (l, s), LongNum.Before(CRLF), Any.RepeatString(20)).Between(Char('$'), CRLF).Parse("$4\r\n83828282828382828282\r\n");

            // https://redis.io/topics/protocol
            foreach (var pattern in new[] { "+Hallo Welt\r\n", "-Exception: That was bad... \r\n", ":5675\r\n", "$2\r\nan\r\n" })
            {
                Console.WriteLine($"Pidgin:  {makePrintable(pattern)} => {pattern.ParsePidgin().ToNiceString()}");
                Console.WriteLine($"Sprache: {makePrintable(pattern)} => {pattern.ParseSprache().ToNiceString()}");
            }

            static string makePrintable(string val) => val.Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }

    public static class SpracheParser
    {
        public static IRedisCommand ParseSprache(this string input) => RedisParser.Parse(input);

        private static readonly Sprache.Parser<string> NonCRLF = Parse.Char(c => c != '\r' && c != '\n', "no crlf").Many().Text();
        private static readonly Sprache.Parser<IEnumerable<char>> CRLF = Parse.String("\r\n");

        public static readonly Sprache.Parser<IRedisCommand> RedisSimpleStringParser =
            from leadingPlus in Parse.Char('+')
            from stringValue in NonCRLF
            from cr in CRLF
            select RedisSimpleString.From(stringValue);

        public static readonly Sprache.Parser<IRedisCommand> RedisErrorParser =
            from leadingMinus in Parse.Char('-')
            from stringValue in NonCRLF
            from cr in CRLF
            select RedisError.From(stringValue);

        public static readonly Sprache.Parser<IRedisCommand> RedisIntegerParser =
            from leadingColon in Parse.Char(':')
            from sign in Parse.Char('-').Optional()
            from numberValue in Parse.Digit.Many().Text()
            from cr in CRLF
            select RedisInteger.From(long.Parse($"{(sign.IsDefined ? "-" : "")}{numberValue}"));

        public static readonly Sprache.Parser<IRedisCommand> RedisBulkStringParser =
            from leadingColon in Parse.Char('$')
            from count in Parse.Decimal.Select(int.Parse)
            from cr1 in CRLF
            from stringValue in Parse.AnyChar.Repeat(count).Text() // THIS IS WRONG
            from cr2 in CRLF
            select RedisBulkString.From(Encoding.UTF8.GetBytes(stringValue)); // THIS IS WRONG, TOO

        public static readonly Sprache.Parser<IRedisCommand> RedisParser =
            RedisSimpleStringParser
            .Or(RedisErrorParser)
            .Or(RedisIntegerParser)
            .Or(RedisBulkStringParser);
    }

    public static class PidginParser
    {
        public static IRedisCommand ParsePidgin(this string input) => RedisParser.Parse(input) switch
        {
            var x when x.Success => x.Value,
            _ => null
        };

        private static Parser<char, Unit> RedisCommandPrefix(char prefix) => Char(prefix).IgnoreResult();
        private static readonly Parser<char, Unit> CRLF = String("\r\n").IgnoreResult();
        private static readonly Parser<char, string> NonCRLF = Token(c => c != '\r' && c != '\n').ManyString();
        private static readonly Parser<char, IRedisCommand> RedisSimpleStringParser = NonCRLF.Between(RedisCommandPrefix('+'), CRLF).Select(RedisSimpleString.From);
        private static readonly Parser<char, IRedisCommand> RedisErrorParser = NonCRLF.Between(RedisCommandPrefix('-'), CRLF).Select(RedisError.From);
        private static readonly Parser<char, IRedisCommand> RedisIntegerParser = LongNum.Between(RedisCommandPrefix(':'), CRLF).Select(RedisInteger.From);

        //internal static readonly Parser<byte, long> RedisBulkStringLengthParser = 
        //    from dollar in RedisCommandPrefix('$')
        //    from length in LongNum
        //    from crlf in CRLF
        //    from bytes in Just



        //
        // TODO Need to find a way to read the string length in one parsing step and read the data with proper length in the next step. 
        //
        //private static readonly Parser<char, (long, string)> RedisBulkStringParser =
        //    RedisBulkStringPrefixParser.Then(LongNum.Before(CRLF)).Before(l => Any.RepeatString(l.))
        //    Map((len, val) => (len, val), LongNum.Before(CRLF), Any.RepeatString(20)).Between(Char('$'), CRLF);

        // Map((l, s) => (l, s), LongNum.Then(CRLF), Any.RepeatString(20))
        // .Between(Char('$'), CRLF);
        // .Then(Char('s').Repeat(4)).Select(RedisInteger.From);

        private static readonly Parser<char, IRedisCommand> RedisParser = Parser.OneOf<char, IRedisCommand>(RedisSimpleStringParser, RedisErrorParser, RedisIntegerParser);
    }

    public interface IRedisCommand { }

    public static class IRedisCommandExtensions
    {
        public static string ToNiceString(this IRedisCommand command) => command switch
        {
            RedisSimpleString s => $"String: \"{s.Value}\"",
            RedisError e => $"Error: \"{e.Value}\"",
            RedisInteger i => $"Integer: {i.Value}",
            RedisBulkString b => $"BulkString: {b.Value.Length} bytes",
            _ => "Error"
        };
    }

    public class RedisSimpleString : IRedisCommand
    {
        public static IRedisCommand From(string value) => new RedisSimpleString { Value = value };
        public string Value { get; private set; }
        public override string ToString() => Value;
    }

    public class RedisBulkString : IRedisCommand
    {
        public static IRedisCommand From(byte[] value) => new RedisBulkString { Value = value };
        public byte[] Value { get; private set; }
        public override string ToString() => Convert.ToBase64String(this.Value);
    }

    public class RedisError : IRedisCommand
    {
        public static IRedisCommand From(string value) => new RedisError { Value = value };
        public string Value { get; private set; }
        public override string ToString() => $"Error: {Value}";
    }

    public class RedisInteger : IRedisCommand
    {
        public static IRedisCommand From(long value) => new RedisInteger { Value = value };
        public long Value { get; private set; }
        public override string ToString() => $"integer {Value}";
    }
}