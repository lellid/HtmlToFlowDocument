using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public class List : Block
  {
    public ListMarkerStyle? MarkerStyle { get; set; }

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);

      ThrowIfChildIsNoneOfThisTypes(child, typeof(ListItem));
    }
  }



}
