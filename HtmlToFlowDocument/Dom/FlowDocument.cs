// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public class FlowDocument : Block
  {

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);

      if (!(child is Block))
        throw new ArgumentException("Child must by of type Block", nameof(child));
    }
  }
}
