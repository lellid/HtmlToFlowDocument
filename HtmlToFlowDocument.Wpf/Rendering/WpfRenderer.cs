// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using HtmlToFlowDocument.Dom;
using swd = System.Windows.Documents;

namespace HtmlToFlowDocument.Rendering
{
  public class WpfRenderer
  {
    /// <summary>
    /// Gets or sets a value indicating whether the renderer shold invert the colors of the text and the background.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the renderer should invert colors; otherwise, <c>false</c>.
    /// </value>
    public bool InvertColors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether text in a run should be split into separate Runs, each containing one word.
    /// </summary>
    /// <value>
    ///   <c>true</c> if text in a run should be split into separate Runs, each containing one word; otherwise, <c>false</c>.
    /// </value>
    public bool SplitIntoWords { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether text in a run should be split into separate Runs, each containing one sentence.
    /// </summary>
    /// <value>
    ///   <c>true</c> if text in a run should be split into separate Runs, each containing one sentence; otherwise, <c>false</c>.
    /// </value>
    public bool SplitIntoSentences { get; set; } = true;

    /// <summary>
    /// Gets or sets the font dictionary.
    /// </summary>
    /// <value>
    /// The font dictionary. Key is the font name, value is the absolute path of the font file.
    /// </value>
    public Dictionary<string, string> FontDictionary { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a value indicating whether during the conversion process the DOM elements are attached as tags to the UI elements.
    /// This will of course increase the memory footprint, because the DOM elements then could not be reclaimed by the garbage collector.
    /// </summary>
    /// <value>
    ///   <c>true</c> if DOM elements should be attached as tags to the UI elements; otherwise, <c>false</c>.
    /// </value>
    public bool AttachDomAsTags { get; set; }

    /// <summary>
    /// Gets or sets the name of flow document.
    /// </summary>
    /// <value>
    /// The name of flow document.
    /// </value>
    public string NameOfFlowDocument { get; set; } = "_guiFlowDocument";


    public Binding TemplateBindingViewportWidth { get; set; }

    public Binding TemplateBindingViewportHeight { get; set; }

    /// <summary>
    /// Renders the specified DOM flow document into a Wpf flow document.
    /// </summary>
    /// <param name="flowDocument">The DOM flow document to render.</param>
    /// <returns></returns>
    public swd.FlowDocument Render(FlowDocument flowDocument)
    {
      LoadFonts();

      return (swd.FlowDocument)RenderRecursively(flowDocument);
    }

    /// <summary>
    /// Renders the specified DOM section into a Wpf section
    /// </summary>
    /// <param name="section">The DOM section to render.</param>
    /// <returns></returns>
    public swd.Section Render(Section section)
    {
      return (swd.Section)RenderRecursively(section);
    }

    /// <summary>
    /// Renders DOM text elements recursively, i.e. including their childs.
    /// </summary>
    /// <param name="e">The root DOM text element.</param>
    /// <returns>The corresponding Wpf element.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// </exception>
    /// <exception cref="System.NotImplementedException"></exception>
    public object RenderRecursively(TextElement e)
    {
      object wpf = null;

      switch (e)
      {
        case BlockUIContainer buc:
          {
            wpf = new swd.BlockUIContainer();
          }
          break;
        case FlowDocument flowDocument:
          {
            // make sure the standard colors were set
            flowDocument.Foreground = ExCSS.Color.Black;
            flowDocument.Background = ExCSS.Color.White;

            var flowDocumente = new swd.FlowDocument() { Name = NameOfFlowDocument };
            if (TemplateBindingViewportWidth is null)
              TemplateBindingViewportWidth = new Binding("ColumnWidth") { Source = flowDocumente };
            if (TemplateBindingViewportHeight is null)
              TemplateBindingViewportHeight = new Binding("ColumnWidth") { Source = flowDocumente }; // Binding to ColumnWidth is not optimal, but better than nothing!

            if (flowDocument.Background.HasValue)
              flowDocumente.Background = GetBrushFromColor(flowDocument.Background.Value);
            if (flowDocument.Foreground.HasValue)
              flowDocumente.Foreground = GetBrushFromColor(flowDocument.Foreground.Value);

            wpf = flowDocumente;
          }
          break;
        case Hyperlink hl:
          {
            var hle = new swd.Hyperlink();
            if (!string.IsNullOrEmpty(hl.NavigateUri))
            {
              if (System.Uri.TryCreate(hl.NavigateUri, UriKind.RelativeOrAbsolute, out var uri))
                hle.NavigateUri = uri;
            }
            if (!string.IsNullOrEmpty(hl.TargetName))
            {
              hle.TargetName = hl.TargetName;
            }
            wpf = hle;
          }
          break;
        case Image image:
          {
            var imagee = new System.Windows.Controls.Image();
            if (!string.IsNullOrEmpty(image.Source))
            {
              imagee.SetBinding(System.Windows.Controls.Image.SourceProperty, $"ImageProvider[{image.Source}]");
            }

            if (image.Width == null && image.Height == null)
            {
              imagee.Stretch = System.Windows.Media.Stretch.Uniform;

              var binding = new Binding() { RelativeSource = RelativeSource.Self, Path = new System.Windows.PropertyPath("Source") };
              binding.Converter = ImageToImageWidthConverter.Instance;
              imagee.SetBinding(System.Windows.Controls.Image.WidthProperty, binding);
            }
            else
            {
              imagee.Stretch = System.Windows.Media.Stretch.Uniform;
            }

            if (image.Width != null)
            {
              if (image.Width.IsPurelyAbsolute(out var widthPx))
              {
                imagee.Width = widthPx;
              }
              else
              {
                var multibinding = new MultiBinding();
                multibinding.Bindings.Add(new Binding() { Source = TemplateBindingViewportWidth.Source, Path = TemplateBindingViewportWidth.Path });
                multibinding.Bindings.Add(new Binding() { Source = TemplateBindingViewportHeight.Source, Path = TemplateBindingViewportHeight.Path });
                multibinding.Converter = CompoundLengthConverter.Instance;
                multibinding.ConverterParameter = GetCompoundLengthConverterParameters(image.Width);
                imagee.SetBinding(System.Windows.Controls.Image.WidthProperty, multibinding);
              }
            }

            if (image.Height != null)
            {
              if (image.Height.IsPurelyAbsolute(out var heightPx))
              {
                imagee.Height = heightPx;
              }
              else
              {
                var multibinding = new MultiBinding();
                multibinding.Bindings.Add(new Binding() { Source = TemplateBindingViewportWidth.Source, Path = TemplateBindingViewportWidth.Path });
                multibinding.Bindings.Add(new Binding() { Source = TemplateBindingViewportHeight.Source, Path = TemplateBindingViewportHeight.Path });
                multibinding.Converter = CompoundLengthConverter.Instance;
                multibinding.ConverterParameter = GetCompoundLengthConverterParameters(image.Height);
                imagee.SetBinding(System.Windows.Controls.Image.HeightProperty, multibinding);
              }

            }

            // set max-width and max-height
            if (image.MaxWidth != null && image.MaxWidth.Value.IsAbsolute)
            {
              imagee.MaxWidth = image.MaxWidth.Value.ToPixel();
            }
            else if (image.MaxWidth == null || image.MaxWidth.Value.Type == ExCSS.Length.Unit.Vw)
            {
              double vwValue = image.MaxWidth.HasValue ? image.MaxWidth.Value.Value : 100;

              var binding = new Binding() { Source = TemplateBindingViewportWidth.Source, Path = TemplateBindingViewportWidth.Path };
              binding.Converter = RelativeSizeConverter.Instance;
              binding.ConverterParameter = vwValue;
              imagee.SetBinding(System.Windows.Controls.Image.MaxWidthProperty, binding);
            }
            else
            {
              throw new InvalidProgramException();
            }


            if (image.MaxHeight != null && image.MaxHeight.Value.IsAbsolute)
            {
              imagee.MaxHeight = image.MaxHeight.Value.ToPixel();
            }
            else if (image.MaxHeight == null || image.MaxHeight.Value.Type == ExCSS.Length.Unit.Vh)
            {
              double vhValue = image.MaxHeight.HasValue ? image.MaxHeight.Value.Value : 100;
              var binding = new Binding() { Source = TemplateBindingViewportWidth.Source, Path = TemplateBindingViewportWidth.Path };
              binding.Converter = RelativeSizeConverter.Instance;
              binding.ConverterParameter = vhValue;
              imagee.SetBinding(System.Windows.Controls.Image.MaxHeightProperty, binding);
            }
            else
            {
              throw new InvalidProgramException();
            }




            wpf = imagee;
          }
          break;
        case InlineUIContainer iuc:
          {
            wpf = new swd.InlineUIContainer();
          }
          break;
        case LineBreak lb:
          {
            wpf = new swd.LineBreak();
          }
          break;
        case List list:
          {
            var liste = new swd.List();
            if (list.MarkerStyle.HasValue)
            {
              liste.MarkerStyle = ToMarkerStyle(list.MarkerStyle.Value);
            }
            wpf = liste;
          }
          break;
        case ListItem li:
          {
            wpf = new swd.ListItem();
          }
          break;
        case Paragraph p:
          {
            var pe = new swd.Paragraph();
            if (p.TextDecorations.HasValue)
            {
              pe.TextDecorations = ToTextDecorations(p.TextDecorations.Value);
            }
            if (p.TextIndent.HasValue)
            {
              pe.TextIndent = p.TextIndent.Value.IsAbsolute ? p.TextIndent.Value.ToPixel() : 0;
            }
            wpf = pe;
          }
          break;
        case Run run:
          {
            if (SplitIntoWords)
              wpf = CreateTextElement_SeparateWords(run.Text);
            else if (SplitIntoSentences)
              wpf = CreateTextElement_SeparateSentences(run.Text);
            else
              wpf = new swd.Run(run.Text);
          }
          break;
        case Section s:
          {
            wpf = new swd.Section();
          }
          break;
        case Span span:
          {
            wpf = new swd.Span();
          }
          break;
        case Table tb:
          {
            var tbe = new swd.Table();
            foreach (var c in tb.Columns)
            {
              if (c.Width.HasValue)
                tbe.Columns.Add(new swd.TableColumn() { Width = new System.Windows.GridLength(c.Width.Value) });
              else
                tbe.Columns.Add(new swd.TableColumn());
            }
            wpf = tbe;
          }
          break;
        case TableCell tc:
          {
            var tce = new swd.TableCell();
            if (1 != tc.ColumnSpan)
            {
              tce.ColumnSpan = tc.ColumnSpan;
            }

            if (1 != tc.RowSpan)
            {
              tce.RowSpan = tc.RowSpan;
            }
            if (tc.BorderBrush.HasValue)
            {
              tce.BorderBrush = new System.Windows.Media.SolidColorBrush(ToColor(tc.BorderBrush.Value));
            }
            if (tc.BorderThickness.HasValue)
            {
              tce.BorderThickness = ToThickness(tc.BorderThickness.Value);
            }
            wpf = tce;
          }
          break;
        case TableRow trow:
          {
            wpf = new swd.TableRow();
          }
          break;
        case TableRowGroup trg:
          {
            wpf = new swd.TableRowGroup();
          }
          break;
        default:
          {
            wpf = null;
          }
          break;
      }

      // Render TextElement properties

      if (wpf is swd.TextElement te)
      {

        if (!string.IsNullOrEmpty(e.FontFamily))
        {
          te.FontFamily = GetFontFamily(e.FontFamily);
        }

        if (e.FontSize.HasValue)
        {
          var fs = e.FontSize.Value;
          fs = Math.Max(0.003, fs);
          te.FontSize = fs;
        }

        if (e.FontStyle.HasValue)
        {
          te.FontStyle = ToFontStyle(e.FontStyle.Value);
        }

        if (e.FontWeight.HasValue)
        {
          te.FontWeight = ToFontWeight(e.FontWeight.Value);
        }

        if (e.Foreground.HasValue && e.Foreground != e.ForegroundInheritedOnly)
        {
          te.Foreground = GetBrushFromColor(e.Foreground.Value);
        }

        if (e.Background.HasValue && e.Background != e.BackgroundInheritedOnly)
        {
          te.Background = GetBrushFromColor(e.Background.Value);
        }
      }

      // now special properties

      if (e is Block b && wpf is swd.Block be)
      {
        if (b.Margin.HasValue)
        {
          be.Margin = ToThickness(b.Margin.Value);
        }

        if (b.Padding.HasValue)
        {
          be.Padding = ToThickness(b.Padding.Value);
        }

        if (b.BorderBrush.HasValue)
        {
          be.BorderBrush = new System.Windows.Media.SolidColorBrush(ToColor(b.BorderBrush.Value));
        }

        if (b.BorderThickness.HasValue)
        {
          be.BorderThickness = ToThickness(b.BorderThickness.Value);
        }

        if (b.TextAlignment.HasValue)
        {
          be.TextAlignment = ToTextAlignment(b.TextAlignment.Value);
        }

        if (b.LineHeight.HasValue)
        {
          be.LineHeight = b.LineHeight.Value;
        }
      }
      //  finished rendering the attributes


      // now, render all children
      foreach (var child in e.Childs)
      {
        var childe = RenderRecursively(child);

        switch (wpf)
        {
          case swd.Figure figure:
            figure.Blocks.Add((swd.Block)childe);
            break;
          case swd.Floater floater:
            floater.Blocks.Add((swd.Block)childe);
            break;
          case swd.FlowDocument flowDocument:
            flowDocument.Blocks.Add((swd.Block)childe);
            break;
          case swd.List list:
            list.ListItems.Add((swd.ListItem)childe);
            break;
          case swd.ListItem listItem:
            listItem.Blocks.Add((swd.Block)childe);
            break;
          case swd.Section section:
            section.Blocks.Add((swd.Block)childe);
            break;
          case swd.Table table:
            table.RowGroups.Add((swd.TableRowGroup)childe);
            break;
          case swd.TableCell tableCell:
            tableCell.Blocks.Add((swd.Block)childe);
            break;
          case swd.TableRow tableRow:
            tableRow.Cells.Add((swd.TableCell)childe);
            break;
          case swd.TableRowGroup tableRowGroup:
            tableRowGroup.Rows.Add((swd.TableRow)childe);
            break;

          // now elements that can contain inlines
          case swd.Paragraph paragraph:
            paragraph.Inlines.Add((swd.Inline)childe);
            break;
          case swd.Span span:
            span.Inlines.Add((swd.Inline)childe);
            break;

          // now some specialties
          case swd.InlineUIContainer inlineUIContainer:
            if (inlineUIContainer.Child != null)
              throw new InvalidOperationException($"{nameof(swd.InlineUIContainer)} can not contain more than one child");
            inlineUIContainer.Child = (System.Windows.UIElement)childe;
            break;
          case swd.BlockUIContainer blockUIContainer:
            if (blockUIContainer.Child != null)
              throw new InvalidOperationException($"{nameof(swd.BlockUIContainer)} can not contain more than one child");
            blockUIContainer.Child = (System.Windows.UIElement)childe;
            break;

          default:
            throw new NotImplementedException();
        }
      }

      if (AttachDomAsTags)
      {
        if (wpf is System.Windows.FrameworkContentElement conEle)
          conEle.Tag = e;
        else if (wpf is System.Windows.FrameworkElement uiEle)
          uiEle.Tag = e;
      }

      return wpf;
    }

    private static object GetCompoundLengthConverterParameters(CompoundLength compoundLength)
    {
      var converterParameters = new List<(int, double)>();
      foreach (var entry in compoundLength)
      {
        switch (entry.Key)
        {
          case ExCSS.Length.Unit.Px:
            converterParameters.Add((0, entry.Value.Value));
            break;
          case ExCSS.Length.Unit.Vw:
            converterParameters.Add((1, entry.Value.Value));
            break;
          case ExCSS.Length.Unit.Vh:
            converterParameters.Add((2, entry.Value.Value));
            break;
          case ExCSS.Length.Unit.Vmin:
            converterParameters.Add((3, entry.Value.Value));
            break;
          case ExCSS.Length.Unit.Vmax:
            converterParameters.Add((4, entry.Value.Value));
            break;
          default:
            throw new NotImplementedException();
        }
      }
      return converterParameters;
    }

    private object CreateTextElement_SeparateSentences(string text)
    {
      if (SplitIntoSentences)
      {
        int prevIdx = 0;
        List<int> list = null;

        int numberOfWords = 0;
        bool inWord = false;

        for (int i = 0; i < text.Length; ++i)
        {
          char c = text[i];
          if (char.IsWhiteSpace(text[i]))
          {
            inWord = false;
          }
          else
          {
            if (!inWord)
            {
              ++numberOfWords;
            }
            inWord = true;
          }

          if (c == '.' || c == '!' || c == '?')
          {
            if (numberOfWords > 2 || (i - prevIdx) >= 4)
            {
              if (null == list)
              {
                list = new List<int>(text.Length / 5);
                list.Add(0);
              }
              list.Add(i + 1);
              prevIdx = i + 1;
            }
          }
        }
        if (null != list)
        {
          list.Add(text.Length);

          var span = new swd.Span();
          for (int i = 1; i < list.Count; ++i)
          {
            span.Inlines.Add(new swd.Run(text.Substring(list[i - 1], list[i] - list[i - 1])));
          }
          return span;
        }
        else
        {
          return new swd.Run(text);
        }
      }
      else
      {
        return new swd.Run(text);
      }
    }

    private object CreateTextElement_SeparateWords(string text)
    {
      var span = new swd.Span();
      int prevIdx = 0;
      int numberOfWords = 0;
      bool inWord = false;
      swd.Run previousRun = null;

      int i;
      for (i = 0; i < text.Length; ++i)
      {
        if (char.IsWhiteSpace(text[i]))
        {
          inWord = false;
        }
        else
        {
          if (!inWord)
          {
            ++numberOfWords;
            if (null != previousRun)
            {
              span.Inlines.Add(previousRun);
              previousRun = null;
            }
            if (i > prevIdx)
            {
              previousRun = new swd.Run(text.Substring(prevIdx, i - prevIdx));
            }

            prevIdx = i;
          }
          inWord = true;
        }
      }
      if (null != previousRun)
      {
        span.Inlines.Add(previousRun);
        previousRun = null;
      }
      if (i > prevIdx)
      {
        previousRun = new swd.Run(text.Substring(prevIdx, i - prevIdx));
        span.Inlines.Add(previousRun);
      }

      if (span.Inlines.Count > 0)
        return span;
      else
        return previousRun ?? throw new InvalidOperationException();

    }


    #region Conversion helper

    /// <summary>
    /// Converts internal FontStyle enum to WPF fontstyle.
    /// </summary>
    /// <param name="fs">The internal fontstyle value.</param>
    /// <returns>WPF fontstyle.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public System.Windows.FontStyle ToFontStyle(FontStyle fs)
    {
      switch (fs)
      {
        case FontStyle.Normal:
          return System.Windows.FontStyles.Normal;
        case FontStyle.Oblique:
          return System.Windows.FontStyles.Oblique;
        case FontStyle.Italic:
          return System.Windows.FontStyles.Italic;
        default:
          throw new NotImplementedException();
      }
    }

    public System.Windows.FontWeight ToFontWeight(FontWeight fw)
    {
      switch (fw)
      {
        case FontWeight.Thin:
          return System.Windows.FontWeights.Thin;
        case FontWeight.ExtraLight:
          return System.Windows.FontWeights.ExtraLight;
        case FontWeight.Normal:
          return System.Windows.FontWeights.Normal;
        case FontWeight.Medium:
          return System.Windows.FontWeights.Medium;
        case FontWeight.DemiBold:
          return System.Windows.FontWeights.DemiBold;

        case FontWeight.Bold:
          return System.Windows.FontWeights.Bold;
        case FontWeight.ExtraBold:
          return System.Windows.FontWeights.ExtraBold;

        case FontWeight.Black:
          return System.Windows.FontWeights.Black;

        case FontWeight.ExtraBlack:
          return System.Windows.FontWeights.ExtraBlack;
        default:
          throw new NotImplementedException();
      }
    }

    public System.Windows.Media.Color ToColor(ExCSS.Color color)
    {
      if (InvertColors)
        return System.Windows.Media.Color.FromArgb(color.A, (byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B));
      else
        return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public System.Windows.Thickness ToThickness(Thickness t)
    {
      if (t.Left == t.Right && t.Left == t.Top && t.Left == t.Bottom && t.Left.IsAbsolute)
        return new System.Windows.Thickness(t.Left.ToPixel());
      else
        return new System.Windows.Thickness(
          t.Left.IsAbsolute ? Math.Max(0, t.Left.ToPixel()) : 0, // Note: we have to clamp to values >=0, since Wpf does not accept negative values
          t.Top.IsAbsolute ? Math.Max(0, t.Top.ToPixel()) : 0,
          t.Right.IsAbsolute ? Math.Max(0, t.Right.ToPixel()) : 0,
          t.Bottom.IsAbsolute ? Math.Max(0, t.Bottom.ToPixel()) : 0
          );
    }

    private System.Windows.TextAlignment ToTextAlignment(TextAlignment value)
    {
      switch (value)
      {
        case TextAlignment.Left:
          return System.Windows.TextAlignment.Left;
        case TextAlignment.Right:
          return System.Windows.TextAlignment.Right;
        case TextAlignment.Center:
          return System.Windows.TextAlignment.Center;
        case TextAlignment.Justify:
          return System.Windows.TextAlignment.Justify;
        default:
          throw new NotImplementedException();
      }
    }

    private System.Windows.TextDecorationCollection ToTextDecorations(Dom.TextDecorations value)
    {
      switch (value)
      {
        case TextDecorations.None:
          return null;
        case TextDecorations.Underline:
          return System.Windows.TextDecorations.Underline;
        default:
          throw new NotImplementedException();
      }
    }

    private System.Windows.TextMarkerStyle ToMarkerStyle(ListMarkerStyle value)
    {
      switch (value)
      {
        case ListMarkerStyle.None:
          return System.Windows.TextMarkerStyle.None;
        case ListMarkerStyle.Square:
          return System.Windows.TextMarkerStyle.Square;
        case ListMarkerStyle.Box:
          return System.Windows.TextMarkerStyle.Box;
        case ListMarkerStyle.LowerLatin:
          return System.Windows.TextMarkerStyle.LowerLatin;
        case ListMarkerStyle.UpperLatin:
          return System.Windows.TextMarkerStyle.UpperLatin;
        case ListMarkerStyle.LowerRoman:
          return System.Windows.TextMarkerStyle.LowerRoman;
        case ListMarkerStyle.UpperRoman:
          return System.Windows.TextMarkerStyle.UpperRoman;
        case ListMarkerStyle.Decimal:
          return System.Windows.TextMarkerStyle.Decimal;
        case ListMarkerStyle.Disc:
          return System.Windows.TextMarkerStyle.Disc;
        case ListMarkerStyle.Circle:
          return System.Windows.TextMarkerStyle.Circle;
        default:
          throw new NotImplementedException();
      }
    }

    #endregion

    Dictionary<string, System.Windows.Media.FontFamily> _resolvedFontFamilies = new Dictionary<string, System.Windows.Media.FontFamily>();

    void LoadFonts()
    {
      _resolvedFontFamilies.Clear();
      var alreadyTriedFolders = new HashSet<string>();
      foreach (var entry in FontDictionary)
      {
        var folder = System.IO.Path.GetDirectoryName(entry.Value);
        if (!alreadyTriedFolders.Contains(folder))
        {
          alreadyTriedFolders.Add(folder);
          if (folder.StartsWith(@"\\?\"))
            folder = folder.Substring(4);
          folder = folder.Replace('\\', '/');
          folder = "file:///" + folder;
          if (!folder.EndsWith("/"))
            folder += "/";
          var families = System.Windows.Media.Fonts.GetFontFamilies(folder);

          foreach (var family in families)
          {
            var src = family.Source;
            var parts = src.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
              _resolvedFontFamilies[parts[parts.Length - 1].ToLowerInvariant()] = family;
          }
        }
      }
    }

    System.Windows.Media.FontFamily GetFontFamily(string familyName)
    {
      familyName = familyName.ToLowerInvariant();
      var parts = familyName.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

      if (_resolvedFontFamilies.ContainsKey(parts[0].Trim()))
        return _resolvedFontFamilies[parts[0].Trim()];
      else
        return new System.Windows.Media.FontFamily(familyName);
    }


    Dictionary<ExCSS.Color, System.Windows.Media.Brush> _cachedSolidBrushes = new Dictionary<ExCSS.Color, System.Windows.Media.Brush>();

    /// <summary>
    /// Gets a solid color brush from the color specified.
    /// </summary>
    /// <param name="color">The color in RGBA format (A is the least significant byte).</param>
    /// <returns>A solid color brush.</returns>
    public System.Windows.Media.Brush GetBrushFromColor(ExCSS.Color color)
    {
      if (!_cachedSolidBrushes.TryGetValue(color, out var brush))
      {
        brush = new System.Windows.Media.SolidColorBrush(ToColor(color));
        brush.Freeze();
        _cachedSolidBrushes.Add(color, brush);
      }
      return brush;
    }
  }
}
