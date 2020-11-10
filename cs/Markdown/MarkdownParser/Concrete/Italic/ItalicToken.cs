﻿using MarkdownParser.Infrastructure.Tokenization.Abstract;

namespace MarkdownParser.Concrete.Italic
{
    public class ItalicToken : Token
    {
        public ItalicToken(int startPosition, string rawText)
            : base(startPosition, rawText)
        {
        }
    }
}