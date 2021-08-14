﻿
namespace ExCSS
{
    internal sealed class WidowsProperty : Property
    {
        private static readonly IValueConverter StyleConverter = Converters.IntegerConverter.OrDefault(2);

        internal WidowsProperty()
            : base(PropertyNames.Widows, PropertyFlags.Inherited)
        {
        }

        internal override IValueConverter Converter => StyleConverter;
    }
}