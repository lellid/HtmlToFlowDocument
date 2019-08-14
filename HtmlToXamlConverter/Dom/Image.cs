using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public class Image : TextElement
  {
    public string Source { get; set; }

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      throw new ArgumentException("Image can't have any childs", nameof(child));
    }
  }
}
