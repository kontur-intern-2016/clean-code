﻿using Markdown.Core.Tags;

namespace Markdown.Core
{
    class TagToken : Token
    {
        public ITag Tag { get; }
        public bool IsOpening { get; }
        public bool IsSkipped { get; }

        public TagToken(int startPosition, ITag tag, string value, bool isOpening, bool isSkipped) : base(startPosition,
            value)
        {
            Tag = tag;
            IsOpening = isOpening;
            IsSkipped = isSkipped;
        }
    }
}