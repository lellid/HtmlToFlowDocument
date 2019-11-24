// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public abstract class Block : TextElement
  {
    public Thickness? Margin { get; set; }

    public Thickness? Padding { get; set; }

    public ExCSS.Color? BorderBrush { get; set; }

    public Thickness? BorderThickness { get; set; }

    public TextAlignment? TextAlignment { get; set; }

    public double? LineHeight { get; set; }
  }
}
