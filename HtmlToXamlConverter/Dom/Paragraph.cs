using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public class Paragraph : Block
  {
    public TextDecorations? TextDecorations { get; set; }

    public double? TextIndent { get; set; }

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);

      if (!(child is Inline))
        throw new ArgumentException("Child must be derived from Inline", nameof(child));
    }
  }
}
