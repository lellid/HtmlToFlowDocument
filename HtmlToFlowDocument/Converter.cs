// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using HtmlToFlowDocument.Dom;
using System.Linq;

namespace HtmlToFlowDocument
{
  /// <summary>
  /// Converts HTML to a DOM model,
  /// that can then be converted e.g. to a XAML representation of a FlowDocument, or directly to a FlowDocument, or to
  /// another representation such as plain text.
  /// </summary>
  public class Converter
  {
    public const float FontSizeRootPx = 16;

    // ----------------------------------------------------------------
    //
    // Internal Constants
    //
    // ----------------------------------------------------------------

    // The constants reprtesent all Xaml names used in a conversion
    public const string XamlFlowDocument = "FlowDocument";
    public const string XamlRun = "Run";
    public const string XamlSpan = "Span";
    public const string XamlHyperlink = "Hyperlink";
    public const string XamlHyperlinkNavigateUri = "NavigateUri";
    public const string XamlHyperlinkTargetName = "TargetName";
    public const string XamlSection = "Section";
    public const string XamlList = "List";
    public const string XamlListMarkerStyle = "MarkerStyle";
    public const string XamlListMarkerStyleNone = "None";
    public const string XamlListMarkerStyleDecimal = "Decimal";
    public const string XamlListMarkerStyleDisc = "Disc";
    public const string XamlListMarkerStyleCircle = "Circle";
    public const string XamlListMarkerStyleSquare = "Square";
    public const string XamlListMarkerStyleBox = "Box";
    public const string XamlListMarkerStyleLowerLatin = "LowerLatin";
    public const string XamlListMarkerStyleUpperLatin = "UpperLatin";
    public const string XamlListMarkerStyleLowerRoman = "LowerRoman";
    public const string XamlListMarkerStyleUpperRoman = "UpperRoman";
    public const string XamlListItem = "ListItem";
    public const string XamlLineBreak = "LineBreak";
    public const string XamlParagraph = "Paragraph";
    public const string XamlMargin = "Margin";
    public const string XamlPadding = "Padding";
    public const string XamlBorderBrush = "BorderBrush";
    public const string XamlBorderThickness = "BorderThickness";
    public const string XamlTable = "Table";
    public const string XamlTableColumn = "TableColumn";
    public const string XamlTableRowGroup = "TableRowGroup";
    public const string XamlTableRow = "TableRow";
    public const string XamlTableCell = "TableCell";
    public const string XamlTableCellBorderThickness = "BorderThickness";
    public const string XamlTableCellBorderBrush = "BorderBrush";
    public const string XamlTableCellColumnSpan = "ColumnSpan";
    public const string XamlTableCellRowSpan = "RowSpan";
    public const string XamlWidth = "Width";
    public const string XamlBrushesBlack = "Black";
    public const string XamlFontFamily = "FontFamily";
    public const string XamlFontSize = "FontSize";
    public const double XamlFontSizeXxLarge = 22; // "XXLarge";
    public const double XamlFontSizeXLarge = 20; // "XLarge";
    public const double XamlFontSizeLarge = 18; // "Large";
    public const double XamlFontSizeMedium = 16; // "Medium";
    public const double XamlFontSizeSmall = 12; // "Small";
    public const double XamlFontSizeXSmall = 10; // "XSmall";
    public const double XamlFontSizeXxSmall = 8; // "XXSmall";
    public const string XamlFontWeight = "FontWeight";
    public const string XamlFontWeightBold = "Bold";
    public const string XamlFontStyle = "FontStyle";
    public const string XamlForeground = "Foreground";
    public const string XamlBackground = "Background";
    public const string XamlTextDecorations = "TextDecorations";
    public const string XamlTextDecorationsUnderline = "Underline";
    public const string XamlTextIndent = "TextIndent";
    public const string XamlTextAlignment = "TextAlignment";
    // ---------------------------------------------------------------------
    //
    // Private Fields
    //
    // ---------------------------------------------------------------------

    #region Private Fields

    private static readonly string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    #endregion Private Fields

    /// <summary>
    /// Gets or sets a value indicating whether during the conversion process the DOM elements are attached as tags to the UI elements.
    /// This will of course increase the memory footprint, because the DOM elements then could not be reclaimed by the garbage collector.
    /// </summary>
    /// <value>
    ///   <c>true</c> if DOM elements should be attached as tags to the UI elements; otherwise, <c>false</c>.
    /// </value>
    public bool AttachSourceAsTags { get; set; }

    // ---------------------------------------------------------------------
    //
    // Internal Methods
    //
    // ---------------------------------------------------------------------

    #region Public Methods

    /// <summary>
    /// Converts an Html text into a DOM representation, consisting of elements that derive from <see cref="TextElement"/>.
    /// </summary>
    /// <param name="htmlString">
    /// The Html text to convert.
    /// </param>
    /// <param name="asFlowDocument">
    /// A value of true indicates that we need a FlowDocument as a root element;
    /// a value of false means that Section or Span elements will be used
    /// depending on StartFragment/EndFragment comments locations.
    /// </param>
    /// <param name="cssStyleSheetProvider">
    /// A function that provides CSS style sheets on demand.
    /// The 1st argument is a string that is the name (relative or absolute) of the style sheet.
    /// The 2nd argument is a name of the HTML file that references this style sheet. Can be used to get the absolute name of the style sheet, if only a relative name was given.
    /// The return value is the contents of the CSS style sheet with that name.
    /// </param>
    /// <returns>
    /// The root <see cref="TextElement"/> of the Dom tree that represents the XHTML tree.
    /// </returns>
    public TextElement Convert(string htmlString, bool asFlowDocument, Func<string, string, string> cssStyleSheetProvider, string htmlFileName)
    {
      // Create well-formed Xml from Html string
      XmlElement htmlElement = HtmlParsing.HtmlParser.ParseHtml(htmlString);
      return Convert(htmlElement, asFlowDocument, cssStyleSheetProvider, htmlFileName);
    }

    /// <summary>
    /// Converts an XHTML element tree into a DOM representation, consisting of elements that derive from <see cref="TextElement"/>.
    /// </summary>
    /// <param name="htmlElement">
    /// The root element of the XHTML tree. Typically, this is the 'html' element.
    /// </param>
    /// <param name="asFlowDocument">
    /// A value of true indicates that we need a FlowDocument as a root element;
    /// a value of false means that Section or Span elements will be used
    /// depending on StartFragment/EndFragment comments locations.
    /// </param>
    /// <param name="cssStyleSheetProvider">
    /// A function that provides CSS style sheets on demand.
    /// The 1st argument is a string that is the name (relative or absolute) of the style sheet.
    /// The 2nd argument is a name of the HTML file that references this style sheet. Can be used to get the absolute name of the style sheet, if only a relative name was given.
    /// The return value is the contents of the CSS style sheet with that name.
    /// </param>
    /// <returns>
    /// The root <see cref="TextElement"/> of the Dom tree that represents the XHTML tree.
    /// </returns>
    public TextElement Convert(XmlElement htmlElement, bool asFlowDocument, Func<string, string, string> cssStyleSheetProvider, string htmlFileName)
    {
      // Source context is a stack of all elements - ancestors of a parentElement
      var sourceContext = new List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)>(10);


      // Decide what name to use as a root
      TextElement xamlFlowDocumentElement = asFlowDocument ? (Block)new FlowDocument() : (Block)new Section();
      xamlFlowDocumentElement.FontSize = FontSizeRootPx; // base font size of the document
      if (AttachSourceAsTags)
      { xamlFlowDocumentElement.Tag = htmlElement; }

      // Extract style definitions from all STYLE elements in the document
      CssStylesheets stylesheet = new CssStylesheets(htmlElement, cssStyleSheetProvider, htmlFileName);


      // Clear fragment parent
      _inlineFragmentParentElement = null;

      // convert root html element
      var rootProperties = new Dictionary<string, object>
      {
        { "sourceHtmlFileName", htmlFileName },
        { "font-size", new ExCSS.Length(FontSizeRootPx, ExCSS.Length.Unit.Px) },
        { "width", new ExCSS.Length(100, ExCSS.Length.Unit.Vw) },
        { "height", new ExCSS.Length(100, ExCSS.Length.Unit.Vh) },
        { "max-width", new ExCSS.Length(100, ExCSS.Length.Unit.Vw) },
        { "max-height", new ExCSS.Length(100, ExCSS.Length.Unit.Vh) }
      };

      sourceContext.Add((null, rootProperties));

      AddBlock(xamlFlowDocumentElement, htmlElement, stylesheet, sourceContext);

      // In case if the selected fragment is inline, extract it into a separate Span wrapper
      if (!asFlowDocument)
      {
        xamlFlowDocumentElement = ExtractInlineFragment(xamlFlowDocumentElement);
      }

      return xamlFlowDocumentElement;
    }

    /// <summary>
    ///     Returns a value for an attribute by its name (ignoring casing)
    /// </summary>
    /// <param name="element">
    ///     XmlElement in which we are trying to find the specified attribute
    /// </param>
    /// <param name="attributeName">
    ///     String representing the attribute name to be searched for
    /// </param>
    /// <returns></returns>
    public static string GetAttribute(XmlElement element, string attributeName)
    {
      attributeName = attributeName.ToLower();

      for (int i = 0; i < element.Attributes.Count; i++)
      {
        if (element.Attributes[i].Name.ToLower() == attributeName)
        {
          return element.Attributes[i].Value;
        }
      }

      return null;
    }

    /// <summary>
    ///     Returns string extracted from quotation marks
    /// </summary>
    /// <param name="value">
    ///     String representing value enclosed in quotation marks
    /// </param>
    internal static string UnQuote(string value)
    {
      if (value.StartsWith("\"") && value.EndsWith("\"") || value.StartsWith("'") && value.EndsWith("'"))
      {
        value = value.Substring(1, value.Length - 2).Trim();
      }
      return value;
    }

    #endregion Internal Methods

    // ---------------------------------------------------------------------
    //
    // Private Methods
    //
    // ---------------------------------------------------------------------

    #region Private Methods

    /// <summary>
    ///     Analyzes the given htmlElement expecting it to be converted
    ///     into some of xaml Block elements and adds the converted block
    ///     to the children collection of xamlParentElement.
    ///     Analyzes the given XmlElement htmlElement, recognizes it as some HTML element
    ///     and adds it as a child to a xamlParentElement.
    ///     In some cases several following siblings of the given htmlElement
    ///     will be consumed too (e.g. LIs encountered without wrapping UL/OL,
    ///     which must be collected together and wrapped into one implicit List element).
    /// </summary>
    /// <param name="xamlParentElement">
    ///     Parent xaml element, to which new converted element will be added
    /// </param>
    /// <param name="htmlElement">
    ///     Source html element subject to convert to xaml.
    /// </param>
    /// <param name="inheritedProperties">
    ///     Properties inherited from an outer context.
    /// </param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    /// <returns>
    ///     Last processed html node. Normally it should be the same htmlElement
    ///     as was passed as a paramater, but in some irregular cases
    ///     it could one of its following siblings.
    ///     The caller must use this node to get to next sibling from it.
    /// </returns>
    private XmlNode AddBlock(
      TextElement xamlParentElement,
      XmlNode htmlNode,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      if (htmlNode is XmlComment)
      {
        DefineInlineFragmentParent((XmlComment)htmlNode, /*xamlParentElement:*/null);
      }
      else if (htmlNode is XmlText)
      {
        htmlNode = AddImplicitParagraph(xamlParentElement, htmlNode, stylesheet, sourceContext);
      }
      else if (htmlNode is XmlElement htmlElement)
      {
        string htmlElementName = htmlElement.LocalName; // Keep the name case-sensitive to check xml names
        string htmlElementNamespace = htmlElement.NamespaceURI;

        if (htmlElementNamespace != HtmlParsing.HtmlParser.XhtmlNamespace)
        {
          // Non-html element. skip it
          // Isn't it too agressive? What if this is just an error in html tag name?
          // TODO: Consider skipping just a wparrer in recursing into the element tree,
          // which may produce some garbage though coming from xml fragments.
          return htmlElement;
        }

        // Put source element to the stack
        if (!object.ReferenceEquals(htmlElement, sourceContext[sourceContext.Count - 1].xmlElement))
          sourceContext.Add((htmlElement, new Dictionary<string, object>()));

        // Convert the name to lowercase, because html elements are case-insensitive
        htmlElementName = htmlElementName.ToLower();

        // Switch to an appropriate kind of processing depending on html element name
        switch (htmlElementName)
        {
          // Sections:
          case "html":
          case "body":
          case "div":
          case "form": // not a block according to xhtml spec
          case "pre": // Renders text in a fixed-width font
          case "blockquote":
          case "caption":
          case "center":
          case "cite":
            AddSection(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;

          // Paragraphs:
          case "p":
          case "h1":
          case "h2":
          case "h3":
          case "h4":
          case "h5":
          case "h6":
          case "nsrtitle":
          case "textarea":
          case "dd": // ???
          case "dl": // ???
          case "dt": // ???
          case "tt": // ???
            AddParagraph(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;

          case "ol":
          case "ul":
          case "dir": //  treat as UL element
          case "menu": //  treat as UL element
                       // List element conversion
            AddList(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;
          case "li":
            // LI outside of OL/UL
            // Collect all sibling LIs, wrap them into a List and then proceed with the element following the last of LIs
            htmlNode = AddOrphanListItems(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;

          case "img":
            // TODO: Add image processing
            AddImage(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;

          case "table":
            // hand off to table parsing function which will perform special table syntax checks
            AddTable(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;

          case "tbody":
          case "tfoot":
          case "thead":
          case "tr":
          case "td":
          case "th":
            // Table stuff without table wrapper
            // TODO: add special-case processing here for elements that should be within tables when the
            // parent element is NOT a table. If the parent element is a table they can be processed normally.
            // we need to compare against the parent element here, we can't just break on a switch
            goto default; // Thus we will skip this element as unknown, but still recurse into it.

          case "style": // We already pre-processed all style elements. Ignore it now
          case "meta":
          case "head":
          case "title":
          case "script":
            // Ignore these elements
            break;

          default:
            // Wrap a sequence of inlines into an implicit paragraph
            htmlNode = AddImplicitParagraph(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;
        }

        // Remove the element from the stack
        DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == htmlElement);
        sourceContext.RemoveAt(sourceContext.Count - 1);
      }

      // Return last processed node
      return htmlNode;
    }

    // .............................................................
    //
    // Line Breaks
    //
    // .............................................................

    private static void AddBreak(TextElement xamlParentElement, string htmlElementName)
    {
      // Create new xaml element corresponding to this html element

      xamlParentElement.AppendChild(new LineBreak());
      if (htmlElementName == "hr")
      {
        xamlParentElement.AppendChild(new Run("----------------------"));
        xamlParentElement.AppendChild(new LineBreak());
      }
    }

    // .............................................................
    //
    // Text Flow Elements
    //
    // .............................................................

    /// <summary>
    ///     Generates Section or Paragraph element from DIV depending whether it contains any block elements or not
    /// </summary>
    /// <param name="xamlParentElement">
    ///     XmlElement representing Xaml parent to which the converted element should be added
    /// </param>
    /// <param name="htmlElement">
    ///     XmlElement representing Html element to be converted
    /// </param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    /// true indicates that a content added by this call contains at least one block element
    /// </param>
    private void AddSection(
      TextElement xamlParentElement,
      XmlElement htmlElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Analyze the content of htmlElement to decide what xaml element to choose - Section or Paragraph.
      // If this Div has at least one block child then we need to use Section, otherwise use Paragraph
      bool htmlElementContainsBlocks = false;
      for (XmlNode htmlChildNode = htmlElement.FirstChild;
          htmlChildNode != null;
          htmlChildNode = htmlChildNode.NextSibling)
      {
        if (htmlChildNode is XmlElement)
        {
          string htmlChildName = ((XmlElement)htmlChildNode).LocalName.ToLower();
          if (HtmlSchema.IsBlockElement(htmlChildName))
          {
            htmlElementContainsBlocks = true;
            break;
          }
        }
      }

      if (!htmlElementContainsBlocks)
      {
        // The Div does not contain any block elements, so we can treat it as a Paragraph
        AddParagraph(xamlParentElement, htmlElement, stylesheet, sourceContext);
      }
      else
      {
        // The Div has some nested blocks, so we treat it as a Section

        // Create currentProperties as a compilation of local, set localProperties
        GetElementProperties(htmlElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

        // Create a XAML element corresponding to this html element
        TextElement xamlElement = new Section() { Parent = xamlParentElement, Tag = AttachSourceAsTags ? htmlElement : null };
        ApplyLocalProperties(xamlElement, sourceContext, isBlock: true);

        // Decide whether we can unwrap this element as not having any formatting significance.
        if (!xamlElement.HasAttributes)
        {
          // This elements is a group of block elements whitout any additional formatting.
          // We can add blocks directly to xamlParentElement and avoid
          // creating unnecessary Sections nesting.
          xamlElement = xamlParentElement;
        }

        // Recurse into element subtree
        for (XmlNode htmlChildNode = htmlElement.FirstChild;
            htmlChildNode != null;
            htmlChildNode = htmlChildNode?.NextSibling)
        {
          htmlChildNode = AddBlock(xamlElement, htmlChildNode, stylesheet, sourceContext);
        }

        // Add the new element to the parent.
        if (xamlElement != xamlParentElement)
        {
          xamlParentElement.AppendChild(xamlElement);
        }
      }
    }

    /// <summary>
    ///     Generates Paragraph element from P, H1-H7, Center etc.
    /// </summary>
    /// <param name="xamlParentElement">
    ///     XmlElement representing Xaml parent to which the converted element should be added
    /// </param>
    /// <param name="htmlElement">
    ///     XmlElement representing Html element to be converted
    /// </param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    /// true indicates that a content added by this call contains at least one block element
    /// </param>
    private void AddParagraph(
      TextElement xamlParentElement,
      XmlElement htmlElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement,
      Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Create currentProperties as a compilation of local, set localProperties
      GetElementProperties(htmlElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

      // Create a XAML element corresponding to this html element
      Block xamlElement = new Paragraph() { Parent = xamlParentElement, Tag = AttachSourceAsTags ? htmlElement : null };
      ApplyLocalProperties(xamlElement, sourceContext, isBlock: true);

      // Recurse into element subtree
      for (XmlNode htmlChildNode = htmlElement.FirstChild;
          htmlChildNode != null;
          htmlChildNode = htmlChildNode.NextSibling)
      {
        AddInline(xamlElement, htmlChildNode, stylesheet, sourceContext);
      }

      // Add the new element to the parent.
      xamlParentElement.AppendChild(xamlElement);
    }

    /// <summary>
    ///     Creates a Paragraph element and adds all nodes starting from htmlNode
    ///     converted to appropriate Inlines.
    /// </summary>
    /// <param name="xamlParentElement">
    ///     XmlElement representing Xaml parent to which the converted element should be added
    /// </param>
    /// <param name="htmlNode">
    ///     XmlNode starting a collection of implicitly wrapped inlines.
    /// </param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    /// true indicates that a content added by this call contains at least one block element
    /// </param>
    /// <returns>
    ///     The last htmlNode added to the implicit paragraph
    /// </returns>
    private XmlNode AddImplicitParagraph(
      TextElement xamlParentElement,
      XmlNode htmlNode,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Collect all non-block elements and wrap them into implicit Paragraph
      Block xamlParagraph = new Paragraph() { Parent = xamlParentElement, Tag = AttachSourceAsTags ? htmlNode : null };
      XmlNode lastNodeProcessed = null;
      while (htmlNode != null)
      {
        if (htmlNode is XmlComment)
        {
          DefineInlineFragmentParent((XmlComment)htmlNode, /*xamlParentElement:*/null);
        }
        else if (htmlNode is XmlText)
        {
          if (htmlNode.Value.Trim().Length > 0)
          {
            AddTextRun(xamlParagraph, htmlNode.Value);
          }
        }
        else if (htmlNode is XmlElement)
        {
          string htmlChildName = ((XmlElement)htmlNode).LocalName.ToLower();
          if (HtmlSchema.IsBlockElement(htmlChildName))
          {
            // The sequence of non-blocked inlines ended. Stop implicit loop here.
            break;
          }
          AddInline(xamlParagraph, (XmlElement)htmlNode, stylesheet, sourceContext);
        }

        // Store last processed node to return it at the end
        lastNodeProcessed = htmlNode;
        htmlNode = htmlNode.NextSibling;
      }

      // Add the Paragraph to the parent
      // If only whitespaces and commens have been encountered,
      // then we have nothing to add in implicit paragraph; forget it.
      if (xamlParagraph.HasChildNodes)
      {
        xamlParentElement.AppendChild(xamlParagraph);
      }

      // Need to return last processed node
      return lastNodeProcessed;
    }

    // .............................................................
    //
    // Inline Elements
    //
    // .............................................................

    private void AddInline(
      TextElement xamlParentElement,
      XmlNode htmlNode,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      if (htmlNode is XmlComment)
      {
        DefineInlineFragmentParent((XmlComment)htmlNode, xamlParentElement);
      }
      else if (htmlNode is XmlText)
      {
        AddTextRun(xamlParentElement, htmlNode.Value);
      }
      else if (htmlNode is XmlElement)
      {
        XmlElement htmlElement = (XmlElement)htmlNode;

        // Check whether this is an html element
        if (htmlElement.NamespaceURI != HtmlParsing.HtmlParser.XhtmlNamespace)
        {
          return; // Skip non-html elements
        }

        // Identify element name
        string htmlElementName = htmlElement.LocalName.ToLower();

        // Put source element to the stack
        sourceContext.Add((htmlElement, new Dictionary<string, object>()));

        switch (htmlElementName)
        {
          case "a":
            AddHyperlink(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;
          case "img":
            AddImage(xamlParentElement, htmlElement, stylesheet, sourceContext);
            break;
          case "br":
          case "hr":
            AddBreak(xamlParentElement, htmlElementName);
            break;
          default:
            if (HtmlSchema.IsInlineElement(htmlElementName) || HtmlSchema.IsBlockElement(htmlElementName))
            {
              // Note: actually we do not expect block elements here,
              // but if it happens to be here, we will treat it as a Span.

              AddSpanOrRun(xamlParentElement, htmlElement, stylesheet, sourceContext);
            }
            break;
        }
        // Ignore all other elements non-(block/inline/image)

        // Remove the element from the stack
        DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == htmlElement);
        sourceContext.RemoveAt(sourceContext.Count - 1);
      }
    }

    private void AddSpanOrRun(
      TextElement xamlParentElement,
      XmlElement htmlElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Decide what XAML element to use for this inline element.
      // Check whether it contains any nested inlines
      bool elementHasChildren = false;
      for (XmlNode htmlNode = htmlElement.FirstChild; htmlNode != null; htmlNode = htmlNode.NextSibling)
      {
        if (htmlNode is XmlElement)
        {
          string htmlChildName = ((XmlElement)htmlNode).LocalName.ToLower();
          if (HtmlSchema.IsInlineElement(htmlChildName) || HtmlSchema.IsBlockElement(htmlChildName) ||
              htmlChildName == "img" || htmlChildName == "br" || htmlChildName == "hr")
          {
            elementHasChildren = true;
            break;
          }
        }
      }

      var xamlElement = elementHasChildren ? (Inline)new Span() : (Inline)new Run();
      xamlElement.Parent = xamlParentElement;
      xamlElement.Tag = AttachSourceAsTags ? htmlElement : null;

      // Create currentProperties as a compilation of local, set localProperties
      GetElementProperties(htmlElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

      ApplyLocalProperties(xamlElement, sourceContext, isBlock: false);

      // Recurse into element subtree
      for (XmlNode htmlChildNode = htmlElement.FirstChild;
          htmlChildNode != null;
          htmlChildNode = htmlChildNode.NextSibling)
      {
        AddInline(xamlElement, htmlChildNode, stylesheet, sourceContext);
      }

      // Add the new element to the parent.

      if (xamlElement is Run run && !string.IsNullOrEmpty(run.Text))
        xamlParentElement.AppendChild(xamlElement);
      else if (xamlElement is Span span && 0 != span.Childs.Count)
        xamlParentElement.AppendChild(xamlElement);

    }

    // Adds a text run to a xaml tree
    private static void AddTextRun(TextElement xamlElement, string textData)
    {
      // Remove control characters
      for (int i = 0; i < textData.Length; i++)
      {
        if (char.IsControl(textData[i]))
        {
          textData = textData.Remove(i--, 1); // decrement i to compensate for character removal
        }
      }

      // Replace No-Breaks by spaces (160 is a code of &nbsp; entity in html)
      //  This is a work around since WPF/XAML does not support &nbsp.
      // textData = textData.Replace((char)160, ' ');

      if (textData.Length > 0)
      {
        if (xamlElement is Run run)
          run.Text += textData;
        else if (xamlElement is Span span)
          span.AppendChild(new Run(textData));
        else if (xamlElement is Block block)
          block.AppendChild(new Run(textData));
        else
          throw new NotImplementedException();
      }
    }

    private void AddHyperlink(
      TextElement xamlParentElement,
      XmlElement htmlElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Convert href attribute into NavigateUri and TargetName
      string href = GetAttribute(htmlElement, "href");
      if (href == null)
      {
        // When href attribute is missing - ignore the hyperlink
        AddSpanOrRun(xamlParentElement, htmlElement, stylesheet, sourceContext);
      }
      else
      {
        // Create currentProperties as a compilation of local, set localProperties
        GetElementProperties(htmlElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

        // Create a XAML element corresponding to this html element
        var xamlElement = new Hyperlink() { Parent = xamlParentElement, Tag = AttachSourceAsTags ? htmlElement : null };
        ApplyLocalProperties(xamlElement, sourceContext, isBlock: false);

        string[] hrefParts = href.Split('#');
        if (hrefParts.Length > 0 && hrefParts[0].Trim().Length > 0)
        {
          xamlElement.NavigateUri = hrefParts[0].Trim();
        }
        if (hrefParts.Length == 2 && hrefParts[1].Trim().Length > 0)
        {
          xamlElement.TargetName = hrefParts[1].Trim();
        }

        // Recurse into element subtree
        for (XmlNode htmlChildNode = htmlElement.FirstChild;
            htmlChildNode != null;
            htmlChildNode = htmlChildNode.NextSibling)
        {
          AddInline(xamlElement, htmlChildNode, stylesheet, sourceContext);
        }

        // Add the new element to the parent.
        xamlParentElement.AppendChild(xamlElement);
      }
    }

    // Stores a parent xaml element for the case when selected fragment is inline.
    private static TextElement _inlineFragmentParentElement;

    // Called when html comment is encountered to store a parent element
    // for the case when the fragment is inline - to extract it to a separate
    // Span wrapper after the conversion.
    private static void DefineInlineFragmentParent(XmlComment htmlComment, TextElement xamlParentElement)
    {
      if (htmlComment.Value == "StartFragment")
      {
        _inlineFragmentParentElement = xamlParentElement;
      }
      else if (htmlComment.Value == "EndFragment")
      {
        if (_inlineFragmentParentElement == null && xamlParentElement != null)
        {
          // Normally this cannot happen if comments produced by correct copying code
          // in Word or IE, but when it is produced manually then fragment boundary
          // markers can be inconsistent. In this case StartFragment takes precedence,
          // but if it is not set, then we get the value from EndFragment marker.
          _inlineFragmentParentElement = xamlParentElement;
        }
      }
    }

    // Extracts a content of an element stored as InlineFragmentParentElement
    // into a separate Span wrapper.
    // Note: when selected content does not cross paragraph boundaries,
    // the fragment is marked within
    private static TextElement ExtractInlineFragment(TextElement xamlFlowDocumentElement)
    {
      if (_inlineFragmentParentElement != null)
      {
        Span spanElement;
        if (_inlineFragmentParentElement is Span span)
        {
          spanElement = span;
        }
        else
        {
          spanElement = new Span();
          while (_inlineFragmentParentElement.FirstChild != null)
          {
            var copyNode = _inlineFragmentParentElement.FirstChild;
            _inlineFragmentParentElement.RemoveChild(copyNode);
            copyNode.Parent = null;
            spanElement.AppendChild(copyNode);
          }
        }

        xamlFlowDocumentElement = spanElement;
      }

      return xamlFlowDocumentElement;
    }

    // .............................................................
    //
    // Images
    //
    // .............................................................

    private void AddImage(
      TextElement xamlParentElement,
      XmlElement htmlElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // test if htmlElement has a src attribute, otherwise we can skip over this
      var imageSource = htmlElement.GetAttribute("src");
      if (string.IsNullOrEmpty(imageSource))
        return;

      // if source is a relative name, convert it to an absolute name
      imageSource = CssStylesheets.GetAbsoluteFileNameForFileRelativeToHtmlFile(imageSource, (string)sourceContext[0].elementProperties["sourceHtmlFileName"]);

      GetElementProperties(htmlElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);



      var xamlContainerElement = xamlParentElement is Section || xamlParentElement is FlowDocument || xamlParentElement is ListItem || xamlParentElement is TableCell ? (TextElement)new BlockUIContainer() : (TextElement)new InlineUIContainer();
      xamlContainerElement.Parent = xamlParentElement;

      var xamlImageElement = new Image
      {
        Source = imageSource,
        Tag = AttachSourceAsTags ? htmlElement : null,
      };

      // Image Width and Height
      // are special insofar that the CSS width and height has priority over the attribute width and height
      // the attribute width and height are treated as hints about the true dimensions of the image
      // the CSS width and height then scale the image
      // if CSS width and height both are set to auto: then the image size is set to its true dimensions
      // if either CSS width or height are set to auto: then the other dimension is set to its value, and this dimension is set to preserve aspect ratio
      Dictionary<string, object> attributeProperties = new Dictionary<string, object>();
      stylesheet.GetElementProperties_Attributes_Only(htmlElement, sourceContext, attributeProperties);

      object localWidth = null;
      object localHeight = null;

      if (attributeProperties.ContainsKey("width") || attributeProperties.ContainsKey("height"))
      {
        attributeProperties.TryGetValue("width", out localWidth);
        attributeProperties.TryGetValue("height", out localHeight);
        Dictionary<string, object> cssProperties = new Dictionary<string, object>();
        stylesheet.GetElementProperties_CSS_Only(htmlElement, sourceContext, cssProperties);

        if (cssProperties.ContainsKey("width"))
          sourceContext[sourceContext.Count - 1].elementProperties["width"] = cssProperties["width"];
        if (cssProperties.ContainsKey("height"))
          sourceContext[sourceContext.Count - 1].elementProperties["height"] = cssProperties["height"];
      }

      var elementProperties = sourceContext[sourceContext.Count - 1].elementProperties;

      if (elementProperties.ContainsKey("width") && (elementProperties["width"] as string) == "auto" && elementProperties.ContainsKey("height") && (elementProperties["height"] as string) == "auto")
      {
        // image should be displayed in its original size
        xamlImageElement.Width = null;
        xamlImageElement.Height = null;

      }
      else if (elementProperties.ContainsKey("width") && (elementProperties["width"] as string) == "auto")
      {
        // heigth must be determined, width is the set to keep aspect ratio
        xamlImageElement.Width = null;
        xamlImageElement.Height = GetCompoundWidthOrHeightForContext("height", sourceContext);
      }
      else if (elementProperties.ContainsKey("height") && (elementProperties["height"] as string) == "auto")
      {
        // width must be determined, height is then set to keep aspect ratio
        xamlImageElement.Width = GetCompoundWidthOrHeightForContext("width", sourceContext);
        xamlImageElement.Height = null;

      }
      else
      {
        xamlImageElement.Width = GetCompoundWidthOrHeightForContext("width", sourceContext);
        xamlImageElement.Height = GetCompoundWidthOrHeightForContext("height", sourceContext);
      }


      // max-width and max-height
      xamlImageElement.MaxWidth = GetMaxWidthOrMaxHeightForContext("max-width", sourceContext);
      xamlImageElement.MaxHeight = GetMaxWidthOrMaxHeightForContext("max-height", sourceContext);




      xamlContainerElement.AppendChild(xamlImageElement); // put the image in the container
      xamlParentElement.AppendChild(xamlContainerElement); // put container in the document
    }



    // .............................................................
    //
    // Lists
    //
    // .............................................................

    /// <summary>
    ///     Converts Html ul or ol element into Xaml list element. During conversion if the ul/ol element has any children
    ///     that are not li elements, they are ignored and not added to the list element
    /// </summary>
    /// <param name="xamlParentElement">
    ///     XmlElement representing Xaml parent to which the converted element should be added
    /// </param>
    /// <param name="htmlListElement">
    ///     XmlElement representing Html ul/ol element to be converted
    /// </param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    private void AddList(
      TextElement xamlParentElement,
      XmlElement htmlListElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      string htmlListElementName = htmlListElement.LocalName.ToLower();

      GetElementProperties(htmlListElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

      // Create Xaml List element
      var xamlListElement = new List()
      {
        Parent = xamlParentElement,
        Tag = AttachSourceAsTags ? htmlListElement : null,
        MarkerStyle = htmlListElementName == "ol" ? ListMarkerStyle.Decimal : ListMarkerStyle.Disc
      };



      // Apply local properties to list to set marker attribute if specified
      // TODO: Should we have separate list attribute processing function?
      ApplyLocalProperties(xamlListElement, sourceContext, isBlock: true);

      // Recurse into list subtree
      for (XmlNode htmlChildNode = htmlListElement.FirstChild;
          htmlChildNode != null;
          htmlChildNode = htmlChildNode.NextSibling)
      {
        if (htmlChildNode is XmlElement && htmlChildNode.LocalName.ToLower() == "li")
        {
          sourceContext.Add(((XmlElement)htmlChildNode, new Dictionary<string, object>()));
          AddListItem(xamlListElement, (XmlElement)htmlChildNode, stylesheet, sourceContext);
          DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == htmlChildNode);
          sourceContext.RemoveAt(sourceContext.Count - 1);
        }
      }

      // Add the List element to xaml tree - if it is not empty
      if (xamlListElement.HasChildNodes)
      {
        xamlParentElement.AppendChild(xamlListElement);
      }
    }

    /// <summary>
    ///     If li items are found without a parent ul/ol element in Html string, creates xamlListElement as their parent and
    ///     adds
    ///     them to it. If the previously added node to the same xamlParentElement was a List, adds the elements to that list.
    ///     Otherwise, we create a new xamlListElement and add them to it. Elements are added as long as li elements appear
    ///     sequentially.
    ///     The first non-li or text node stops the addition.
    /// </summary>
    /// <param name="xamlParentElement">
    ///     Parent element for the list
    /// </param>
    /// <param name="htmlLiElement">
    ///     Start Html li element without parent list
    /// </param>
    /// <returns>
    ///     XmlNode representing the first non-li node in the input after one or more li's have been processed.
    /// </returns>
    private XmlElement AddOrphanListItems(
      TextElement xamlParentElement,
      XmlElement htmlLiElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      DebugAssert(htmlLiElement.LocalName.ToLower() == "li");

      XmlElement lastProcessedListItemElement = null;

      // Find out the last element attached to the xamlParentElement, which is the previous sibling of this node
      var xamlListItemElementPreviousSibling = xamlParentElement.LastChild;
      List xamlListElement;
      if (xamlListItemElementPreviousSibling is List list)
      {
        // Previously added Xaml element was a list. We will add the new li to it
        xamlListElement = list;
      }
      else
      {
        // No list element near. Create our own.
        xamlListElement = new List() { Parent = xamlParentElement, Tag = AttachSourceAsTags ? htmlLiElement : null };
        xamlParentElement.AppendChild(xamlListElement);
      }

      XmlNode htmlChildNode = htmlLiElement;
      string htmlChildNodeName = htmlChildNode == null ? null : htmlChildNode.LocalName.ToLower();

      //  Current element properties missed here.
      //currentProperties = GetElementProperties(htmlLIElement, inheritedProperties, out localProperties, stylesheet);

      // Add li elements to the parent xamlListElement we created as long as they appear sequentially
      // Use properties inherited from xamlParentElement for context
      while (htmlChildNode != null && htmlChildNodeName == "li")
      {
        AddListItem(xamlListElement, (XmlElement)htmlChildNode, stylesheet, sourceContext);
        lastProcessedListItemElement = (XmlElement)htmlChildNode;
        htmlChildNode = htmlChildNode.NextSibling;
        htmlChildNodeName = htmlChildNode?.LocalName.ToLower();
      }

      return lastProcessedListItemElement;
    }

    /// <summary>
    ///     Converts htmlLIElement into Xaml ListItem element, and appends it to the parent xamlListElement
    /// </summary>
    /// <param name="xamlListElement">
    ///     XmlElement representing Xaml List element to which the converted td/th should be added
    /// </param>
    /// <param name="htmlLiElement">
    ///     XmlElement representing Html li element to be converted
    /// </param>
    /// <param name="inheritedProperties">
    ///     Properties inherited from parent context
    /// </param>
    private void AddListItem(
      List xamlListElement,
      XmlElement htmlLiElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Parameter validation
      DebugAssert(xamlListElement != null);

      DebugAssert(htmlLiElement != null);
      DebugAssert(htmlLiElement.LocalName.ToLower() == "li");

      GetElementProperties(htmlLiElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

      var xamlListItemElement = new ListItem() { Parent = xamlListElement, Tag = AttachSourceAsTags ? htmlLiElement : null };

      // TODO: process local properties for li element

      // Process children of the ListItem
      for (XmlNode htmlChildNode = htmlLiElement.FirstChild;
          htmlChildNode != null;
          htmlChildNode = htmlChildNode?.NextSibling)
      {
        htmlChildNode = AddBlock(xamlListItemElement, htmlChildNode, stylesheet, sourceContext);
      }

      // Add resulting ListBoxItem to a xaml parent
      xamlListElement.AppendChild(xamlListItemElement);
    }

    // .............................................................
    //
    // Tables
    //
    // .............................................................

    /// <summary>
    ///     Converts htmlTableElement to a Xaml Table element. Adds tbody elements if they are missing so
    ///     that a resulting Xaml Table element is properly formed.
    /// </summary>
    /// <param name="xamlParentElement">
    ///     Parent xaml element to which a converted table must be added.
    /// </param>
    /// <param name="htmlTableElement">
    ///     XmlElement reprsenting the Html table element to be converted
    /// </param>
    private void AddTable(
      TextElement xamlParentElement,
      XmlElement htmlTableElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Parameter validation
      DebugAssert(htmlTableElement.LocalName.ToLower() == "table");
      DebugAssert(xamlParentElement != null);

      // Create current properties to be used by children as inherited properties, set local properties
      GetElementProperties(htmlTableElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

      // TODO: process localProperties for tables to override defaults, decide cell spacing defaults

      // Check if the table contains only one cell - we want to take only its content
      XmlElement singleCell = GetCellFromSingleCellTable(htmlTableElement);

      if (singleCell != null)
      {
        //  Need to push skipped table elements onto sourceContext
        sourceContext.Add((singleCell, new Dictionary<string, object>()));

        // Add the cell's content directly to parent
        for (XmlNode htmlChildNode = singleCell.FirstChild;
            htmlChildNode != null;
            htmlChildNode = htmlChildNode?.NextSibling)
        {
          htmlChildNode = AddBlock(xamlParentElement, htmlChildNode, stylesheet, sourceContext);
        }

        DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == singleCell);
        sourceContext.RemoveAt(sourceContext.Count - 1);
      }
      else
      {
        // Create xamlTableElement
        var xamlTableElement = new Table() { Parent = xamlParentElement, Tag = AttachSourceAsTags ? htmlTableElement : null };

        // Analyze table structure for column widths and rowspan attributes
        ArrayList columnStarts = AnalyzeTableStructure(htmlTableElement, stylesheet);

        // Process COLGROUP & COL elements
        AddColumnInformation(htmlTableElement, xamlTableElement, columnStarts, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet,
            sourceContext);

        // Process table body - TBODY and TR elements
        XmlNode htmlChildNode = htmlTableElement.FirstChild;

        htmlChildNode = ProcessTableChildNode(stylesheet, sourceContext, sourceContext[sourceContext.Count - 1].elementProperties, xamlTableElement, columnStarts, htmlChildNode);

        if (xamlTableElement.HasChildNodes)
        {
          xamlParentElement.AppendChild(xamlTableElement);
        }
      }
    }

    private XmlNode ProcessTableChildNode(CssStylesheets stylesheet, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext, Dictionary<string, object> currentProperties, Table xamlTableElement, ArrayList columnStarts, XmlNode htmlChildNode)
    {
      while (htmlChildNode != null)
      {
        string htmlChildName = htmlChildNode.LocalName.ToLower();

        // Process the element
        if (htmlChildName == "tbody" || htmlChildName == "thead" || htmlChildName == "tfoot")
        {
          //  Add more special processing for TableHeader and TableFooter
          var xamlTableBodyElement = new TableRowGroup() { Parent = xamlTableElement, Tag = AttachSourceAsTags ? htmlChildNode : null };


          sourceContext.Add(((XmlElement)htmlChildNode, new Dictionary<string, object>()));

          // Get properties of Html tbody element
          GetElementProperties((XmlElement)htmlChildNode, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);
          // TODO: apply local properties for tbody

          // Process children of htmlChildNode, which is tbody, for tr elements
          AddTableRowsToTableBody(xamlTableBodyElement, htmlChildNode.FirstChild,
              sourceContext[sourceContext.Count - 1].elementProperties,
              columnStarts, stylesheet, sourceContext);
          if (xamlTableBodyElement.HasChildNodes)
          {
            xamlTableElement.AppendChild(xamlTableBodyElement);
            // else: if there is no TRs in this TBody, we simply ignore it
          }

          DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == htmlChildNode);
          sourceContext.RemoveAt(sourceContext.Count - 1);

          htmlChildNode = htmlChildNode.NextSibling;
        }
        else if (htmlChildName == "tr")
        {
          // Tbody is not present, but tr element is present. Tr is wrapped in tbody
          var xamlTableBodyElement = new TableRowGroup() { Parent = xamlTableElement, Tag = AttachSourceAsTags ? htmlChildNode : null };

          // We use currentProperties of xamlTableElement when adding rows since the tbody element is artificially created and has
          // no properties of its own

          htmlChildNode = AddTableRowsToTableBody(xamlTableBodyElement, htmlChildNode, currentProperties,
              columnStarts,
              stylesheet, sourceContext);
          if (xamlTableBodyElement.HasChildNodes)
          {
            xamlTableElement.AppendChild(xamlTableBodyElement);
          }
        }
        else if (htmlChildName == "div")
        {
          sourceContext.Add(((XmlElement)htmlChildNode, new Dictionary<string, object>()));

          // Make a recursive call
          ProcessTableChildNode(stylesheet, sourceContext, currentProperties, xamlTableElement, columnStarts, htmlChildNode.FirstChild);

          DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == htmlChildNode);
          sourceContext.RemoveAt(sourceContext.Count - 1);
          htmlChildNode = htmlChildNode.NextSibling;

        }
        else
        {
          // Element is not tbody or tr. Ignore it.
          // TODO: add processing for thead, tfoot elements and recovery for td elements
          htmlChildNode = htmlChildNode.NextSibling;
        }
      }

      return htmlChildNode;
    }

    private static XmlElement GetCellFromSingleCellTable(XmlElement htmlTableElement)
    {
      XmlElement singleCell = null;

      for (XmlNode tableChild = htmlTableElement.FirstChild;
          tableChild != null;
          tableChild = tableChild.NextSibling)
      {
        string elementName = tableChild.LocalName.ToLower();
        if (elementName == "tbody" || elementName == "thead" || elementName == "tfoot")
        {
          if (singleCell != null)
          {
            return null;
          }
          for (XmlNode tbodyChild = tableChild.FirstChild;
              tbodyChild != null;
              tbodyChild = tbodyChild.NextSibling)
          {
            if (tbodyChild.LocalName.ToLower() == "tr")
            {
              if (singleCell != null)
              {
                return null;
              }
              for (XmlNode trChild = tbodyChild.FirstChild;
                  trChild != null;
                  trChild = trChild.NextSibling)
              {
                string cellName = trChild.LocalName.ToLower();
                if (cellName == "td" || cellName == "th")
                {
                  if (singleCell != null)
                  {
                    return null;
                  }
                  singleCell = (XmlElement)trChild;
                }
              }
            }
          }
        }
        else if (tableChild.LocalName.ToLower() == "tr")
        {
          if (singleCell != null)
          {
            return null;
          }
          for (XmlNode trChild = tableChild.FirstChild; trChild != null; trChild = trChild.NextSibling)
          {
            string cellName = trChild.LocalName.ToLower();
            if (cellName == "td" || cellName == "th")
            {
              if (singleCell != null)
              {
                return null;
              }
              singleCell = (XmlElement)trChild;
            }
          }
        }
      }

      return singleCell;
    }

    /// <summary>
    ///     Processes the information about table columns - COLGROUP and COL html elements.
    /// </summary>
    /// <param name="htmlTableElement">
    ///     XmlElement representing a source html table.
    /// </param>
    /// <param name="xamlTableElement">
    ///     XmlElement repesenting a resulting xaml table.
    /// </param>
    /// <param name="columnStartsAllRows">
    ///     Array of doubles - column start coordinates.
    ///     Can be null, which means that column size information is not available
    ///     and we must use source colgroup/col information.
    ///     In case wneh it's not null, we will ignore source colgroup/col information.
    /// </param>
    /// <param name="currentProperties"></param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    private static void AddColumnInformation(XmlElement htmlTableElement, Table xamlTableElement,
        ArrayList columnStartsAllRows, Dictionary<string, object> currentProperties, CssStylesheets stylesheet,
        List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Add column information
      if (columnStartsAllRows != null)
      {
        // We have consistent information derived from table cells; use it
        // The last element in columnStarts represents the end of the table
        for (int columnIndex = 0; columnIndex < columnStartsAllRows.Count - 1; columnIndex++)
        {


          var xamlColumnElement = new TableColumn()
          {
            Width = ((double)columnStartsAllRows[columnIndex + 1] - (double)columnStartsAllRows[columnIndex])
          };

          xamlTableElement.Columns.Add(xamlColumnElement);
        }
      }
      else
      {
        // We do not have consistent information from table cells;
        // Translate blindly colgroups from html.
        for (XmlNode htmlChildNode = htmlTableElement.FirstChild;
            htmlChildNode != null;
            htmlChildNode = htmlChildNode.NextSibling)
        {
          if (htmlChildNode.LocalName.ToLower() == "colgroup")
          {
            // TODO: add column width information to this function as a parameter and process it
            AddTableColumnGroup(xamlTableElement, (XmlElement)htmlChildNode, stylesheet, sourceContext);
          }
          else if (htmlChildNode.LocalName.ToLower() == "col")
          {
            AddTableColumn(xamlTableElement, (XmlElement)htmlChildNode, stylesheet, sourceContext);
          }
          else if (htmlChildNode is XmlElement)
          {
            // Some element which belongs to table body. Stop column loop.
            break;
          }
        }
      }
    }

    /// <summary>
    ///     Converts htmlColgroupElement into Xaml TableColumnGroup element, and appends it to the parent
    ///     xamlTableElement
    /// </summary>
    /// <param name="xamlTableElement">
    ///     XmlElement representing Xaml Table element to which the converted column group should be added
    /// </param>
    /// <param name="htmlColgroupElement">
    ///     XmlElement representing Html colgroup element to be converted
    ///     <param name="inheritedProperties">
    ///         Properties inherited from parent context
    ///     </param>
    private static void AddTableColumnGroup(
      Table xamlTableElement,
      XmlElement htmlColgroupElement,
      CssStylesheets stylesheet, List<(XmlElement xmlElement,
      Dictionary<string, object> elementProperties)> sourceContext)
    {
      GetElementProperties(htmlColgroupElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

      // TODO: process local properties for colgroup

      // Process children of colgroup. Colgroup may contain only col elements.
      for (XmlNode htmlNode = htmlColgroupElement.FirstChild; htmlNode != null; htmlNode = htmlNode.NextSibling)
      {
        if (htmlNode is XmlElement && htmlNode.LocalName.ToLower() == "col")
        {
          AddTableColumn(xamlTableElement, (XmlElement)htmlNode, stylesheet, sourceContext);
        }
      }
    }

    /// <summary>
    ///     Converts htmlColElement into Xaml TableColumn element, and appends it to the parent
    ///     xamlTableColumnGroupElement
    /// </summary>
    /// <param name="xamlTableElement"></param>
    /// <param name="htmlColElement">
    ///     XmlElement representing Html col element to be converted
    /// </param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    private static void AddTableColumn(
      Table xamlTableElement,
      XmlElement htmlColElement,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      GetElementProperties(htmlColElement, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

      var xamlTableColumnElement = new TableColumn();

      // TODO: process local properties for TableColumn element

      // Col is an empty element, with no subtree
      xamlTableElement.Columns.Add(xamlTableColumnElement);
    }

    /// <summary>
    ///     Adds TableRow elements to xamlTableBodyElement. The rows are converted from Html tr elements that
    ///     may be the children of an Html tbody element or an Html table element with tbody missing
    /// </summary>
    /// <param name="xamlTableBodyElement">
    ///     XmlElement representing Xaml TableRowGroup element to which the converted rows should be added
    /// </param>
    /// <param name="htmlTrStartNode">
    ///     XmlElement representing the first tr child of the tbody element to be read
    /// </param>
    /// <param name="currentProperties">
    ///     Dictionary<string, object> representing current properties of the tbody element that are generated and applied in the
    ///     AddTable function; to be used as inheritedProperties when adding tr elements
    /// </param>
    /// <param name="columnStarts"></param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    /// <returns>
    ///     XmlNode representing the current position of the iterator among tr elements
    /// </returns>
    private XmlNode AddTableRowsToTableBody(
      TableRowGroup xamlTableBodyElement,
      XmlNode htmlTrStartNode,
      Dictionary<string, object> currentProperties,
      ArrayList columnStarts,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Parameter validation

      DebugAssert(currentProperties != null);

      // Initialize child node for iteratimg through children to the first tr element
      XmlNode htmlChildNode = htmlTrStartNode;
      List<int> activeRowSpans = null;
      if (columnStarts != null)
      {
        activeRowSpans = new List<int>();
        InitializeActiveRowSpans(activeRowSpans, columnStarts.Count);
      }

      while (htmlChildNode != null && htmlChildNode.LocalName.ToLower() != "tbody")
      {
        if (htmlChildNode.LocalName.ToLower() == "tr")
        {
          var xamlTableRowElement = new TableRow() { Parent = xamlTableBodyElement, Tag = AttachSourceAsTags ? htmlChildNode : null };

          sourceContext.Add(((XmlElement)htmlChildNode, new Dictionary<string, object>()));

          // Get tr element properties
          GetElementProperties((XmlElement)htmlChildNode, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);
          // TODO: apply local properties to tr element

          AddTableCellsToTableRow(xamlTableRowElement, htmlChildNode.FirstChild, sourceContext[sourceContext.Count - 1].elementProperties,
              columnStarts,
              activeRowSpans, stylesheet, sourceContext);

          if (xamlTableRowElement.HasChildNodes)
          {
            xamlTableBodyElement.AppendChild(xamlTableRowElement);
          }

          DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == htmlChildNode);
          sourceContext.RemoveAt(sourceContext.Count - 1);

          // Advance
          htmlChildNode = htmlChildNode.NextSibling;
        }
        else if (htmlChildNode.LocalName.ToLower() == "td")
        {
          // Tr element is not present. We create one and add td elements to it
          var xamlTableRowElement = new TableRow() { Parent = xamlTableBodyElement, Tag = AttachSourceAsTags ? htmlChildNode : null };

          // This is incorrect formatting and the column starts should not be set in this case
          DebugAssert(columnStarts == null);

          htmlChildNode = AddTableCellsToTableRow(xamlTableRowElement, htmlChildNode, currentProperties,
              columnStarts,
              activeRowSpans, stylesheet, sourceContext);
          if (xamlTableRowElement.HasChildNodes)
          {
            xamlTableBodyElement.AppendChild(xamlTableRowElement);
          }
        }
        else
        {
          // Not a tr or td  element. Ignore it.
          // TODO: consider better recovery here
          htmlChildNode = htmlChildNode.NextSibling;
        }
      }
      return htmlChildNode;
    }

    /// <summary>
    ///     Adds TableCell elements to xamlTableRowElement.
    /// </summary>
    /// <param name="xamlTableRowElement">
    ///     XmlElement representing Xaml TableRow element to which the converted cells should be added
    /// </param>
    /// <param name="htmlTdStartNode">
    ///     XmlElement representing the child of tr or tbody element from which we should start adding td elements
    /// </param>
    /// <param name="currentProperties">
    ///     properties of the current html tr element to which cells are to be added
    /// </param>
    /// <returns>
    ///     XmlElement representing the current position of the iterator among the children of the parent Html tbody/tr element
    /// </returns>
    private XmlNode AddTableCellsToTableRow(
      TableRow xamlTableRowElement,
      XmlNode htmlTdStartNode,
      Dictionary<string, object> currentProperties,
      ArrayList columnStarts,
      List<int> activeRowSpans,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // parameter validation

      DebugAssert(currentProperties != null);
      if (columnStarts != null)
      {
        DebugAssert(activeRowSpans.Count == columnStarts.Count);
      }

      XmlNode htmlChildNode = htmlTdStartNode;
      double columnStart = 0;
      double columnWidth = 0;
      int columnIndex = 0;
      int columnSpan = 0;

      while (htmlChildNode != null && htmlChildNode.LocalName.ToLower() != "tr" &&
             htmlChildNode.LocalName.ToLower() != "tbody" && htmlChildNode.LocalName.ToLower() != "thead" &&
             htmlChildNode.LocalName.ToLower() != "tfoot")
      {
        if (htmlChildNode.LocalName.ToLower() == "td" || htmlChildNode.LocalName.ToLower() == "th")
        {
          var xamlTableCellElement = new TableCell() { Parent = xamlTableRowElement, Tag = AttachSourceAsTags ? htmlChildNode : null };

          sourceContext.Add(((XmlElement)htmlChildNode, new Dictionary<string, object>()));

          GetElementProperties((XmlElement)htmlChildNode, sourceContext[sourceContext.Count - 1].elementProperties, stylesheet, sourceContext);

          // TODO: determine if localProperties can be used instead of htmlChildNode in this call, and if they can,
          // make necessary changes and use them instead.
          ApplyPropertiesToTableCellElement((XmlElement)htmlChildNode, xamlTableCellElement);

          if (columnStarts != null)
          {
            DebugAssert(columnIndex < columnStarts.Count);
            while (columnIndex < activeRowSpans.Count && (int)activeRowSpans[columnIndex] > 0)
            {
              activeRowSpans[columnIndex] = (int)activeRowSpans[columnIndex] - 1;
              DebugAssert((int)activeRowSpans[columnIndex] >= 0);
              columnIndex++;
            }
            DebugAssert(columnIndex < columnStarts.Count);
            columnStart = (double)columnStarts[columnIndex];
            columnWidth = GetColumnWidth((XmlElement)htmlChildNode);
            columnSpan = CalculateColumnSpan(columnIndex, columnWidth, columnStarts);
            int rowSpan = GetRowSpan((XmlElement)htmlChildNode);

            // Column cannot have no span
            DebugAssert(columnSpan >= 0);
            DebugAssert(columnIndex + columnSpan <= columnStarts.Count);

            xamlTableCellElement.ColumnSpan = columnSpan;

            // Apply row span
            for (int spannedColumnIndex = columnIndex;
                spannedColumnIndex < columnIndex + columnSpan;
                spannedColumnIndex++)
            {
              DebugAssert(spannedColumnIndex < activeRowSpans.Count);
              activeRowSpans[spannedColumnIndex] = (rowSpan - 1);
              DebugAssert((int)activeRowSpans[spannedColumnIndex] >= 0);
            }

            columnIndex = columnIndex + columnSpan;
          }

          AddDataToTableCell(xamlTableCellElement, htmlChildNode.FirstChild, sourceContext[sourceContext.Count - 1].elementProperties,
              stylesheet,
              sourceContext);
          if (xamlTableCellElement.HasChildNodes)
          {
            xamlTableRowElement.AppendChild(xamlTableCellElement);
          }

          DebugAssert(sourceContext.Count > 0 && sourceContext[sourceContext.Count - 1].xmlElement == htmlChildNode);
          sourceContext.RemoveAt(sourceContext.Count - 1);

          htmlChildNode = htmlChildNode.NextSibling;
        }
        else
        {
          // Not td element. Ignore it.
          // TODO: Consider better recovery
          htmlChildNode = htmlChildNode.NextSibling;
        }
      }

      // decrement all active row spans to the right of the last column
      if (null != activeRowSpans)
      {
        for (int i = columnIndex; i < activeRowSpans.Count; ++i)
        {
          activeRowSpans[i] = Math.Max(activeRowSpans[i] - 1, 0);
        }
      }

      return htmlChildNode;
    }

    /// <summary>
    ///     adds table cell data to xamlTableCellElement
    /// </summary>
    /// <param name="xamlTableCellElement">
    ///     XmlElement representing Xaml TableCell element to which the converted data should be added
    /// </param>
    /// <param name="htmlDataStartNode">
    ///     XmlElement representing the start element of data to be added to xamlTableCellElement
    /// </param>
    /// <param name="currentProperties">
    ///     Current properties for the html td/th element corresponding to xamlTableCellElement
    /// </param>
    private void AddDataToTableCell(
      TableCell xamlTableCellElement,
      XmlNode htmlDataStartNode,
      Dictionary<string, object> currentProperties,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      // Parameter validation

      DebugAssert(currentProperties != null);

      for (XmlNode htmlChildNode = htmlDataStartNode;
          htmlChildNode != null;
          htmlChildNode = htmlChildNode?.NextSibling)
      {
        // Process a new html element and add it to the td element
        htmlChildNode = AddBlock(xamlTableCellElement, htmlChildNode, stylesheet, sourceContext);
      }
    }

    /// <summary>
    ///     Performs a parsing pass over a table to read information about column width and rowspan attributes. This
    ///     information
    ///     is used to determine the starting point of each column.
    /// </summary>
    /// <param name="htmlTableElement">
    ///     XmlElement representing Html table whose structure is to be analyzed
    /// </param>
    /// <returns>
    ///     ArrayList of type double which contains the function output. If analysis is successful, this ArrayList contains
    ///     all the points which are the starting position of any column in the table, ordered from left to right.
    ///     In case if analisys was impossible we return null.
    /// </returns>
    private static ArrayList AnalyzeTableStructure(XmlElement htmlTableElement, CssStylesheets stylesheet)
    {
      // Parameter validation
      DebugAssert(htmlTableElement.LocalName.ToLower() == "table");
      if (!htmlTableElement.HasChildNodes)
      {
        return null;
      }

      bool columnWidthsAvailable = true;

      ArrayList columnStarts = new ArrayList();
      ArrayList activeRowSpans = new ArrayList();
      DebugAssert(columnStarts.Count == activeRowSpans.Count);

      XmlNode htmlChildNode = htmlTableElement.FirstChild;
      double tableWidth = 0; // Keep track of table width which is the width of its widest row

      // Analyze tbody and tr elements
      while (htmlChildNode != null && columnWidthsAvailable)
      {
        DebugAssert(columnStarts.Count == activeRowSpans.Count);

        switch (htmlChildNode.LocalName.ToLower())
        {
          case "tbody":
            // Tbody element, we should analyze its children for trows
            double tbodyWidth = AnalyzeTbodyStructure((XmlElement)htmlChildNode, columnStarts,
                activeRowSpans, tableWidth,
                stylesheet);
            if (tbodyWidth > tableWidth)
            {
              // Table width must be increased to supported newly added wide row
              tableWidth = tbodyWidth;
            }
            else if (tbodyWidth == 0)
            {
              // Tbody analysis may return 0, probably due to unprocessable format.
              // We should also fail.
              columnWidthsAvailable = false; // interrupt the analisys
            }
            break;
          case "tr":
            // Table row. Analyze column structure within row directly
            double trWidth = AnalyzeTrStructure((XmlElement)htmlChildNode, columnStarts, activeRowSpans,
                tableWidth,
                stylesheet);
            if (trWidth > tableWidth)
            {
              tableWidth = trWidth;
            }
            else if (trWidth == 0)
            {
              columnWidthsAvailable = false; // interrupt the analisys
            }
            break;
          case "td":
            // Incorrect formatting, too deep to analyze at this level. Return null.
            // TODO: implement analysis at this level, possibly by creating a new tr
            columnWidthsAvailable = false; // interrupt the analisys
            break;
          default:
            // Element should not occur directly in table. Ignore it.
            break;
        }

        htmlChildNode = htmlChildNode.NextSibling;
      }

      if (columnWidthsAvailable)
      {
        // Add an item for whole table width
        columnStarts.Add(tableWidth);
        VerifyColumnStartsAscendingOrder(columnStarts);
      }
      else
      {
        columnStarts = null;
      }

      return columnStarts;
    }

    /// <summary>
    ///     Performs a parsing pass over a tbody to read information about column width and rowspan attributes. Information
    ///     read about width
    ///     attributes is stored in the reference ArrayList parameter columnStarts, which contains a list of all starting
    ///     positions of all columns in the table, ordered from left to right. Row spans are taken into consideration when
    ///     computing column starts
    /// </summary>
    /// <param name="htmlTbodyElement">
    ///     XmlElement representing Html tbody whose structure is to be analyzed
    /// </param>
    /// <param name="columnStarts">
    ///     ArrayList of type double which contains the function output. If analysis fails, this parameter is set to null
    /// </param>
    /// <param name="tableWidth">
    ///     Current width of the table. This is used to determine if a new column when added to the end of table should
    ///     come after the last column in the table or is actually splitting the last column in two. If it is only splitting
    ///     the last column it should inherit row span for that column
    /// </param>
    /// <returns>
    ///     Calculated width of a tbody.
    ///     In case of non-analizable column width structure return 0;
    /// </returns>
    private static double AnalyzeTbodyStructure(XmlElement htmlTbodyElement, ArrayList columnStarts,
        ArrayList activeRowSpans, double tableWidth, CssStylesheets stylesheet)
    {
      // Parameter validation
      DebugAssert(htmlTbodyElement.LocalName.ToLower() == "tbody");
      DebugAssert(columnStarts != null);

      double tbodyWidth = 0;
      bool columnWidthsAvailable = true;

      if (!htmlTbodyElement.HasChildNodes)
      {
        return tbodyWidth;
      }

      // Set active row spans to 0 - thus ignoring row spans crossing tbody boundaries
      ClearActiveRowSpans(activeRowSpans);

      XmlNode htmlChildNode = htmlTbodyElement.FirstChild;

      // Analyze tr elements
      while (htmlChildNode != null && columnWidthsAvailable)
      {
        switch (htmlChildNode.LocalName.ToLower())
        {
          case "tr":
            double trWidth = AnalyzeTrStructure((XmlElement)htmlChildNode, columnStarts, activeRowSpans,
                tbodyWidth,
                stylesheet);
            if (trWidth > tbodyWidth)
            {
              tbodyWidth = trWidth;
            }
            break;
          case "td":
            columnWidthsAvailable = false; // interrupt the analisys
            break;
          default:
            break;
        }
        htmlChildNode = htmlChildNode.NextSibling;
      }

      // Set active row spans to 0 - thus ignoring row spans crossing tbody boundaries
      ClearActiveRowSpans(activeRowSpans);

      return columnWidthsAvailable ? tbodyWidth : 0;
    }

    static int _debugCounter = 0;

    /// <summary>
    ///     Performs a parsing pass over a tr element to read information about column width and rowspan attributes.
    /// </summary>
    /// <param name="htmlTrElement">
    ///     XmlElement representing Html tr element whose structure is to be analyzed
    /// </param>
    /// <param name="columnStarts">
    ///     ArrayList of type double which contains the function output. If analysis is successful, this ArrayList contains
    ///     all the points which are the starting position of any column in the tr, ordered from left to right. If analysis
    ///     fails,
    ///     the ArrayList is set to null
    /// </param>
    /// <param name="activeRowSpans">
    ///     ArrayList representing all columns currently spanned by an earlier row span attribute. These columns should
    ///     not be used for data in this row. The ArrayList actually contains notation for all columns in the table, if the
    ///     active row span is set to 0 that column is not presently spanned but if it is > 0 the column is presently spanned
    /// </param>
    /// <param name="tableWidth">
    ///     Double value representing the current width of the table.
    ///     Return 0 if analisys was insuccessful.
    /// </param>
    private static double AnalyzeTrStructure(XmlElement htmlTrElement, ArrayList columnStarts,
        ArrayList activeRowSpans,
        double tableWidth, CssStylesheets stylesheet)
    {
      ++_debugCounter;

      double columnWidth;

      // Parameter validation
      DebugAssert(htmlTrElement.LocalName.ToLower() == "tr");
      DebugAssert(columnStarts != null);
      DebugAssert(activeRowSpans != null);
      DebugAssert(columnStarts.Count == activeRowSpans.Count);

      if (!htmlTrElement.HasChildNodes)
      {
        return 0;
      }

      bool columnWidthsAvailable = true;

      double columnStart = 0; // starting position of current column
      XmlNode htmlChildNode = htmlTrElement.FirstChild;
      int columnIndex = 0;
      double trWidth = 0;

      // Skip spanned columns to get to real column start
      if (columnIndex < activeRowSpans.Count)
      {
        DebugAssert((double)columnStarts[columnIndex] >= columnStart);
        if ((double)columnStarts[columnIndex] == columnStart)
        {
          // The new column may be in a spanned area
          while (columnIndex < activeRowSpans.Count && (int)activeRowSpans[columnIndex] > 0)
          {
            activeRowSpans[columnIndex] = (int)activeRowSpans[columnIndex] - 1;
            DebugAssert((int)activeRowSpans[columnIndex] >= 0);
            columnIndex++;
            columnStart = (double)columnStarts[columnIndex];
          }
        }
      }

      while (htmlChildNode != null && columnWidthsAvailable)
      {
        DebugAssert(columnStarts.Count == activeRowSpans.Count);

        VerifyColumnStartsAscendingOrder(columnStarts);

        switch (htmlChildNode.LocalName.ToLower())
        {
          case "td":
            DebugAssert(columnIndex <= columnStarts.Count);
            if (columnIndex < columnStarts.Count)
            {
              DebugAssert(columnStart <= (double)columnStarts[columnIndex]);
              if (columnStart < (double)columnStarts[columnIndex])
              {
                columnStarts.Insert(columnIndex, columnStart);
                // There can be no row spans now - the column data will appear here
                // Row spans may appear only during the column analysis
                activeRowSpans.Insert(columnIndex, 0);
              }
            }
            else
            {
              // Column start is greater than all previous starts. Row span must still be 0 because
              // we are either adding after another column of the same row, in which case it should not inherit
              // the previous column's span. Otherwise we are adding after the last column of some previous
              // row, and assuming the table widths line up, we should not be spanned by it. If there is
              // an incorrect tbale structure where a columns starts in the middle of a row span, we do not
              // guarantee correct output
              columnStarts.Add(columnStart);
              activeRowSpans.Add(0);
            }
            columnWidth = GetColumnWidth((XmlElement)htmlChildNode);
            if (columnWidth != -1)
            {
              int nextColumnIndex;
              int rowSpan = GetRowSpan((XmlElement)htmlChildNode);

              nextColumnIndex = GetNextColumnIndex(columnIndex, columnWidth, columnStarts, activeRowSpans);
              if (nextColumnIndex != -1)
              {
                // Entire column width can be processed without hitting conflicting row span. This means that
                // column widths line up and we can process them
                DebugAssert(nextColumnIndex <= columnStarts.Count);

                // Apply row span to affected columns
                for (int spannedColumnIndex = columnIndex;
                    spannedColumnIndex < nextColumnIndex;
                    spannedColumnIndex++)
                {
                  activeRowSpans[spannedColumnIndex] = rowSpan - 1;
                  DebugAssert((int)activeRowSpans[spannedColumnIndex] >= 0);
                }

                columnIndex = nextColumnIndex;

                // Calculate columnsStart for the next cell
                columnStart = columnStart + columnWidth;

                if (columnIndex < activeRowSpans.Count)
                {
                  DebugAssert((double)columnStarts[columnIndex] >= columnStart);
                  if ((double)columnStarts[columnIndex] == columnStart)
                  {
                    // The new column may be in a spanned area
                    while (columnIndex < activeRowSpans.Count &&
                           (int)activeRowSpans[columnIndex] > 0)
                    {
                      activeRowSpans[columnIndex] = (int)activeRowSpans[columnIndex] - 1;
                      DebugAssert((int)activeRowSpans[columnIndex] >= 0);
                      columnIndex++;
                      if (columnIndex < columnStarts.Count)
                        columnStart = (double)columnStarts[columnIndex];
                    }
                  }
                  // else: the new column does not start at the same time as a pre existing column
                  // so we don't have to check it for active row spans, it starts in the middle
                  // of another column which has been checked already by the GetNextColumnIndex function
                }
              }
              else
              {
                // Full column width cannot be processed without a pre existing row span.
                // We cannot analyze widths
                columnWidthsAvailable = false;
              }
            }
            else
            {
              // Incorrect column width, stop processing
              columnWidthsAvailable = false;
            }
            break;
          default:
            break;
        }

        htmlChildNode = htmlChildNode.NextSibling;
      }

      // The width of the tr element is the position at which it's last td element ends, which is calculated in
      // the columnStart value after each td element is processed
      trWidth = columnWidthsAvailable ? columnStart : 0;

      return trWidth;
    }

    /// <summary>
    ///     Gets row span attribute from htmlTDElement. Returns an integer representing the value of the rowspan attribute.
    ///     Default value if attribute is not specified or if it is invalid is 1
    /// </summary>
    /// <param name="htmlTdElement">
    ///     Html td element to be searched for rowspan attribute
    /// </param>
    private static int GetRowSpan(XmlElement htmlTdElement)
    {
      string rowSpanAsString;
      int rowSpan;

      rowSpanAsString = GetAttribute(htmlTdElement, "rowspan");
      if (rowSpanAsString != null)
      {
        if (int.TryParse(rowSpanAsString, out rowSpan))
        {
          if (rowSpan < 1)
          {
            rowSpan = 1;
          }
        }
        else
        {
          // Ignore invalid value of rowspan; treat it as 1
          rowSpan = 1;
        }
      }

      else
      {
        // No row span, default is 1
        rowSpan = 1;
      }
      return rowSpan;
    }

    /// <summary>
    ///     Gets index at which a column should be inseerted into the columnStarts ArrayList. This is
    ///     decided by the value columnStart. The columnStarts ArrayList is ordered in ascending order.
    ///     Returns an integer representing the index at which the column should be inserted
    /// </summary>
    /// <param name="columnStarts">
    ///     Array list representing starting coordinates of all columns in the table
    /// </param>
    /// <param name="columnStart">
    ///     Starting coordinate of column we wish to insert into columnStart
    /// </param>
    /// <param name="columnIndex">
    ///     Int representing the current column index. This acts as a clue while finding the insertion index.
    ///     If the value of columnStarts at columnIndex is the same as columnStart, then this position alrady exists
    ///     in the array and we can jsut return columnIndex.
    /// </param>
    /// <returns></returns>
    private static int GetNextColumnIndex(int columnIndex, double columnWidth, ArrayList columnStarts,
        ArrayList activeRowSpans)
    {
      double columnStart;
      int spannedColumnIndex;

      // Parameter validation
      DebugAssert(columnStarts != null);
      DebugAssert(0 <= columnIndex && columnIndex <= columnStarts.Count);
      DebugAssert(columnWidth > 0);

      columnStart = (double)columnStarts[columnIndex];
      spannedColumnIndex = columnIndex + 1;

      while (spannedColumnIndex < columnStarts.Count &&
             (double)columnStarts[spannedColumnIndex] < columnStart + columnWidth && spannedColumnIndex != -1)
      {
        if ((int)activeRowSpans[spannedColumnIndex] > 0)
        {
          // The current column should span this area, but something else is already spanning it
          // Not analyzable
          spannedColumnIndex = -1;
        }
        else
        {
          spannedColumnIndex++;
        }
      }

      return spannedColumnIndex;
    }


    /// <summary>
    ///     Used for clearing activeRowSpans array in the beginning/end of each tbody
    /// </summary>
    /// <param name="activeRowSpans">
    ///     ArrayList representing currently active row spans
    /// </param>
    private static void ClearActiveRowSpans(ArrayList activeRowSpans)
    {
      for (int columnIndex = 0; columnIndex < activeRowSpans.Count; columnIndex++)
      {
        activeRowSpans[columnIndex] = 0;
      }
    }

    /// <summary>
    ///     Used for initializing activeRowSpans array in the before adding rows to tbody element
    /// </summary>
    /// <param name="activeRowSpans">
    ///     ArrayList representing currently active row spans
    /// </param>
    /// <param name="count">
    ///     Size to be give to array list
    /// </param>
    private static void InitializeActiveRowSpans(List<int> activeRowSpans, int count)
    {
      for (int columnIndex = 0; columnIndex < count; columnIndex++)
      {
        activeRowSpans.Add(0);
      }
    }


    /// <summary>
    ///     Calculates width of next TD element based on starting position of current element and it's width, which
    ///     is calculated byt he function
    /// </summary>
    /// <param name="htmlTdElement">
    ///     XmlElement representing Html td element whose width is to be read
    /// </param>
    /// <param name="columnStart">
    ///     Starting position of current column
    /// </param>
    private static double GetNextColumnStart(XmlElement htmlTdElement, double columnStart)
    {
      double columnWidth;
      double nextColumnStart;

      // Parameter validation
      DebugAssert(htmlTdElement.LocalName.ToLower() == "td" || htmlTdElement.LocalName.ToLower() == "th");
      DebugAssert(columnStart >= 0);

      nextColumnStart = -1; // -1 indicates inability to calculate columnStart width

      columnWidth = GetColumnWidth(htmlTdElement);

      if (columnWidth == -1)
      {
        nextColumnStart = -1;
      }
      else
      {
        nextColumnStart = columnStart + columnWidth;
      }

      return nextColumnStart;
    }


    private static double GetColumnWidth(XmlElement htmlTdElement)
    {
      string columnWidthAsString;
      double columnWidth;

      columnWidthAsString = null;
      columnWidth = -1;

      // Get string valkue for the width
      columnWidthAsString = GetAttribute(htmlTdElement, "width") ??
                            GetCssAttribute(GetAttribute(htmlTdElement, "style"), "width");

      // We do not allow column width to be 0, if specified as 0 we will fail to record it
      if (!TryGetLengthValue(columnWidthAsString, out columnWidth) || columnWidth == 0)
      {
        columnWidth = 100;
      }
      return columnWidth;
    }

    /// <summary>
    ///     Calculates column span based the column width and the widths of all other columns. Returns an integer representing
    ///     the column span
    /// </summary>
    /// <param name="columnIndex">
    ///     Index of the current column
    /// </param>
    /// <param name="columnWidth">
    ///     Width of the current column
    /// </param>
    /// <param name="columnStarts">
    ///     ArrayList repsenting starting coordinates of all columns
    /// </param>
    private static int CalculateColumnSpan(int columnIndex, double columnWidth, ArrayList columnStarts)
    {
      // Current status of column width. Indicates the amount of width that has been scanned already
      double columnSpanningValue;
      int columnSpanningIndex;
      int columnSpan;
      double subColumnWidth; // Width of the smallest-grain columns in the table

      DebugAssert(columnStarts != null);
      DebugAssert(columnIndex < columnStarts.Count);
      DebugAssert((double)columnStarts[columnIndex] >= 0);
      DebugAssert(columnWidth >= 0);

      columnSpanningIndex = columnIndex;
      columnSpanningValue = 0;
      columnSpan = 0;
      subColumnWidth = 0;

      while (columnSpanningValue < columnWidth && columnSpanningIndex < columnStarts.Count - 1)
      {
        subColumnWidth = (double)columnStarts[columnSpanningIndex + 1] -
                         (double)columnStarts[columnSpanningIndex];
        DebugAssert(subColumnWidth > 0);
        columnSpanningValue += subColumnWidth;
        columnSpanningIndex++;
      }

      // Now, we have either covered the width we needed to cover or reached the end of the table, in which
      // case the column spans all the columns until the end


      columnSpan = columnSpanningIndex - columnIndex;
      columnSpan = Math.Max(1, columnSpan);
      DebugAssert(columnSpan > 0);

      return columnSpan;
    }

    /// <summary>
    ///     Verifies that values in columnStart, which represent starting coordinates of all columns, are arranged
    ///     in ascending order
    /// </summary>
    /// <param name="columnStarts">
    ///     ArrayList representing starting coordinates of all columns
    /// </param>
    private static void VerifyColumnStartsAscendingOrder(ArrayList columnStarts)
    {
      DebugAssert(columnStarts != null);

      double columnStart;

      columnStart = -0.01;

      foreach (object t in columnStarts)
      {
        DebugAssert(columnStart < (double)t);
        columnStart = (double)t;
      }
    }

    // .............................................................
    //
    // Attributes and Properties
    //
    // .............................................................

    /// <summary>
    ///     Analyzes local properties of Html element, converts them into Xaml equivalents, and applies them to xamlElement
    /// </summary>
    /// <param name="xamlElement">
    ///     XmlElement representing Xaml element to which properties are to be applied
    /// </param>
    /// <param name="localProperties">
    ///     Dictionary<string, object> representing local properties of Html element that is converted into xamlElement
    /// </param>
    private static void ApplyLocalProperties(TextElement xamlElement, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext, bool isBlock)
    {
      bool marginSet = false;
      ExCSS.Length? marginTop = null;
      ExCSS.Length? marginBottom = null;
      ExCSS.Length? marginLeft = null;
      ExCSS.Length? marginRight = null;

      bool paddingSet = false;
      ExCSS.Length? paddingTop = null;
      ExCSS.Length? paddingBottom = null;
      ExCSS.Length? paddingLeft = null;
      ExCSS.Length? paddingRight = null;

      ExCSS.Color? borderColor = null;

      bool borderThicknessSet = false;
      ExCSS.Length? borderThicknessTop = null;
      ExCSS.Length? borderThicknessBottom = null;
      ExCSS.Length? borderThicknessLeft = null;
      ExCSS.Length? borderThicknessRight = null;

      var localProperties = sourceContext[sourceContext.Count - 1].elementProperties;

      double? currentFontSize = null;

      foreach (var entry in localProperties)
      {
        switch (entry.Key)
        {
          case "font-family":
            //  Convert from font-family value list into xaml FontFamily value
            xamlElement.FontFamily = (string)entry.Value;
            break;
          case "font-style":
            xamlElement.FontStyle = PropertyExtensions.GetFontStyle((string)entry.Value);
            break;
          case "font-variant":
            //  Convert from font-variant into xaml property
            break;
          case "font-weight":
            xamlElement.FontWeight = PropertyExtensions.GetFontWeight((string)entry.Value);
            break;
          case "font-size":
            //  Convert from css size into FontSize
            currentFontSize = GetAbsoluteFontSizeForContext("font-size", sourceContext, null);
            xamlElement.FontSize = currentFontSize;
            break;
          case "line-height":
            if (xamlElement is Block block)
            {
              if (entry.Value is double d)
                block.LineHeight = d;
              else if (entry.Value is string s)
              {
                switch (s)
                {
                  case "normal":
                    block.LineHeight = 1;
                    break;
                  default:
                    break;
                }
              }
            }
            break;
          case "color":
            xamlElement.Foreground = GetColor(entry.Value, ExCSS.Color.Black);
            break;
          case "background-color":
            xamlElement.Background = GetColor(entry.Value, ExCSS.Color.White);
            break;
          case "text-decoration-underline":
            if (xamlElement is Paragraph pa)
            {
              pa.TextDecorations = TextDecorations.Underline;
            }
            break;
          case "text-decoration-none":
          case "text-decoration-overline":
          case "text-decoration-line-through":
          case "text-decoration-blink":
            //  Convert from all other text-decorations values
            if (!isBlock)
            {
            }
            break;
          case "text-transform":
            //  Convert from text-transform into xaml property
            break;

          case "text-indent":
            if (isBlock)
            {
              if (xamlElement is Section s)
              {
                // Section does not support indent-> instead we use margin
                var val = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
                s.Margin = s.Margin == null ?
                  new Thickness { Left = val ?? new ExCSS.Length(0, ExCSS.Length.Unit.Px) } :
                  s.Margin.Value.WithLeft(Add(s.Margin.Value.Left, val) ?? ZeroPixel);
              }
              else if (xamlElement is Paragraph p)
              {
                p.TextIndent = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
              }
            }
            break;

          case "text-align":
            if (xamlElement is Block bb)
            {
              bb.TextAlignment = PropertyExtensions.GetTextAlignment((string)entry.Value);
            }
            break;

          case "width":
          case "height":
            //  Decide what to do with width and height propeties
            break;

          case "margin-top":
            marginSet = true;
            marginTop = GetMargin(entry.Value, ref currentFontSize, sourceContext);
            break;
          case "margin-right":
            marginSet = true;
            marginRight = GetMargin(entry.Value, ref currentFontSize, sourceContext);
            break;
          case "margin-bottom":
            marginSet = true;
            marginBottom = GetMargin(entry.Value, ref currentFontSize, sourceContext);
            break;
          case "margin-left":
            marginSet = true;
            marginLeft = GetMargin(entry.Value, ref currentFontSize, sourceContext);
            break;

          case "padding-top":
            paddingSet = true;
            paddingTop = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;
          case "padding-right":
            paddingSet = true;
            paddingRight = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;
          case "padding-bottom":
            paddingSet = true;
            paddingBottom = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;
          case "padding-left":
            paddingSet = true;
            paddingLeft = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;

          // NOTE: css names for elementary border styles have side indications in the middle (top/bottom/left/right)
          // In our internal notation we intentionally put them at the end - to unify processing in ParseCssRectangleProperty method
          case "border-color-top":
            borderColor = GetColor(entry.Value, ExCSS.Color.Black);
            break;
          case "border-color-right":
            borderColor = GetColor(entry.Value, ExCSS.Color.Black);
            break;
          case "border-color-bottom":
            borderColor = GetColor(entry.Value, ExCSS.Color.Black);
            break;
          case "border-color-left":
            borderColor = GetColor(entry.Value, ExCSS.Color.Black);
            break;
          case "border-style-top":
          case "border-style-right":
          case "border-style-bottom":
          case "border-style-left":
            //  Implement conversion from border style
            break;
          case "border-width-top":
            borderThicknessSet = true;
            borderThicknessTop = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;
          case "border-width-right":
            borderThicknessSet = true;
            borderThicknessRight = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;
          case "border-width-bottom":
            borderThicknessSet = true;
            borderThicknessBottom = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;
          case "border-width-left":
            borderThicknessSet = true;
            borderThicknessLeft = ResolveRemEmExCh((ExCSS.Length?)entry.Value, ref currentFontSize, sourceContext);
            break;

          case "list-style-type":
            if (xamlElement is List list)
            {

              switch (((string)entry.Value).ToLower())
              {
                case "disc":
                  list.MarkerStyle = ListMarkerStyle.Disc;
                  break;
                case "circle":
                  list.MarkerStyle = ListMarkerStyle.Circle;
                  break;
                case "none":
                  list.MarkerStyle = ListMarkerStyle.None;
                  break;
                case "square":
                  list.MarkerStyle = ListMarkerStyle.Square;
                  break;
                case "box":
                  list.MarkerStyle = ListMarkerStyle.Box;
                  break;
                case "lower-latin":
                  list.MarkerStyle = ListMarkerStyle.LowerLatin;
                  break;
                case "upper-latin":
                  list.MarkerStyle = ListMarkerStyle.UpperLatin;
                  break;
                case "lower-roman":
                  list.MarkerStyle = ListMarkerStyle.LowerRoman;
                  break;
                case "upper-roman":
                  list.MarkerStyle = ListMarkerStyle.UpperRoman;
                  break;
                case "decimal":
                  list.MarkerStyle = ListMarkerStyle.Decimal;
                  break;
                default:
                  list.MarkerStyle = ListMarkerStyle.Disc;
                  break;
              }

            }
            break;

          case "float":
          case "clear":
            if (isBlock)
            {
              //  Convert float and clear properties
            }
            break;

          case "display":
            break;
        }
      }

      if (xamlElement is Block b)
      {
        if (marginSet)
        {
          b.Margin = PropertyExtensions.GetThickness(left: marginLeft, right: marginRight, top: marginTop, bottom: marginBottom);
        }

        if (paddingSet)
        {
          b.Padding = PropertyExtensions.GetThickness(left: paddingLeft, right: paddingRight, top: paddingTop, bottom: paddingBottom);
        }

        if (borderColor != null)
        {
          //  We currently ignore possible difference in brush colors on different border sides. Use the last colored side mentioned
          b.BorderBrush = borderColor;
        }

        if (borderThicknessSet)
        {
          b.BorderThickness = PropertyExtensions.GetThickness(left: borderThicknessLeft, right: borderThicknessRight, top: borderThicknessTop, bottom: borderThicknessBottom);
        }
      }
    }

    static ExCSS.Length? ResolveRemEmExCh(ExCSS.Length? length, ref double? currentFontSizePx, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext, int? currentLevel = null)
    {
      if (length.HasValue)
      {
        var l = length.Value;
        switch (l.Type)
        {
          case ExCSS.Length.Unit.Em:
            currentFontSizePx = currentFontSizePx ?? GetAbsoluteFontSizeForContext("font-size", sourceContext, currentLevel);
            return new ExCSS.Length((float)(l.Value * currentFontSizePx.Value), ExCSS.Length.Unit.Px);
          case ExCSS.Length.Unit.Rem:
            return new ExCSS.Length(l.Value * FontSizeRootPx, ExCSS.Length.Unit.Px);
          case ExCSS.Length.Unit.Ex:
            // we approximate the small X by using the half of the font size
            currentFontSizePx = currentFontSizePx ?? GetAbsoluteFontSizeForContext("font-size", sourceContext, currentLevel);
            return new ExCSS.Length((float)(0.5 * currentFontSizePx.Value), ExCSS.Length.Unit.Px);
          case ExCSS.Length.Unit.Ch:
            // we approximate the width of the 0 by using half of the font size
            currentFontSizePx = currentFontSizePx ?? GetAbsoluteFontSizeForContext("font-size", sourceContext, currentLevel);
            return new ExCSS.Length((float)(0.5 * currentFontSizePx.Value), ExCSS.Length.Unit.Px);
          default:
            return l;

        }
      }
      else
      {
        return length;
      }
    }

    static ExCSS.Color? GetColor(object value, ExCSS.Color normalColor)
    {
      if (value is ExCSS.Color color)
        return color;
      else if (value is string s)
      {
        switch (s)
        {
          case ExCSS.Keywords.Normal:
            return normalColor;
          case ExCSS.Keywords.Inherit:
            return null;
          default:
            return null;
        }
      }
      else
      {
        return null;
      }
    }

    static ExCSS.Length GetMargin(object value, ref double? currentFontSize, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      if (value is ExCSS.Length l)
      {
        return ResolveRemEmExCh(l, ref currentFontSize, sourceContext).Value;
      }
      else if (value is string s)
      {
        switch (s)
        {
          case ExCSS.Keywords.Auto:
            return new ExCSS.Length(25, ExCSS.Length.Unit.Percent);
          default:
            return ZeroPixel;
        }
      }
      else
        return ZeroPixel;
    }

    static CompoundLength GetCompoundWidthOrHeightForContext(string propertyName, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      if (propertyName != "width" && propertyName != "height")
        throw new ArgumentException("must either be 'width' or 'height'", nameof(propertyName));

      CompoundLength result = null;

      for (int i = sourceContext.Count - 1; i >= 0; --i)
      {
        var elementProperties = sourceContext[i].elementProperties;
        CompoundLength currentCompoundLength = null;
        bool isRelevant = false;
        if (propertyName == "width")
        {
          isRelevant =
            elementProperties.ContainsKey(propertyName) ||
            elementProperties.ContainsKey("margin-left") ||
            elementProperties.ContainsKey("margin-right") ||
            elementProperties.ContainsKey("border-left-width") ||
            elementProperties.ContainsKey("border-right-width") ||
            elementProperties.ContainsKey("padding-left") ||
            elementProperties.ContainsKey("padding-right");
        }
        else if (propertyName == "height")
        {
          isRelevant =
            elementProperties.ContainsKey(propertyName) ||
            elementProperties.ContainsKey("margin-top") ||
            elementProperties.ContainsKey("margin-bottom") ||
            elementProperties.ContainsKey("border-top-width") ||
            elementProperties.ContainsKey("border-bottom-width") ||
            elementProperties.ContainsKey("padding-top") ||
            elementProperties.ContainsKey("padding-bottom");
        }

        if (!isRelevant)
          continue;

        elementProperties.TryGetValue(propertyName, out var w);

        if (w is ExCSS.Length length)
        {
          double? currentFontSize = null;
          length = ResolveRemEmExCh(length, ref currentFontSize, sourceContext, i).Value;
          currentCompoundLength = new CompoundLength { [length.Type] = length };
        }
        else if (w is string s)
        {
          switch (s)
          {
            case "auto":
              currentCompoundLength = CreateCompoundLengthFromAutoSize(propertyName, sourceContext[i].elementProperties, sourceContext, i);
              break;
            default:
              throw new NotImplementedException($"The string <<{s}>> is not recognized as a valid length");
          }
        }
        else if (w is null)
        {
          // consider a value of null as autosize
          currentCompoundLength = CreateCompoundLengthFromAutoSize(propertyName, sourceContext[i].elementProperties, sourceContext, i);
        }
        else
        {
          throw new NotImplementedException($"The type <<{w?.GetType()}>> is not recognized as a valid length");
        }


        if (null == result)
        {
          result = currentCompoundLength;
        }
        else
        {
          result = CompoundLength.Multiply(result, currentCompoundLength);
        }

        if (result.IsDetermined())
          return result;
      }

      return new CompoundLength { [ExCSS.Length.Unit.Percent] = new ExCSS.Length(100, ExCSS.Length.Unit.Percent) };
    }


    /// <summary>
    /// Creates a compound length if the size (either 'width' or 'height') of the element is set to 'auto'. In this case, margin, border, and padding
    /// of the element have to be taken into account.
    /// </summary>
    /// <param name="propertyName">Name of the property (has to be either 'width' or 'height').</param>
    /// <param name="elementProperties">The properties of the element.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">GetAutoCompoundLength is suited only for width and height</exception>
    public static CompoundLength CreateCompoundLengthFromAutoSize(string propertyName, Dictionary<string, object> elementProperties, List<(System.Xml.XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext, int? currentLevel = null)
    {
      var result = new CompoundLength
      {
        [ExCSS.Length.Unit.Percent] = new ExCSS.Length(100, ExCSS.Length.Unit.Percent)
      };

      double? currentFontSizePx = null;
      switch (propertyName)
      {
        case "width":
          {
            if (elementProperties.TryGetValue("margin-left", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("margin-right", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("border-left-width", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("border-right-width", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("padding-left", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("padding-right", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          break;
        case "height":
          {
            if (elementProperties.TryGetValue("margin-top", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("margin-bottom", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("border-top-width", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("border-bottom-width", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("padding-top", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          {
            if (elementProperties.TryGetValue("padding-bottom", out var v) && v is ExCSS.Length len)
            {
              len = ResolveRemEmExCh(len, ref currentFontSizePx, sourceContext, currentLevel).Value;
              result.Add(len, -1);
            }
          }
          break;
        default:
          throw new NotImplementedException("GetAutoCompoundLength is suited only for width and height");
      }

      return result;
    }

    static ExCSS.Length? GetMaxWidthOrMaxHeightForContext(string propertyName, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      if (propertyName != "max-width" && propertyName != "max-height")
        throw new ArgumentException("must either be 'max-width' or 'max-height'", nameof(propertyName));

      ExCSS.Length? result = null;

      for (int i = sourceContext.Count - 1; i >= 0; --i)
      {
        var elementProperties = sourceContext[i].elementProperties;
        bool isRelevant = elementProperties.ContainsKey(propertyName);

        if (!isRelevant)
          continue;

        elementProperties.TryGetValue(propertyName, out var w);

        if (w is ExCSS.Length length)
        {
          double? currentFontSize = null;
          length = ResolveRemEmExCh(length, ref currentFontSize, sourceContext, i).Value;
          if (null == result)
            result = length;
          else if (result.Value.Type == ExCSS.Length.Unit.Percent)
            result = new ExCSS.Length(result.Value.Value * length.Value / 100, length.Type);
          else
            throw new InvalidProgramException();
        }
        else if (w is string s)
        {
          switch (s)
          {
            case "none":
              return null;
            case "initial":
              i = Math.Min(1, i);
              break;
            case "inherit":
              break;
            default:
              throw new NotImplementedException($"The string <<{s}>> is not recognized as a valid length");
          }
        }
        else if (w is null)
        {
          // consider a value of null as 100%, and fall through

        }
        else
        {
          throw new NotImplementedException($"The type <<{w?.GetType()}>> is not recognized as a valid length");
        }

        if (result != null && result.Value.Type != ExCSS.Length.Unit.Percent)
          break;
      }

      return result;
    }




    static ExCSS.Length? GetFontSize(object value)
    {
      if (value is ExCSS.Length l)
        return l;
      else if (value is ExCSS.Percent p)
        return new ExCSS.Length(p.Value, ExCSS.Length.Unit.Percent);
      else if (value is string s)
      {
        if (ExCSS.Map.FontSizes.TryGetValue(s, out var fs))
          return ExCSS.ValueExtensions.ToLength(fs);
        else
        {
          switch (s)
          {
            case ExCSS.Keywords.Inherit:
              return null;

            default:
              throw new NotImplementedException($"FontSize string value {s} is not implemented");
          }
        }
      }
      else
      {
        throw new NotImplementedException($"FontSize of type {value?.GetType()} is not implemented");
      }

    }

    static double GetAbsoluteFontSizeForContext(string property, List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext, int? levelToBeginWith)
    {
      ExCSS.Length? result = null;
      levelToBeginWith = levelToBeginWith ?? sourceContext.Count - 1;
      for (int i = levelToBeginWith.Value; i >= 0; --i)
      {
        if (sourceContext[i].elementProperties.TryGetValue(property, out var o) && GetFontSize(o) is ExCSS.Length length)
        {
          if (result is null)
          {
            result = length;
          }
          else
          {
            // result is something relative
            result = Multiply(result.Value, length);
          }

          if (result.Value.IsAbsolute)
          {
            break;
          }
          else if (result.Value.Type == ExCSS.Length.Unit.Rem)
          {
            var rootSizePx = ((ExCSS.Length)sourceContext[0].elementProperties[property]).ToPixel();
            return rootSizePx * result.Value.Value;
          }
        }
      }

      if (result is null)
        throw new Exception("Can not get absolute font size");

      var inPx = result.Value.ToPixel();

      if (!(inPx >= 0))
        inPx = 0;

      return inPx;
    }

    static ExCSS.Length Multiply(ExCSS.Length l1, ExCSS.Length l2)
    {
      if (l1.IsAbsolute && l2.IsAbsolute)
        throw new ArgumentException("Both l1 and l2 are absolute lengths");

      if (l1.Type == ExCSS.Length.Unit.Percent)
        return new ExCSS.Length((l1.Value / 100) * l2.Value, l2.Type);

      if (l2.Type == ExCSS.Length.Unit.Percent)
        return new ExCSS.Length((l2.Value / 100) * l1.Value, l1.Type);

      if (l1.Type == ExCSS.Length.Unit.Em)
        return new ExCSS.Length(l1.Value * l2.Value, l2.Type);

      if (l2.Type == ExCSS.Length.Unit.Em)
        return new ExCSS.Length(l2.Value * l1.Value, l1.Type);



      throw new ArgumentException($"Can not multiply length of type {l1.Type} and {l2.Type}");
    }

    static ExCSS.Length? Add(ExCSS.Length l1, ExCSS.Length? l2n)
    {
      if (l2n is null)
        return l1;

      var l2 = l2n.Value;

      if (l1.Type == l2.Type)
        return new ExCSS.Length(l1.Value + l2.Value, l1.Type);

      if (l1.IsAbsolute && l2.IsAbsolute)
        return new ExCSS.Length(l1.ToPixel() + l2.ToPixel(), ExCSS.Length.Unit.Px);

      throw new ArgumentException($"Can not add length of type {l1.Type} and {l2.Type}");
    }




    /// <summary>
    ///     Analyzes the tag of the htmlElement and infers its associated formatted properties.
    ///     After that parses style attribute and adds all inline css styles.
    ///     The resulting style attributes are collected in output parameter localProperties.
    /// </summary>
    /// <param name="htmlElement">
    /// </param>
    /// <param name="inheritedProperties">
    ///     set of properties inherited from ancestor elements. Currently not used in the code. Reserved for the future
    ///     development.
    /// </param>
    /// <param name="localProperties">
    ///     returns all formatting properties defined by this element - implied by its tag, its attributes, or its css inline
    ///     style
    /// </param>
    /// <param name="stylesheet"></param>
    /// <param name="sourceContext"></param>
    /// <returns>
    ///     returns a combination of previous context with local set of properties.
    ///     This value is not used in the current code - inntended for the future development.
    /// </returns>
    private static void GetElementProperties(
      XmlElement htmlElement,
      Dictionary<string, object> localProperties,
      CssStylesheets stylesheet,
      List<(XmlElement xmlElement, Dictionary<string, object> elementProperties)> sourceContext)
    {
      if (!object.ReferenceEquals(htmlElement, sourceContext[sourceContext.Count - 1].xmlElement))
      {
        throw new ArgumentException("Top element of sourceContext does not contain htmlElement");
      }
      if (!object.ReferenceEquals(localProperties, sourceContext[sourceContext.Count - 1].elementProperties))
      {
        throw new ArgumentException("Top element of sourceContext does not contain elementProperties");
      }


      // Identify element name
      string elementName = htmlElement.LocalName.ToLower();
      string elementNamespace = htmlElement.NamespaceURI;

      // update current formatting properties depending on element tag
      switch (elementName)
      {
        // Character formatting
        case "i":
        case "italic":
        case "em":
          localProperties["font-style"] = ExCSS.Keywords.Italic;
          break;
        case "b":
        case "bold":
        case "strong":
        case "dfn":
          localProperties["font-weight"] = ExCSS.Keywords.Bold;
          break;
        case "u":
        case "underline":
          localProperties["text-decoration-underline"] = "true";
          break;
        case "font":
          {
            if (htmlElement.GetAttribute("face") is string faceString && !string.IsNullOrEmpty(faceString))
            {
              localProperties["font-family"] = faceString;
            }
            if (htmlElement.GetAttribute("size") is string sizeString && !string.IsNullOrEmpty(sizeString))
            {
              if (ExCSS.Length.TryParse(sizeString, out var fontSize))
                localProperties["font-size"] = fontSize;
              else if (float.TryParse(sizeString, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSizePx))
                localProperties["font-size"] = new ExCSS.Length(fontSizePx, ExCSS.Length.Unit.Px);
              else
                throw new NotImplementedException();
            }
            if (htmlElement.GetAttribute("color") is string colorString && !string.IsNullOrEmpty(colorString))
            {
              if (PropertyConverter.TryParseColor(colorString, out var color))
                localProperties["color"] = color;
              else
                throw new NotImplementedException();
            }

          }
          break;
        case "samp":
          localProperties["font-family"] = "Courier New"; // code sample
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Tiny);
          localProperties["text-align"] = ExCSS.Keywords.Left;
          break;
        case "sub":
          break;
        case "sup":
          break;

        // Hyperlinks
        case "a": // href, hreflang, urn, methods, rel, rev, title
                  //  Set default hyperlink properties
          break;
        case "acronym":
          break;

        // Paragraph formatting:
        case "p":
          //  Set default paragraph properties
          break;
        case "div":
          //  Set default div properties
          break;
        case "pre":
          localProperties["font-family"] = "Courier New"; // renders text in a fixed-width font
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Tiny);
          localProperties["text-align"] = ExCSS.Keywords.Left;
          break;
        case "blockquote":
          localProperties["margin-left"] = ExCSS.Keywords.Medium;
          break;

        case "h1":
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Huge);
          break;
        case "h2":
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Big);
          break;
        case "h3":
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Large);
          break;
        case "h4":
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Medium);
          break;
        case "h5":
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Small);
          break;
        case "h6":
          localProperties["font-size"] = ExCSS.ValueExtensions.ToLength(ExCSS.FontSize.Little);
          break;
        // List properties
        case "ul":
          localProperties["list-style-type"] = ExCSS.Keywords.Disc;
          break;
        case "ol":
          localProperties["list-style-type"] = ExCSS.Keywords.Decimal;
          break;


        case "table":
        case "body":
        case "html":
          break;
      }

      // Override html defaults by css attributes - from stylesheets and inline settings
      stylesheet.GetElementProperties(htmlElement, sourceContext, localProperties);
    }


    /// <summary>
    ///     Extracts a value of css attribute from css style definition.
    /// </summary>
    /// <param name="cssStyle">
    ///     Source csll style definition
    /// </param>
    /// <param name="attributeName">
    ///     A name of css attribute to extract
    /// </param>
    /// <returns>
    ///     A string rrepresentation of an attribute value if found;
    ///     null if there is no such attribute in a given string.
    /// </returns>
    private static string GetCssAttribute(string cssStyle, string attributeName)
    {
      //  This is poor man's attribute parsing. Replace it by real css parsing
      if (cssStyle != null)
      {
        string[] styleValues;

        attributeName = attributeName.ToLower();

        // Check for width specification in style string
        styleValues = cssStyle.Split(';');

        foreach (string t in styleValues)
        {
          string[] styleNameValue;

          styleNameValue = t.Split(':');
          if (styleNameValue.Length == 2)
          {
            if (styleNameValue[0].Trim().ToLower() == attributeName)
            {
              return styleNameValue[1].Trim();
            }
          }
        }
      }

      return null;
    }

    /// <summary>
    ///     Converts a length value from string representation to a double.
    /// </summary>
    /// <param name="lengthAsString">
    ///     Source string value of a length.
    /// </param>
    /// <param name="length"></param>
    /// <returns></returns>
    private static bool TryGetLengthValue(string lengthAsString, out double length)
    {
      length = double.NaN;

      if (lengthAsString != null)
      {
        lengthAsString = lengthAsString.Trim().ToLower();

        // We try to convert currentColumnWidthAsString into a double. This will eliminate widths of type "50%", etc.
        if (lengthAsString.EndsWith("pt"))
        {
          lengthAsString = lengthAsString.Substring(0, lengthAsString.Length - 2);
          if (double.TryParse(lengthAsString, out length))
          {
            length = (length * 96.0) / 72.0; // convert from points to pixels
          }
          else
          {
            length = double.NaN;
          }
        }
        else if (lengthAsString.EndsWith("px"))
        {
          lengthAsString = lengthAsString.Substring(0, lengthAsString.Length - 2);
          if (!double.TryParse(lengthAsString, out length))
          {
            length = double.NaN;
          }
        }
        else
        {
          if (!double.TryParse(lengthAsString, out length)) // Assuming pixels
          {
            length = double.NaN;
          }
        }
      }

      return !double.IsNaN(length);
    }

    // .................................................................
    //
    // Pasring Color Attribute
    //
    // .................................................................

    private static string GetColorValue(string colorValue)
    {
      return colorValue;
    }

    /// <summary>
    ///     Applies properties to xamlTableCellElement based on the html td element it is converted from.
    /// </summary>
    /// <param name="htmlChildNode">
    ///     Html td/th element to be converted to xaml
    /// </param>
    /// <param name="xamlTableCellElement">
    ///     XmlElement representing Xaml element for which properties are to be processed
    /// </param>
    /// <remarks>
    ///     TODO: Use the processed properties for htmlChildNode instead of using the node itself
    /// </remarks>
    private static void ApplyPropertiesToTableCellElement(XmlElement htmlChildNode, TableCell xamlTableCellElement)
    {
      // Parameter validation
      DebugAssert(htmlChildNode.LocalName.ToLower() == "td" || htmlChildNode.LocalName.ToLower() == "th");


      // set default border thickness for xamlTableCellElement to enable gridlines
      xamlTableCellElement.BorderThickness = new Thickness { Left = OnePixel, Right = OnePixel, Top = OnePixel, Bottom = OnePixel };
      xamlTableCellElement.BorderBrush = ExCSS.Color.Black;

      string rowSpanString = GetAttribute(htmlChildNode, "rowspan");
      if (rowSpanString != null)
      {
        var val = PropertyExtensions.GetInteger(rowSpanString);
        if (val.HasValue)
          xamlTableCellElement.RowSpan = val.Value;
      }
    }

    static readonly ExCSS.Length ZeroPixel = new ExCSS.Length(0, ExCSS.Length.Unit.Px);
    static readonly ExCSS.Length OnePixel = new ExCSS.Length(1, ExCSS.Length.Unit.Px);

    #endregion Private Methods

    static void DebugAssert(bool condition)
    {
      if (!condition)
      {
        throw new InvalidOperationException();
      }
    }

    static (double? Value, string Unit) GetLocalSizeFromInheritedBlockSize((double? Value, string Unit) inherited, (double? Value, string Unit) local)
    {
      if (local.Unit == "%")
      {
        return (inherited.Value * local.Value / 100, inherited.Unit);
      }
      else
      {
        return local;
      }
    }
  }
}
