using System;

namespace ExCSS
{
  [Flags]
  public enum TextDecorationLine : byte
  {
    None = 0x00,
    Underline = 0x01,
    Overline = 0x02,
    LineThrough = 0x04,
    Blink = 0x08
  }
}
