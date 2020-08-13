using System;
using System.Diagnostics.CodeAnalysis;

namespace SimpleChattyServer.Data
{
    public sealed class LolModel : IEquatable<LolModel>
    {
        public string Tag { get; set; }
        public int Count { get; set; }

        public override bool Equals(object obj) =>
            obj is LolModel model && Tag == model.Tag && Count == model.Count;

        public bool Equals([AllowNull] LolModel other) =>
            other != null && Equals(obj: other);

        public override int GetHashCode() =>
            HashCode.Combine(Tag, Count);
    }
}
