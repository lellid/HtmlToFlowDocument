using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToXamlConverter.Dom
{
  public class Hyperlink : Span
  {
    public string NavigateUri { get; set; }
    public string TargetName { get; set; }
  }
}
