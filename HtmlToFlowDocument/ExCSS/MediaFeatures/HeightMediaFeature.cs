﻿
namespace ExCSS
{
    internal sealed class HeightMediaFeature : MediaFeature
    {
        public HeightMediaFeature(string name)
            : base(name)
        {
        }

        internal override IValueConverter Converter => Converters.LengthConverter;

    }
}