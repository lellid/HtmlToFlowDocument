﻿
namespace ExCSS
{
    internal sealed class StrokeLinejoinProperty : Property
    {
        private static readonly IValueConverter StyleConverter = Converters.StrokeLinejoinConverter;

        public StrokeLinejoinProperty()
            : base(PropertyNames.StrokeLinejoin, PropertyFlags.Animatable)
        {
        }

        internal override IValueConverter Converter => StyleConverter;
    }
}