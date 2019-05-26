# HTML to XAML Converter

This library aims to convert an HTML document into an Extensible Application Markup Language (XAML) [FlowDocument](https://docs.microsoft.com/de-de/dotnet/framework/wpf/advanced/flow-document-overview). Of course, this is possible only for simple layouts.

The project is composed of two parts:

- The HTML to Xaml converter, which was copied from Microsoft's Demo [HTMLToXamlConverter](https://github.com/microsoft/WPF-Samples/tree/master/Sample%20Applications/HtmlToXamlDemo) (MIT license)
- [The ExCSS project](https://github.com/TylerBrinks/ExCSS) from Tyler Brinks (MIT license)

In this project, Microsoft's demo HtmlToXamlConverter is extended to support the following features:

- use of externally provided .css files (by means of the ExCSS parser)
- support for images
- implementation of colors (text, background)
- support for em units in fonts and all kind of sizes, by converting them to absolute units
- some other fixes, like support for trailing spaces in text


## Usage

The main function is 

```
public static string ConvertHtmlToXaml(string htmlString, bool asFlowDocument, Func<string, string> cssStyleSheetProvider)
```

of the class `HtmlToXamlConverter`.

You have to provide three arguments:

1. *htmlString*: the entire HTML document to convert as a text string (ATTENTION: this represents the HTML document, **not** the URL!)
2. *asFlowDocument*: a boolean variable. If true, a XAML document is returned in which the topmost node is a [FlowDocument](https://docs.microsoft.com/de-de/dotnet/api/system.windows.documents.flowdocument). If false, the topmost node
is a [Section](https://docs.microsoft.com/de-de/dotnet/api/system.windows.documents.section), which can then be inserted in a [FlowDocument](https://docs.microsoft.com/de-de/dotnet/api/system.windows.documents.flowdocument) later on.
3. *cssStyleSheetProvider*: a function, which takes as only argument the relative name of a .CSS file. It is your responsibility to fetch the .CSS file, and return the text of the .CSS file as the return value of the provided function.

The return value of `ConvertHtmlToXaml` is a XAML document, according to the 2nd argument either with a FlowDocument or with a Section at the top.

### Providing images

I decided against encoding images directly into the XAML document. 
Instead, late binding is used to show images only after the XAML is converted into the corresponding Wpf class.
For example, the Html image tag:
```
<img src="foo.png" />
```

is converted to the following XAML string:

```
<Image Source="{Binding ImageProvider[foo.png]}" />
```

This means, if you later show the document, the data context of your document must contain a property named `ImageProvider`.
The class that is returned by the property `ImageProvider` must contain an indexed property, which takes a string as the argument. The string represents the relative file name of the image. It is your responsibility to load the image and return it as a Wpf imagesource.

*Example:*
```
public class ImageSource
{
  public object this[string relativeFileName]
  {
    get
    {
      var fullFileName = Path.Combine(baseDirectory, relativeFileName);
      return new BitmapImage(new Uri(fullFileName));
    }
  }
}
```

Then, inside the data context of the FlowDocument:
```
public ImageSource ImageProvider {get;} = new ImageSource();
```
