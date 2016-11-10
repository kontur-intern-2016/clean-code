﻿using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Markdown.Tests
{
    [TestFixture]
    public class Tokenizer_Should
    {
        private Tokenizer tokenizer;
        private List<IShell> shells;
        [SetUp]
        public void SetUp()
        {
            shells = new List<IShell>()
            {
                new SingleUnderline(),
                new DoubleUnderline()
            };
            tokenizer = new Tokenizer();
        }
        
        [Test]
        public void notChangeText_WhenNotFormatting()
        {
            var tokens = tokenizer.SplitToTokens("some text without shell", shells).ToList();
            tokens.Count.Should().Be(1);
            tokens.First().HasShell().Should().BeFalse();
        }

        [Test]
        public void notFindShell_WhenNotFormatting()
        {
            var text = "_abc_text";
            var position = 5;
            var shell = tokenizer.ReadNextShell(text, ref position, shells);
            position.Should().Be(5);
            shell.Should().BeNull();
        }

        [Test]
        public void findShell_WhenPositionInSingleUnderline()
        {
            var text = "_italic text_";
            var position = 0;
            var shell = tokenizer.ReadNextShell(text, ref position, shells);
            position.Should().Be(1);
            shell.Should().BeOfType(typeof(SingleUnderline));
        }

        [Test]
        public void findShell_WhenPositionInDoubleUnderline()
        {
            var text = "some text__bold text__";
            var position = 9;
            var shell = tokenizer.ReadNextShell(text, ref position, shells);
            position.Should().Be(11);
            shell.Should().BeOfType(typeof(DoubleUnderline));
        }

        [Test]
        public void notFindShell_WhenPositionIsOutside()
        {
            var text = "abcd efgh";
            var position = 10;
            tokenizer.ReadNextShell(text, ref position, shells).Should().BeNull();
        }


        [Test]
        public void notFindShell_WhenSpaceAfterPrefix()
        {
            var text = "abc __ def__";
            var position = 4;
            tokenizer.ReadNextShell(text, ref position, shells).Should().BeNull();
            position.Should().Be(4);
        }

        [Test]
        public void findEndToken_WhenNotFormatting()
        {
            var text = "_abc_ not formatted text_abc_";
            var position = 5;
            IShell currentShell;
            var endToken = tokenizer.GetEndPositionToken(text, position, shells, out currentShell);
            endToken.Should().Be(23);
            currentShell.Should().BeNull();
        }


    }
}
