// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Bundles.ValueCollections;

partial record struct ValueAppendList<T>
{
    public struct Enumerator(ValueAppendList<T> list)
    {
        private int index;

        public ref readonly T Current => ref list[this.index];

        public bool MoveNext() 
            => ++this.index != list.Count;
    }
}
