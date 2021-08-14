﻿
namespace ExCSS
{
    internal sealed class StrokeDashoffsetProperty : Property
    {
        private static readonly IValueConverter StyleConverter = Converters.LengthOrPercentConverter;

        public StrokeDashoffsetProperty()
            : base(PropertyNames.StrokeDashoffset, PropertyFlags.Animatable)
        {
        }

        internal override IValueConverter Converter => StyleConverter;
    }
}