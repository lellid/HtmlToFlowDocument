﻿using System;
using System.Collections.Generic;
using System.Globalization;

namespace ExCSS
{
  internal sealed class StructValueConverter<T> : IValueConverter
      where T : struct, IFormattable
  {
    private readonly Func<IEnumerable<Token>, T?> _converter;

    public StructValueConverter(Func<IEnumerable<Token>, T?> converter)
    {
      _converter = converter;
    }

    public IPropertyValue Convert(IEnumerable<Token> value)
    {
      var val = _converter(value);
      return val.HasValue ? new StructValue(val.Value, value) : null;
    }

    public IPropertyValue Construct(Property[] properties)
    {
      return properties.Guard<StructValue>();
    }

    public sealed class StructValue : IPropertyValue
    {
      public readonly T _value;

      public StructValue(T value, IEnumerable<Token> tokens)
      {
        _value = value;
        Original = new TokenValue(tokens);
      }

      public string CssText => _value.ToString(null, CultureInfo.InvariantCulture);

      public TokenValue Original { get; }

      public TokenValue ExtractFor(string name)
      {
        return Original;
      }
    }
  }
}
