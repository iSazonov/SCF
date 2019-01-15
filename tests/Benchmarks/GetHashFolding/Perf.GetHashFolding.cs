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
            //Console.WriteLine("Result: {0}", SCFMarvin.ComputeHash32OrdinalIgnoreCase1("cASEfOLDING2", SCFMarvin.DefaultSeed));
            //Console.WriteLine("Result: {0}", SCFMarvin.ComputeHash32OrdinalIgnoreCase1("яЯяЯяЯяЯяЯя2", SCFMarvin.DefaultSeed));
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    [RyuJitX64Job]
    public class IntroBenchmarkBaseline
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public int CoreFXMarvinOrdinalIgnoreCase(string StrA)
        {
            return Marvin.ComputeHash32OrdinalIgnoreCase(StrA, Marvin.DefaultSeed);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public int SCFMarvinOrdinalIgnoreCase(string StrA)
        {
            return SCFMarvin.ComputeHash32OrdinalIgnoreCase1(StrA, SCFMarvin.DefaultSeed);
        }

        public IEnumerable<object> Data()
        {
            yield return "CaseFolding1";
            yield return "ЯяЯяЯяЯяЯяЯ1";
        }
    }
}
