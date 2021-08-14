// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public class Run : Inline
  {
    public string Text { get; set; }
    public Run()
    {

    }

    public Run(string text)
    {
      Text = text;
    }

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      throw new ArgumentException("Run can't have child elements");
    }
  }
}
