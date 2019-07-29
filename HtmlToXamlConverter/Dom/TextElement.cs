using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public abstract class TextElement
  {
    public TextElement Parent { get; set; }

    public string FontFamily { get; set; }

    public double? FontSize { get; set; }

    public FontStyle? FontStyle { get; set; }

    public FontWeight? FontWeight { get; set; }

    public int? Foreground { get; set; }

    public int? Background { get; set; }

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

  }
}
