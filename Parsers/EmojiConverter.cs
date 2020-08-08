using System.Collections.Generic;
using System.Text;

namespace SimpleChattyServer.Parsers
{
    public sealed class EmojiConverter
    {
        private readonly UTF8Encoding _utf8Encoding;

        public EmojiConverter()
        {
            _utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        public string ConvertEmojisToEntities(string html)
        {
            var bytes = _utf8Encoding.GetBytes(html);
            var stringBuilder = new List<byte>();
            var offset = 0;

            if (html.Length == 0)
                return "";

            while (offset >= 0)
            {
                var decValue = ReadUnicodeCodePoint(bytes, ref offset);
                if (decValue >= 128)
                {
                    var entity = $"&#{decValue};";
                    foreach (var ch in _utf8Encoding.GetBytes(entity))
                        stringBuilder.Add(ch);
                }
                else
                {
                    stringBuilder.Add((byte)decValue);
                }
            }

            return _utf8Encoding.GetString(bytes);
        }

        private static uint ReadUnicodeCodePoint(byte[] utf8Bytes, ref int offset)
        {
            var code = (uint)utf8Bytes[offset];
            if (code >= 128)
            {
                var bytesnumber =
                    code < 224 ? 2 :
                    code < 240 ? 3 :
                    code < 248 ? 4 : 1;
                var codetemp = (uint)(code - 192 - (bytesnumber > 2 ? 32 : 0) - (bytesnumber > 3 ? 16 : 0));
                for (var i = 2; i <= bytesnumber; i++)
                {
                    offset++;
                    var code2 = utf8Bytes[offset] - 128;
                    codetemp = (uint)(codetemp * 64 + code2);
                }
                code = codetemp;
            }
            offset += 1;
            if (offset > utf8Bytes.Length)
                offset = -1;
            return code;
        }
    }
}
