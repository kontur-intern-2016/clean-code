﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markdown
{
    public class MarkdownParser : IParser
    {
        private List<TagPair> tags;
        private int index;
        private readonly TagPair emptyTagPair = new TagPair("", "", "", "");
        private string markdownString;

        public string ParseTo(string rowString, Markup markup)
        {
            if (markup == Markups.Markdown)
                return rowString;

            markdownString = rowString;
            tags = GetTags(Markups.Markdown, markup);
            index = 0;

            var mainSpan = new Span(emptyTagPair, 0, markdownString.Length) { IsMainSpan = true };
            var openedSpans = new List<Span>();

            for (; index < markdownString.Length; index++)
            {
                if (markdownString[index] == '\\')
                {
                    mainSpan.PutSpan(new Span(emptyTagPair, index, index + 1));
                    index += 1;
                    continue;
                }

                if (openedSpans.Count != 0)
                {
                    var span = FindSpanEnd(openedSpans);
                    if (span != null)
                    {
                        openedSpans = openedSpans.Where(t => t.EndIndex == 0).ToList();
                        continue;
                    }
                }

                var newSpan = FindSpanStart();
                if (newSpan != null)
                {
                    mainSpan.PutSpan(newSpan);
                    openedSpans.Add(newSpan);
                }
            }

            mainSpan.RemoveNotClosedSpans();
            return mainSpan.Assembly(markdownString);
        }

        private Span FindSpanStart()
        {
            if (index > 0 && !char.IsWhiteSpace(markdownString[index - 1]))
                return null;

            var tag = MatchTag(t => t.InitialOpen);
            if (tag == null)
                return null;

            var startIndex = index;
            index = index + tag.InitialOpen.Length - 1;
            return new Span(tag, startIndex);

        }
        private Span FindSpanEnd(List<Span> openedSpans)
        {

            var tag = MatchTag(t => t.InitialClose);
            if (tag == null)
                return null;

            foreach (var openedSpan in openedSpans)
            {
                if (Equals(openedSpan.TagPair, tag) &&
                    (index + openedSpan.TagPair.InitialClose.Length - 1 == markdownString.Length - 1 ||
                     char.IsWhiteSpace(markdownString[index + openedSpan.TagPair.InitialClose.Length])))
                {
                    openedSpan.EndIndex = index;
                    index = index + openedSpan.TagPair.InitialClose.Length - 1;
                    return openedSpan;
                }

            }

            return null;
        }

        private TagPair MatchTag(Func<TagPair, string> param)
        {
            var possibleTags = new List<TagPair>();
            foreach (var tag in tags)
            {
                var str = param(tag);
                var length = str.Length;
                if (markdownString.Length - index < length)
                    length = markdownString.Length - index;

                if (str == markdownString.Substring(index, length))
                    possibleTags.Add(tag);
            }

            return possibleTags.OrderByDescending(t => param(t).Length).FirstOrDefault();
        }

        private List<TagPair> GetTags(Markup from, Markup to)
        {
            var result = new List<TagPair>();
            foreach (var tag in from.Tags)
            {
                var endTag = to.Tags.FirstOrDefault(t => t.Name == tag.Name);
                if (endTag == null)
                    continue;

                result.Add(new TagPair(tag.Open, tag.Close, endTag.Open, endTag.Close, endTag.CanBeInside && tag.CanBeInside));
            }

            return result;
        }
    }
}
