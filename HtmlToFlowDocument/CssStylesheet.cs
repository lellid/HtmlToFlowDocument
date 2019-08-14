// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace HtmlToFlowDocument
{
  internal class CssStylesheet
  {

    /// <summary>
    /// A function that provides CSS style sheets on demand. The argument is a string that is the name of the style sheet.
    /// The return value is the contents of the CSS style sheet with that name.
    /// </summary>
    private Func<string, string> _cssStyleSheetProvider;

    /// <summary>
    /// The layers of style sheets. The stylesheet with the highest priority is at the
    /// end of this list.
    /// </summary>
    private List<ExCSS.Stylesheet> _styleSheets = new List<ExCSS.Stylesheet>();



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
                _styleSheets.Add(styleSheet);
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
            _styleSheets.Add(styleSheet);
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


    /// <summary>
    /// Get all styles that apply to the provided element as string ( individual styles are semicolon-separated).
    /// </summary>
    /// <param name="elementName">Name of the element.</param>
    /// <param name="sourceContext">The source context.</param>
    /// <returns>All styles that apply to the provided element as string ( individual styles are semicolon-separated).</returns>
    public string GetStyle(string elementName, List<XmlElement> sourceContext)
    {
      Debug.Assert(sourceContext.Count > 0);
      Debug.Assert(elementName == sourceContext[sourceContext.Count - 1].LocalName);


      for (int i = _styleSheets.Count - 1; i >= 0; --i) // in reverse order because highest priority stylesheet is at the end of the lit
      {
        var styleSheet = _styleSheets[i];
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
            if (idx >= 0) // Strip off curly braces if ruleText is enclosed in them
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

    /// <summary>
    /// Determines if the selectorLevel applies to a given <paramref name="xmlElement"/> by analyzing the selectorLevel and then
    /// comparing it to either the local name of the <paramref name="xmlElement"/>, its 'id' attribute, or its 'class' attribute.
    /// </summary>
    /// <param name="selectorLevel">The selector level.</param>
    /// <param name="xmlElement">The XML element.</param>
    /// <returns>True if the <paramref name="selectorLevel"/> applies to the <paramref name="xmlElement"/>; otherwise, False.</returns>
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

      if (selectorId != null && Converter.GetAttribute(xmlElement, "id") != selectorId)
      {
        return false;
      }

      if (selectorClass != null && Converter.GetAttribute(xmlElement, "class") != selectorClass)
      {
        return false;
      }

      return true;
    }
  }
}
