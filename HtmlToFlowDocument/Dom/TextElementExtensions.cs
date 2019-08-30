// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public static class TextElementExtensions
  {
    public static IEnumerable<TextElement> GetThisAndAllAfter(this TextElement te)
    {
      foreach (var children in GetThisAndAllChilds(te))
        yield return children;

      foreach (var other in GetElementsAfter(te))
        yield return other;
    }

    public static IEnumerable<TextElement> GetThisAndAllChilds(this TextElement te)
    {
      yield return te;
      foreach (var child in te.Childs)
        foreach (var e in GetThisAndAllChilds(child))
          yield return e;
    }

    public static IEnumerable<TextElement> GetElementsAfter(this TextElement te)
    {
      var parent = te.Parent;
      if (null == parent)
        yield break;

      bool stopAtNext = false;
      TextElement nextChild = null;
      foreach (var c in parent.Childs)
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
        foreach (var c in GetElementsAfter(parent))
          yield return parent;
      }
      else
      {
        foreach (var c in GetThisAndAllChilds(nextChild))
          yield return c;
        foreach (var c in GetElementsAfter(nextChild))
          yield return c;
      }
    }

  }
}
