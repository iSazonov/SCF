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
        public string CoreFXCompare(string StrA)
        {
            return StrA.ToUpperInvariant();
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public string SimpleCaseFoldCompare(string StrA)
        {
            return SimpleCaseFolding.SimpleCaseFold(StrA);
        }

        public IEnumerable<object> Data()
        {
            yield return "CaseFolding1";
            yield return "ЯяЯяЯяЯяЯяЯ1";
        }
    }
}
