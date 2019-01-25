// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace System.Text.CaseFolding
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<IntroBenchmarkBaseline>();
            Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("cASEfOLDING2"));
            Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("яЯяЯяЯяЯяЯя2"));
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    [RyuJitX64Job]
    public class IntroBenchmarkBaseline
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public char FixedCharFold(char c)
        {
            return SimpleCaseFolding.SimpleCaseFold(c);
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public char CharFold(char c)
        {
            return SimpleCaseFolding.SimpleCaseFold1(c);
        }

        public IEnumerable<object> Data()
        {
            yield return '\u0600';
        }
    }
}
