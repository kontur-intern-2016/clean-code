﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Markdown.Tokenizing;

namespace Markdown.Languages
{
    public class Language
    {
        public ReadOnlyDictionary<Tag, string> OpeningTags => new ReadOnlyDictionary<Tag, string>(openingTags);
        public ReadOnlyDictionary<Tag, string> ClosingTags => new ReadOnlyDictionary<Tag, string>(closingTags);

        private Dictionary<Tag, string> openingTags;
        private Dictionary<Tag, string> closingTags;

        public int MaxTagLength => openingTags.Concat(closingTags).Max(pair => pair.Value.Length);

        public Language()
        {
            openingTags = new Dictionary<Tag, string>();
            closingTags = new Dictionary<Tag, string>();
        }

        protected void AddTag(Tag tag, string openingTag, string closingTag)
        {
            openingTags.Add(tag, openingTag);
            closingTags.Add(tag, closingTag);
        }
    }
}