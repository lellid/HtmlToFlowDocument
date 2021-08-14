﻿// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HtmlToFlowDocument.Dom
{
  public abstract class TextElement
  {
    public object Tag { get; set; }

    public TextElement Parent { get; set; }

    public string FontFamily { get; set; }

    public double? FontSize { get; set; }

    public FontStyle? FontStyle { get; set; }

    public FontWeight? FontWeight { get; set; }

    public ExCSS.Color? Foreground { get; set; }

    public ExCSS.Color? Background { get; set; }


    /// <summary>
    /// Gets a value indicating whether this instance has local properties set.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has properties set; otherwise, <c>false</c>.
    /// </value>
    public virtual bool HasAttributes
    {
      get
      {
        return
          null != FontSize ||
          null != FontFamily ||
          null != FontStyle ||
          null != FontWeight ||
          null != Foreground ||
          null != Background;
        ;
      }

    }

    public double? FontSizeLocalOrInherited
    {
      get
      {
        var instance = this;
        double? result = null;
        while (instance != null && result == null)
        {

          result = instance.FontSize;
          instance = instance.Parent;
        }

        return result;
      }
    }

    /// <summary>
    /// Gets the foreground color this text element would inherit. The local value of ForegroundColor is not put into consideration.
    /// </summary>
    /// <value>
    /// The foreground color this text element would inherit.
    /// </value>
    public ExCSS.Color? ForegroundInheritedOnly
    {
      get
      {
        var instance = this.Parent;
        ExCSS.Color? result = null;
        while (instance != null && result == null)
        {

          result = instance.Foreground;
          instance = instance.Parent;
        }
        return result;
      }
    }

    /// <summary>
    /// Gets the background color this text element would inherit. The local value of BackgroundColor is not put into consideration.
    /// </summary>
    /// <value>
    /// The background color this text element would inherit.
    /// </value>
    public ExCSS.Color? BackgroundInheritedOnly
    {
      get
      {
        var instance = this.Parent;
        ExCSS.Color? result = null;
        while (instance != null && result == null)
        {

          result = instance.Background;
          instance = instance.Parent;
        }
        return result;
      }
    }




    #region Children

    private List<TextElement> _childs = new List<TextElement>();


    public IReadOnlyList<TextElement> Childs { get { return _childs; } }

    /// <summary>
    /// Appends a child to this element.
    /// </summary>
    /// <param name="child">The child to append.</param>
    public virtual void AppendChild(TextElement child)
    {
      ThrowOnInvalidChildElement(child);
      child.Parent = this;
      _childs.Add(child);
    }

    protected virtual void ThrowOnInvalidChildElement(TextElement child)
    {
      // we throw an exception if the child belongs to another parent
      // Note: often it is neccessary to assign the future parent before it is actually
      // added here. This is because we need the inherited font size for some calculations
      if (child.Parent != null && !object.ReferenceEquals(this, child.Parent))
        throw new ArgumentException("Child element already has a parent", nameof(child));

      if (_childs.Contains(child))
        throw new ArgumentException("Child element already contained in this collection");
    }

    protected void ThrowIfChildIsNoneOfThisTypes(TextElement child, params Type[] types)
    {
      var ct = child.GetType();
      foreach (var t in types)
      {
        if (ct.IsAssignableFrom(t))
          return;
      }
      throw new ArgumentException("Child element is none of the allowed types for this element", nameof(child));
    }

    /// <summary>
    /// Gets a value indicating whether this instance has child nodes.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has child nodes; otherwise, <c>false</c>.
    /// </value>
    public bool HasChildNodes
    {
      get
      {
        return _childs.Count > 0;
      }
    }

    public TextElement FirstChild
    {
      get
      {
        return _childs?.Count > 0 ? _childs[0] : null;
      }
    }
    public TextElement LastChild
    {
      get
      {
        return _childs?.Count > 0 ? _childs[_childs.Count - 1] : null;
      }
    }

    public bool RemoveChild(TextElement child)
    {
      if (child is null)
        return false;
      else
        return this._childs.Remove(child);
    }
    #endregion

    #region Debugging and Displaying

    public virtual string ShortDescription
    {
      get
      {
        if (this is Run r && r.Text != null)
        {
          return string.Concat(this.GetType().Name, " | ", r.Text.Substring(0, Math.Min(32, r.Text.Length)));
        }
        else if (TextElementExtensions.GetThisAndAllChilds(this).OfType<Run>().FirstOrDefault((rr => rr.Text?.Length > 0)) is Run rchild)
        {
          return string.Concat(this.GetType().Name, " | ", rchild.Text.Substring(0, Math.Min(32, rchild.Text.Length)));
        }
        else
        {
          return this.GetType().Name;
        }
      }
    }

    #endregion

  }
}
