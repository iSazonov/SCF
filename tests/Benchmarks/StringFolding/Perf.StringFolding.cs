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
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Running;

namespace System.Text.CaseFolding
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<StringFoldingBenchmark_SimpleCaseFold>();
            Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("cASEfOLDING2"));
            Console.WriteLine("Result: {0}", SimpleCaseFolding.SimpleCaseFold("яЯяЯяЯяЯяЯя2"));
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 2)]
    [Config(typeof(ConfigWithCustomEnvVars))]
    public class StringFoldingBenchmark_SimpleCaseFold
    {
        private class ConfigWithCustomEnvVars : ManualConfig
        {
             private const string JitNoInline = "COMPlus_TieredCompilation";

            public ConfigWithCustomEnvVars()
            {
                Add(Job.Core
                    .With(new[] { new EnvironmentVariable(JitNoInline, "1") })
//                    .WithTargetCount(48)
//                    .WithUnrollFactor(16)
                    .With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp30))
                    .WithId("TC Enabled"));

                Add(Job.Core
                    .With(new[] { new EnvironmentVariable(JitNoInline, "1") })
//                    .WithTargetCount(48)
//                    .WithUnrollFactor(16)
                    .With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp30))
                    .WithId("TC Disabled"));
            }
        }

        [Params('A', '\u0600')]
        public char TestChar { get; set; }

        [Benchmark(Baseline = true)]
        public char SimpleCaseFold()
        {
            return SimpleCaseFolding.SimpleCaseFold(TestChar);
        }

        [Benchmark]
        public char SimpleCaseFoldTest()
        {
            return SimpleCaseFolding.SimpleCaseFoldTest(TestChar);
        }
    }

    [DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    public class StringFoldingBenchmark
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Data))]
        public string CoreFXToUpperInvariant(string StrA)
        {
            return StrA.ToUpperInvariant();
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public string CoreFXToLowerInvariant(string StrA)
        {
            return StrA.ToLowerInvariant();
        }

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public string SimpleCaseFold(string StrA)
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
