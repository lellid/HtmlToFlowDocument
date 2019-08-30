// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public class TableCell : TextElement
  {
    int _columnSpan = 1;
    public int ColumnSpan
    {
      get { return _columnSpan; }
      set
      {
        if (value >= 1)
          _columnSpan = value;
        else
          throw new ArgumentException("Should be >=1", nameof(ColumnSpan));
      }
    }

    int _rowSpan = 1;
    public int RowSpan
    {
      get { return _rowSpan; }
      set
      {
        if (value >= 1)
          _rowSpan = value;
        else
          throw new ArgumentException("Should be >=1", nameof(RowSpan));
      }
    }

    public int? BorderBrush { get; set; }

    public Thickness? BorderThickness { get; set; }


    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      ThrowIfChildIsNoneOfThisTypes(child, typeof(BlockUIContainer), typeof(List), typeof(Paragraph), typeof(Section), typeof(Table));
    }
  }
}
