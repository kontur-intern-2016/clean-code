﻿namespace MarkdownProcessor.TextWraps
{
    public struct SingleUnderscoreWrap : ITextWrap
    {
        public string OpenWrapMarker => "_";
        public string CloseWrapMarker => OpenWrapMarker;

        public string HtmlRepresentationOfOpenMarker => "<em>";
        public string HtmlRepresentationOfCloseMarker => "</em>";
    }
}