﻿using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using MarkDown;
using MarkDown.TokenParsers;
using FluentAssertions;

namespace MarkDown_Tests
{
    class LineParser_Tests
    {
        private static LineParser lineParser;
        private static ParserGetter ParserGetter;
        [SetUp]
        public void SetUp()
        {
            ParserGetter = new ParserGetter();
            lineParser = new LineParser(ParserGetter);

        }

        [Test]
        public void Should_NotChangeLineWithoutTags()
        {
            var line = "abcdefg";

            var result = lineParser.GetParsedLineFrom(line);

            result.Should().Be(line);
        }
        [Test]
        public void Should_FindTag()
        {
            var line = "_a_";

            var result = lineParser.GetParsedLineFrom(line);

            result.Should().Be(@"<em>a</em>");
        }
        [Test]
        public void Should_SkipTagsWithoutPair()
        {
            var line = "__a_";

            var result = lineParser.GetParsedLineFrom(line);

            result.Should().Be(line);
        }
        [Test]
        public void Should_FindDifferentTags()
        {
            var line = "_a_ __b__";

            var result = lineParser.GetParsedLineFrom(line);

            result.Should().Be(@"<em>a</em> <strong>b</strong>");
        }
    }
}
