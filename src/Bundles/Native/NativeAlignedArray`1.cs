// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

#pragma warning disable IDE0079         // remove unnecessary suppression; the suppression is necessary
#pragma warning disable CS0659, CS0661  // we don't want to override Object.GetHashCode. we're not interested.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Bundles.Native;

/// <summary>
/// Represents an aligned array to the specified alignment, allocated in native memory.
/// </summary>
/// <remarks>
/// If you aren't absolutely sure you need to be using this specifically, maybe... don't.
/// </remarks>
/// <typeparam name="T">The type handled by this array.</typeparam>
/// <param name="length">The length of this allocation.</param>
/// <param name="alignment">The alignment of this allocation. This will be unobservable after construction.</param>
[SkipLocalsInit]
[DebuggerTypeProxy(typeof(NativeAlignedArray<>.NativeAlignedArrayDebugView))]
public readonly unsafe partial struct NativeAlignedArray<T>(nuint length, nuint alignment) : IDisposable
    where T : unmanaged
{
    /// <summary>
    /// The length of this array.
    /// </summary>
    public nuint Length { get; } = length;

    private readonly T* pointer = (T*)NativeMemory.AlignedAlloc(length * (nuint)sizeof(T), alignment);

    /// <summary>
    /// Creates a new native aligned array from the specified pointer with the specified length. If you are using this, please
    /// reconsider your life choices.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static NativeAlignedArray<T> CreateUnsafe(T* pointer, nuint length)
    {
        Unsafe.SkipInit(out NativeAlignedArray<T> value);

        Vector128.Create(length, (nuint)pointer).Store((ulong*)&value);

        return value;
    }

    /// <summary>
    /// Gets a reference to the element at the specified index.
    /// </summary>
    public ref T this[nuint index] => ref Unsafe.AsRef<T>(pointer + index);

    /// <summary>
    /// Gets a reference to the element at index zero.
    /// </summary>
    public readonly ref T Start => ref Unsafe.AsRef<T>(pointer);

    /// <summary>
    /// Gets a reference to the first element out of bounds. Do not dereference this - it is provided for cheap range checks.
    /// Dereferencing this might catastrophically fail.
    /// </summary>
    public readonly ref T AfterEnd => ref Unsafe.AsRef<T>(pointer + this.Length);

    public static implicit operator Span<T>(NativeAlignedArray<T> value)
        => new(value.pointer, (int)value.Length);

    public static implicit operator ReadOnlySpan<T>(NativeAlignedArray<T> value)
        => new(value.pointer, (int)value.Length);

    public override bool Equals([NotNullWhen(true)] object? obj) 
        => obj is NativeAlignedArray<T> array && array.Length == this.Length && array.pointer == this.pointer;

    public static bool operator ==(NativeAlignedArray<T> left, NativeAlignedArray<T> right) => left.Equals(right);
    public static bool operator !=(NativeAlignedArray<T> left, NativeAlignedArray<T> right) => !left.Equals(right);

    /// <summary>
    /// Compares equality between two native aligned arrays based on the specified comparison.
    /// </summary>
    public bool Equals(NativeAlignedArray<T> array, CollectionEqualityComparison comparison)
    {
        return comparison switch
        {
            CollectionEqualityComparison.Reference => this.Equals(array),
            CollectionEqualityComparison.Value => this.Length == array.Length && ((Span<T>)this).SequenceEqual(array),
            CollectionEqualityComparison.Slice => (nuint)array.pointer >= (nuint)this.pointer 
                && (nuint)array.pointer <= (nuint)this.pointer + this.Length,
            _ => false,
        };
    }

    public void Dispose() => NativeMemory.AlignedFree(pointer);
}
