using System;
using SimpleChattyServer.Exceptions;

namespace SimpleChattyServer.Services
{
    public sealed class Parser
    {
        private readonly string _html;

        public int Length => _html.Length;
        public int[] Cursors { get; } = new[] { 0, 0, 0 };

        public Parser(string html)
        {
            _html = html;
        }

        public int Peek(int cursor, string keyword) =>
            _html.IndexOf(keyword, Cursors[cursor], StringComparison.Ordinal);

        public string Clip(string[] beforeKeywords, string afterKeyword)
        {
            Seek(1, beforeKeywords);
            Increment(1);
            Seek(2, afterKeyword);
            return Read();
        }

        public void Increment(int cursor)
        {
            Cursors[cursor]++;

            if (Cursors[cursor] >= _html.Length)
                throw new Exception("Unexpected end of HTML data.");
        }

        public void Seek(int cursor, string[] keywords)
        {
            foreach (var keyword in keywords)
                Seek(cursor, keyword);
        }

        public void Seek(int cursor, string keyword)
        {
            var i = Cursors[1];
            var j = _html.IndexOf(keyword, i, StringComparison.Ordinal);
            if (j == -1)
                throw new Api500Exception(Api500Exception.Codes.SERVER,
                    $"Did not find '{keyword}' starting at index {i}.");
            else
                Cursors[cursor] = j;
        }

        public string Read()
        {
            var c1 = Cursors[1];
            var c2 = Cursors[2];
            return _html.Substring(c1, c2 - c1);
        }
    }
}
