using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public class Table : Block
  {
    public List<TableColumn> Columns { get; } = new List<TableColumn>();

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      ThrowIfChildIsNoneOfThisTypes(child, typeof(TableRowGroup));
    }
  }
}
