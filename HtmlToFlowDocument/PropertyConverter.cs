// // Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using ExCSS;

namespace HtmlToFlowDocument
{
  /// <summary>
  /// Converts properties from either ExCss <see cref="Property"/>s or from attribute value strings to a useful value.
  /// </summary>
  public static class PropertyConverter
  {

    #region Convert from an ExCSS property
    /// <summary>
    /// Converts the specified EsCSS property and writes the results in the <paramref name="propertyDictionary"/>.
    /// </summary>
    /// <param name="property">The ExCSS property.</param>
    /// <param name="propertyDictionary">The property dictionary to store the results.</param>
    /// <exception cref="NotImplementedException"></exception>
    public static void Convert(Property property, Dictionary<string, object> propertyDictionary)
    {
      var propertyName = property.Name;
      var propertyValue = property.Value;

      if (property.IsInitial)
      {
        propertyDictionary[propertyName] = Keywords.Initial;
      }
      else if (property.IsInherited)
      {
        propertyDictionary[propertyName] = Keywords.Inherit;
      }
      else if (property.HasValue)
      {
        switch (property.DeclaredValue)
        {
          case StructValueConverter<Number>.StructValue numberValue:
            propertyDictionary[propertyName] = numberValue._value;
            break;
          case StructValueConverter<Percent>.StructValue percentValue:
            propertyDictionary[propertyName] = percentValue._value;
            break;
          case StructValueConverter<Length>.StructValue lengthValue:
            propertyDictionary[propertyName] = lengthValue._value;
            break;
          case StructValueConverter<Color>.StructValue colorValue:
            propertyDictionary[propertyName] = colorValue._value;
            break;
          case StructValueConverter<float>.StructValue floatValue:
            propertyDictionary[propertyName] = (double)floatValue._value;
            break;
          case StructValueConverter<double>.StructValue doubleValue:
            propertyDictionary[propertyName] = doubleValue._value;
            break;
          case DictionaryValueConverter<VerticalAlignment>.EnumeratedValue enVerticalAlignment:
            if (Map.VerticalAlignments.TryGetValue(enVerticalAlignment.CssText, out var vertValue))
              propertyDictionary[propertyName] = vertValue;
            break;
          default:
            propertyDictionary[propertyName] = property.Value;
            break;
        }
      }
      else
      {
        throw new NotImplementedException();
      }
    }

    #endregion



    #region Convert from an attribute value string

    public static void Convert(string propertyName, string propertyValue, Dictionary<string, object> propertyDictionary)
    {
      switch (propertyName)
      {
        case "background-color":
          {
            if (TryParseColor(propertyValue, out var color))
              propertyDictionary[propertyName] = color;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "border-top":
        case "border-right":
        case "border-left":
        case "border-bottom":
          //  Parse css border style
          propertyDictionary[propertyName] = propertyValue;
          break;

        // NOTE: css names for elementary border styles have side indications in the middle (top/bottom/left/right)
        // In our internal notation we intentionally put them at the end - to unify processing in ParseCssRectangleProperty method
        case "border-top-style":
        case "border-right-style":
        case "border-left-style":
        case "border-bottom-style":
        case "border-top-color":
        case "border-right-color":
        case "border-left-color":
        case "border-bottom-color":
        case "border-top-width":
        case "border-right-width":
        case "border-left-width":
        case "border-bottom-width":
          //  Parse css border style
          propertyDictionary[propertyName] = propertyValue;
          break;

        case "clear":
          {
            if (TryParseEnum(typeof(ClearMode), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "display":
          {
            if (TryParse(propertyValue, Map.DisplayModes, out DisplayMode v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "color":
          {
            if (TryParseColor(propertyValue, out var color))
              propertyDictionary[propertyName] = color;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "float":
          {
            if (TryParseEnum(typeof(Floating), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "font":
          {
            if (TryParseFont(propertyValue, propertyDictionary))
            {
            }
            else
              ReportParseError(propertyName, propertyValue);

          }
          break;

        case "font-family":
          {
            var pv = propertyValue.Trim(new char[] { ' ', '\"' });
            if (!string.IsNullOrEmpty(pv))
              propertyDictionary[propertyName] = pv;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "font-size":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "font-style":
          {
            if (TryParseEnum(typeof(FontStyle), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "font-variant":
          {
            if (TryParseEnum(typeof(FontVariant), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "font-weight":
          {
            if (TryParseEnum(typeof(FontWeight), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else if (TryParseInteger(propertyValue, out var i))
              propertyDictionary.Add(propertyName, i);
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "height":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "left":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "letter-spacing":
          //  Implement letter-spacing conversion
          propertyDictionary[propertyName] = propertyValue;
          break;

        case "line-height":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "list-style-type":
          {
            if (TryParseEnum(typeof(ListStyle), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "margin-top":
        case "margin-right":
        case "margin-bottom":
        case "margin-left":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "max-height":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "max-width":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "outline-width":
        case "outline-style":
        case "outline-color":
          propertyDictionary[propertyName] = propertyValue;
          break;


        case "padding-top":
        case "padding-right":
        case "padding-bottom":
        case "padding-left":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "page-break-after":
        case "page-break-before":
        case "page-break-inside":
          {
            if (TryParseEnum(typeof(BreakMode), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "position":
          {
            if (TryParseEnum(typeof(PositionMode), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;


        case "src":
          propertyDictionary[propertyName] = propertyValue;
          break;

        case "text-align":
          {
            if (TryParseEnum(typeof(TextAlignLast), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "text-decoration":
          {
            if (TryParseEnum(typeof(TextDecorationLine), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "text-decoration-color":
          break;

        case "text-decoration-line":
          {
            if (TryParseFlagsEnum(propertyValue, out TextDecorationLine v))
            {
              if (v != TextDecorationLine.None)
                propertyDictionary[propertyName] = v;
            }
            else
            {
              ReportParseError(propertyName, propertyValue);
            }
          }
          break;

        case "text-decoration-style":
          {
            if (TryParseEnum(propertyValue, out TextDecorationStyle v))
            {
              propertyDictionary[propertyName] = v;
            }
            else
            {
              ReportParseError(propertyName, propertyValue);
            }
          }
          break;

        case "text-indent":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "text-transform":
          {
            if (TryParseEnum(typeof(TextTransform), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "top":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "vertical-align":
          {
            if (TryParseEnum(typeof(VerticalAlignment), propertyValue, out var v))
              propertyDictionary[propertyName] = v;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "width":
          {
            if (Length.TryParse(propertyValue, out var length))
              propertyDictionary[propertyName] = length;
            else
              ReportParseError(propertyName, propertyValue);
          }
          break;

        case "word-spacing":
          //  Implement word-spacing conversion
          propertyDictionary[propertyName] = propertyValue;
          break;

        default:
          {
            ReportNotImplementedPropertyError(propertyName, propertyValue);
          }
          break;
      }
    }

    #endregion

    #region Error handling

    static void ReportParseError(string propertyName, string propertyValue)
    {
      System.Diagnostics.Debug.WriteLine($"Property parse error: Name: {propertyName}, Value: {propertyValue}");
    }

    static void ReportNotImplementedPropertyError(string propertyName, string propertyValue)
    {
      System.Diagnostics.Debug.WriteLine($"Property parsing not implemented: Name: {propertyName}, Value: {propertyValue}");
    }

    #endregion

    #region Parsing from string


    static bool TryParseInteger(string stringValue, out int value)
    {
      return int.TryParse(stringValue, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    static bool TryParse<T>(string stringValue, IDictionary<string, T> dict, out T value) where T : struct
    {
      if (dict.TryGetValue(stringValue, out value))
      {
        return true;
      }
      else
      {
        value = default;
        return false;
      }
    }

    static bool TryParseEnum<T>(string stringValue, out T value) where T : struct
    {
      foreach (var v in Enum.GetValues(typeof(T)))
      {
        if (stringValue == Enum.GetName(typeof(T), v).ToLowerInvariant())
        {
          value = (T)v;
          return true;
        }
      }

      value = default;
      return false;
    }

    static bool TryParseEnum(Type enumType, string stringValue, out Enum value)
    {
      foreach (var v in Enum.GetValues(enumType))
      {
        if (stringValue == Enum.GetName(enumType, v).ToLowerInvariant())
        {
          value = (Enum)v;
          return true;
        }
      }

      value = default;
      return false;
    }

    static bool TryParseFlagsEnum<T>(string stringValue, out T value) where T : struct
    {
      foreach (var v in Enum.GetValues(typeof(T)))
      {
        if (stringValue == Enum.GetName(typeof(T), v).ToLowerInvariant())
        {
          value = (T)v;
          return true;
        }
      }

      value = default;
      return false;
    }

    static bool TryParseFlagsEnum(Type enumType, string stringValue, out Enum value)
    {
      foreach (var v in Enum.GetValues(enumType))
      {
        if (stringValue == Enum.GetName(enumType, v).ToLowerInvariant())
        {
          value = (Enum)v;
          return true;
        }
      }

      value = default;
      return false;
    }

    public static bool TryParseColor(string stringValue, out Color value)
    {
      var c1 = Colors.GetColor(stringValue);
      if (c1.HasValue)
      {
        value = c1.Value;
        return true;
      }

      if (stringValue.StartsWith("#") && Color.TryFromHex(stringValue.Substring(1), out var c2))
      {
        value = c2;
        return true;
      }

      if (stringValue.StartsWith("rgb"))
      {
        var rgb = new byte[3]; // default is r=0, g=0, b=0, which is black, seems to be a reasonable default

        try
        {
          var colorValueStrings = stringValue.Substring(3).Split(new char[] { ',', '(', ')' }, System.StringSplitOptions.RemoveEmptyEntries);
          if (colorValueStrings.Length == 3)
          {
            for (int i = 0; i < 3; ++i)
            {
              if (colorValueStrings[i].EndsWith("%"))
              {
                rgb[i] = (byte)(255 * double.Parse(colorValueStrings[i].Substring(0, colorValueStrings[i].Length - 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture));
              }
              else
              {
                rgb[i] = (byte)int.Parse(colorValueStrings[i], System.Globalization.CultureInfo.InvariantCulture);
              }
            }

            value = Color.FromRgb(rgb[0], rgb[1], rgb[2]);
            return true;
          }
        }
        catch (Exception)
        {

        }
      }
      value = default;
      return false;
    }

    private static bool TryParseFont(string stringValue, Dictionary<string, object> propertyDictionary)
    {
      throw new NotImplementedException();
      return false;

      /*
      var nextIndex = 0;

      ParseCssFontStyle(styleValue, ref nextIndex, localProperties);
      ParseCssFontVariant(styleValue, ref nextIndex, localProperties);
      ParseCssFontWeight(styleValue, ref nextIndex, localProperties);

      ParseCssSize(styleValue, ref nextIndex, localProperties, "font-size", true, (double)localProperties["font-size"]);

      ParseWhiteSpace(styleValue, ref nextIndex);
      if (nextIndex < styleValue.Length && styleValue[nextIndex] == '/')
      {
        nextIndex++;
        ParseCssSize(styleValue, ref nextIndex, localProperties, "line-height", true, (double)localProperties["font-size"]);
      }

      ParseCssFontFamily(styleValue, ref nextIndex, localProperties);
      */
    }

    #endregion
  }
}
