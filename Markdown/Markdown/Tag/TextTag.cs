﻿using System.Collections.Generic;
using Markdown.Extensions;

namespace Markdown.Tag
{
    public class TextTag : ITag
    {
        public string Symbol { get; }
        public int OpenIndex { get; set; }
        public int CloseIndex { get; set; }
        public string Html { get; }
        public int Length { get; }
        public string Content { get; set; }
        public MdType Type => MdType.Text;
        public List<MdType> AllowedInnerTypes { get; }

        public int FindCloseIndex(string text)
        {
            throw new System.NotImplementedException();
        }

        public string GetContent(string text) => text.Substring(OpenIndex, CloseIndex - OpenIndex + 1);
        public IAttribute Attribute { get; set; }
    }
}
