﻿// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace HtmlToFlowDocument.Rendering
{
  /// <summary>
  /// Helper functions for enumeration through the tree of <see cref="TextElement"/>s.
  /// </summary>
  public static class WpfHelper
  {
    /// <summary>
    /// Gets the immediate childs (but not the grand childs) of the given Wpf <see cref="TextElement"/> or <see cref="FlowDocument"/>.
    /// </summary>
    /// <param name="wpf">The Wpf text element.</param>
    /// <returns>The immediate childs (but not the grand childs) of the given <see cref="TextElement"/> or <see cref="FlowDocument"/>.</returns>
    public static IEnumerable<TextElement> GetImmediateChildsOf(FrameworkContentElement wpf)
    {
      switch (wpf)
      {
        case Figure figure:
          return figure.Blocks;
        case Floater floater:
          return floater.Blocks;
        case FlowDocument flowDocument:
          return flowDocument.Blocks;
        case List list:
          return list.ListItems;
        case ListItem listItem:
          return listItem.Blocks;
        case Section section:
          return section.Blocks;
        case Table table:
          return table.RowGroups;
        case TableCell tableCell:
          return tableCell.Blocks;
        case TableRow tableRow:
          return tableRow.Cells;
        case TableRowGroup tableRowGroup:
          return tableRowGroup.Rows;
        // now elements that can contain inlines
        case Paragraph paragraph:
          return paragraph.Inlines;
        case Span span:
          return span.Inlines;
        default:
          return new TextElement[0];
      }
    }

    /// <summary>
    /// Gets a bookmark string from a given text element. In a document of exactly the same structure this bookmark string
    /// can be used to find the TextElement again by using <see cref="GetTextElementFromBookmark(FrameworkContentElement, string)"/>.
    /// </summary>
    /// <param name="wpf">The text element.</param>
    /// <returns>A bookmark string that can be used to find that text element again.</returns>
    public static string GetBookmarkFromTextElement(FrameworkContentElement wpf)
    {
      var result = new StringBuilder();
      var child = wpf;
      while (child.Parent is FrameworkContentElement parent)
      {

        int index = 0;
        foreach (var c in GetImmediateChildsOf(parent))
        {
          if (object.ReferenceEquals(c, child))
          {
            break;
          }
          else
          {
            ++index;
          }
        }

        result.Insert(0, string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} ", index));

        child = parent;
      }
      return result.ToString();
    }

    /// <summary>
    /// Given the bookmark string returned by <see cref="GetBookmarkFromTextElement(FrameworkContentElement)"/>, in a FlowDocument
    /// of the same structure this function returns the same text element again.
    /// </summary>
    /// <param name="doc">The flow document.</param>
    /// <param name="bookmark">The bookmark string.</param>
    /// <returns>The text element that corresponds with the bookmark string.</returns>
    public static FrameworkContentElement GetTextElementFromBookmark(FlowDocument doc, string bookmark)
    {
      var bm = bookmark.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      FrameworkContentElement result = doc;

      foreach (var numStr in bm)
      {
        int num = int.Parse(numStr, System.Globalization.CultureInfo.InvariantCulture);

        foreach (var c in GetImmediateChildsOf(result))
        {
          if (num == 0)
          {
            result = c;
            break;
          }
          else
          {
            --num;
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Gets the text elements beginning with the text element given in the argument, and then all subsequent text elements of higher, equal, or lower levels until the end of the document.
    /// </summary>
    /// <param name="te">The <see cref="TextElement"/> to start with.</param>
    /// <returns>An enumeration beginning with the text element given in the argument, and then all subsequent text elements of higher, equal, or lower levels, until the end of the document.</returns>
    public static IEnumerable<TextElement> GetTextElementsBeginningWith(TextElement te)
    {
      foreach (var children in GetThisAndAllChildTextElements(te))
        yield return children;

      foreach (var other in GetTextElementsAfter(te))
        yield return other;
    }

    /// <summary>
    /// Returns an enumeration which contains the TextElement given in the argument and all child elements, including grandchilds and childs of higher levels.
    /// </summary>
    /// <param name="te">The <see cref="TextElement"/> to start the enumeration with.</param>
    /// <returns>An enumeration which contains the TextElement given in the argument and all child elements, including grandchilds and childs of higher levels.</returns>
    public static IEnumerable<TextElement> GetThisAndAllChildTextElements(TextElement te)
    {
      yield return te;
      foreach (var child in GetImmediateChildsOf(te))
        foreach (var e in GetThisAndAllChildTextElements(child).OfType<TextElement>())
          yield return e;
    }

    /// <summary>
    /// Returns an enumeration which contains all <see cref="TextElement"/>s which come after the <see cref="TextElement"/> given in the argument.
    /// The <see cref="TextElement"/> given in the argument is <b>not</b> included in the returned enumeration.
    /// </summary>
    /// <param name="te">The <see cref="TextElement"/> which is not included in the enumeration.</param>
    /// <returns>An enumeration which contains all <see cref="TextElement"/>s which come after the <see cref="TextElement"/> given in the argument.
    /// The <see cref="TextElement"/> given in the argument is <b>not</b> included in the returned enumeration.
    /// </returns>
    public static IEnumerable<TextElement> GetTextElementsAfter(TextElement te)
    {
      var parent = te.Parent as FrameworkContentElement;
      if (null == parent)
        yield break;

      bool stopAtNext = false;
      TextElement nextChild = null;
      foreach (var c in GetImmediateChildsOf(parent))
      {
        if (stopAtNext)
        {
          nextChild = c;
          break;
        }
        if (object.ReferenceEquals(te, c))
        {
          stopAtNext = true;
        }
      }

      if (null == nextChild)
      {
        // go one level back
        if (parent is TextElement parentAsTextElement)
        {
          foreach (var c in GetTextElementsAfter(parentAsTextElement))
            yield return c;
        }
      }
      else
      {
        foreach (var c in GetThisAndAllChildTextElements(nextChild))
          yield return c;
        foreach (var c in GetTextElementsAfter(nextChild))
          yield return c;
      }
    }
  }
}
