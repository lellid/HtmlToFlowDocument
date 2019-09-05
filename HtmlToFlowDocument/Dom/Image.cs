// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public class Image : TextElement
  {
    public string Source { get; set; }

    public double? Width { get; set; }

    public bool IsWidthInPercentOfPage { get; set; }

    public double? Height { get; set; }

    public bool IsHeightInPercentOfPage { get; set; }

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      throw new ArgumentException("Image can't have any childs", nameof(child));
    }
  }
}
