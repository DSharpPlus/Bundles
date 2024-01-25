// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bundles.Native;

/// <summary>
/// Contains extensions on <seealso cref="NativeAlignedArray{T}"/> for easier manipulation and operation.
/// </summary>
public static unsafe class NativeAlignedArray
{
    /// <summary>
    /// Gets a span over the specified array.
    /// </summary>
    public static Span<T> AsSpan<T>(this NativeAlignedArray<T> array)
        where T : unmanaged
        => array;

    /// <summary>
    /// Gets a span over the specified array, starting at the specified index.
    /// </summary>
    public static Span<T> AsSpan<T>(this NativeAlignedArray<T> array, nuint startIndex)
        where T : unmanaged
        => new((T*)Unsafe.AsPointer(ref array.Start) + startIndex, (int)(array.Length - startIndex));

    /// <summary>
    /// Gets a span over the specified array, starting at the specified index to the specified length.
    /// </summary>
    public static Span<T> AsSpan<T>(this NativeAlignedArray<T> array, nuint startIndex, nuint length)
        where T : unmanaged
        => new((T*)Unsafe.AsPointer(ref array.Start) + startIndex, (int)length);

    /// <summary>
    /// Clears the contents of this native array.
    /// </summary>
    public static void Clear<T>(this NativeAlignedArray<T> array)
        where T : unmanaged
        => MemoryHelpers.ZeroMemoryArbitraryAlignment(Unsafe.AsPointer(ref array.Start), array.Length * (nuint)sizeof(T));

    /// <summary>
    /// Clears the contents of this native array, if the array was aligned to 16 bytes.
    /// </summary>
    /// <remarks>
    /// Since the codegen for this method strictly relies on alignment, calling this without the required alignment guarantees
    /// will very likely lead to fatal issues.
    /// </remarks>
    public static void Clear16bAligned<T>(this NativeAlignedArray<T> array)
        where T : unmanaged
        => MemoryHelpers.ZeroMemory16bAlignment(Unsafe.AsPointer(ref array.Start), array.Length * (nuint)sizeof(T));

    /// <summary>
    /// Clears the contents of this native array, if the array was aligned to 32 bytes.
    /// </summary>
    /// <remarks>
    /// Since the codegen for this method strictly relies on alignment, calling this without the required alignment guarantees
    /// will very likely lead to fatal issues.
    /// </remarks>
    public static void Clear32bAligned<T>(this NativeAlignedArray<T> array)
        where T : unmanaged
        => MemoryHelpers.ZeroMemory32bAlignment(Unsafe.AsPointer(ref array.Start), array.Length * (nuint)sizeof(T));

    /// <summary>
    /// Clears the contents of this native array, if the array was aligned to 64 bytes.
    /// </summary>
    /// <remarks>
    /// Since the codegen for this method strictly relies on alignment, calling this without the required alignment guarantees
    /// will very likely lead to fatal issues.
    /// </remarks>
    public static void Clear64bAligned<T>(this NativeAlignedArray<T> array)
        where T : unmanaged
        => MemoryHelpers.ZeroMemory64bAlignment(Unsafe.AsPointer(ref array.Start), array.Length * (nuint)sizeof(T));

    /// <summary>
    /// Creates a slice of the specified native aligned array. This voids all alignment guarantees of the contained data.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static NativeAlignedArray<T> Slice<T>(this NativeAlignedArray<T> array, nuint start, nuint length)
        where T : unmanaged
        => NativeAlignedArray<T>.CreateUnsafe((T*)Unsafe.AsPointer(ref array.Start) + start, length);

    /// <summary>
    /// Copies the contents of one native aligned array into another.
    /// </summary>
    public static void CopyTo<T>(this NativeAlignedArray<T> src, NativeAlignedArray<T> dst)
        where T : unmanaged
    {
        MemoryHelpers.CopyMemoryArbitraryAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref dst.Start),
            dst.Length * (nuint)sizeof(T)
        );
    }

    /// <summary>
    /// Copies the contents of one native aligned array into a span.
    /// </summary>
    public static void CopyTo<T>(this NativeAlignedArray<T> src, Span<T> dst)
        where T : unmanaged

    {
        MemoryHelpers.CopyMemoryArbitraryAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(dst)),
            (nuint)dst.Length * (nuint)sizeof(T)
        );
    }

    /// <summary>
    /// Copies the contents of one native aligned array into another. Both containers are expected to be aligned to 16 bytes.
    /// </summary>
    public static void CopyTo16bAligned<T>(this NativeAlignedArray<T> src, NativeAlignedArray<T> dst)
        where T : unmanaged
    {
        MemoryHelpers.CopyMemory16bAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref dst.Start),
            dst.Length * (nuint)sizeof(T)
        );
    }

    /// <summary>
    /// Copies the contents of one native aligned array into a span. Both containers are expected to be aligned to 16 bytes.
    /// </summary>
    public static void CopyTo16bAlignment<T>(this NativeAlignedArray<T> src, Span<T> dst)
        where T : unmanaged

    {
        MemoryHelpers.CopyMemory16bAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(dst)),
            (nuint)dst.Length * (nuint)sizeof(T)
        );
    }

    /// <summary>
    /// Copies the contents of one native aligned array into another. Both containers are expected to be aligned to 32 bytes.
    /// </summary>
    public static void CopyTo32bAligned<T>(this NativeAlignedArray<T> src, NativeAlignedArray<T> dst)
        where T : unmanaged
    {
        MemoryHelpers.CopyMemory32bAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref dst.Start),
            dst.Length * (nuint)sizeof(T)
        );
    }

    /// <summary>
    /// Copies the contents of one native aligned array into a span. Both containers are expected to be aligned to 32 bytes.
    /// </summary>
    public static void CopyTo32bAligned<T>(this NativeAlignedArray<T> src, Span<T> dst)
        where T : unmanaged

    {
        MemoryHelpers.CopyMemory32bAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(dst)),
            (nuint)dst.Length * (nuint)sizeof(T)
        );
    }

    /// <summary>
    /// Copies the contents of one native aligned array into another. Both containers are expected to be aligned to 64 bytes.
    /// </summary>
    public static void CopyTo64bAlignment<T>(this NativeAlignedArray<T> src, NativeAlignedArray<T> dst)
        where T : unmanaged
    {
        MemoryHelpers.CopyMemory64bAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref dst.Start),
            dst.Length * (nuint)sizeof(T)
        );
    }

    /// <summary>
    /// Copies the contents of one native aligned array into a span. Both containers are expected to be aligned to 64 bytes.
    /// </summary>
    public static void CopyTo64bAligned<T>(this NativeAlignedArray<T> src, Span<T> dst)
        where T : unmanaged

    {
        MemoryHelpers.CopyMemory64bAlignment
        (
            (T*)Unsafe.AsPointer(ref src.Start),
            src.Length * (nuint)sizeof(T),
            (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(dst)),
            (nuint)dst.Length * (nuint)sizeof(T)
        );
    }
}
