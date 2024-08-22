// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Buffers;

namespace Bundles.ValueCollections;

/// <summary>
/// A value-type list type that can only be modified by appending, never by removing. Use after disposal is undefined behaviour.
/// </summary>
/// <typeparam name="T">The element type controlled by this list.</typeparam>
public partial record struct ValueAppendList<T> : IDisposable
{
    private T[] buffer;
    private int index;
    private bool disposed;

    /// <summary>
    /// Gets a readonly reference to the element at the specified index.
    /// </summary>
    public readonly ref readonly T this[int index] => ref this.buffer[index];

    /// <summary>
    /// Gets the current capacity of this list.
    /// </summary>
    public readonly int Capacity => this.buffer.Length;

    /// <summary>
    /// Gets the element count of this list.
    /// </summary>
    public readonly int Count => this.index + 1;

    public readonly ref readonly T GetNewestElement()
        => ref this.buffer[this.index];

    /// <summary>
    /// Adds an element to the list, returning a readonly reference to the newly added element.
    /// </summary>
    public ref readonly T Add(T value)
    {
        if (++this.index == buffer.Length)
        {
            this.ResizeBuffer();
        }

        this.buffer[this.index] = value;
        return ref this.buffer[this.index];
    }

    /// <summary>
    /// Gets an enumerator over the present structure.
    /// </summary>
    /// <returns></returns>
    public readonly Enumerator GetEnumerator()
        => new(this);

    public void Dispose()
    {
        if (!this.disposed && typeof(T).IsPrimitive)
        {
            ArrayPool<T>.Shared.Return(this.buffer);
        }

        this.disposed = true;
    }

    private void ResizeBuffer()
    {
        int newLength = this.buffer.Length * 2;
        T[] newBuffer = typeof(T).IsPrimitive ? ArrayPool<T>.Shared.Rent(newLength) : new T[newLength];

        this.buffer.CopyTo(newBuffer, 0);

        if (typeof(T).IsPrimitive)
        {
            ArrayPool<T>.Shared.Return(this.buffer);
        }

        this.buffer = newBuffer;
    }
}
