﻿﻿using System;
 using System.Collections;
 using System.Collections.Generic;
 using System.Linq;
 using System.Net.Configuration;
 using Markdown.Tokenizer;

namespace Markdown.MdTokens
{
    public class MdTokenizer : ITokenizer
    {
        private readonly HashSet<string> specialSymbols;
        private readonly Dictionary<string, bool> isSymbolPaired;
        private Stack<MdToken> pairedSymbols;
        private List<MdToken> tokens;
        public MdTokenizer()
        {
            specialSymbols = new HashSet<string> {"_", "#", "__"};
            isSymbolPaired = new Dictionary<string, bool>
            {
                {"_", true}, 
                {"__", true}, 
                {"#", false}, 
                {@"\", false}, 
                {"NONE", false}
            };
            
        }
        public IEnumerable<IToken> MakeTokens(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text == "") throw new ArgumentException("Text is empty");
            tokens = new List<MdToken>();
            pairedSymbols = new Stack<MdToken>();
            foreach (var str in text.Split().Where(str => str != ""))
            {
                var token = MakeToken(str);
                HandlePairedTokens(token);
                tokens.Add(token);
            }
            ClearPairedStack();
            return tokens;
        }

        private void ClearPairedStack()
        {
            while (pairedSymbols.Count != 0)
            {
                var stackTop = pairedSymbols.Pop();
                CorrectPairedTokenBeginning(stackTop);
            }
        }

        private void HandlePairedTokens(MdToken token)
        {
            if (token.BeginningSpecialSymbol != "NONE")
                if (isSymbolPaired[token.BeginningSpecialSymbol] && token.EndingSpecialSymbol != token.BeginningSpecialSymbol)
                    pairedSymbols.Push(token);
            
            if (token.EndingSpecialSymbol != "NONE")
                if (token.EndingSpecialSymbol != token.BeginningSpecialSymbol && isSymbolPaired[token.EndingSpecialSymbol])
                    UpdatePairedTokens(token);
        }

        private void UpdatePairedTokens(MdToken token)
        {
            if (pairedSymbols.Count != 0)
            {
                var stackTop = pairedSymbols.Pop();
                while (stackTop.BeginningSpecialSymbol != token.EndingSpecialSymbol && pairedSymbols.Count != 0)
                {
                    CorrectPairedTokenBeginning(stackTop);
                    stackTop = pairedSymbols.Pop();
                }
                if (stackTop.BeginningSpecialSymbol == token.EndingSpecialSymbol) return;
                CorrectPairedTokenBeginning(stackTop);
                CorrectPairedTokenEnding(token);
            }
            else
                CorrectPairedTokenEnding(token);
        }

        private static void CorrectPairedTokenBeginning(MdToken token)
        {
            token.Content = token.BeginningSpecialSymbol + token.Content;
            token.BeginningSpecialSymbol = "NONE";
        }

        private static void CorrectPairedTokenEnding(MdToken token)
        {
            token.Content += token.EndingSpecialSymbol;
            token.EndingSpecialSymbol = "NONE";
        }

        private MdToken MakeToken(string text)
        {
            var token = new MdToken(text, "NONE", "NONE");
            if (text.Length <= 2) return token;
            var shieldingIndexes = FindShieldedSymbols(text);
            var textWithoutShieldedSymbols = ReplaceShieldingWithSpaces(text, shieldingIndexes);
            var beginningSymbol = GetBeginningSpecialSymbol(textWithoutShieldedSymbols);
            var endingSymbol = GetEndingSpecialSymbol(textWithoutShieldedSymbols);
            var content = GetContent(text, beginningSymbol, endingSymbol);
            shieldingIndexes = FindShieldedSymbols(content);
            content = ClearContentShielding(content, shieldingIndexes);
            token = new MdToken(content, beginningSymbol, endingSymbol);
            return token;
        }

        private List<int> FindShieldedSymbols(string text)
        {
            var shieldingIndexes = new List<int>();
            for (var i = 0; i < text.Length - 1; i++)
            {
                var character = text[i].ToString();
                if (character == @"\")
                {
                    var nextCharacter = text[i + 1].ToString();
                    if (specialSymbols.Contains(nextCharacter))
                        shieldingIndexes.Add(i);
                }
            }

            return shieldingIndexes;
        }

        private string ReplaceShieldingWithSpaces(string text, List<int> shieldingIndexes)
        {
            foreach (var index in shieldingIndexes)
            {
                var symbolsToReplace = text[index].ToString() + text[index + 1];
                text = text.Replace(symbolsToReplace, "  ");
            }
                
            return text;
        }

        private string GetBeginningSpecialSymbol(string text)
        {
            var specialSymbolBeginning = text[0].ToString();
            var specialSymbolBeginningDoubles = "" + text[0] + text[1];
            return GetSpecialSymbol(specialSymbolBeginning, specialSymbolBeginningDoubles);
        }
        
        private string GetEndingSpecialSymbol(string text)
        {
            var specialSymbolEnding = text[text.Length - 1].ToString();
            var specialSymbolEndingDoubles = "" + text[text.Length - 1] + text[text.Length - 2];
            return GetSpecialSymbol(specialSymbolEnding, specialSymbolEndingDoubles);
        }
        
        private string GetContent(string text, string beginningSymbol, string endingSymbol)
        {
            var content = text;
            if (IsSymbolShielded(beginningSymbol))
                content = content.Substring(1);
            else if(beginningSymbol != "NONE")
                content = content.Substring(beginningSymbol.Length);
            if (IsSymbolShielded(endingSymbol))
                content = content.Remove(content.Length - 2, 1);
            else if(endingSymbol != "NONE")
                content = content.Remove(content.Length - endingSymbol.Length);
            return content;
        }

        private string ClearContentShielding(string text, List<int> shieldingIndexes)
        {
            var removedCount = 0;
            foreach (var index in shieldingIndexes)
            {
                text = text.Remove(index - removedCount, 1);
                removedCount++;
            }
                
            
            return text;
        }
        
        private string GetSpecialSymbol(string singular, string doubled)
        {
            var specialSymbol = singular;
            if (!specialSymbols.Contains(singular))
                specialSymbol =  "NONE";
            if (IsSymbolShielded(doubled))
                specialSymbol = @"\";
            else if (specialSymbols.Contains(doubled))
                specialSymbol = doubled;
            
            return specialSymbol;
        }

        private bool IsSymbolShielded(string symbols)
        {
            if (symbols.Contains(@"\"))
                return specialSymbols.Contains(symbols.Replace(@"\", ""));
            return false;
        }
    }
}