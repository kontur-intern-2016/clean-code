﻿namespace Markdown.Tag
{
	public class SingleUnderLineTag : ITag
	{
		public string Symbol { get; set; } = "_";
		public int Length { get; set; } = 1;
		public int OpenIndex { get; set; }
		public int CloseIndex { get; set; }
		public string HtmlOpen { get; set; } = "<em>";
		public string HtmlClose { get; set; } = "</em>";

		public int FindCloseIndex(TextStream stream)
		{
			var text = stream.Text;

			for (var i = OpenIndex + 2; i < text.Length; i++)
			{
				var symbolAfterTag = stream.LookAt(i + Length);
				var symbolBeforeTag = stream.LookAt(i - 1);

				if (text.Substring(i, Length) == Symbol && (char.IsWhiteSpace(symbolAfterTag) || i == text.Length - 1)
				                                        && char.IsLetter(symbolBeforeTag))
					return i;
			}

			return -1;
		}

		public string Body(string text) => text.Substring(OpenIndex + Length, CloseIndex - OpenIndex - Length);
	}
}