// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation.Unicode;
using Xunit;

namespace PSTests.Parallel.System.Management.Automation.Unicode
{
    public class StringComparerUsingSimpleCaseFoldingTests
    {
        // The tests come from CoreFX tests: src/System.Runtime.Extensions/tests/System/StringComparer.cs

        [Fact]
        public static void TestOrdinal_EmbeddedNull_ReturnsDifferentHashCodes()
        {
            StringComparerUsingSimpleCaseFolding sc = new StringComparerUsingSimpleCaseFolding();
            Assert.NotEqual(sc.GetHashCode("\0AAAAAAAAA"), sc.GetHashCode("\0BBBBBBBBBBBB"));
        }

        [Theory]
        [InlineData("AAA", "aaa")]
        [InlineData("BaC", "bAc")]
        public static void TestGetHashCode_ReturnsHashCodes_Equal(string strA, string strB)
        {
            StringComparerUsingSimpleCaseFolding sc = new StringComparerUsingSimpleCaseFolding();
            Assert.Equal(sc.GetHashCode(strA), sc.GetHashCode(strB));
            Assert.Equal(sc.GetHashCode((object)strA), sc.GetHashCode((object)strB));
        }

        [Theory]
        [InlineData("AAA", "AAB")]
        [InlineData("AAA", "AAb")]
        public static void TestGetHashCode_ReturnsHashCodes_NotEqual(string strA, string strB)
        {
            StringComparerUsingSimpleCaseFolding sc = new StringComparerUsingSimpleCaseFolding();
            Assert.NotEqual(sc.GetHashCode(strA), sc.GetHashCode(strB));
            Assert.NotEqual(sc.GetHashCode((object)strA), sc.GetHashCode((object)strB));
        }

        [Theory]
        [InlineData("Hello", "Hello")]
        [InlineData("\0AAAAAAAAA", "\0AAAAAAAAA")]
        [InlineData("Ёлки-Палки", "ёлки-палкИ")]
        public static void VerifyStringComparer_Equal(string strA, string strB)
        {
            StringComparerUsingSimpleCaseFolding sc = new StringComparerUsingSimpleCaseFolding();
            Assert.True(sc.Equals(strA, strB));
            Assert.True(sc.Equals((object)strA, (object)strB));
            Assert.True(sc.Equals((object)strA, strB));
            Assert.True(sc.Equals(strA, (object)strB));

            Assert.Equal(0, sc.Compare(strA, strB));
            Assert.Equal(0, ((IComparer)sc).Compare(strA, strB));
            Assert.True(((IEqualityComparer)sc).Equals(strA, strB));
        }

        [Theory]
        [InlineData("", "Hello", -1)]
        [InlineData("Hello", "", 1)]
        [InlineData("Hello1", "Hello2", -1)]
        [InlineData("Hello", "There", -1)]
        [InlineData("", "\0AAAAAAAAA", -1)]
        [InlineData("\0AAAAAAAAA", "", 1)]
        [InlineData("\0AAAAAAAAA", "\0BBBBBBBBBBBB", -1)]
        [InlineData("Ёлки-ПалкиЯ", "ёлки-палкИq", 1)]
        public static void VerifyStringComparer_NotEqual(string strA, string strB, int result)
        {
            StringComparerUsingSimpleCaseFolding sc = new StringComparerUsingSimpleCaseFolding();
            Assert.False(sc.Equals(strA, strB));
            Assert.False(sc.Equals((object)strA, (object)strB));
            Assert.False(sc.Equals((object)strA, strB));
            Assert.False(sc.Equals(strA, (object)strB));

            Assert.True(sc.Compare(strA, strB) * result > 0);
            Assert.True(((IComparer)sc).Compare(strA, strB) * result > 0);
            Assert.False(((IEqualityComparer)sc).Equals(strA, strB));
        }

        [Fact]
        public static void VerifyComparer()
        {
            StringComparerUsingSimpleCaseFolding sc = new StringComparerUsingSimpleCaseFolding();
            string s1 = "Hello";
            string s1a = "Hello";
            string s1b = "HELLO";
            string s2 = "ЯЯЯ2There";
            string aa = "\0AAAAAAAAA";
            string bb = "\0BBBBBBBBBBBB";

            Assert.True(sc.Equals(s1, s1a));
            Assert.True(sc.Equals((object)s1, (object)s1a));

            Assert.Equal(0, sc.Compare(s1, s1a));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1a));

            Assert.True(sc.Equals(s1, s1));
            Assert.True(((IEqualityComparer)sc).Equals(s1, s1));
            Assert.Equal(0, sc.Compare(s1, s1));
            Assert.Equal(0, ((IComparer)sc).Compare(s1, s1));

            Assert.False(sc.Equals(s1, s2));
            Assert.False(((IEqualityComparer)sc).Equals(s1, s2));
            Assert.True(sc.Compare(s1, s2) < 0);
            Assert.True(((IComparer)sc).Compare(s1, s2) < 0);

            Assert.True(sc.Equals(s1, s1b));
            Assert.True(((IEqualityComparer)sc).Equals(s1, s1b));

            Assert.NotEqual(0, ((IComparer)sc).Compare(aa, bb));
            Assert.False(sc.Equals(aa, bb));
            Assert.False(((IEqualityComparer)sc).Equals(aa, bb));
            Assert.True(sc.Compare(aa, bb) < 0);
            Assert.True(((IComparer)sc).Compare(aa, bb) < 0);

            int result = sc.Compare(s1, s1b);
            Assert.Equal(0, result);

            result = ((IComparer)sc).Compare(s1, s1b);
            Assert.Equal(0, result);
        }
    }
}
