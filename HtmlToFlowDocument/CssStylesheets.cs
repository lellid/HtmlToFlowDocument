// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using ExCSS;

namespace HtmlToFlowDocument
{
  /// <summary>
  /// Manages one or more stylesheets that belong to a given (X)Html document.
  /// </summary>
  public class CssStylesheets
  {

    /// <summary>
    /// A function that provides CSS style sheets on demand.
    /// The 1st argument is a string that is the name (relative or absolute) of the style sheet.
    /// The 2nd argument is a name of the HTML file that references this style sheet. Can be used to get the absolute name of the style sheet, if only a relative name was given.
    /// The return value is the contents of the CSS style sheet with that name.
    /// </summary>
    private Func<string, string, string> _cssStyleSheetProvider;

    /// <summary>
    /// File name of the HTML file which is parsed. The name is used to get the absolute name of the CSS style sheet, since often they are referenced with relative name only.
    /// </summary>
    private string _htmlFileName;

    /// <summary>
    /// The layers of style sheets. The stylesheets are in the order of their discovery, i.e. the sheet with the highest priority is at the
    /// end of this list.
    /// </summary>
    private List<ExCSS.Stylesheet> _styleSheets = new List<ExCSS.Stylesheet>();



    /// <summary>
    /// Initializes a new instance of the <see cref="CssStylesheets"/> class.
    /// </summary>
    /// <param name="htmlElement">The root element of the HTML text to parse.</param>
    /// <param name="cssStyleSheetProvider">The CSS style sheet provider.
    /// The 1st argument is a string that is the name (relative or absolute) of the style sheet.
    /// The 2nd argument is a name of the HTML file that references this style sheet. Can be used to get the absolute name of the style sheet, if only a relative name was given.
    /// The return value is the contents of the CSS style sheet with that name.</param>
    public CssStylesheets(XmlElement htmlElement, Func<string, string, string> cssStyleSheetProvider, string htmlFileName)
    {
      _cssStyleSheetProvider = cssStyleSheetProvider;
      _htmlFileName = htmlFileName;

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
            if (htmlElement.HasAttributes && htmlElement.GetAttribute("rel").ToLowerInvariant() == "stylesheet" && htmlElement.GetAttribute("type") == "text/css")
            {
              var fileName = htmlElement.GetAttribute("href");
              var cssContent = _cssStyleSheetProvider?.Invoke(fileName, _htmlFileName);
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

    ElementRules _elementRules = new ElementRules();


    /// <summary>
    /// Gets the element properties new.
    /// </summary>
    /// <param name="elementName">The XHTML element. Has to be the topmost element on the sourceContext.</param>
    /// <param name="sourceContext">The source context.</param>
    /// <param name="propertyDictionary">The property dictionary to store the element properties to.</param>
    public void GetElementProperties(
      XmlElement htmlElement,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext,
      Dictionary<string, object> propertyDictionary
      )
    {
      GetElementProperties(htmlElement, sourceContext, propertyDictionary, out var beforeElementProperties, out var afterElementProperties);
    }

    /// <summary>
    /// Gets the element properties new.
    /// </summary>
    /// <param name="elementName">The XHTML element. Has to be the topmost element on the sourceContext.</param>
    /// <param name="sourceContext">The source context.</param>
    /// <param name="propertyDictionary">The property dictionary to store the element properties to.</param>
    /// <param name="beforeElementProperties">If for the given <paramref name="htmlElement"/> a '::before' pseudo element rule exists, then on return this dictionary contains the properties of the ::before pseudo element (otherwise, the returned value is null).</param>
    /// <param name="afterElementProperties">If for the given <paramref name="htmlElement"/> an '::after' pseudo element rule exists, then on return this dictionary contains the properties of the ::after pseudo element (otherwise, the returned value is null).</param>
    public void GetElementProperties(
    XmlElement htmlElement,
    List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext,
    Dictionary<string, object> propertyDictionary,
    out Dictionary<string, object> beforeElementProperties,
    out Dictionary<string, object> afterElementProperties
    )
    {
      Debug.Assert(sourceContext.Count > 0);
      Debug.Assert(object.ReferenceEquals(htmlElement, sourceContext[sourceContext.Count - 1].xmlElement));

      _elementRules.CreateFor(sourceContext, _styleSheets);
      var allPropertyNames = _elementRules.GetAllPropertyNames();
      foreach (var pn in allPropertyNames)
      {
        _elementRules.GetProperty(pn, propertyDictionary);
      }

      if (_elementRules.HasPseudoRule("::before"))
      {
        beforeElementProperties = new Dictionary<string, object>();
        _elementRules.GetPseudoProperties("::before", beforeElementProperties);
      }
      else
      {
        beforeElementProperties = null;
      }

      if (_elementRules.HasPseudoRule("::after"))
      {
        afterElementProperties = new Dictionary<string, object>();
        _elementRules.GetPseudoProperties("::after", afterElementProperties);
      }
      else
      {
        afterElementProperties = null;
      }

    }


    /// <summary>
    /// Gets the element properties new.
    /// </summary>
    /// <param name="elementName">The XHTML element. Has to be the topmost element on the sourceContext.</param>
    /// <param name="sourceContext">The source context.</param>
    /// <param name="propertyDictionary">The property dictionary to store the element properties to.</param>
    public void GetElementProperties_Attributes_Only(XmlElement htmlElement, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext, Dictionary<string, object> propertyDictionary)
    {
      Debug.Assert(sourceContext.Count > 0);
      Debug.Assert(object.ReferenceEquals(htmlElement, sourceContext[sourceContext.Count - 1].xmlElement));

      _elementRules.CreateFor(sourceContext, _styleSheets);
      var allPropertyNames = _elementRules.GetAllPropertyNames();
      foreach (var pn in allPropertyNames)
      {
        _elementRules.GetProperty_Attributes_Only(pn, propertyDictionary);
      }
    }

    /// <summary>
    /// Gets the element properties new.
    /// </summary>
    /// <param name="elementName">The XHTML element. Has to be the topmost element on the sourceContext.</param>
    /// <param name="sourceContext">The source context.</param>
    /// <param name="propertyDictionary">The property dictionary to store the element properties to.</param>
    public void GetElementProperties_CSS_Only(XmlElement htmlElement, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext, Dictionary<string, object> propertyDictionary)
    {
      Debug.Assert(sourceContext.Count > 0);
      Debug.Assert(object.ReferenceEquals(htmlElement, sourceContext[sourceContext.Count - 1].xmlElement));

      _elementRules.CreateFor(sourceContext, _styleSheets);
      var allPropertyNames = _elementRules.GetAllPropertyNames();
      foreach (var pn in allPropertyNames)
      {
        _elementRules.GetProperty_CSS_Only(pn, propertyDictionary);
      }
    }


    /// <summary>
    /// Given a relative file name referenced in an Html document, the function gets the absolute file name of this file.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="htmlFileName">Name of the HTML file.</param>
    /// <returns></returns>
    public static string GetAbsoluteFileNameForFileRelativeToHtmlFile(string fileName, string htmlFileName)
    {
      int idx = htmlFileName.LastIndexOf("/");
      var directory = idx > 0 ? htmlFileName.Substring(0, idx) : string.Empty;

      while (fileName.StartsWith("../"))
      {
        idx = directory.LastIndexOf("/");
        directory = idx > 0 ? htmlFileName.Substring(0, idx) : string.Empty;
        fileName = fileName.Substring(3);
      }

      return string.IsNullOrEmpty(directory) ? fileName : directory + "/" + fileName;
    }
  }
}
