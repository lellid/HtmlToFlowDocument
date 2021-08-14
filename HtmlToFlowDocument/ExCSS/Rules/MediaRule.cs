﻿using System.IO;
using System.Linq;

namespace ExCSS
{
    internal sealed class MediaRule : ConditionRule, IMediaRule
    {
        internal MediaRule(StylesheetParser parser) : base(RuleType.Media, parser)
        {
            AppendChild(new MediaList(parser));
        }
        
        public override void ToCss(TextWriter writer, IStyleFormatter formatter)
        {
            var rules = formatter.Block(Rules);
            writer.Write(formatter.Rule("@media", Media.MediaText, rules));
        }
       
        public string ConditionText
        {
            get { return Media.MediaText; }
            set { Media.MediaText = value; }
        }

        public MediaList Media => Children.OfType<MediaList>().FirstOrDefault();
    }
}