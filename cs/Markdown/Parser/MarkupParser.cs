﻿using System;
using System.Collections.Generic;
using System.Linq;
using Markdown.Tag;

namespace Markdown.Parser
{
    public class MarkupParser : IMarkupParser
    {
        private readonly Dictionary<string, ITagData> openBorderToTag;
        private readonly PrefixTree openTagTree;
        private readonly PrefixTree closeTagTree;
        
        private TextData fullTextData;
        private List<TextToken> currentTokens;
        private bool previousIsEscapeSymbol;
        
        public MarkupParser(params ITagData[] tags)
        {
            openBorderToTag = tags.ToDictionary(tag => tag.IncomingBorder.Open,
                tag => tag);

            openTagTree = new PrefixTree(tags
                .Select(tag => tag.IncomingBorder.Open).ToList());
            closeTagTree = new PrefixTree(tags
                .Select(tag => tag.IncomingBorder.Close).ToList());
        }

        public TextData Parse(string text)
        {
            fullTextData = new TextData(text);
            currentTokens = new List<TextToken>();
            previousIsEscapeSymbol = false;
            
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                    AtLineEnd(i + 1);

                if (text[i] == '\\')
                {
                    if (previousIsEscapeSymbol)
                    {
                        previousIsEscapeSymbol = false;
                        fullTextData.AddDataToRemove("\\", i - 1);
                        continue;
                    }
                    previousIsEscapeSymbol = true;
                    continue;
                }
                
                // Самый некрасивый участок кода. Но лучше я не смог придумать
                var offset = TryToCloseTokenAndGetOffset(text, i);
                if (offset > 0)
                {
                    i += offset - 1;
                    continue;
                }

                offset = TryToOpenTokenAndGetOffset(text, i);
                if (offset > 0)
                    i += offset - 1;
                
                if (previousIsEscapeSymbol)
                    previousIsEscapeSymbol = false;
            }
            
            AtLineEnd(text.Length, true);

            CancelNotAllowedNestedTokens();
            return fullTextData;
        }

        private void CancelNotAllowedNestedTokens()
        {
            var previousTokens = new HashSet<TextToken>();
            foreach (var baseToken in fullTextData.Tokens)
            {
                CancelNotAllowedNestedTokens(previousTokens, baseToken);
            }
        }

        private void CancelNotAllowedNestedTokens(HashSet<TextToken> previousTokens, TextToken currentToken)
        {
            previousTokens.Add(currentToken);
            var nestedTokensToMove = new List<TextToken>();
            var nestedTokensToRemove = new HashSet<TextToken>();
            foreach (var nestedToken in currentToken.SubTokens)
            {
                CancelNotAllowedNestedTokens(previousTokens, nestedToken);
                if (!IsTokenCanNestedInTokens(previousTokens, nestedToken))
                {
                    nestedTokensToMove.AddRange(nestedToken.SubTokens);
                    nestedTokensToRemove.Add(nestedToken);
                }
            }

            currentToken.AddNestedTokens(nestedTokensToMove.ToArray());
            currentToken.RemoveNestedTokens(nestedTokensToRemove.ToArray());
            
            previousTokens.Remove(currentToken);
        }

        private bool IsTokenCanNestedInTokens(HashSet<TextToken> tokens, TextToken tokenToNeste)
        {
            foreach (var token in tokens)
            {
                if (!token.Tag.CanNested(tokenToNeste.Tag))
                    return false;
            }

            return true;
        }

        private void AtLineEnd(int position, bool isTextEnd=false)
        {
            for (var i = currentTokens.Count - 1; i >= 0; i--)
            {
                var atLineEndTokenAction = currentTokens[i].Tag.AtLineEndAction;
                if (!isTextEnd && 
                    (atLineEndTokenAction == EndOfLineAction.ContinueAndCompleteAtEOF
                     ||atLineEndTokenAction == EndOfLineAction.ContinueAndCancelAtEOF))
                    break;
                
                switch (atLineEndTokenAction)
                {
                    case EndOfLineAction.Cancel:
                        CancelSubsequentTokens(i - 1);
                        break;
                    
                    case EndOfLineAction.ContinueAndCancelAtEOF:
                        CancelSubsequentTokens(i - 1);
                        break;
                    
                    case EndOfLineAction.Complete:
                        if (IsValidToClose(i, position))
                            CloseToken(i, position);
                        break;
                    
                    case EndOfLineAction.ContinueAndCompleteAtEOF:
                        if (IsValidToClose(i, position))
                            CloseToken(i, position);
                        break;
                }
            }
        }
        
        private int TryToOpenTokenAndGetOffset(string text, int position)
        {
            var openBorder = GetMatchesFromTree(openTagTree, text, position).LastOrDefault();
            if (openBorder == null)
                return 0;
            
            var tag = openBorderToTag[openBorder];
            
            if (!IsValidToOpen(tag, position))
                return openBorder.Length;
            if (previousIsEscapeSymbol)
            {
                previousIsEscapeSymbol = false;
                fullTextData.AddDataToRemove("\\", position - 1);
                return 0;
            }
            OpenToken(tag, position);
            return openBorder.Length;
        }

        private bool IsValidToOpen(ITagData tag, int startPosition)
        {
            return tag.IsValidAtOpen(fullTextData.Value, startPosition);
        }

        private void OpenToken(ITagData tag, int position)
        {
            var newToken = new TextToken(tag, position);
            currentTokens.Add(newToken);
        }

        private int TryToCloseTokenAndGetOffset(string text, int position)
        {
            var closeBorders = GetMatchesFromTree(closeTagTree, text, position);
            closeBorders.Reverse();
            if (closeBorders.Count == 0)
                return 0;
            
            for (var i = currentTokens.Count - 1; i >= 0; i--)
            {
                foreach (var closeBorder in closeBorders)
                {
                    var currentTag = currentTokens[i].Tag;
                    if (closeBorder == currentTag.IncomingBorder.Close)
                    {
                        if (IsValidToClose(i, position))
                        {
                            if (previousIsEscapeSymbol)
                            {
                                previousIsEscapeSymbol = false;
                                fullTextData.AddDataToRemove("\\", position - 1);
                                return 0;
                            }
                            CloseToken(i, position);
                            return closeBorder.Length;
                        }
                    }
                }
            }
            return 0;
        }

        private bool IsValidToClose(int tokenNumber, int endPosition)
        {
            return currentTokens[tokenNumber].Tag.IsValidAtClose(fullTextData.Value,
                currentTokens[tokenNumber].Start, endPosition);
        }

        private void CloseToken(int tokenNumber, int endPosition)
        {
            if (currentTokens[tokenNumber].Tag.IsBreaksWhenNestedNotComplete
                && tokenNumber < currentTokens.Count - 1)
            {
                CancelSubsequentTokens(tokenNumber - 1);
                return;
            }
            CancelSubsequentTokens(tokenNumber);
            
            var token = currentTokens[tokenNumber];
            token.End = endPosition;
            if (tokenNumber > 0)
            {
                currentTokens[tokenNumber - 1].AddNestedTokens(token);
            }
            else
            {
                fullTextData.AddTokens(token);
            }
            currentTokens.RemoveAt(tokenNumber);
        }

        private void CancelSubsequentTokens(int lastTokenNumber)
        {
            if (lastTokenNumber < 0)
                for (var i = currentTokens.Count - 1; i >= 0; i--)
                {
                    fullTextData.AddTokens(currentTokens[i].SubTokens.ToArray());
                    currentTokens.RemoveAt(i);
                }
            for (var i = currentTokens.Count - 1; i > lastTokenNumber; i--)
            {
                currentTokens[lastTokenNumber]
                    .AddNestedTokens(currentTokens[i].SubTokens.ToArray());
                currentTokens.RemoveAt(i);
            }
        }

        private static List<string> GetMatchesFromTree(PrefixTree tree, string text, int pos)
        {
            var matches = new List<string>();
            var currentTreeNode = tree.Root;
            while (currentTreeNode.Connections.ContainsKey(text[pos]))
            {
                currentTreeNode = currentTreeNode.Connections[text[pos]];
                if (currentTreeNode.IsFinishNode)
                    matches.Add(currentTreeNode.Value);
                pos++;
                if (pos == text.Length)
                    break;
            }

            return matches;
        }
    }
}