// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace HtmlToXaml
{
  internal class CssStylesheet
  {
    private List<StyleDefinition> _styleDefinitions;

    /// <summary>
    /// A function that provides CSS style sheets on demand. The argument is a string that is the name of the style sheet.
    /// The return value is the contents of the CSS style sheet with that name.
    /// </summary>
    private Func<string, string> _cssStyleSheetProvider;

    /// <summary>
    /// The layers of style sheets.
    /// </summary>
    private List<ExCSS.Stylesheet> _externalStyleSheets = new List<ExCSS.Stylesheet>();



    /// <summary>
    /// Initializes a new instance of the <see cref="CssStylesheet"/> class.
    /// </summary>
    /// <param name="htmlElement">The root element of the HTML text to parse.</param>
    /// <param name="cssStyleSheetProvider">The CSS style sheet provider. The argument of this function is a string that is the name of the style sheet.
    /// The return value is the contents of the CSS style sheet with that name.</param>
    public CssStylesheet(XmlElement htmlElement, Func<string, string> cssStyleSheetProvider)
    {
      _cssStyleSheetProvider = cssStyleSheetProvider;

      if (htmlElement != null)
      {
        DiscoverStyleDefinitions(htmlElement);
      }
    }


    /// <summary>
    /// Recursively traverses an html tree, discovers STYLE elements and creates a style definition table
    /// for further cascading style application
    /// </summary>
    /// <param name="htmlElement">The current HTML element.</param>
    public void DiscoverStyleDefinitions(XmlElement htmlElement)
    {
      switch (htmlElement.LocalName.ToLower())
      {
        case "link":
          {
            if (htmlElement.HasAttributes && htmlElement.GetAttribute("rel") == "stylesheet" && htmlElement.GetAttribute("type") == "text/css")
            {
              var fileName = htmlElement.GetAttribute("href");
              var cssContent = _cssStyleSheetProvider?.Invoke(fileName);
              if (!string.IsNullOrEmpty(cssContent))
              {
                var styleSheet = ExCSS.StylesheetParser.Default.Parse(cssContent);
                _externalStyleSheets.Add(styleSheet);
              }
            }
          }
          break;

        case "style":
          {
            // Add style definitions from this style.

            // Collect all text from this style definition
            var stylesheetBuffer = new StringBuilder();

            for (var htmlChildNode = htmlElement.FirstChild;
                htmlChildNode != null;
                htmlChildNode = htmlChildNode.NextSibling)
            {
              if (htmlChildNode is XmlText || htmlChildNode is XmlComment)
              {
                stylesheetBuffer.Append(RemoveComments(htmlChildNode.Value));
              }
            }

            var styleSheet = ExCSS.StylesheetParser.Default.Parse(stylesheetBuffer.ToString());
            _externalStyleSheets.Add(styleSheet);
          }
          break;

        default:
          {
            // This is something else. Recurse into this element by calling this function recursively
            for (var htmlChildNode = htmlElement.FirstChild;
                htmlChildNode != null;
                htmlChildNode = htmlChildNode.NextSibling)
            {
              if (htmlChildNode is XmlElement)
              {
                DiscoverStyleDefinitions((XmlElement)htmlChildNode);
              }
            }
          }
          break;
      } // end switch
    }

    // Returns a string with all c-style comments replaced by spaces
    private string RemoveComments(string text)
    {
      var commentStart = text.IndexOf("/*", StringComparison.Ordinal);
      if (commentStart < 0)
      {
        return text;
      }

      var commentEnd = text.IndexOf("*/", commentStart + 2, StringComparison.Ordinal);
      if (commentEnd < 0)
      {
        return text.Substring(0, commentStart);
      }

      return text.Substring(0, commentStart) + " " + RemoveComments(text.Substring(commentEnd + 2));
    }

    public void AddStyleDefinition(string selector, string definition)
    {
      // Notrmalize parameter values
      selector = selector.Trim().ToLower();
      definition = definition.Trim().ToLower();
      if (selector.Length == 0 || definition.Length == 0)
      {
        return;
      }

      if (_styleDefinitions == null)
      {
        _styleDefinitions = new List<StyleDefinition>();
      }

      var simpleSelectors = selector.Split(',');

      foreach (string t in simpleSelectors)
      {
        var simpleSelector = t.Trim();
        if (simpleSelector.Length > 0)
        {
          _styleDefinitions.Add(new StyleDefinition(simpleSelector, definition));
        }
      }
    }

    public string GetStyle(string elementName, List<XmlElement> sourceContext)
    {
      Debug.Assert(sourceContext.Count > 0);
      Debug.Assert(elementName == sourceContext[sourceContext.Count - 1].LocalName);

      //  Add id processing for style selectors
      if (_styleDefinitions != null)
      {
        for (var i = _styleDefinitions.Count - 1; i >= 0; i--)
        {
          var selector = _styleDefinitions[i].Selector;

          var selectorLevels = selector.Split(' ');

          var indexInSelector = selectorLevels.Length - 1;
          var indexInContext = sourceContext.Count - 1;
          var selectorLevel = selectorLevels[indexInSelector].Trim();

          if (MatchSelectorLevel(selectorLevel, sourceContext[sourceContext.Count - 1]))
          {
            return _styleDefinitions[i].Definition;
          }
        }
      }

      foreach (var styleSheet in _externalStyleSheets)
      {
        foreach (var rule in styleSheet.Rules.OfType<ExCSS.StyleRule>())
        {
          var selector = rule.SelectorText;
          var selectorLevels = selector.Split(' ');

          var indexInSelector = selectorLevels.Length - 1;
          var indexInContext = sourceContext.Count - 1;
          var selectorLevel = selectorLevels[indexInSelector].Trim();

          if (MatchSelectorLevel(selectorLevel, sourceContext[sourceContext.Count - 1]))
          {
            var ruleText = rule.Text;

            var idx = ruleText.IndexOf("{");
            if (idx >= 0)
            {
              ruleText = ruleText.Substring(idx + 1);
              ruleText = ruleText.TrimEnd();
              ruleText = ruleText.TrimEnd('}');
            }

            return ruleText;
          }
        }
      }

      return null;
    }

    private bool MatchSelectorLevel(string selectorLevel, XmlElement xmlElement)
    {
      if (selectorLevel.Length == 0)
      {
        return false;
      }

      var indexOfDot = selectorLevel.IndexOf('.');
      var indexOfPound = selectorLevel.IndexOf('#');

      string selectorClass = null;
      string selectorId = null;
      string selectorTag = null;
      if (indexOfDot >= 0)
      {
        if (indexOfDot > 0)
        {
          selectorTag = selectorLevel.Substring(0, indexOfDot);
        }
        selectorClass = selectorLevel.Substring(indexOfDot + 1);
      }
      else if (indexOfPound >= 0)
      {
        if (indexOfPound > 0)
        {
          selectorTag = selectorLevel.Substring(0, indexOfPound);
        }
        selectorId = selectorLevel.Substring(indexOfPound + 1);
      }
      else
      {
        selectorTag = selectorLevel;
      }

      if (selectorTag != null && selectorTag != xmlElement.LocalName)
      {
        return false;
      }

      if (selectorId != null && HtmlToXamlConverter.GetAttribute(xmlElement, "id") != selectorId)
      {
        return false;
      }

      if (selectorClass != null && HtmlToXamlConverter.GetAttribute(xmlElement, "class") != selectorClass)
      {
        return false;
      }

      return true;
    }

    private class StyleDefinition
    {
      public readonly string Definition;
      public readonly string Selector;

      public StyleDefinition(string selector, string definition)
      {
        Selector = selector;
        Definition = definition;
      }
    }
  }
}
