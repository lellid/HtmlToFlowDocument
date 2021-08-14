﻿
namespace ExCSS
{
    internal sealed class StrokeDasharrayProperty : Property
    {
        private static readonly IValueConverter StyleConverter = Converters.StrokeDasharrayConverter;

        public StrokeDasharrayProperty()
            : base(PropertyNames.StrokeDasharray, PropertyFlags.Animatable | PropertyFlags.Unitless)
        {
        }

        internal override IValueConverter Converter => StyleConverter;
    }
}