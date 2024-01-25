// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bundles.Native;

/// <summary>
/// Contains concerning code to operate on native memory.
/// </summary>
internal static unsafe class MemoryHelpers
{
    public static unsafe void CopyMemoryArbitraryAlignment(void* src, nuint lengthSource, void* dst, nuint lengthDestination) 
        => Buffer.MemoryCopy(src, dst, lengthDestination, nuint.Min(lengthSource, lengthDestination));

    public static unsafe void CopyMemory16bAlignment(void* src, nuint lengthSource, void* dst, nuint lengthDestination)
    {
        nuint length = nuint.Min(lengthSource, lengthDestination);

        if (length < 32)
        {
            CopyMemory16bAlignment0to31(src, dst, length);
        }
        else if ((src >= dst) || (((nuint)src + length) <= (nuint)dst))
        {
            CopyMemory16bAlignmentLargeV128NonOverlapping(src, dst, length);
        }
        else
        {
            CopyMemory16bAlignmentLargeV128Overlapping(src, dst, length);
        }
    }

    public static unsafe void CopyMemory32bAlignment(void* src, nuint lengthSource, void* dst, nuint lengthDestination)
    {
        nuint length = nuint.Min(lengthSource, lengthDestination);

        if (length < 32)
        {
            CopyMemory16bAlignment0to31(src, dst, length);
        }
        else if (length < 64)
        {
            CopyMemory32bAlignment32to63(src, dst, length);
        }
        else if ((src >= dst) || (((nuint)src + length) <= (nuint)dst))
        {
            if (Vector256.IsHardwareAccelerated)
            {
                CopyMemory32bAlignmentLargeV256NonOverlapping(src, dst, length);
            }
            else
            {
                CopyMemory16bAlignmentLargeV128NonOverlapping(src, dst, length);
            }
        }
        else
        {
            if (Vector256.IsHardwareAccelerated)
            {
                CopyMemory32bAlignmentLargeV256Overlapping(src, dst, length);
            }
            else
            {
                CopyMemory16bAlignmentLargeV128Overlapping(src, dst, length);
            }
        }
    }

    public static unsafe void CopyMemory64bAlignment(void* src, nuint lengthSource, void* dst, nuint lengthDestination)
    {
        nuint length = nuint.Min(lengthSource, lengthDestination);

        if (length < 32)
        {
            CopyMemory16bAlignment0to31(src, dst, length);
        }
        else if (length < 64)
        {
            CopyMemory32bAlignment32to63(src, dst, length);
        }
        else if ((src >= dst) || (((nuint)src + length) <= (nuint)dst))
        {
            if (Vector512.IsHardwareAccelerated)
            {
                CopyMemory64bAlignmentLargeV512NonOverlapping(src, dst, length);
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                CopyMemory32bAlignmentLargeV256NonOverlapping(src, dst, length);
            }
            else
            {
                CopyMemory16bAlignmentLargeV128NonOverlapping(src, dst, length);
            }
        }
        else
        {
            if (Vector512.IsHardwareAccelerated)
            {
                CopyMemory64bAlignmentLargeV512Overlapping(src, dst, length);
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                CopyMemory32bAlignmentLargeV256Overlapping(src, dst, length);
            }
            else
            {
                CopyMemory16bAlignmentLargeV128Overlapping(src, dst, length);
            }
        }
    }

    private static unsafe void CopyMemory16bAlignment0to31(void* src, void* dst, nuint length)
    {
        Debug.Assert(length <= 31);

        switch (length)
        {
            case 1:
            {
                byte value = Unsafe.Read<byte>(src);
                Unsafe.Write(dst, value);
                break;
            }

            case 2:
            {
                ushort value = Unsafe.Read<ushort>(src);
                Unsafe.Write(dst, value);
                break;
            }

            case 3:
            {
                ushort lower = Unsafe.Read<ushort>(src);
                ushort upper = Unsafe.ReadUnaligned<ushort>((byte*)src + 1);
                
                Unsafe.Write(dst, lower);
                Unsafe.WriteUnaligned((byte*)dst + 1, upper);
                break;
            }

            case 4:
            {
                uint value = Unsafe.Read<uint>(src);
                Unsafe.Write(dst, value);
                break;
            }

            case 5:
            case 6:
            case 7:
            {
                uint lower = Unsafe.Read<uint>(src);
                uint upper = Unsafe.ReadUnaligned<uint>((byte*)src + length - 4);

                Unsafe.Write(dst, lower);
                Unsafe.WriteUnaligned((byte*)dst + length - 4, upper);
                break;
            }

            case 8:
            {
                ulong value = Unsafe.Read<ulong>(src);
                Unsafe.Write(dst, value);
                break;
            }

            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            {
                ulong lower = Unsafe.Read<ulong>(src);
                ulong upper = Unsafe.ReadUnaligned<ulong>((byte*)src + length - 8);

                Unsafe.Write(dst, lower);
                Unsafe.WriteUnaligned((byte*)dst + length - 8, upper);
                break;
            }

            case 16:
            {
                Vector128<byte> value = Vector128.LoadAlignedNonTemporal((byte*)src);
                value.StoreAlignedNonTemporal((byte*)dst);
                break;
            }

            default:
            {
                Vector128<byte> lower = Vector128.LoadAlignedNonTemporal((byte*)src);
                Vector128<byte> upper = Vector128.Load((byte*)src + length - 16);

                lower.StoreAlignedNonTemporal((byte*)dst);
                upper.Store((byte*)dst + length - 16);
                break;
            }
        }
    }

    private static unsafe void CopyMemory32bAlignment32to63(void* src, void* dst, nuint length)
    {
        Debug.Assert(length is >= 32 and <= 63);

        if (Vector256.IsHardwareAccelerated)
        {
            switch (length)
            {
                case 32:
                {
                    Vector256<byte> value = Vector256.LoadAlignedNonTemporal((byte*)src);
                    value.StoreAlignedNonTemporal((byte*)dst);
                    break;
                }

                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                {
                    Vector256<byte> lower = Vector256.LoadAlignedNonTemporal((byte*)src);
                    Vector128<byte> upper = Vector128.Load((byte*)src + length - 16);

                    lower.StoreAlignedNonTemporal((byte*)dst);
                    upper.Store((byte*)dst + length - 16);
                    break;
                }

                case 48:
                {
                    Vector256<byte> lower = Vector256.LoadAlignedNonTemporal((byte*)src);
                    Vector128<byte> upper = Vector128.LoadAlignedNonTemporal((byte*)src + 32);

                    lower.StoreAlignedNonTemporal((byte*)dst);
                    upper.StoreAlignedNonTemporal((byte*)dst + 32);
                    break;
                }

                default:
                {
                    Vector256<byte> lower = Vector256.LoadAlignedNonTemporal((byte*)src);
                    Vector256<byte> upper = Vector256.Load((byte*)src + length - 32);

                    lower.StoreAlignedNonTemporal((byte*)dst);
                    upper.Store((byte*)dst + length - 32);
                    break;
                }
            }
        }
    }

    private static unsafe void CopyMemory16bAlignmentLargeV128NonOverlapping(void* src, void* dst, nuint length)
    {
        nuint index = 0;

        for (; index < length - 128; index += 128)
        {
            Vector128<byte> xmm0 = Vector128.LoadAlignedNonTemporal((byte*)src + index);
            Vector128<byte> xmm1 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 16);
            Vector128<byte> xmm2 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector128<byte> xmm3 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 48);
            Vector128<byte> xmm4 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector128<byte> xmm5 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 80);
            Vector128<byte> xmm6 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 96);
            Vector128<byte> xmm7 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 112);

            xmm0.StoreAlignedNonTemporal((byte*)dst + index);
            xmm1.StoreAlignedNonTemporal((byte*)dst + index + 16);
            xmm2.StoreAlignedNonTemporal((byte*)dst + index + 32);
            xmm3.StoreAlignedNonTemporal((byte*)dst + index + 48);
            xmm4.StoreAlignedNonTemporal((byte*)dst + index + 64);
            xmm5.StoreAlignedNonTemporal((byte*)dst + index + 80);
            xmm6.StoreAlignedNonTemporal((byte*)dst + index + 96);
            xmm7.StoreAlignedNonTemporal((byte*)dst + index + 112);
        }

        if (length - index >= 64)
        {
            Vector128<byte> xmm0 = Vector128.LoadAlignedNonTemporal((byte*)src + index);
            Vector128<byte> xmm1 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 16);
            Vector128<byte> xmm2 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector128<byte> xmm3 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 48);

            xmm0.StoreAlignedNonTemporal((byte*)dst + index);
            xmm1.StoreAlignedNonTemporal((byte*)dst + index + 16);
            xmm2.StoreAlignedNonTemporal((byte*)dst + index + 32);
            xmm3.StoreAlignedNonTemporal((byte*)dst + index + 48);

            index += 64;
        }

        if (length - index >= 32)
        {
            Vector128<byte> xmm0 = Vector128.LoadAlignedNonTemporal((byte*)src + index);
            Vector128<byte> xmm1 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 16);

            xmm0.StoreAlignedNonTemporal((byte*)dst + index);
            xmm1.StoreAlignedNonTemporal((byte*)dst + index + 16);

            index += 32;
        }

        CopyMemory16bAlignment0to31((byte*)src + index, (byte*)dst + index, length - index);
    }

    private static unsafe void CopyMemory32bAlignmentLargeV256NonOverlapping(void* src, void* dst, nuint length)
    {
        nuint index = 0;

        for (; index < length - 256; index += 256)
        {
            Vector256<byte> ymm0 = Vector256.LoadAlignedNonTemporal((byte*)src + index);
            Vector256<byte> ymm1 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector256<byte> ymm2 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector256<byte> ymm3 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 96);
            Vector256<byte> ymm4 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 128);
            Vector256<byte> ymm5 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 160);
            Vector256<byte> ymm6 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 192);
            Vector256<byte> ymm7 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 224);

            ymm0.StoreAlignedNonTemporal((byte*)dst + index);
            ymm1.StoreAlignedNonTemporal((byte*)dst + index + 32);
            ymm2.StoreAlignedNonTemporal((byte*)dst + index + 64);
            ymm3.StoreAlignedNonTemporal((byte*)dst + index + 96);
            ymm4.StoreAlignedNonTemporal((byte*)dst + index + 128);
            ymm5.StoreAlignedNonTemporal((byte*)dst + index + 160);
            ymm6.StoreAlignedNonTemporal((byte*)dst + index + 192);
            ymm7.StoreAlignedNonTemporal((byte*)dst + index + 224);
        }

        if (length - index >= 128)
        {
            Vector256<byte> ymm0 = Vector256.LoadAlignedNonTemporal((byte*)src + index);
            Vector256<byte> ymm1 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector256<byte> ymm2 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector256<byte> ymm3 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 96);

            ymm0.StoreAlignedNonTemporal((byte*)dst + index);
            ymm1.StoreAlignedNonTemporal((byte*)dst + index + 32);
            ymm2.StoreAlignedNonTemporal((byte*)dst + index + 64);
            ymm3.StoreAlignedNonTemporal((byte*)dst + index + 96);

            index += 128;
        }

        if (length - index >= 64)
        {
            Vector256<byte> ymm0 = Vector256.LoadAlignedNonTemporal((byte*)src + index);
            Vector256<byte> ymm1 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 32);

            ymm0.StoreAlignedNonTemporal((byte*)dst + index);
            ymm1.StoreAlignedNonTemporal((byte*)dst + index + 32);

            index += 64;
        }

        if (length - index >= 32)
        {
            CopyMemory32bAlignment32to63((byte*)src + index, (byte*)dst + index, length - index);
        }
        else
        {
            CopyMemory16bAlignment0to31((byte*)src + index, (byte*)dst + index, length - index);
        }
    }

    private static unsafe void CopyMemory64bAlignmentLargeV512NonOverlapping(void* src, void* dst, nuint length)
    {
        nuint index = 0;

        for (; index < length - 512; index += 512)
        {
            Vector512<byte> zmm0 = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            Vector512<byte> zmm1 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector512<byte> zmm2 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 128);
            Vector512<byte> zmm3 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 192);
            Vector512<byte> zmm4 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 256);
            Vector512<byte> zmm5 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 320);
            Vector512<byte> zmm6 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 384);
            Vector512<byte> zmm7 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 448);

            zmm0.StoreAlignedNonTemporal((byte*)dst + index);
            zmm1.StoreAlignedNonTemporal((byte*)dst + index + 64);
            zmm2.StoreAlignedNonTemporal((byte*)dst + index + 128);
            zmm3.StoreAlignedNonTemporal((byte*)dst + index + 192);
            zmm4.StoreAlignedNonTemporal((byte*)dst + index + 256);
            zmm5.StoreAlignedNonTemporal((byte*)dst + index + 320);
            zmm6.StoreAlignedNonTemporal((byte*)dst + index + 384);
            zmm7.StoreAlignedNonTemporal((byte*)dst + index + 448);
        }

        if (length - index >= 256)
        {
            Vector512<byte> zmm0 = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            Vector512<byte> zmm1 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector512<byte> zmm2 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 128);
            Vector512<byte> zmm3 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 192);

            zmm0.StoreAlignedNonTemporal((byte*)dst + index);
            zmm1.StoreAlignedNonTemporal((byte*)dst + index + 64);
            zmm2.StoreAlignedNonTemporal((byte*)dst + index + 128);
            zmm3.StoreAlignedNonTemporal((byte*)dst + index + 192);

            index += 256;
        }

        if (length - index >= 128)
        {
            Vector512<byte> zmm0 = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            Vector512<byte> zmm1 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 64);

            zmm0.StoreAlignedNonTemporal((byte*)dst + index);
            zmm1.StoreAlignedNonTemporal((byte*)dst + index + 64);

            index += 128;
        }

        if (length - index >= 64)
        {
            Vector512<byte> value = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            value.StoreAlignedNonTemporal((byte*)dst + index);

            index += 64;
        }

        if (length - index >= 32)
        {
            CopyMemory32bAlignment32to63((byte*)src + index, (byte*)dst + index, length - index);
        }
        else
        {
            CopyMemory16bAlignment0to31((byte*)src + index, (byte*)dst + index, length - index);
        }
    }

    private static unsafe void CopyMemory16bAlignmentLargeV128Overlapping(void* src, void* dst, nuint length)
    {
        nuint index = length - 128;

        for (; index >= 128; index -= 128)
        {
            Vector128<byte> xmm0 = Vector128.LoadAlignedNonTemporal((byte*)src + index);
            Vector128<byte> xmm1 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 16);
            Vector128<byte> xmm2 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector128<byte> xmm3 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 48);
            Vector128<byte> xmm4 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector128<byte> xmm5 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 80);
            Vector128<byte> xmm6 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 96);
            Vector128<byte> xmm7 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 112);

            xmm0.StoreAlignedNonTemporal((byte*)dst + index);
            xmm1.StoreAlignedNonTemporal((byte*)dst + index + 16);
            xmm2.StoreAlignedNonTemporal((byte*)dst + index + 32);
            xmm3.StoreAlignedNonTemporal((byte*)dst + index + 48);
            xmm4.StoreAlignedNonTemporal((byte*)dst + index + 64);
            xmm5.StoreAlignedNonTemporal((byte*)dst + index + 80);
            xmm6.StoreAlignedNonTemporal((byte*)dst + index + 96);
            xmm7.StoreAlignedNonTemporal((byte*)dst + index + 112);
        }

        if (index >= 64)
        {
            Vector128<byte> xmm0 = Vector128.LoadAlignedNonTemporal((byte*)src + index);
            Vector128<byte> xmm1 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 16);
            Vector128<byte> xmm2 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector128<byte> xmm3 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 48);

            xmm0.StoreAlignedNonTemporal((byte*)dst + index);
            xmm1.StoreAlignedNonTemporal((byte*)dst + index + 16);
            xmm2.StoreAlignedNonTemporal((byte*)dst + index + 32);
            xmm3.StoreAlignedNonTemporal((byte*)dst + index + 48);

            index -= 64;
        }

        if (index >= 32)
        {
            Vector128<byte> xmm0 = Vector128.LoadAlignedNonTemporal((byte*)src + index);
            Vector128<byte> xmm1 = Vector128.LoadAlignedNonTemporal((byte*)src + index + 16);

            xmm0.StoreAlignedNonTemporal((byte*)dst + index);
            xmm1.StoreAlignedNonTemporal((byte*)dst + index + 16);

            index -= 32;
        }

        CopyMemory16bAlignment0to31((byte*)src, (byte*)dst, index);
    }

    private static unsafe void CopyMemory32bAlignmentLargeV256Overlapping(void* src, void* dst, nuint length)
    {
        nuint index = length - 256;

        for (; index >= 256; index -= 256)
        {
            Vector256<byte> ymm0 = Vector256.LoadAlignedNonTemporal((byte*)src + index);
            Vector256<byte> ymm1 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector256<byte> ymm2 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector256<byte> ymm3 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 96);
            Vector256<byte> ymm4 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 128);
            Vector256<byte> ymm5 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 160);
            Vector256<byte> ymm6 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 192);
            Vector256<byte> ymm7 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 224);

            ymm0.StoreAlignedNonTemporal((byte*)dst + index);
            ymm1.StoreAlignedNonTemporal((byte*)dst + index + 32);
            ymm2.StoreAlignedNonTemporal((byte*)dst + index + 64);
            ymm3.StoreAlignedNonTemporal((byte*)dst + index + 96);
            ymm4.StoreAlignedNonTemporal((byte*)dst + index + 128);
            ymm5.StoreAlignedNonTemporal((byte*)dst + index + 160);
            ymm6.StoreAlignedNonTemporal((byte*)dst + index + 192);
            ymm7.StoreAlignedNonTemporal((byte*)dst + index + 224);
        }

        if (index >= 128)
        {
            Vector256<byte> ymm0 = Vector256.LoadAlignedNonTemporal((byte*)src + index);
            Vector256<byte> ymm1 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 32);
            Vector256<byte> ymm2 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector256<byte> ymm3 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 96);

            ymm0.StoreAlignedNonTemporal((byte*)dst + index);
            ymm1.StoreAlignedNonTemporal((byte*)dst + index + 32);
            ymm2.StoreAlignedNonTemporal((byte*)dst + index + 64);
            ymm3.StoreAlignedNonTemporal((byte*)dst + index + 96);

            index -= 128;
        }

        if (index >= 64)
        {
            Vector256<byte> ymm0 = Vector256.LoadAlignedNonTemporal((byte*)src + index);
            Vector256<byte> ymm1 = Vector256.LoadAlignedNonTemporal((byte*)src + index + 32);

            ymm0.StoreAlignedNonTemporal((byte*)dst + index);
            ymm1.StoreAlignedNonTemporal((byte*)dst + index + 32);

            index -= 64;
        }

        if (index >= 32)
        {
            CopyMemory32bAlignment32to63(src, dst, index);
        }
        else
        {
            CopyMemory16bAlignment0to31(src, dst, index);
        }
    }

    private static unsafe void CopyMemory64bAlignmentLargeV512Overlapping(void* src, void* dst, nuint length)
    {
        nuint index = length - 512;

        for (; index >= 512; index -= 512)
        {
            Vector512<byte> zmm0 = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            Vector512<byte> zmm1 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector512<byte> zmm2 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 128);
            Vector512<byte> zmm3 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 192);
            Vector512<byte> zmm4 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 256);
            Vector512<byte> zmm5 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 320);
            Vector512<byte> zmm6 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 384);
            Vector512<byte> zmm7 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 448);

            zmm0.StoreAlignedNonTemporal((byte*)dst + index);
            zmm1.StoreAlignedNonTemporal((byte*)dst + index + 64);
            zmm2.StoreAlignedNonTemporal((byte*)dst + index + 128);
            zmm3.StoreAlignedNonTemporal((byte*)dst + index + 192);
            zmm4.StoreAlignedNonTemporal((byte*)dst + index + 256);
            zmm5.StoreAlignedNonTemporal((byte*)dst + index + 320);
            zmm6.StoreAlignedNonTemporal((byte*)dst + index + 384);
            zmm7.StoreAlignedNonTemporal((byte*)dst + index + 448);
        }

        if (index >= 256)
        {
            Vector512<byte> zmm0 = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            Vector512<byte> zmm1 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 64);
            Vector512<byte> zmm2 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 128);
            Vector512<byte> zmm3 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 192);

            zmm0.StoreAlignedNonTemporal((byte*)dst + index);
            zmm1.StoreAlignedNonTemporal((byte*)dst + index + 64);
            zmm2.StoreAlignedNonTemporal((byte*)dst + index + 128);
            zmm3.StoreAlignedNonTemporal((byte*)dst + index + 192);

            index -= 256;
        }

        if (index >= 128)
        {
            Vector512<byte> zmm0 = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            Vector512<byte> zmm1 = Vector512.LoadAlignedNonTemporal((byte*)src + index + 64);

            zmm0.StoreAlignedNonTemporal((byte*)dst + index);
            zmm1.StoreAlignedNonTemporal((byte*)dst + index + 64);

            index -= 128;
        }

        if (index >= 64)
        {
            Vector512<byte> value = Vector512.LoadAlignedNonTemporal((byte*)src + index);
            value.StoreAlignedNonTemporal((byte*)dst + index);

            index -= 64;
        }

        if (index >= 32)
        {
            CopyMemory32bAlignment32to63(src, dst, index);
        }
        else
        {
            CopyMemory16bAlignment0to31(src, dst, index);
        }
    }

    public static unsafe void ZeroMemoryArbitraryAlignment(void* address, nuint length)
    {
        if(length < int.MaxValue)
        {
            Span<byte> span = new(address, (int)length);
            span.Clear();

            return;
        }

        for (nuint i = 0; i < length; i += int.MaxValue)
        {
            Span<byte> span = new((byte*)address + i, int.Min(int.MaxValue, (int)(length - i)));
            span.Clear();
        }
    }

    public static unsafe void ZeroMemory16bAlignment(void* address, nuint length)
    {
        if (length > 31)
        {
            ZeroMemory16bAlignmentLargeV128(address, length);
        }
        else
        {
            ZeroMemory16bAlignment0to31(address, length);
        }
    }

    public static unsafe void ZeroMemory32bAlignment(void* address, nuint length)
    {
        if (length > 63)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                ZeroMemory32bAlignmentLargeV256(address, length);
            }
            else
            {
                ZeroMemory16bAlignmentLargeV128(address, length);
            }
        }
        else
        {
            ZeroMemory16bAlignment(address, length);
        }
    }

    public static unsafe void ZeroMemory64bAlignment(void* address, nuint length)
    {
        if (length > 63)
        {
            if (Avx512F.IsSupported)
            {
                ZeroMemory64bAlignmentLargeV512(address, length);
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                ZeroMemory32bAlignmentLargeV256(address, length);
            }
            else
            {
                ZeroMemory16bAlignmentLargeV128(address, length);
            }
        }
        else
        {
            ZeroMemory16bAlignment(address, length);
        }
    }

    /// <summary>
    /// Zeroes out 16-byte-or-greater-aligned memory, 0-31 bytes in length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ZeroMemory16bAlignment0to31(void* address, nuint length)
    {
        Debug.Assert(length <= 31);

        switch (length)
        {
            case 1:
                Unsafe.Write<byte>(address, default);
                break;

            case 2:
                Unsafe.Write<ushort>(address, default);
                break;

            case 3:
                Unsafe.Write<ushort>(address, default);
                Unsafe.WriteUnaligned<ushort>((byte*)address + 1, default);
                break;

            case 4:
                Unsafe.Write<uint>(address, default);
                break;

            case 5:
            case 6:
            case 7:
                Unsafe.Write<uint>(address, default);
                Unsafe.WriteUnaligned<uint>((byte*)address + length - 4, default);
                break;

            case 8:
                Unsafe.Write<ulong>(address, default);
                break;

            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
                Unsafe.Write<ulong>(address, default);
                Unsafe.WriteUnaligned<ulong>((byte*)address + length - 8, default);
                break;

            case 16:
                default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address);
                break;

            default:
                default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address);
                default(Vector128<byte>).Store((byte*)address + length - 16);
                break;
        }
    }

    /// <summary>
    /// Zeroes out 32-byte-or-greater-aligned memory, 32-63 bytes in length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ZeroMemory32bAlignment32to63(void* address, nuint length)
    {
        Debug.Assert(length is >= 32 and <= 63);

        if (Vector256.IsHardwareAccelerated)
        {
            switch (length)
            {
                case 32:
                    default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address);
                    break;

                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                    default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address);
                    default(Vector128<byte>).Store((byte*)address + length - 16);
                    break;

                case 48:
                    default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address);
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + 16);
                    break;

                default:
                    default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address);
                    default(Vector256<byte>).Store((byte*)address + length - 32);
                    break;
            }
        }
        else
        {
            switch (length)
            {
                case 32:
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address);
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + 16);
                    break;

                case 33:
                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                case 39:
                case 40:
                case 41:
                case 42:
                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address);
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + 16);
                    default(Vector128<byte>).Store((byte*)address + length - 16);
                    break;

                case 48:
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address);
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + 16);
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + 32);
                    break;

                default:
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address);
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + 16);
                    default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + 32);
                    default(Vector128<byte>).Store((byte*)address + length - 16);
                    break;
            }
        }
    }

    private static unsafe void ZeroMemory16bAlignmentLargeV128(void* address, nuint length)
    {
        nuint index = 0;

        for (; index < length - 128; index += 128)
        {
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 16);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 32);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 48);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 64);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 80);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 96);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 112);
        }

        if (length - index >= 64)
        {
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 16);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 32);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 48);

            index += 64;
        }

        if (length - index >= 32)
        {
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index);
            default(Vector128<byte>).StoreAlignedNonTemporal((byte*)address + index + 16);

            index += 32;
        }

        ZeroMemory16bAlignment0to31((byte*)address + index, length - index);
    }

    private static unsafe void ZeroMemory32bAlignmentLargeV256(void* address, nuint length)
    {
        nuint index = 0;

        for (; index < length - 256; index += 256)
        {
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 32);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 64);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 96);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 128);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 160);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 192);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 224);
        }

        if (length - index >= 128)
        {
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 32);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 64);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 96);

            index += 128;
        }

        if (length - index >= 64)
        {
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index);
            default(Vector256<byte>).StoreAlignedNonTemporal((byte*)address + index + 32);

            index += 64;
        }

        if (length - index >= 32)
        {
            ZeroMemory32bAlignment32to63((byte*)address + index, length - index);
        }
        else
        {
            ZeroMemory16bAlignment0to31((byte*)address + index, length - index);
        }
    }

    private static unsafe void ZeroMemory64bAlignmentLargeV512(void* address, nuint length)
    {
        nuint index = 0;

        for (; index < length - 512; index += 512)
        {
            Avx512F.StoreAlignedNonTemporal((byte*)address + index, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 64, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 128, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 192, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 256, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 320, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 384, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 448, default);
        }

        if (length - index >= 256)
        {
            Avx512F.StoreAlignedNonTemporal((byte*)address + index, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 64, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 128, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 192, default);

            index += 256;
        }

        if (length - index >= 128)
        {
            Avx512F.StoreAlignedNonTemporal((byte*)address + index, default);
            Avx512F.StoreAlignedNonTemporal((byte*)address + index + 64, default);

            index += 128;
        }

        if (length - index >= 64)
        {
            Avx512F.StoreAlignedNonTemporal((byte*)address + index, default);

            index += 64;
        }

        if (length - index >= 32)
        {
            ZeroMemory32bAlignment32to63((byte*)address + index, length - index);
        }
        else
        {
            ZeroMemory16bAlignment0to31((byte*)address + index, length - index);
        }
    }
}
