// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
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
