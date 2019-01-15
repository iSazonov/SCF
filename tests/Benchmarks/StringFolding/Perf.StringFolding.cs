// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace System.Management.Automation.Unicode
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<IntroBenchmarkBaseline>();
            var comparer = new StringComparerUsingSimpleCaseFolding();
            //var r = comparer.Compare("ЯяЫяЯяЯяЯяЯ1", "яЯяЯяЯяЯяЯя2");
            var r = comparer.Compare("CaseFolding1", "cASEfOLDING2");
            Console.WriteLine("Result: {0}", r);
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    [RyuJitX64Job]
    public class IntroBenchmarkBaseline
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public void CoreFXCompare(string StrA, string StrB)
        {
            string.Compare(StrA, StrB, StringComparison.OrdinalIgnoreCase);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int SimpleCaseFoldCompare(string StrA, string StrB)
        {
            var comparer = new StringComparerUsingSimpleCaseFolding();
            return comparer.Compare(StrA, StrB);
        }

        public IEnumerable<object[]> Data()
        {
            yield return new object[] { "CaseFolding1", "cASEfOLDING2" };
            yield return new object[] { "ЯяЯяЯяЯяЯяЯ1", "яЯяЯяЯяЯяЯя2" };
        }
    }
}
