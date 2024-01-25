// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

#pragma warning disable IDE0079         // "remove unnecessary suppression"; the suppression is necessary
#pragma warning disable CS0659, CS0661  // we don't want to override Object.GetHashCode. we're not interested.

// i like how after suppressing these in NativeAlignedArray`1.cs the diagnostics just migrated here

using System;
using System.Diagnostics;

namespace Bundles.Native;

unsafe partial struct NativeAlignedArray<T>
{
    internal sealed class NativeAlignedArrayDebugView(NativeAlignedArray<T> array)
    {
        public bool IsNull => array.pointer == null;

        public nuint Length => array.Length;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                nuint count = nuint.Min(array.Length, nuint.MaxValue);
                T[] items = GC.AllocateUninitializedArray<T>((int)count);

                fixed (T* pItems = items)
                {
                    Span<T> span = new(pItems, (int)count);
                    array.CopyTo(span);
                }

                return items;
            }
        }
    }
}
