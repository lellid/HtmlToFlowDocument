﻿
namespace ExCSS
{
    internal sealed class BorderBottomColorProperty : Property
    {
        private static readonly IValueConverter StyleConverter =
            Converters.CurrentColorConverter.OrDefault(Color.Transparent);

        internal BorderBottomColorProperty()
            : base(PropertyNames.BorderBottomColor)
        {
        }

        internal override IValueConverter Converter => StyleConverter;
    }
}