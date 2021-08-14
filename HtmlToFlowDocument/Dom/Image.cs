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

    /// <summary>
    /// Gets or sets the width of the element. It contains of one or multiple length values, which must be added up to get the final length.
    /// </summary>
    /// <value>
    /// The width of the element.
    /// </value>
    public CompoundLength Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the element. It contains of one or multiple length values, which must be added up to get the final length.
    /// </summary>
    /// <value>
    /// The height of the element.
    /// </value>
    public CompoundLength Height { get; set; }


    /// <summary>
    /// Gets or sets the maximum width of the element. It contains of one or multiple length values, which must be added up to get the final length.
    /// </summary>
    /// <value>
    /// The maximum width of the element.
    /// </value>
    public ExCSS.Length? MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the maximum height of the element. It contains of one or multiple length values, which must be added up to get the final length.
    /// </summary>
    /// <value>
    /// The maximum height of the element.
    /// </value>
    public ExCSS.Length? MaxHeight { get; set; }

    protected override void ThrowOnInvalidChildElement(TextElement child)
    {
      base.ThrowOnInvalidChildElement(child);
      throw new ArgumentException("Image can't have any childs", nameof(child));
    }
  }
}
