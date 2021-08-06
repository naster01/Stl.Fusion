using System;
using System.Collections.Generic;
using Cysharp.Text;
using Stl.Collections;
using Stl.Internal;

namespace Stl.Text
{
    public ref struct ListParser
    {
        public ListFormat Format => new(Delimiter, Escape);
        public readonly char Delimiter;
        public readonly char Escape;
        public ReadOnlySpan<char> Source;
        public Utf16ValueStringBuilder ItemBuilder;
        public readonly bool OwnsItemBuilder;
        public int ItemIndex;
        public string Item => ItemBuilder.ToString();

        internal ListParser(
            ListFormat format,
            in ReadOnlySpan<char> source,
            in Utf16ValueStringBuilder itemBuilder,
            bool ownsItemBuilder,
            int itemIndex)
        {
            Delimiter = format.Delimiter;
            Escape = format.Escape;
            Source = source;
            ItemBuilder = itemBuilder;
            OwnsItemBuilder = ownsItemBuilder;
            ItemIndex = itemIndex;
        }

        public void Dispose()
        {
            if (OwnsItemBuilder)
                ItemBuilder.Dispose();
        }

        public bool TryParseNext(bool clearItemBuilder = true)
        {
            if (clearItemBuilder)
                ItemBuilder.Clear();
            ItemIndex++;
            var startLength = ItemBuilder.Length;
            for (var index = 0; index < Source.Length; index++) {
                var c = Source[index];
                if (c == Escape) {
                    if (++index >= Source.Length) {
                        if (ItemIndex == 1 && ItemBuilder.Length == 0) {
                            // Special case: single Escape = an empty list
                            Source = Source[..0];
                            return false;
                        }
                        ItemBuilder.Append(Escape);
                        break;
                    }
                }
                else if (c == Delimiter) {
                    Source = Source[(index + 1)..];
                    return true;
                }
                ItemBuilder.Append(Source[index]);
            }

            Source = Source[..0];
            return ItemIndex == 1 || ItemBuilder.Length > 0;
        }

        public void ParseNext(bool clearItemBuilder = true)
        {
            if (!TryParseNext(clearItemBuilder))
                throw Errors.InvalidListFormat();
        }

        public List<string> ParseAll()
        {
            var result = new List<string>();
            while (TryParseNext())
                result.Add(Item);
            return result;
        }

        public void ParseAll(MemoryBuffer<string> buffer)
        {
            while (TryParseNext())
                buffer.Add(Item);
        }
    }
}