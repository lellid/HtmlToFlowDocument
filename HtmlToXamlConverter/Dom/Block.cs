using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public abstract class Block : TextElement
  {
    public Thickness? Margin { get; set; }

    public Thickness? Padding { get; set; }

    public int? BorderBrush { get; set; }

    public Thickness? BorderThickness { get; set; }

    public TextAlignment? TextAlignment { get; set; }
  }
}
