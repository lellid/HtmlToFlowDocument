// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public class LineBreak : Inline
  {

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);

      throw new ArgumentException("LineBreak can't have any childs");
    }
  }
}
