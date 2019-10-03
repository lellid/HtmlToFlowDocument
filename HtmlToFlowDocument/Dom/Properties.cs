// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public enum FontStyle
  {
    Normal,
    Oblique,
    Italic
  }

  public enum FontWeight
  {

    Thin = 100,
    ExtraLight = 300,

    UltraLight = 300,

    Light = 300,

    Normal = 400,

    Regular = 400,
    Medium = 500,
    DemiBold = 600,

    SemiBold = 600,
    Bold = 700,
    ExtraBold = 800,

    UltraBold = 800,
    Black = 900,

    Heavy = 900,
    ExtraBlack = 950,

    UltraBlack = 950
  }

  public enum TextDecorations
  {
    None = 0,
    Underline = 1,
  }

  public enum TextAlignment
  {
    Left = 0,
    Right = 1,
    Center = 2,
    Justify = 3
  }


  public struct Thickness
  {
    public double Left;
    public double Right;
    public double Top;
    public double Bottom;

    public Thickness WithLeft(double l)
    {
      return new Thickness { Bottom = this.Bottom, Top = this.Top, Right = this.Right, Left = l };
    }

    public override string ToString()
    {
      if (Left == Right && Left == Top && Left == Bottom)
        return Left.ToString();
      else
        return string.Concat("(", Left.ToString(), ", ", Top.ToString(), ", ", Right.ToString(), ", ", Bottom.ToString(), ")");
    }
  }

  public enum ListMarkerStyle
  {
    None,
    Square,
    Box,
    LowerLatin,
    UpperLatin,
    LowerRoman,
    UpperRoman,
    Decimal,
    Disc,
    Circle,
  }
  public static class PropertyExtensions
  {
    public static FontStyle GetFontStyle(string txt)
    {
      switch (txt.ToLowerInvariant().Trim())
      {
        case "oblique":
          return FontStyle.Oblique;
        case "italic":
          return FontStyle.Italic;
        default:
          return FontStyle.Normal;
      }
    }

    public static FontWeight GetFontWeight(string txt)
    {
      switch (txt.ToLowerInvariant().Trim())
      {
        case "thin":
          return FontWeight.Thin;
        case "ultralight":
          return FontWeight.UltraLight;
        case "extralight":
          return FontWeight.ExtraLight;
        case "light":
          return FontWeight.Light;
        case "normal":
          return FontWeight.Normal;
        case "regular":
          return FontWeight.Regular;
        case "medium":
          return FontWeight.Medium;
        case "demibold":
          return FontWeight.DemiBold;
        case "semibold":
          return FontWeight.SemiBold;
        case "bold":
          return FontWeight.Bold;
        case "extrabold":
          return FontWeight.ExtraBold;
        case "ultrabold":
          return FontWeight.UltraBold;
        case "black":
          return FontWeight.Black;
        case "heavy":
          return FontWeight.Heavy;
        case "extrablack":
          return FontWeight.ExtraBlack;
        case "ultrablack":
          return FontWeight.UltraBlack;
        default:
          return FontWeight.Normal;
      }
    }

    public static double? GetFontSize(string fontSizeString, double defaultFontSize)
    {
      fontSizeString = fontSizeString.ToLowerInvariant().Trim();

      int sizeUnit = 0;
      double defaultFontSizeFactor = 0;
      double fontSizeFactor = 1;


      if (fontSizeString.EndsWith("pt"))
      {
        sizeUnit = 2;
        fontSizeFactor = 1;
        defaultFontSizeFactor = 0;

      }
      else if (fontSizeString.EndsWith("em"))
      {
        sizeUnit = 2;
        fontSizeFactor = 0;
        defaultFontSizeFactor = 1;
      }
      else
      {
        sizeUnit = 0;
        fontSizeFactor = 1;
        defaultFontSizeFactor = 0;
      }

      var valStrg = fontSizeString.Substring(0, fontSizeString.Length - sizeUnit);

      if (double.TryParse(valStrg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fontSizeRaw))
      {
        return fontSizeRaw * fontSizeFactor + defaultFontSize * defaultFontSizeFactor;
      }
      else
      {
        return null;
      }
    }

    public static int? GetColor(string styleValue)
    {
      // Implement color parsing
      // rgb(100%,53.5%,10%)
      // rgb(255,91,26)
      // #FF5B1A
      // black | silver | gray | ... | aqua
      // transparent - for background-color


      styleValue = styleValue?.Trim();

      if (string.IsNullOrEmpty(styleValue))
        return null;

      int? color = null;

      if (styleValue[0] == '#')
      {
        if (styleValue.Length == 7 || styleValue.Length == 9)
        {
          if (uint.TryParse(styleValue.Substring(1), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var result))
          {
            if (styleValue.Length == 7) // Shift so that R is the MSB
            {
              result <<= 8;
              result |= 0xFF; // and add opaquness
            }

            color = (int)result;
          }
        }
      }
      else if (styleValue.Substring(0, 3).ToLower() == "rgb")
      {
        int nextIndex = 0;
        var oldIndex = nextIndex + 3;
        //  Implement real rgb() color parsing
        while (nextIndex < styleValue.Length && styleValue[nextIndex] != ')')
        {
          nextIndex++;
        }

        var rgb = new byte[3]; // default is r=0, g=0, b=0, which is black, seems to be a reasonable default

        try
        {
          var colorValueStrings = styleValue.Substring(oldIndex, nextIndex - oldIndex).Split(new char[] { ',', '(', ')' }, System.StringSplitOptions.RemoveEmptyEntries);
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
          }
        }
        catch (Exception)
        {

        }


        if (nextIndex < styleValue.Length)
        {
          nextIndex++; // to skip ')'
        }

        uint result = 0;
        result |= rgb[0];
        result <<= 8;
        result |= rgb[1];
        result <<= 8;
        result |= rgb[2];
        result <<= 8;
        result |= 0xFF;


        color = (int)result;
      }
      else if (char.IsLetter(styleValue[0]))
      {
        if (KnownColorsByName.TryGetValue(styleValue, out var c))
        {
          color = (int)(c << 8 | 0xFF);
        }
      }


      return color;
    }

    private static Dictionary<string, int> KnownColorsByName = new Dictionary<string, int>()
    {
      ["AliceBlue"] = 0xF0F8FF,
      ["AntiqueWhite"] = 0xFAEBD7,
      ["Aqua"] = 0x00FFFF,
      ["Aquamarine"] = 0x7FFFD4,
      ["Azure"] = 0xF0FFFF,
      ["Beige"] = 0xF5F5DC,
      ["Bisque"] = 0xFFE4C4,
      ["Black"] = 0x000000,
      ["BlanchedAlmond"] = 0xFFEBCD,
      ["Blue"] = 0x0000FF,
      ["BlueViolet"] = 0x8A2BE2,
      ["Brown"] = 0xA52A2A,
      ["BurlyWood"] = 0xDEB887,
      ["CadetBlue"] = 0x5F9EA0,
      ["Chartreuse"] = 0x7FFF00,
      ["Chocolate"] = 0xD2691E,
      ["Coral"] = 0xFF7F50,
      ["CornflowerBlue"] = 0x6495ED,
      ["Cornsilk"] = 0xFFF8DC,
      ["Crimson"] = 0xDC143C,
      ["Cyan"] = 0x00FFFF,
      ["DarkBlue"] = 0x00008B,
      ["DarkCyan"] = 0x008B8B,
      ["DarkGoldenrod"] = 0xB8860B,
      ["DarkGray"] = 0xA9A9A9,
      ["DarkGreen"] = 0x006400,
      ["DarkKhaki"] = 0xBDB76B,
      ["DarkMagenta"] = 0x8B008B,
      ["DarkOliveGreen"] = 0x556B2F,
      ["DarkOrange"] = 0xFF8C00,
      ["DarkOrchid"] = 0x9932CC,
      ["DarkRed"] = 0x8B0000,
      ["DarkSalmon"] = 0xE9967A,
      ["DarkSeaGreen"] = 0x8FBC8F,
      ["DarkSlateBlue"] = 0x483D8B,
      ["DarkSlateGray"] = 0x2F4F4F,
      ["DarkTurquoise"] = 0x00CED1,
      ["DarkViolet"] = 0x9400D3,
      ["DeepPink"] = 0xFF1493,
      ["DeepSkyBlue"] = 0x00BFFF,
      ["DimGray"] = 0x696969,
      ["DodgerBlue"] = 0x1E90FF,
      ["Firebrick"] = 0xB22222,
      ["FloralWhite"] = 0xFFFAF0,
      ["ForestGreen"] = 0x228B22,
      ["Fuchsia"] = 0xFF00FF,
      ["Gainsboro"] = 0xDCDCDC,
      ["GhostWhite"] = 0xF8F8FF,
      ["Gold"] = 0xFFD700,
      ["Goldenrod"] = 0xDAA520,
      ["Gray"] = 0x808080,
      ["Green"] = 0x008000,
      ["GreenYellow"] = 0xADFF2F,
      ["Honeydew"] = 0xF0FFF0,
      ["HotPink"] = 0xFF69B4,
      ["IndianRed"] = 0xCD5C5C,
      ["Indigo"] = 0x4B0082,
      ["Ivory"] = 0xFFFFF0,
      ["Khaki"] = 0xF0E68C,
      ["Lavender"] = 0xE6E6FA,
      ["LavenderBlush"] = 0xFFF0F5,
      ["LawnGreen"] = 0x7CFC00,
      ["LemonChiffon"] = 0xFFFACD,
      ["LightBlue"] = 0xADD8E6,
      ["LightCoral"] = 0xF08080,
      ["LightCyan"] = 0xE0FFFF,
      ["LightGoldenrodYellow"] = 0xFAFAD2,
      ["LightGray"] = 0xD3D3D3,
      ["LightGreen"] = 0x90EE90,
      ["LightPink"] = 0xFFB6C1,
      ["LightSalmon"] = 0xFFA07A,
      ["LightSeaGreen"] = 0x20B2AA,
      ["LightSkyBlue"] = 0x87CEFA,
      ["LightSlateGray"] = 0x778899,
      ["LightSteelBlue"] = 0xB0C4DE,
      ["LightYellow"] = 0xFFFFE0,
      ["Lime"] = 0x00FF00,
      ["LimeGreen"] = 0x32CD32,
      ["Linen"] = 0xFAF0E6,
      ["Magenta"] = 0xFF00FF,
      ["Maroon"] = 0x800000,
      ["MediumAquamarine"] = 0x66CDAA,
      ["MediumBlue"] = 0x0000CD,
      ["MediumOrchid"] = 0xBA55D3,
      ["MediumPurple"] = 0x9370DB,
      ["MediumSeaGreen"] = 0x3CB371,
      ["MediumSlateBlue"] = 0x7B68EE,
      ["MediumSpringGreen"] = 0x00FA9A,
      ["MediumTurquoise"] = 0x48D1CC,
      ["MediumVioletRed"] = 0xC71585,
      ["MidnightBlue"] = 0x191970,
      ["MintCream"] = 0xF5FFFA,
      ["MistyRose"] = 0xFFE4E1,
      ["Moccasin"] = 0xFFE4B5,
      ["NavajoWhite"] = 0xFFDEAD,
      ["Navy"] = 0x000080,
      ["OldLace"] = 0xFDF5E6,
      ["Olive"] = 0x808000,
      ["OliveDrab"] = 0x6B8E23,
      ["Orange"] = 0xFFA500,
      ["OrangeRed"] = 0xFF4500,
      ["Orchid"] = 0xDA70D6,
      ["PaleGoldenrod"] = 0xEEE8AA,
      ["PaleGreen"] = 0x98FB98,
      ["PaleTurquoise"] = 0xAFEEEE,
      ["PaleVioletRed"] = 0xDB7093,
      ["PapayaWhip"] = 0xFFEFD5,
      ["PeachPuff"] = 0xFFDAB9,
      ["Peru"] = 0xCD853F,
      ["Pink"] = 0xFFC0CB,
      ["Plum"] = 0xDDA0DD,
      ["PowderBlue"] = 0xB0E0E6,
      ["Purple"] = 0x800080,
      ["Red"] = 0xFF0000,
      ["RosyBrown"] = 0xBC8F8F,
      ["RoyalBlue"] = 0x4169E1,
      ["SaddleBrown"] = 0x8B4513,
      ["Salmon"] = 0xFA8072,
      ["SandyBrown"] = 0xF4A460,
      ["SeaGreen"] = 0x2E8B57,
      ["SeaShell"] = 0xFFF5EE,
      ["Sienna"] = 0xA0522D,
      ["Silver"] = 0xC0C0C0,
      ["SkyBlue"] = 0x87CEEB,
      ["SlateBlue"] = 0x6A5ACD,
      ["SlateGray"] = 0x708090,
      ["Snow"] = 0xFFFAFA,
      ["SpringGreen"] = 0x00FF7F,
      ["SteelBlue"] = 0x4682B4,
      ["Tan"] = 0xD2B48C,
      ["Teal"] = 0x008080,
      ["Thistle"] = 0xD8BFD8,
      ["Tomato"] = 0xFF6347,
      ["Transparent"] = 0xFFFFFF,
      ["Turquoise"] = 0x40E0D0,
      ["Violet"] = 0xEE82EE,
      ["Wheat"] = 0xF5DEB3,
      ["White"] = 0xFFFFFF,
      ["WhiteSmoke"] = 0xF5F5F5,
      ["Yellow"] = 0xFFFF00,
      ["YellowGreen"] = 0x9ACD32,

    };

    private static Dictionary<int, string> KnownColorsByValue = new Dictionary<int, string>()
    {
      [0xF0F8FF] = "AliceBlue",
      [0xFAEBD7] = "AntiqueWhite",
      [0x00FFFF] = "Aqua",
      [0x7FFFD4] = "Aquamarine",
      [0xF0FFFF] = "Azure",
      [0xF5F5DC] = "Beige",
      [0xFFE4C4] = "Bisque",
      [0x000000] = "Black",
      [0xFFEBCD] = "BlanchedAlmond",
      [0x0000FF] = "Blue",
      [0x8A2BE2] = "BlueViolet",
      [0xA52A2A] = "Brown",
      [0xDEB887] = "BurlyWood",
      [0x5F9EA0] = "CadetBlue",
      [0x7FFF00] = "Chartreuse",
      [0xD2691E] = "Chocolate",
      [0xFF7F50] = "Coral",
      [0x6495ED] = "CornflowerBlue",
      [0xFFF8DC] = "Cornsilk",
      [0xDC143C] = "Crimson",
      [0x00FFFF] = "Cyan",
      [0x00008B] = "DarkBlue",
      [0x008B8B] = "DarkCyan",
      [0xB8860B] = "DarkGoldenrod",
      [0xA9A9A9] = "DarkGray",
      [0x006400] = "DarkGreen",
      [0xBDB76B] = "DarkKhaki",
      [0x8B008B] = "DarkMagenta",
      [0x556B2F] = "DarkOliveGreen",
      [0xFF8C00] = "DarkOrange",
      [0x9932CC] = "DarkOrchid",
      [0x8B0000] = "DarkRed",
      [0xE9967A] = "DarkSalmon",
      [0x8FBC8F] = "DarkSeaGreen",
      [0x483D8B] = "DarkSlateBlue",
      [0x2F4F4F] = "DarkSlateGray",
      [0x00CED1] = "DarkTurquoise",
      [0x9400D3] = "DarkViolet",
      [0xFF1493] = "DeepPink",
      [0x00BFFF] = "DeepSkyBlue",
      [0x696969] = "DimGray",
      [0x1E90FF] = "DodgerBlue",
      [0xB22222] = "Firebrick",
      [0xFFFAF0] = "FloralWhite",
      [0x228B22] = "ForestGreen",
      [0xFF00FF] = "Fuchsia",
      [0xDCDCDC] = "Gainsboro",
      [0xF8F8FF] = "GhostWhite",
      [0xFFD700] = "Gold",
      [0xDAA520] = "Goldenrod",
      [0x808080] = "Gray",
      [0x008000] = "Green",
      [0xADFF2F] = "GreenYellow",
      [0xF0FFF0] = "Honeydew",
      [0xFF69B4] = "HotPink",
      [0xCD5C5C] = "IndianRed",
      [0x4B0082] = "Indigo",
      [0xFFFFF0] = "Ivory",
      [0xF0E68C] = "Khaki",
      [0xE6E6FA] = "Lavender",
      [0xFFF0F5] = "LavenderBlush",
      [0x7CFC00] = "LawnGreen",
      [0xFFFACD] = "LemonChiffon",
      [0xADD8E6] = "LightBlue",
      [0xF08080] = "LightCoral",
      [0xE0FFFF] = "LightCyan",
      [0xFAFAD2] = "LightGoldenrodYellow",
      [0xD3D3D3] = "LightGray",
      [0x90EE90] = "LightGreen",
      [0xFFB6C1] = "LightPink",
      [0xFFA07A] = "LightSalmon",
      [0x20B2AA] = "LightSeaGreen",
      [0x87CEFA] = "LightSkyBlue",
      [0x778899] = "LightSlateGray",
      [0xB0C4DE] = "LightSteelBlue",
      [0xFFFFE0] = "LightYellow",
      [0x00FF00] = "Lime",
      [0x32CD32] = "LimeGreen",
      [0xFAF0E6] = "Linen",
      [0xFF00FF] = "Magenta",
      [0x800000] = "Maroon",
      [0x66CDAA] = "MediumAquamarine",
      [0x0000CD] = "MediumBlue",
      [0xBA55D3] = "MediumOrchid",
      [0x9370DB] = "MediumPurple",
      [0x3CB371] = "MediumSeaGreen",
      [0x7B68EE] = "MediumSlateBlue",
      [0x00FA9A] = "MediumSpringGreen",
      [0x48D1CC] = "MediumTurquoise",
      [0xC71585] = "MediumVioletRed",
      [0x191970] = "MidnightBlue",
      [0xF5FFFA] = "MintCream",
      [0xFFE4E1] = "MistyRose",
      [0xFFE4B5] = "Moccasin",
      [0xFFDEAD] = "NavajoWhite",
      [0x000080] = "Navy",
      [0xFDF5E6] = "OldLace",
      [0x808000] = "Olive",
      [0x6B8E23] = "OliveDrab",
      [0xFFA500] = "Orange",
      [0xFF4500] = "OrangeRed",
      [0xDA70D6] = "Orchid",
      [0xEEE8AA] = "PaleGoldenrod",
      [0x98FB98] = "PaleGreen",
      [0xAFEEEE] = "PaleTurquoise",
      [0xDB7093] = "PaleVioletRed",
      [0xFFEFD5] = "PapayaWhip",
      [0xFFDAB9] = "PeachPuff",
      [0xCD853F] = "Peru",
      [0xFFC0CB] = "Pink",
      [0xDDA0DD] = "Plum",
      [0xB0E0E6] = "PowderBlue",
      [0x800080] = "Purple",
      [0xFF0000] = "Red",
      [0xBC8F8F] = "RosyBrown",
      [0x4169E1] = "RoyalBlue",
      [0x8B4513] = "SaddleBrown",
      [0xFA8072] = "Salmon",
      [0xF4A460] = "SandyBrown",
      [0x2E8B57] = "SeaGreen",
      [0xFFF5EE] = "SeaShell",
      [0xA0522D] = "Sienna",
      [0xC0C0C0] = "Silver",
      [0x87CEEB] = "SkyBlue",
      [0x6A5ACD] = "SlateBlue",
      [0x708090] = "SlateGray",
      [0xFFFAFA] = "Snow",
      [0x00FF7F] = "SpringGreen",
      [0x4682B4] = "SteelBlue",
      [0xD2B48C] = "Tan",
      [0x008080] = "Teal",
      [0xD8BFD8] = "Thistle",
      [0xFF6347] = "Tomato",
      [0xFFFFFF] = "Transparent",
      [0x40E0D0] = "Turquoise",
      [0xEE82EE] = "Violet",
      [0xF5DEB3] = "Wheat",
      [0xFFFFFF] = "White",
      [0xF5F5F5] = "WhiteSmoke",
      [0xFFFF00] = "Yellow",
      [0x9ACD32] = "YellowGreen",
    };


    public static TextAlignment? GetTextAlignment(string val)
    {
      switch (val.ToLowerInvariant().Trim())
      {
        case "left":
          return TextAlignment.Left;
        case "right":
          return TextAlignment.Right;
        case "center":
          return TextAlignment.Center;
        case "justify":
          return TextAlignment.Justify;
        default:
          return null;
      }
    }

    public static int? GetInteger(string s)
    {
      if (int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var result))
        return result;
      else return null;
    }

    // Create syntactically optimized four-value Thickness
    public static Thickness? GetThickness(
      string left,
      string right,
      string top,
      string bottom,
      double defaultFontSize)
    {
      var l = GetFontSize(left, defaultFontSize);
      var r = GetFontSize(right, defaultFontSize);
      var t = GetFontSize(top, defaultFontSize);
      var b = GetFontSize(bottom, defaultFontSize);

      var result = new Thickness();

      if (l.HasValue)
        result.Left = l.Value;
      if (r.HasValue)
        result.Right = r.Value;
      if (t.HasValue)
        result.Top = t.Value;
      if (b.HasValue)
        result.Bottom = b.Value;

      return l.HasValue || r.HasValue || t.HasValue || b.HasValue ? (Thickness?)result : null;
    }

    public static Thickness? GetThickness(double? left, double? right, double? top, double? bottom)
    {
      var result = new Thickness();

      if (left.HasValue)
        result.Left = left.Value;
      if (right.HasValue)
        result.Right = right.Value;
      if (top.HasValue)
        result.Top = top.Value;
      if (bottom.HasValue)
        result.Bottom = bottom.Value;

      return left.HasValue || right.HasValue || top.HasValue || bottom.HasValue ? (Thickness?)result : null;
    }

  }
}
