using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public class ListItem : TextElement
  {

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);

      if (!(child is Block))
        throw new ArgumentException("Child must be derived from Block", nameof(child));
    }
  }
}
