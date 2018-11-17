﻿using System;
using System.Collections.Generic;
using System.Linq;
using Markdown.Data;
using Markdown.Data.Nodes;
using Markdown.Data.TagsInfo;

namespace Markdown.TreeBuilder
{
    public class MarkdownTokenTreeBuilder : ITokenTreeBuilder
    {
        private readonly Dictionary<string, ITagInfo> tagsInfo = new Dictionary<string, ITagInfo>();

        public MarkdownTokenTreeBuilder(IEnumerable<ITagInfo> tagsInfo)
        {
            foreach (var tagInfo in tagsInfo)
            {
                this.tagsInfo[tagInfo.OpeningTag] = tagInfo;
                this.tagsInfo[tagInfo.ClosingTag] = tagInfo;
            }
        }

        public TokenTreeNode BuildTree(IEnumerable<string> tokens)
        {
            var openedTags = new Stack<TagTreeNode>();
            openedTags.Push(new RootTreeNode());
            var tokensArray = tokens as string[] ?? tokens.ToArray();
            for (var i = 0; i < tokensArray.Length; i++)
            {
                var token = tokensArray[i];
                var previousTokenType = i > 0 ? GetTokenType(tokensArray[i - 1]) : TokenType.ParagraphStart;
                var nextTokenType = i < tokensArray.Length - 1 ? GetTokenType(tokensArray[i + 1]) : TokenType.ParagraphEnd;

                AddTokenToTree(openedTags, token, previousTokenType, nextTokenType);
            }
            var root = GetTreeRoot(openedTags);
            FixTagsNesting(root);
            return root;
        }

        private void AddTokenToTree(Stack<TagTreeNode> openedTags, string token, TokenType previousTokenType, TokenType nextTokenType)
        {
            var currentTag = openedTags.Peek();
            var tokenType = GetTokenType(token);

            switch (tokenType)
            {
                case TokenType.Space:
                    currentTag.Children.Add(new SpaceTreeNode());
                    break;
                case TokenType.Text:
                    currentTag.Children.Add(new TextTreeNode(token));
                    break;
                case TokenType.EscapeSymbol when previousTokenType == TokenType.EscapeSymbol:
                    currentTag.Children.Add(new TextTreeNode("\\"));
                    break;
                case TokenType.Tag:
                    AddTagToken(openedTags, token, previousTokenType, nextTokenType);
                    break;
            }
        }

        private void AddTagToken(Stack<TagTreeNode> openedTags, string token, TokenType previousTokenType, TokenType nexTokenType)
        {
            var tagIsOpened = openedTags.Any(node => node.TagInfo?.ClosingTag == token);
            var tagInfo = tagsInfo[token];

            if (tagInfo.MustBeOpened(tagIsOpened, previousTokenType, nexTokenType))
                openedTags.Push(new TagTreeNode(tagInfo));
            else if (tagInfo.MustBeClosed(tagIsOpened, previousTokenType, nexTokenType))
                CloseTag(openedTags, token);
            else
                openedTags.Peek().Children.Add(new TextTreeNode(token));
        }

        private static void CloseTag(Stack<TagTreeNode> nodeStack, string token)
        {
            while (nodeStack.Peek().TagInfo.ClosingTag != token)
                RemoveOpenedTag(nodeStack);
            var tag = nodeStack.Pop();
            nodeStack.Peek().Children.Add(tag);
        }

        private static void FixTagsNesting(TokenTreeNode node, bool isInTag = false)
        {
            if (!(node is TagTreeNode tagNode))
                return;
            if (isInTag && !tagNode.TagInfo.CanBeInsideOtherTag)
                tagNode.IsRaw = true;
            foreach (var child in node.Children)
                FixTagsNesting(child, !(tagNode is RootTreeNode));
        }

        private static TokenTreeNode GetTreeRoot(Stack<TagTreeNode> openedTags)
        {
            if (openedTags.Count == 1)
                return openedTags.Peek();
            while (openedTags.Count > 1)
                RemoveOpenedTag(openedTags);
            return openedTags.Peek();
        }

        private static void RemoveOpenedTag(Stack<TagTreeNode> nodeStack)
        {
            var previousNode = nodeStack.Pop();
            var parent = nodeStack.Peek();
            parent.Children.Add(new TextTreeNode(previousNode.TagInfo.OpeningTag));
            parent.Children.AddRange(previousNode.Children);
        }

        private TokenType GetTokenType(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("token should be not empty string");
            if (token == "\\")
                return TokenType.EscapeSymbol;
            if (tagsInfo.ContainsKey(token))
                return TokenType.Tag;
            if (string.IsNullOrWhiteSpace(token))
                return TokenType.Space;
            return TokenType.Text;
        }
    }
}