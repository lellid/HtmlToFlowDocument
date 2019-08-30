// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public class Span : Inline
  {
    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      if (!(child is Inline))
        throw new ArgumentException("Child must be derived from Inline", nameof(child));

      base.ThrowOnInvalidChildElement(child);
    }
  }
}
