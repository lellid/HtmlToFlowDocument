// // Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using ExCSS;

namespace HtmlToFlowDocument
{
  /// <summary>
  /// Responsible for retrieving CSS rules for a specified XHTML element.
  /// </summary>
  public class ElementRules
  {
    static readonly char[] WhitespacesInClassValues = new char[] { ' ', '\t' };
    static HashSet<string> _validPropertyNames;

    /// <summary>
    /// The XHTML element under consideration.
    /// </summary>
    XmlElement _xmlElement;

    /// <summary>
    /// Holds the style rules of the HTML element this instance was constructed from
    /// (from the local styles given by the 'style' attribute and the linked styles of the HTML document).
    /// Please note that this list will not hold the properties of the HTML element given in its other attributes!
    /// The rules are sorted so that the higher priority rules are at the end of the list
    /// </summary>
    List<StyleRule> _ruleList = new List<StyleRule>();

    static ElementRules()
    {
      _validPropertyNames = new HashSet<string>();
      var fieldInfos = typeof(PropertyNames).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
      foreach (var info in fieldInfos)
      {
        if (info.GetValue(null) is string s)
        {
          _validPropertyNames.Add(s);
        }
      }
    }

    #region Construction

    /// <summary>
    /// Creates the element rules for the topmost element of the elementHierarchy.
    /// </summary>
    /// <param name="elementHierarchy">The element hierarchy.</param>
    /// <param name="styleSheets">The style sheets of the Html document.</param>
    public void CreateFor(IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy, IReadOnlyList<Stylesheet> styleSheets)
    {
      CreateFor(elementHierarchy[elementHierarchy.Count - 1].xmlElement, elementHierarchy, styleSheets);
    }

    /// <summary>
    /// Creates the element rules for a given Html element. The given element has to be the topmost element of the element hierarchy.
    /// </summary>
    /// <param name="xmlElement">The XHTML element for which the rules to create.</param>
    /// <param name="elementHierarchy">The element hierarchy (the topmost element must be identical to <paramref name="xmlElement"/>).</param>
    /// <param name="styleSheets">The style sheets of the Html document.</param>
    /// <exception cref="ArgumentNullException">xmlElement</exception>
    public void CreateFor(XmlElement xmlElement, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy, IReadOnlyList<Stylesheet> styleSheets)
    {
      _xmlElement = xmlElement ?? throw new ArgumentNullException(nameof(xmlElement));

      if (!(elementHierarchy.Count > 0 && object.ReferenceEquals(elementHierarchy[elementHierarchy.Count - 1].xmlElement, xmlElement)))
        throw new ArgumentOutOfRangeException("Last element of elementHierarchy must contain the provided element");

      _ruleList.Clear();

      IEnumerable<StyleRule> styleRules = null;

      // Note: we design the enumeration so that higher priority rules (priority here  merely given by its position)
      // are at the end of the list
      // we assume here that the same rules given later in the document will override rules given before
      // furthermore we assume that rules given in the local style attributes will override rules given in the CSS file


      // Concat any style rules given by the linked style sheets
      for (int i = 0; i < styleSheets.Count; ++i)
      {
        if (styleRules is null)
          styleRules = styleSheets[i].Rules.OfType<StyleRule>();
        else
          styleRules = styleRules.Concat(styleSheets[i].Rules.OfType<StyleRule>());
      }

      //Concat local styles given by the 'style' attribute
      var localStyle = xmlElement.GetAttribute("style");
      if (!string.IsNullOrEmpty(localStyle))
      {
        var localStyleSheet = ExCSS.StylesheetParser.Default.Parse(string.Concat("* {", localStyle, " }"));

        if (styleRules is null)
          styleRules = localStyleSheet.Rules.OfType<StyleRule>();
        else
          styleRules = styleRules.Concat(localStyleSheet.Rules.OfType<StyleRule>());
      }

      // Note that at this point the rules with higher priority as given by their position
      // are at the end of the enumeration 'styleRules' !

      if (!(styleRules is null))
      {
        _ruleList.AddRange(GetStyleRulesFor(xmlElement, elementHierarchy, styleRules));
        _ruleList.Sort((x, y) => Comparer<Priority>.Default.Compare(x.Selector.Specifity, y.Selector.Specifity));
      }
    }


    #endregion Construction

    #region Queries

    /// <summary>
    /// Gets a specified property, first by searching the attributes of the Html element, and then the rules given in the styles that apply to the element.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>If the property is present, the property value; otherwise, null.</returns>
    public void GetProperty(string propertyName, Dictionary<string, object> propertyDictionary)
    {
      Property property;

      if (_xmlElement.HasAttribute(propertyName))
      {
        PropertyConverter.Convert(propertyName, _xmlElement.GetAttribute(propertyName), propertyDictionary);
      }
      else if (null != (property = GetPropertyFromRules(propertyName)))
      {
        PropertyConverter.Convert(property, propertyDictionary);
      }
    }

    /// <summary>
    /// Gets a specified property, by searching the attributes of the Html element, but not in the CSS rules.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>If the property is present, the property value; otherwise, null.</returns>
    public void GetProperty_Attributes_Only(string propertyName, Dictionary<string, object> propertyDictionary)
    {
      if (_xmlElement.HasAttribute(propertyName))
      {
        PropertyConverter.Convert(propertyName, _xmlElement.GetAttribute(propertyName), propertyDictionary);
      }
    }

    /// <summary>
    /// Gets a specified property, by searching the rules given in the styles that apply to the element, but not by searching in the attributes.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>If the property is present, the property value; otherwise, null.</returns>
    public void GetProperty_CSS_Only(string propertyName, Dictionary<string, object> propertyDictionary)
    {
      Property property;

      if (null != (property = GetPropertyFromRules(propertyName)))
      {
        PropertyConverter.Convert(property, propertyDictionary);
      }
    }

    /// <summary>
    /// Gets a given property from the style rules.
    /// </summary>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns>If found, the property; otherwise, null.</returns>
    protected Property GetPropertyFromRules(string propertyName)
    {
      Property result = null;

      for (int i = _ruleList.Count - 1; i >= 0; --i)
      {
        var rule = _ruleList[i];

        foreach (var prop in AllChildrenOf(rule).OfType<Property>())
        {
          if (prop.Name == propertyName)
          {
            if (result is null)
              result = prop;

            if (prop.IsImportant)
              return result;
          }
        }
      }

      return result;
    }

    private IEnumerable<IStylesheetNode> AllChildrenOf(IStylesheetNode rule)
    {
      foreach (var c1 in rule.Children)
      {
        yield return c1;

        foreach (var c2 in AllChildrenOf(c1))
          yield return c2;
      }
    }

    /// <summary>
    /// Gets all name of all properties the Html element has.
    /// </summary>
    /// <returns></returns>
    public HashSet<string> GetAllPropertyNames()
    {
      var result = new HashSet<string>();

      if (_xmlElement.Attributes != null)
      {
        foreach (XmlAttribute att in _xmlElement.Attributes)
        {
          if (_validPropertyNames.Contains(att.Name))
            result.Add(att.Name);
        }
      }


      foreach (var rule in _ruleList)
      {
        foreach (var property in rule.Style.Children.OfType<Property>())
          result.Add(property.Name);
      }

      return result;
    }




    /// <summary>
    /// Gets all style rules for a given Html element.
    /// </summary>
    /// <param name="xmlElement">The Html element.</param>
    /// <param name="elementHierarchy">The element hierarchy. The element on top of the hierarchy is either the element given in <paramref name="xmlElement"/>, or its parent element.</param>
    /// <param name="allRules">All rules fro all style sheets, including the local styles of the element given in its style attribute.</param>
    /// <returns>Only those rules that apply to the given Html element.</returns>
    /// <exception cref="NotImplementedException"></exception>
    static IEnumerable<StyleRule> GetStyleRulesFor(XmlElement xmlElement, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy, IEnumerable<StyleRule> allRules)
    {
      string elementName = !string.IsNullOrEmpty(xmlElement.LocalName) ? xmlElement.LocalName : null;
      string id = xmlElement.GetAttribute("id");
      string elementId = string.IsNullOrEmpty(id) ? null : "#" + id;
      string[] elementClasses = (xmlElement.GetAttribute("class") ?? string.Empty).Split(WhitespacesInClassValues, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < elementClasses.Length; ++i)
        elementClasses[i] = "." + elementClasses[i];


      foreach (var rule in allRules)
      {
        if (IsSelected(rule.Selector, elementName, elementId, elementClasses, elementHierarchy))
        {
          yield return rule;
        }
      }
    }

    #endregion

    #region Selector code


    /// <summary>
    /// Determines whether the XHTML element is selected by this <see cref="SimpleSelector"/>.
    /// </summary>
    /// <param name="simpleSel">The simple selecctor.</param>
    /// <param name="elementName">Name of the XHTML element.</param>
    /// <param name="elementId">The Id attribute value of the XHTML element.</param>
    /// <param name="elementClasses">The classes of the XHTML element.</param>
    /// <returns>
    /// <c>true</c> if the XHTML element is selected by the selector; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    static bool IsSelected(SimpleSelector simpleSel, string elementName, string elementId, string[] elementClasses)
    {
      if (simpleSel.Text == "*")
        return true;
      else if (simpleSel.Specifity.Tags != 0)
        return elementName == simpleSel.Text;
      else if (simpleSel.Specifity.Ids != 0)
        return elementId == simpleSel.Text;
      else if (simpleSel.Specifity.Classes != 0)
        return elementClasses.Contains(simpleSel.Text);
      else if (simpleSel.Specifity.Inlines != 0)
        throw new NotImplementedException();
      else
        return false;
    }

    /// <summary>
    /// Determines whether the XHTML element is selected by this <see cref="CompoundSelector"/>.
    /// </summary>
    /// <param name="compoundSelector">The compound selecctor.</param>
    /// <param name="elementName">Name of the XHTML element.</param>
    /// <param name="elementId">The Id attribute value of the XHTML element.</param>
    /// <param name="elementClasses">The classes of the XHTML element.</param>
    /// <param name="elementHierarchy">The hierarchy of XHTML elements. The element under consideration has to be the topmost element.</param>
    /// <returns>
    /// <c>true</c> if the XHTML element is selected by the selector; otherwise, <c>false</c>.
    /// </returns>
    static bool IsSelected(CompoundSelector compoundSelector, string elementName, string elementId, string[] elementClasses, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy)
    {
      // all of the elements must give true
      bool isSelected = true;
      bool hasElements = false;
      foreach (var sel in compoundSelector)
      {
        isSelected &= IsSelected(sel, elementName, elementId, elementClasses, elementHierarchy);
        hasElements = true;
      }
      return isSelected && hasElements;
    }

    /// <summary>
    /// Determines whether the XHTML element is selected by this <see cref="ListSelector"/>.
    /// </summary>
    /// <param name="listSelector">The list selecctor.</param>
    /// <param name="elementName">Name of the XHTML element.</param>
    /// <param name="elementId">The Id attribute value of the XHTML element.</param>
    /// <param name="elementClasses">The classes of the XHTML element.</param>
    /// <param name="elementHierarchy">The hierarchy of XHTML elements. The element under consideration has to be the topmost element.</param>
    /// <returns>
    /// <c>true</c> if the XHTML element is selected by the selector; otherwise, <c>false</c>.
    /// </returns>
    static bool IsSelected(ListSelector listSelector, string elementName, string elementId, string[] elementClasses, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy)
    {
      // one of the selectors must return true;
      bool isSelected = false;

      foreach (var sel in listSelector)
      {
        isSelected |= IsSelected(sel, elementName, elementId, elementClasses, elementHierarchy);
        if (isSelected)
          break;
      }
      return isSelected;
    }


    /// <summary>
    /// Determines whether the XHTML element is selected by this <see cref="ComplexSelector"/>.
    /// </summary>
    /// <param name="complexSelector">The complex selecctor.</param>
    /// <param name="elementName">Name of the XHTML element.</param>
    /// <param name="elementId">The Id attribute value of the XHTML element.</param>
    /// <param name="elementClasses">The classes of the XHTML element.</param>
    /// <param name="elementHierarchy">The hierarchy of XHTML elements. The element under consideration has to be the topmost element.</param>
    /// <returns>
    /// <c>true</c> if the XHTML element is selected by the selector; otherwise, <c>false</c>.
    /// </returns>
    static bool IsSelected(ComplexSelector complexSelector, string elementName, string elementId, string[] elementClasses, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy)
    {
      if (!(elementHierarchy[elementHierarchy.Count - 1].xmlElement.Name == elementName))
        throw new ArgumentOutOfRangeException("Last element of elementHierarchy must contain the provided element");

      bool isSelected = true;
      int selectorLevel = -1;
      int hierarchyLevel = elementHierarchy.Count;
      using (var iterator = complexSelector.SelectorsReverse.GetEnumerator())
      {
        while (iterator.MoveNext())
        {
          ++selectorLevel;
          --hierarchyLevel;

          if (hierarchyLevel < 0)
          {
            isSelected = false;
            break;
          }

          var s = iterator.Current;

          if (selectorLevel == 0)
          {
            isSelected &= IsSelected(s.Selector, elementName, elementId, elementClasses, elementHierarchy);
          }
          else
          {
            switch (s.Delimiter)
            {
              case ">":
                {
                  isSelected &= IsSelected(s.Selector, elementHierarchy[hierarchyLevel].xmlElement, elementHierarchy);
                }
                break;
              case " ":
                {
                  var iss = IsSelected(s.Selector, elementHierarchy[hierarchyLevel].xmlElement, elementHierarchy);
                  while (iss == false && hierarchyLevel > 0)
                  {
                    --hierarchyLevel;
                    iss = IsSelected(s.Selector, elementHierarchy[hierarchyLevel].xmlElement, elementHierarchy);
                  }
                  isSelected &= iss;
                }
                break;
              case "+": // adjacent sibling operator
                {
                  ++hierarchyLevel; // we have to increase hierarchy level because we should be at the same level as the previous element
                  XmlNode previousSibling = elementHierarchy[hierarchyLevel].xmlElement.PreviousSibling;
                  while (previousSibling != null && !(previousSibling is XmlElement))
                  {
                    previousSibling = previousSibling.PreviousSibling;
                  }
                  isSelected &= previousSibling is XmlElement previousElement && IsSelected(s.Selector, previousElement, elementHierarchy);
                }
                break;
              case "~": // General sibling operator
                {
                  bool localIsSelected = false;
                  ++hierarchyLevel; // we have to increase hierarchy level because we should be at the same level as the previous element
                  XmlNode previousSibling = elementHierarchy[hierarchyLevel].xmlElement.PreviousSibling;
                  while (previousSibling != null)
                  {
                    if (previousSibling is XmlElement previousElement && IsSelected(s.Selector, previousElement, elementHierarchy))
                    {
                      localIsSelected = true;
                      break;
                    }
                    previousSibling = previousSibling.PreviousSibling;
                  }
                  isSelected &= localIsSelected;
                }
                break;
              default:
                throw new NotImplementedException();

            }
          }
          if (!isSelected)
            break;
        }
      }

      return isSelected && selectorLevel > 0;
    }

    /// <summary>
    /// Determines whether the XHTML element is selected by this <see cref="FirstChildSelector"/>.
    /// </summary>
    /// <param name="firstChildSelector">The first child selecctor.</param>
    /// <param name="elementName">Name of the XHTML element.</param>
    /// <param name="elementId">The Id attribute value of the XHTML element.</param>
    /// <param name="elementClasses">The classes of the XHTML element.</param>
    /// <param name="elementHierarchy">The hierarchy of XHTML elements. The element under consideration has to be the topmost element.</param>
    /// <returns>
    /// <c>true</c> if the XHTML element is selected by the selector; otherwise, <c>false</c>.
    /// </returns>
    static bool IsSelected(FirstChildSelector firstChildSelector, string elementName, string elementId, string[] elementClasses, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy)
    {
      var xmlElement = elementHierarchy[elementHierarchy.Count - 1].xmlElement;

      if (xmlElement is null || xmlElement.ParentNode is null)
        return false;

      // Count forward to start

      int count = 0;
      XmlNode sib = xmlElement;
      while (!(sib is null))
      {
        if (sib is XmlElement)
          ++count;
        sib = sib.PreviousSibling;
      }
      // Count is now 1 if xmlElement is the first element

      count -= firstChildSelector.Offset;
      return firstChildSelector.Step <= 0 ? count == 0 : count % firstChildSelector.Step == 0;
    }

    /// <summary>
    /// Determines whether the XHTML element is selected by this <see cref="ISelector"/>.
    /// </summary>
    /// <param name="selector">The selector.</param>
    /// <param name="elementName">Name of the XHTML element.</param>
    /// <param name="elementId">The Id attribute value of the XHTML element.</param>
    /// <param name="elementClasses">The classes of the XHTML element.</param>
    /// <param name="elementHierarchy">The hierarchy of XHTML elements. The element under consideration has to be the topmost element.</param>
    /// <returns>
    /// <c>true</c> if the XHTML element is selected by the selector; otherwise, <c>false</c>.
    /// </returns>
    static bool IsSelected(ISelector selector, string elementName, string elementId, string[] elementClasses, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy)
    {
      switch (selector)
      {
        case SimpleSelector simpleSelector:
          return IsSelected(simpleSelector, elementName, elementId, elementClasses);
        case ListSelector listSelector:
          return IsSelected(listSelector, elementName, elementId, elementClasses, elementHierarchy);
        case CompoundSelector compoundSelector:
          return IsSelected(compoundSelector, elementName, elementId, elementClasses, elementHierarchy);
        case ComplexSelector complexSelector:
          return IsSelected(complexSelector, elementName, elementId, elementClasses, elementHierarchy);
        case FirstChildSelector firstChildSelector:
          return IsSelected(firstChildSelector, elementName, elementId, elementClasses, elementHierarchy);
        default:
          throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Determines whether the XHTML element is selected by this <see cref="ISelector"/>.
    /// </summary>
    /// <param name="selector">The selector.</param>
    /// <param name="xmlElement">The XHTML element.</param>
    /// <param name="elementHierarchy">The hierarchy of XHTML elements. The element under consideration has to be the topmost element.</param>
    /// <returns>
    /// <c>true</c> if the XHTML element is selected by the selector; otherwise, <c>false</c>.
    /// </returns>
    static bool IsSelected(ISelector selector, XmlElement xmlElement, IReadOnlyList<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> elementHierarchy)
    {
      if (xmlElement is null)
        return false;

      string elementName = !string.IsNullOrEmpty(xmlElement.LocalName) ? xmlElement.LocalName : null;
      string id = xmlElement.GetAttribute("id");
      string elementId = string.IsNullOrEmpty(id) ? null : "#" + id;
      string[] elementClasses = (xmlElement.GetAttribute("class") ?? string.Empty).Split(WhitespacesInClassValues, StringSplitOptions.RemoveEmptyEntries);
      for (int i = 0; i < elementClasses.Length; ++i)
        elementClasses[i] = "." + elementClasses[i];

      return IsSelected(selector, elementName, elementId, elementClasses, elementHierarchy);
    }

    #endregion



  }
}
