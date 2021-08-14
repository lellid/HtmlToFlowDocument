﻿
namespace ExCSS
{
    internal sealed class TransitionTimingFunctionProperty : Property
    {
        private static readonly IValueConverter ListConverter =
            Converters.TransitionConverter.FromList().OrDefault(Map.TimingFunctions[Keywords.Ease]);

        internal TransitionTimingFunctionProperty()
            : base(PropertyNames.TransitionTimingFunction)
        {
        }

        internal override IValueConverter Converter => ListConverter;
    }
}