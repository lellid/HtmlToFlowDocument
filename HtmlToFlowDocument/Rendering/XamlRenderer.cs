using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using HtmlToFlowDocument.Dom;

namespace HtmlToFlowDocument.Rendering
{
  public class XamlRenderer
  {
    private static readonly string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";


    public void Render(TextElement rootElement, System.IO.Stream stream)
    {

      XmlWriterSettings settings = new XmlWriterSettings
      {
        Indent = true
      };

      using (XmlWriter writer = XmlWriter.Create(stream, settings))
      {
        writer.WriteStartDocument();

        Render(rootElement, writer);

        writer.WriteEndDocument();
      }
    }

    public void Render(TextElement e, XmlWriter wr)
    {
      // Render TextElement properties

      wr.WriteStartElement(e.GetType().Name, XamlNamespace);

      if (!string.IsNullOrEmpty(e.FontFamily))
      {
        wr.WriteAttributeString("FontFamily", e.FontFamily);
      }

      if (e.FontSize.HasValue)
      {
        wr.WriteAttributeString("FontSize", e.FontSize.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
      }

      if (e.FontStyle.HasValue)
      {
        wr.WriteAttributeString("FontStyle", Enum.GetName(typeof(FontStyle), e.FontStyle.Value));
      }

      if (e.FontWeight.HasValue)
      {
        wr.WriteAttributeString("FontWeight", Enum.GetName(typeof(FontWeight), e.FontWeight.Value));
      }

      if (e.Foreground.HasValue)
      {
        wr.WriteAttributeString("Foreground", ToColorString(e.Foreground.Value));
      }

      if (e.Background.HasValue)
      {
        wr.WriteAttributeString("Background", ToColorString(e.Background.Value));
      }

      // now special properties

      switch (e)
      {
        case Block b:

          if (b.Margin.HasValue)
          {
            wr.WriteAttributeString("Margin", ToThicknessString(b.Margin.Value));
          }

          if (b.Padding.HasValue)
          {
            wr.WriteAttributeString("Padding", ToThicknessString(b.Padding.Value));
          }

          if (b.BorderBrush.HasValue)
          {
            wr.WriteAttributeString("BorderBrush", ToColorString(b.BorderBrush.Value));
          }

          if (b.BorderThickness.HasValue)
          {
            wr.WriteAttributeString("BorderThickness", ToThicknessString(b.BorderThickness.Value));
          }

          if (b.TextAlignment.HasValue)
          {
            wr.WriteAttributeString("TextAlignment", Enum.GetName(typeof(TextAlignment), b.TextAlignment.Value));
          }

          break;
        case Inline i:
          break;
      }


      switch (e)
      {
        case Hyperlink hl:
          if (!string.IsNullOrEmpty(hl.NavigateUri))
          {
            if (Uri.IsWellFormedUriString(hl.NavigateUri, UriKind.RelativeOrAbsolute))
            {
              wr.WriteAttributeString("NavigateUri", hl.NavigateUri);
            }
            else
            {

            }
          }
          if (!string.IsNullOrEmpty(hl.TargetName))
          {
            wr.WriteAttributeString("TargetName", hl.TargetName);
          }
          break;
        case Image img:
          if (!string.IsNullOrEmpty(img.Source))
          {
            wr.WriteAttributeString("Source", string.Format("{{Binding ImageProvider[{0}]}}", img.Source));
          }
          break;

        case List list:
          if (list.MarkerStyle.HasValue)
          {
            wr.WriteAttributeString("MarkerStyle", Enum.GetName(typeof(ListMarkerStyle), list.MarkerStyle.Value));
          }
          break;

        case Paragraph p:
          if (p.TextDecorations.HasValue)
          {
            wr.WriteAttributeString("TextDecorations", Enum.GetName(typeof(TextDecorations), p.TextDecorations.Value));
          }
          if (p.TextIndent.HasValue)
          {
            wr.WriteAttributeString("TextIndent", XmlConvert.ToString(p.TextIndent.Value));
          }
          break;
        case TableCell tc:
          if (1 != tc.ColumnSpan)
          {
            wr.WriteAttributeString("ColumnSpan", XmlConvert.ToString(tc.ColumnSpan));
          }

          if (1 != tc.RowSpan)
          {
            wr.WriteAttributeString("RowSpan", XmlConvert.ToString(tc.RowSpan));
          }
          if (tc.BorderBrush.HasValue)
          {
            wr.WriteAttributeString("BorderBrush", ToColorString(tc.BorderBrush.Value));
          }
          if (tc.BorderThickness.HasValue)
          {
            wr.WriteAttributeString("BorderThickness", ToThicknessString(tc.BorderThickness.Value));
          }
          break;
      }

      //  finished rendering the attributes

      // --------------  now render the contents ------------------------------

      if (e is Run r)
      {
        wr.WriteString(r.Text);
      }
      else if (e is Table tab)
      {
        if (tab.Columns.Count > 0)
        {
          wr.WriteStartElement("Table.Columns");
          foreach (var c in tab.Columns)
          {
            wr.WriteStartElement("TableColumn");
            if (c.Width.HasValue)
              wr.WriteAttributeString("Width", XmlConvert.ToString(c.Width.Value));
            wr.WriteEndElement();
          }
          wr.WriteEndElement();
        }

        foreach (var child in e.Childs)
        {
          Render(child, wr);
        }


      }
      else
      {
        // now, render all children
        foreach (var child in e.Childs)
        {
          Render(child, wr);
        }

      }

      wr.WriteEndElement();
    }

    #region Conversion helper

    public static string ToColorString(int color)
    {
      uint c = (uint)color;
      if (0xFF == (color & 0xFF)) // fully opaque
        return string.Format("#{0:X6}", c >> 8);
      else
        return string.Format("#{0:X8}", c);
    }

    public static string ToThicknessString(Thickness t)
    {
      if (t.Left == t.Right && t.Left == t.Top && t.Left == t.Bottom)
        return t.Left.ToString(System.Globalization.CultureInfo.InvariantCulture);
      else
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3}", t.Left, t.Top, t.Right, t.Bottom);
    }

    #endregion

  }
}
