// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Management.Automation.Unicode
{
    /// <summary>
    /// </summary>
    internal static partial class SimpleCaseFolding

    {
        private static ref ushort s_MapLevel1 => ref MapLevel1[0];
        private static ref char s_refMapData => ref MapData[0];
        private static ref ushort s_refMapSurrogateLevel1 => ref MapSurrogateLevel1[0];
        private static ref (char, char) s_refMapSurrogateData => ref MapSurrogateData[0];

        /// <summary>
        /// </summary>
        internal static char SimpleCaseFold(char c)
        {
            if (c <= 0x5ff)
            {
                return (char)MapBelow5FF[c];
            }

            //var v = L1[c >> 8];
            //var ch = L3[v + (c & 0xFF)];
            var v = Unsafe.Add(ref s_MapLevel1, c >> 8);
            var ch = Unsafe.Add(ref s_refMapData, v + (c & 0xFF));

            return ch == 0 ? c : ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SimpleCaseFoldCompareAbove05ff(char c1, char c2, ref ushort refMapLevel1, ref char refMapData)
        {
            var v1 =  Unsafe.Add(ref refMapLevel1, c1 >> 8);
            var ch1 = Unsafe.Add(ref refMapData, v1 + (c1 & 0xFF));
            if (ch1 == 0)
            {
                ch1 = c1;
            }

            var v2 =  Unsafe.Add(ref refMapLevel1, c2 >> 8);
            var ch2 = Unsafe.Add(ref refMapData, v2 + (c2 & 0xFF));
            if (ch2 == 0)
            {
                ch2 = c2;
            }

            return ch1 - ch2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SimpleCaseFoldCompareSurrogates(char c1, char c2, ref ushort refMapLevel1, ref int refMapData)
        {
            var v1 =  Unsafe.Add(ref refMapLevel1, c1 >> 8);
            var ch1 = Unsafe.Add(ref refMapData, v1 + (c1 & 0xFF));
            if (ch1 == 0)
            {
                ch1 = c1;
            }

            var v2 =  Unsafe.Add(ref refMapLevel1, c2 >> 8);
            var ch2 = Unsafe.Add(ref refMapData, v2 + (c2 & 0xFF));
            if (ch2 == 0)
            {
                ch2 = c2;
            }

            return ch1 - ch2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SimpleCaseFoldCompare(char c1, char c2)
        {
            ref ushort refMapLevel1 = ref s_MapLevel1;
            ref char refMapData = ref s_refMapData;

            var v1 =  Unsafe.Add(ref refMapLevel1, c1 >> 8);
            var ch1 = Unsafe.Add(ref refMapData, v1 + (c1 & 0xFF));
            if (ch1 == 0)
            {
                ch1 = c1;
            }
            var v2 =  Unsafe.Add(ref refMapLevel1, c2 >> 8);
            var ch2 = Unsafe.Add(ref refMapData, v2 + (c2 & 0xFF));
            if (ch2 == 0)
            {
                ch2 = c2;
            }

            return ch1 - ch2;
        }

        /// <summary>
        /// Compare strings using Unicode Simple Case Folding.
        /// </summary>
        internal static int CompareUsingSimpleCaseFolding(this string strA, string strB)
        {
            if (object.ReferenceEquals(strA, strB))
            {
                return 0;
            }

            if (strA == null)
            {
                return -1;
            }

            if (strB == null)
            {
                return 1;
            }

            var spanA = strA.AsSpan();
            ref char refA = ref MemoryMarshal.GetReference(spanA);
            var lengthA = spanA.Length;
            var spanB = strB.AsSpan();
            ref char refB = ref MemoryMarshal.GetReference(spanB);
            var lengthB = spanB.Length;

            return CompareUsingSimpleCaseFolding(ref refA, lengthA, ref refB, lengthB);
        }

        /// <summary>
        /// Compare spans using Unicode Simple Case Folding.
        /// </summary>
        internal static int CompareUsingSimpleCaseFolding(this ReadOnlySpan<char> spanA, ReadOnlySpan<char> spanB)
        {
            ref char refA = ref MemoryMarshal.GetReference(spanA);
            ref char refB = ref MemoryMarshal.GetReference(spanB);

            return CompareUsingSimpleCaseFolding(ref refA, spanA.Length, ref refB, spanB.Length);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareUsingSimpleCaseFolding(ref char refA, int lengthA, ref char refB, int lengthB)
        {
            var result = lengthA - lengthB;
            var length = Math.Min(lengthA, lengthB);

            //var l0AsSpan = MapBelow5FF.AsSpan();
            //ref char refMapBelow5FF = ref MemoryMarshal.GetReference(l0AsSpan);
            ref char refMapBelow5FF = ref MapBelow5FF[0];

            // For char below 0x5ff use fastest 1-level mapping.
            while (length != 0 && refA <= MaxChar && refB <= MaxChar)
            {
                var compare1 = Unsafe.Add(ref refMapBelow5FF, refA) - Unsafe.Add(ref refMapBelow5FF, refB);
                if (compare1 == 0)
                {
                    length--;
                    refA = ref Unsafe.Add(ref refA, 1);
                    refB = ref Unsafe.Add(ref refB, 1);
                }
                else
                {
                    return compare1;
                }
            }

            if (length == 0)
            {
                return result;
            }

            ref ushort refMapLevel1 = ref s_MapLevel1;
            ref char refMapData = ref s_refMapData;

            // We catch a char above 0x5ff.
            // Process it with more slow two-level mapping.
            while (length != 0 && !IsSurrogate(refA) && !IsSurrogate(refB))
            {
                var compare2 = SimpleCaseFoldCompareAbove05ff(refA, refB, ref refMapLevel1, ref refMapData);

                if (compare2 == 0)
                {
                    length--;
                    refA = ref Unsafe.Add(ref refA, 1);
                    refB = ref Unsafe.Add(ref refB, 1);
                }
                else
                {
                    return compare2;
                }
            }


            if (length == 0)
            {
                return result;
            }

            return result;
/*
            return CompareUsingSimpleCaseFolding(
                        ref refA,
                        ref refB,
                        result,
                        length,
                        ref refMapBelow5FF);
*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CompareUsingSimpleCaseFolding(
            ref char refA,
            ref char refB,
            int result, int length,
            ref char refMapBelow5FF)
        {
            ref ushort refMapLevel1 = ref s_MapLevel1;
            ref char refMapData = ref s_refMapData;

            // We catch a char above 0x5ff.
            // Process it with more slow two-level mapping.
            while (length != 0 && !IsSurrogate(refA) && !IsSurrogate(refB))
            {
                var compare2 = SimpleCaseFoldCompareAbove05ff(refA, refB, ref refMapLevel1, ref refMapData);

                if (compare2 == 0)
                {
                    length--;
                    refA = ref Unsafe.Add(ref refA, 1);
                    refB = ref Unsafe.Add(ref refB, 1);
                }
                else
                {
                    return compare2;
                }
            }

            ref ushort refMapSurrogateLevel1 = ref s_refMapSurrogateLevel1;
            ref int refMapSurrogateData = ref Unsafe.As<(char, char), int>(ref s_refMapSurrogateData);

            while (length != 0)
            {
                // We catch a high or low surrogate.
                // Process it and fallback to fastest options.
                var c1 = refA;
                var isHighSurrogateA = IsHighSurrogate(c1);
                var c2 = refB;
                var isHighSurrogateB = IsHighSurrogate(c2);

                if (isHighSurrogateA && isHighSurrogateB)
                {
                    // Both char is high surrogates.
                    // Get low surrogates.
                    length--;
                    if (length == 0)
                    {
                        // No low surrogate - throw?
                        return -1;
                    }

                    refA = ref Unsafe.Add(ref refA, 1);
                    var c1Low = refA;
                    refB = ref Unsafe.Add(ref refB, 1);
                    var c2Low = refB;

                    if (!IsLowSurrogate(c1Low) || !IsLowSurrogate(c2Low))
                    {
                        // No low surrogate - throw?
                        return -1;
                    }

                    // The index is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                    var index1 = ((c1 - HIGH_SURROGATE_START) * 0x400) + (c1Low - LOW_SURROGATE_START);
                    var index2 = ((c2 - HIGH_SURROGATE_START) * 0x400) + (c2Low - LOW_SURROGATE_START);

                    var compare4 = SimpleCaseFoldCompareSurrogates((char)index1, (char)index2, ref refMapSurrogateLevel1, ref refMapSurrogateData);;

                    if (compare4 != 0)
                    {
                        return compare4;
                    }

                    // Move to next char.
                    length--;
                    refA = ref Unsafe.Add(ref refA, 1);
                    refB = ref Unsafe.Add(ref refB, 1);
                }
                else
                {
                    if (isHighSurrogateA || isHighSurrogateB)
                    {
                        // Only one char is a surrogate.
                        return isHighSurrogateA ? 1 : -1;
                    }
                    else
                    {
                        // We expect a high surrogate but get a low surrogate - throw?
                        return -1;
                    }
                }

                // Both char is not surrogates. 'length--' was already done.
                while (length != 0 && refA <= MaxChar && refB <= MaxChar)
                {
                    var compare1 = Unsafe.Add(ref refMapBelow5FF, refA) - Unsafe.Add(ref refMapBelow5FF, refB);
                    if (compare1 == 0)
                    {
                        length--;
                        refA = ref Unsafe.Add(ref refA, 1);
                        refB = ref Unsafe.Add(ref refB, 1);
                    }
                    else
                    {
                        return compare1;
                    }
                }

                if (length == 0)
                {
                    return result;
                }

                while (length != 0 && !IsSurrogate(refA) && !IsSurrogate(refB))
                {
                    var compare2 = SimpleCaseFoldCompareAbove05ff(refA, refB, ref refMapLevel1, ref refMapData);

                    if (compare2 == 0)
                    {
                        length--;
                        refA = ref Unsafe.Add(ref refA, 1);
                        refB = ref Unsafe.Add(ref refB, 1);
                    }
                    else
                    {
                        return compare2;
                    }
                }

                if (length == 0)
                {
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        ///  Simple case folding of the string.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>
        /// Returns folded string.
        /// </returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SimpleCaseFold(this string source)
        {
            return string.Create(
                source.Length,
                source,
                (chars, sourceString) =>
                {
                    SpanSimpleCaseFold(chars, sourceString);
                });
        }

        /// <summary>
        ///  Simple case folding of the Span&lt;char&gt;.
        /// </summary>
        /// <param name="source">Source string.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SimpleCaseFold(this Span<char> source)
        {
            SpanSimpleCaseFold(source, source);
        }

        /// <summary>
        ///  Simple case folding of the ReadOnlySpan&lt;char&gt;.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <returns>
        /// Returns folded string.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<char> SimpleCaseFold(this ReadOnlySpan<char> source)
        {
            Span<char> destination = new char[source.Length];

            SpanSimpleCaseFold(destination, source);

            return destination;
        }

        /// <summary>
        /// </summary>
        public static void SpanSimpleCaseFold(Span<char> destination, ReadOnlySpan<char> source)
        {
            //Diagnostics.Assert(destination.Length >= source.Length, "Destination span length must be equal or greater then source span length.");
            ref char res = ref MemoryMarshal.GetReference(destination);
            ref char src = ref MemoryMarshal.GetReference(source);

            var length = source.Length;
            int i = 0;
            var ch = src;

            for (; i < length; i++)
            {
                //var ch = source[i];
                ch = Unsafe.Add(ref src, i);

                if (IsAscii(ch))
                {
                    if ((uint)(ch - 'A') <= (uint)('Z' - 'A'))
                    {
                        //destination[i] = (char)(ch | 0x20);
                        Unsafe.Add(ref res, i) = (char)(ch | 0x20);
                    }
                    else
                    {
                         //destination[i] = ch;
                         Unsafe.Add(ref res, i) = ch;
                    }

                    continue;
                }

                if (!IsSurrogate(ch))
                {
                    //destination[i] = (char)s_simpleCaseFoldingTableBMPane1[ch];
                    //Unsafe.Add(ref res, i) = s_simpleCaseFoldingTableBMPane1[ch];
                    //Unsafe.Add(ref res, i) = simpleCaseFoldingTableBMPane1[ch];
                    Unsafe.Add(ref res, i) = SimpleCaseFold(ch);
                }
                else
                {
                    if ((i + 1) < length)
                    {
                        var ch2 = Unsafe.Add(ref src, 1);
                        if ((ch2 >= LOW_SURROGATE_START) && (ch2 <= LOW_SURROGATE_END))
                        {
                            // The index is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                            // We subtract 0x10000 because we packed Plane01 (from 65536 to 131071)
                            // to an array with size uint (index from 0 to 65535).
                            var index = ((ch - HIGH_SURROGATE_START) * 0x400) + (ch2 - LOW_SURROGATE_START);

                            // The utf32 is Utf32 - 0x10000 (UNICODE_PLANE01_START)
                            var utf32 = SimpleCaseFold((char)index);
                            Unsafe.Add(ref res, i) = (char)((utf32 / 0x400) + (int)HIGH_SURROGATE_START);
                            i++;
                            Unsafe.Add(ref res, i) = (char)((utf32 % 0x400) + (int)LOW_SURROGATE_START);
                        }
                        else
                        {
                            // Broken unicode - throw?
                            // We expect a low surrogate on (i + 1) position but get a full char
                            // so we copy a high surrogate and convert the full char.
                            Unsafe.Add(ref res, i) = ch;
                            i++;
                            Unsafe.Add(ref res, i) = SimpleCaseFold(ch);
                        }
                    }
                    else
                    {
                        // Broken unicode - throw?
                        // We catch a surrogate on last position but we had to process it on previous step (i-1)
                        // so we copy the surrogate.
                        Unsafe.Add(ref res, i) = ch;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAscii(char c)
        {
            return c < 0x80;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsHighSurrogate(char c)
        {
            return (uint)(c - HIGH_SURROGATE_START) <= (uint)(HIGH_SURROGATE_END - HIGH_SURROGATE_START);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowSurrogate(char c)
        {
            return (uint)(c - LOW_SURROGATE_START) <= (uint)(LOW_SURROGATE_END - LOW_SURROGATE_START);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSurrogate(char c)
        {
            return (uint)(c - HIGH_SURROGATE_START) <= (uint)(LOW_SURROGATE_END - HIGH_SURROGATE_START);
        }

        /// <summary>
        /// Search the char position in the string with simple case folding.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="ch">Char to search.</param>
        /// <returns>
        /// Returns an index the char in the string or -1 if not found.
        /// </returns>
        public static int IndexOfFolded(this string source, char ch)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return IndexOfFolded(source.AsSpan(), ch);
        }

        /// <summary>
        /// Search the char position in the ReadOnlySpan&lt;char&gt; with simple case folding.
        /// </summary>
        /// <param name="source">Source string.</param>
        /// <param name="ch">Char to search.</param>
        /// <returns>
        /// Returns an index the char in the ReadOnlySpan&lt;char&gt; or -1 if not found.
        /// </returns>
        public static int IndexOfFolded(this ReadOnlySpan<char> source, char ch)
        {
            var foldedChar = SimpleCaseFold(ch);

            for (int i = 0; i < source.Length; i++)
            {
                if (SimpleCaseFold(source[i]) == foldedChar)
                {
                    return i;
                }
            }

            return -1;
        }

        internal const char MaxChar = (char)0x5ff;
        internal const char HIGH_SURROGATE_START = '\ud800';
        internal const char HIGH_SURROGATE_END = '\udbff';
        internal const char LOW_SURROGATE_START = '\udc00';
        internal const char LOW_SURROGATE_END = '\udfff';
        internal const int HIGH_SURROGATE_RANGE = 0x3FF;
    }

    /// <summary>
    /// String comparer with simple case folding.
    /// </summary>
    public class StringComparerUsingSimpleCaseFolding : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
    {
        // Based on CoreFX StringComparer code

        /// <summary>
        /// Initializes a new instance of the <see cref="StringComparerUsingSimpleCaseFolding"/> class.
        /// </summary>
        public StringComparerUsingSimpleCaseFolding()
        {
        }

        /// <summary>
        /// IComparer.Compare() implementation.
        /// </summary>
        /// <param name="x">Object to compare.</param>
        /// <param name="y">Object to compare.</param>
        /// <returns>
        /// Returns 0 - if equal, -1 - if x &lt; y, +1 - if x &gt; y.
        /// </returns>
        public int Compare(object x, object y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (x is string sa && y is string sb)
            {
                return SimpleCaseFolding.CompareUsingSimpleCaseFolding(sa, sb);
            }

            if (x is IComparable ia)
            {
                return ia.CompareTo(y);
            }

            throw new ArgumentException("SR.Argument_ImplementIComparable");
        }

        /// <summary>
        /// IEqualityComparer.Equal() implementation.
        /// </summary>
        /// <param name="x">Object to compare.</param>
        /// <param name="y">Object to compare.</param>
        /// <returns>
        /// Returns true if equal.
        /// </returns>
        public new bool Equals(object x, object y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x is string sa && y is string sb)
            {
                return Equals(sa, sb);
            }

            return x.Equals(y);
        }

        /// <summary>
        /// IEqualityComparer.GetHashCode() implementation.
        /// </summary>
        /// <param name="obj">Object for which to get a hash.</param>
        /// <returns>
        /// Returns a hash code.
        /// </returns>
        public int GetHashCode(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj is string s)
            {
                return GetHashCodeSimpleCaseFolding(s);
            }

            return obj.GetHashCode();
        }

        private static int GetHashCodeSimpleCaseFolding(string source)
        {
            //Diagnostics.Assert(source != null, "source must not be null");

            // Do not allocate on the stack if string is empty
            if (source.Length == 0)
            {
                return source.GetHashCode();
            }

            char[] borrowedArr = null;
            Span<char> span = source.Length <= 255 ?
                stackalloc char[source.Length] :
                (borrowedArr = ArrayPool<char>.Shared.Rent(source.Length));

            SimpleCaseFolding.SpanSimpleCaseFold(span, source);

            int hash = HashByteArray(MemoryMarshal.AsBytes(span));

            // Return the borrowed array if necessary.
            if (borrowedArr != null)
            {
                ArrayPool<char>.Shared.Return(borrowedArr);
            }

            return hash;
        }

        // The code come from CoreFX SqlBinary.HashByteArray()
        internal static int HashByteArray(ReadOnlySpan<byte> rgbValue)
        {
            int length = rgbValue.Length;

            if (length <= 0)
            {
                return 0;
            }

            int ulValue = DefaultSeed;
            int ulHi;

            // Size of CRC window (hashing bytes, ssstr, sswstr, numeric)
            const int XcbCrcWindow = 4;

            // const int IntShiftVal = (sizeof ulValue) * (8*sizeof(char)) - XcbCrcWindow;
            const int IntShiftVal = (4 * 8) - XcbCrcWindow;

            for (int i = 0; i < length; i++)
            {
                ulHi = (ulValue >> IntShiftVal) & 0xff;
                ulValue <<= XcbCrcWindow;
                ulValue = ulValue ^ rgbValue[i] ^ ulHi;
            }

            return ulValue;
        }

        private static int DefaultSeed { get; } = GenerateSeed();

        private static int GenerateSeed()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[sizeof(ulong)];
                rng.GetBytes(bytes);
                var hash64 = BitConverter.ToUInt64(bytes, 0);
                return ((int)(hash64 >> 32)) ^ (int)hash64;
            }
        }

        /// <summary>
        /// IComparer&lt;string&gt;.GetHashCode() implementation.
        /// </summary>
        /// <param name="x">Left object to compare.</param>
        /// <param name="y">Right object to compare.</param>
        /// <returns>
        /// Returns 0 - if equal, -1 - if x &lt; y, +1 - if x &gt; y.
        /// </returns>
        public int Compare(string x, string y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return SimpleCaseFolding.CompareUsingSimpleCaseFolding(x, y);
        }

        /// <summary>
        /// IEqualityComparer&lt;string&gt;.Equals() implementation.
        /// </summary>
        /// <param name="x">Left object to compare.</param>
        /// <param name="y">Right object to compare.</param>
        /// <returns>
        /// Returns true if equal.
        /// </returns>
        public bool Equals(string x, string y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return SimpleCaseFolding.CompareUsingSimpleCaseFolding(x, y) == 0;
        }

        /// <summary>
        /// IEqualityComparer&lt;string&gt;.GetHashCode() implementation.
        /// </summary>
        /// <param name="obj">Object for which to get a hash.</param>
        /// <returns>
        /// Returns a hash code.
        /// </returns>
        public int GetHashCode(string obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return GetHashCodeSimpleCaseFolding(obj);
        }
    }
}
