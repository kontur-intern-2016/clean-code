﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markdown
{
    public class TextParser
    {
        public List<TextToken> GetTextTokens(string text)
        {
            if(text == null)
                throw new ArgumentException("string was null");

            var splittedText = new List<TextToken>();

            for (var index = 0; index < text.Length; index++)
            {
                var startTokenIndex = index;
                int endTokenIndex, tokenLength;
                TokenType typeOfToken;
                switch (text[index])
                {
                    case '_':
                        startTokenIndex++;
                        endTokenIndex = FindIndexOfClosingElement('_', startTokenIndex, text);
                        typeOfToken = TokenType.Emphasized;
                        tokenLength = endTokenIndex - startTokenIndex;
                        break;

                    default:
                        endTokenIndex = FindIndexOfEndText(index,text);
                        tokenLength = endTokenIndex - startTokenIndex;
                        endTokenIndex--;
                        typeOfToken = TokenType.Text;
                        break;
                }

                splittedText.Add(new TextToken(startTokenIndex, tokenLength, typeOfToken, text.Substring(startTokenIndex, tokenLength)));
                index = endTokenIndex;
            }
            return splittedText;
        }

        private static int FindIndexOfClosingElement(char elementToFind,int startIndex, string text)
        {
            for (var index = startIndex; index < text.Length; index++)
            {
                if (text[index] == elementToFind)
                    return index;
            }
            throw new ArgumentException("No closing underlining");
        }

        private static int FindIndexOfEndText(int startIndex, string text)
        {
            var specialSymbols = new char[]{'_', '#'};
            for (var index = startIndex; index < text.Length; index++)
            {
                if (specialSymbols.Contains(text[index]))
                    return index;
            }

            return text.Length;
        }
    }
}
