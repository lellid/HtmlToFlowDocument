using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public class TableCell : TextElement
  {
    public int ColumnSpan { get; set; } = 1;

    public int RowSpan { get; set; } = 1;

    public int? BorderBrush { get; set; }

    public Thickness? BorderThickness { get; set; }


    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      ThrowIfChildIsNoneOfThisTypes(child, typeof(BlockUIContainer), typeof(List), typeof(Paragraph), typeof(Section), typeof(Table));
    }
  }
}
