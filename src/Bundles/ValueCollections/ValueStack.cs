// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bundles.ValueCollections;

/// <summary>
/// Represents an allocation-free stack of unmanaged items. This stack cannot resize.
/// </summary>
/// <typeparam name="T">The item type of this collection.</typeparam>
[DebuggerDisplay("Capacity = {Capacity}; Count = {Count}")]
public unsafe ref struct ValueStack<T>
    where T : unmanaged
{
    private readonly Span<T> items;
    private int count;

    /// <summary>
    /// Creates a new, all-zero <see cref="ValueStack{T}"/>.
    /// </summary>
    public ValueStack()
    {
        this.items = Span<T>.Empty;
        this.count = 0;
    }

    /// <summary>
    /// Creates a new <see cref="ValueStack{T}"/>.
    /// </summary>
    /// <param name="memory">The backing memory for this instance.</param>
    public ValueStack
    (
        Span<T> memory
    )
    {
        this.items = memory;
        this.count = 0;
    }

    public ValueStack
    (
        T* memory,
        nuint length
    )
    {
        this.items = MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(memory), (int)length);
        this.count = 0;
    }

    /// <summary>
    /// Creates a new <see cref="ValueStack{T}"/> by implicitly casting, enabling syntax such as
    /// <c><![CDATA[ValueStack<int> stack = stackalloc int[4];]]></c>
    /// </summary>
    /// <param name="memory">The backing memory for this instance</param>
    public static implicit operator ValueStack<T>
    (
        Span<T> memory
    )
        => new(memory);

    /// <summary>
    /// Gets the number of items currently contained in the stack.
    /// </summary>
    public readonly nuint Count => (nuint)this.count;

    /// <summary>
    /// Gets the maximum number of items in this stack.
    /// </summary>
    public readonly nuint Capacity => (nuint)this.items.Length;

    /// <summary>
    /// Clears this stack of all items.
    /// </summary>
    public void Clear()
        => this.count = 0;

    /// <summary>
    /// Returns a reference to the first <typeparamref name="T"/> in the controlled memory region that can be
    /// used for pinning. This method is intended to support .NET compilers and is not intended to be called
    /// by user code.
    /// </summary>
    /// <returns>A reference to the <typeparamref name="T"/> at index 0, or null if the stack is empty.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref readonly T GetPinnableReference() 
        => ref this.items.GetPinnableReference();

    /// <summary>
    /// Pushes a new item to the stack, failing if the stack ran out of space.
    /// </summary>
    /// <param name="item">The item to push to the stack.</param>
    /// <returns><c>true</c> to indicate success, <c>false</c> to indicate failure.</returns>
    public bool TryPush
    (
        T item
    )
    {
        if (this.count == this.items.Length)
        {
            return false;
        }

        this.items[count++] = item;
        return true;
    }

    /// <summary>
    /// Pops an item from the stack, failing if the stack was empty.
    /// </summary>
    /// <param name="item">The item popped from the stack.</param>
    /// <returns><c>true</c> to indicate success, <c>false</c> to indicate failure.</returns>
    public bool TryPop
    (
        out T item
    )
    {
        if (this.count == 0)
        {
            item = default;
            return false;
        }

        item = this.items[--count];
        return true;
    }

    /// <summary>
    /// Peeks the top of the stack, failing if the stack was empty.
    /// </summary>
    /// <param name="item">The item peeked from the stack.</param>
    /// <returns><c>true</c> to indicate success, <c>false</c> to indicate failure.</returns>
    public readonly bool TryPeek
    (
        out T item
    )
    {
        if (this.count == 0)
        {
            item = default;
            return false;
        }

        item = this.items[count - 1];
        return true;
    }

    /// <summary>
    /// Pushes a new item to the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the stack was full.</exception>
    public void Push
    (
        T item
    )
    {
        if (!this.TryPush(item))
        {
            ThrowHelper.ThrowNonResizingCollectionFull();
        }
    }

    /// <summary>
    /// Pops an item from the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the stack was empty.</exception>
    public T Pop()
    {
        if (!this.TryPop(out T item))
        {
            return item;
        }

        ThrowHelper.ThrowCollectionEmpty();
        return default;
    }

    /// <summary>
    /// Peeks an item from the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the stack was empty.</exception>
    public readonly T Peek()
    {
        if (this.TryPeek(out T item))
        {
            return item;
        }

        ThrowHelper.ThrowCollectionEmpty();
        return default;
    }

    /// <summary>
    /// Peeks an item from the stack, returning it by ref so that it can be modified without incurring copies
    /// and popping-pushing the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the stack was empty.</exception>
    public readonly ref T PeekRef()
    {
        if (this.count != 0)
        {
            return ref this.items[count];
        }

        ThrowHelper.ThrowCollectionEmpty();
        return ref Unsafe.NullRef<T>();
    }
}
