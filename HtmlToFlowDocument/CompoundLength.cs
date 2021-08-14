// // Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument
{
  /// <summary>
  /// Represents a length that can contain both absolute and relativ length values. All values contained in the compound will be added up to give the final result.
  /// </summary>
  /// <seealso cref="System.Collections.Generic.Dictionary{ExCSS.Length.Unit, ExCSS.Length}" />
  public class CompoundLength : Dictionary<ExCSS.Length.Unit, ExCSS.Length>
  {
    /// <summary>
    /// Returns true if the resulting length does not depend on the parent element. This is the case if the compound no longer contains percentage values.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the resulting length does not depend on the parent element; otherwise, <c>false</c>.
    /// </returns>
    public bool IsDetermined()
    {
      return IsDetermined(this);
    }

    /// <summary>
    /// Returns true if the resulting length of the compound does not depend on the parent element. This is the case if the compound no longer contains percentage values.
    /// </summary>
    /// <param name="lengthComposition">The compound length.</param>
    /// <returns>
    ///   <c>true</c> if the resulting length does not depend on the parent element; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsDetermined(Dictionary<ExCSS.Length.Unit, ExCSS.Length> lengthComposition)
    {
      // the length composition is determined, if it does not longer depend on the size of a parent element
      // thus: if it contains percent, it is not determined

      return !lengthComposition.ContainsKey(ExCSS.Length.Unit.Percent);
    }

    /// <summary>
    /// Multiplies two compound lengths. Attention! The order of the arguments matter!
    /// </summary>
    /// <param name="higherLevel">The compound length of the higher level element (child element).</param>
    /// <param name="lowerLevel">The compound length of the lower level element (parent element).</param>
    /// <returns>The resulting compound length.</returns>
    /// <exception cref="InvalidProgramException">The function should not be called if hierLevel does not contain a percent value</exception>
    /// <remarks>A percentage value found in the compound length of the child element is multiplied with all entries of the parent length,
    /// then the rest of the values from the child element is added to the result.</remarks>
    public static CompoundLength Multiply(Dictionary<ExCSS.Length.Unit, ExCSS.Length> higherLevel, Dictionary<ExCSS.Length.Unit, ExCSS.Length> lowerLevel)
    {
      var result = new CompoundLength();
      // we multiply the percent value of the higher level with all quantities of the lower level and replace then the percent value of the higher level with the result

      if (!higherLevel.TryGetValue(ExCSS.Length.Unit.Percent, out var percentUnit))
        throw new InvalidProgramException("The function should not be called if hierLevel does not contain a percent value");

      double factor = percentUnit.Value / 100.0;

      foreach (var entry in lowerLevel) // Multiply with factor and store in the result
      {
        result.Add(entry.Key, new ExCSS.Length((float)(entry.Value.Value * factor), entry.Key));
      }

      // now add the remaining items from the higher level (except the percentage value
      foreach (var entry in higherLevel)
      {
        if (entry.Key != ExCSS.Length.Unit.Percent)
        {
          AddCompoundLength(result, entry.Value);
        }
      }

      return result;
    }

    /// <summary>
    /// Adds the specified length value to the compound. Note that all absolute values will be converted to pixels.
    /// </summary>
    /// <param name="l">The length value.</param>
    /// <param name="factor">A factor the length value is multiplied with before added to the compound.</param>
    public void Add(ExCSS.Length l, double factor = 1)
    {
      AddCompoundLength(this, l, factor);
    }

    /// <summary>
    /// Adds the specified length value to the compound. Note that all absolute values will be converted to pixels.
    /// </summary>
    /// <param name="compoundLength">The compound length the value is added to. This parameter is modified upon return.</param>
    /// <param name="l">The length value.</param>
    /// <param name="factor">A factor the length value is multiplied with before added to the compound.</param>
    public static void AddCompoundLength(Dictionary<ExCSS.Length.Unit, ExCSS.Length> compoundLength, ExCSS.Length l, double factor = 1)
    {
      if (l.IsAbsolute)
        l = new ExCSS.Length(l.ToPixel(), ExCSS.Length.Unit.Px); // in order to maintain only one absolute unit in the compound

      if (compoundLength.TryGetValue(l.Type, out var existingLength))
      {
        compoundLength[l.Type] = new ExCSS.Length((float)(existingLength.Value + factor * l.Value), l.Type);
      }
      else
      {
        compoundLength.Add(l.Type, factor == 1 ? l : new ExCSS.Length((float)(factor * l.Value), l.Type));
      }
    }


    /// <summary>
    /// Determines whether this compound represents an absolute length value.
    /// </summary>
    /// <param name="absoluteValueInPixels">If the return value is true, this parameter contains the absolute length value in pixels.</param>
    /// <returns>
    /// <c>true</c> if this compound represents an absolute length value; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPurelyAbsolute(out double absoluteValueInPixels)
    {
      return IsPurelyAbsolute(this, out absoluteValueInPixels);
    }

    /// <summary>
    /// Determines whether this compound represents an absolute length value.
    /// </summary>
    /// <param name="compoundLength">The compound length.</param>
    /// <param name="absoluteValueInPixels">If the return value is true, this parameter contains the absolute length value in pixels.</param>
    /// <returns>
    /// <c>true</c> if this compound represents an absolute length value; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsPurelyAbsolute(Dictionary<ExCSS.Length.Unit, ExCSS.Length> compoundLength, out double absoluteValueInPixels)
    {
      double result = 0;

      foreach (var entry in compoundLength)
      {
        if (entry.Value.IsAbsolute)
          result += entry.Value.ToPixel();
        else
        {
          absoluteValueInPixels = double.NaN;
          return false;
        }
      }

      absoluteValueInPixels = result;
      return true;
    }

    public bool IsPurelyVw(out double vwValue)
    {
      return IsPurelyVw(this, out vwValue);
    }

    public static bool IsPurelyVw(Dictionary<ExCSS.Length.Unit, ExCSS.Length> compoundLength, out double vwValue)
    {
      if (compoundLength.Count == 1 && compoundLength.TryGetValue(ExCSS.Length.Unit.Vw, out var vw))
      {
        vwValue = vw.Value;
        return true;
      }
      else
      {
        vwValue = double.NaN;
        return false;
      }
    }

    public bool IsPurelyVh(out double vhValue)
    {
      return IsPurelyVh(this, out vhValue);
    }
    public static bool IsPurelyVh(Dictionary<ExCSS.Length.Unit, ExCSS.Length> compoundLength, out double vhValue)
    {
      if (compoundLength.Count == 1 && compoundLength.TryGetValue(ExCSS.Length.Unit.Vh, out var vh))
      {
        vhValue = vh.Value;
        return true;
      }
      else
      {
        vhValue = double.NaN;
        return false;
      }
    }


  }
}
