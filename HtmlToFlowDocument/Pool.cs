// // Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlToFlowDocument
{
  public static class Pool<T> where T : class, new()
  {
    static System.Collections.Concurrent.ConcurrentStack<T> _stack = new System.Collections.Concurrent.ConcurrentStack<T>();
    public static Action<T> Clear { get; set; }

    public static T FromPool()
    {
      return _stack.TryPop(out var t) ? t : new T();
    }

    public static void ToPool(T obj)
    {
      if (!(obj is null))
        _stack.Push(obj);
    }
  }
}
