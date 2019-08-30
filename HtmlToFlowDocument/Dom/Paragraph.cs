// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
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
