using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public class TableRowGroup : TextElement
  {

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      ThrowIfChildIsNoneOfThisTypes(child, typeof(TableRow));
    }
  }
}
