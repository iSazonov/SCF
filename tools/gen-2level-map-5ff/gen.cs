using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CaseFolding
{
    class Program
    {
        private static ushort[] l0;
        private static ushort[] l1;
        private static ushort[] l3;

        static void Main(string[] args)
        {
            Dictionary<ushort, ushort> simpleFoldingMapping = ReadCaseFolding(@"CaseFolding.txt");
            GenerateTable8_4_4(simpleFoldingMapping, out l0, out l1, out l3);

            DumpTable3(l0, "MapBelow5FF");
            DumpTable(l1, "MapLevel1");
            DumpTable3(l3, "MapData");

            var sizel0 = l0.Length * sizeof(ushort);
            var sizel1 = l1.Length * sizeof(ushort);
            var sizel3 = l3.Length * sizeof(char);

            Console.WriteLine($"MapBelow5FF Size     = {sizel0, 4}");
            Console.WriteLine($"MapBelow5FF Length     = {l0.Length, 4}");
            Console.WriteLine($"MapLevel1 Size     = {sizel1, 4}");
            Console.WriteLine($"MapLevel1 Length     = {l1.Length, 4}");
            Console.WriteLine($"MapData Size     = {sizel3, 4}");
            Console.WriteLine($"MapData Length     = {l3.Length, 4}");
            Console.WriteLine($"Total size = {sizel0 + sizel1 + sizel3}");

            // Validate the generated tables

            foreach (char kv in simpleFoldingMapping.Keys)
            {
                ushort c = GetFoldCase(kv);
                if ((ushort) c != simpleFoldingMapping[kv])
                {
                    Console.WriteLine($"... {kv:x4}:  {c:x4} != {simpleFoldingMapping[kv]:x4}");
                }
            }
        }

        private static Dictionary<ushort, ushort> ReadCaseFolding(string CaseFoldingFilePath)
        {
            Dictionary<ushort, ushort> simpleFoldingMapping = new Dictionary<ushort, ushort>();

            using (StreamReader sr = new StreamReader(CaseFoldingFilePath))
            {
                while (!sr.EndOfStream)
                {
                    String line = sr.ReadLine().Trim();
                    if (String.IsNullOrEmpty(line) || line.IndexOf('#') == 0)
                    {
                        continue;
                    }

                    string[] parts = line.Split(';');
                    parts[1] = parts[1].Trim();

                    if (parts.Length < 4 || (parts[1] != "C" && parts[1] != "S"))
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int n1) ||
                        !int.TryParse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int n2))
                    {
                        continue;
                    }

                    if (n1 > 0xFFFF)
                    {
                        break;
                    }

                    simpleFoldingMapping[(ushort)n1] = (ushort)n2;
                }
            }

            return simpleFoldingMapping;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char GetFoldCase(char c)
        {
            if (c <= 0x5ff)
            {
                return (char)l0[c];
            }

            ushort v = l1[c >> 8];
            v = l3[v + (c & 0xFF)];

            return v == 0 ? c : (char)v;
        }

        private static void GenerateTable8_4_4(Dictionary<ushort, ushort> rawData, out ushort [] l0, out ushort [] l1, out ushort [] l3)
        {
            Dictionary<string, ushort> level3Hash = new Dictionary<string, ushort>();

            List<ushort> level1Data0 = new List<ushort>();
            List<ushort> level1Index = new List<ushort>();
            List<ushort> level3Data  = new List<ushort>();

            for (ushort i = 0; i <= 0x05ff; i++)
            {
                if (rawData.TryGetValue(i, out ushort value))
                {
                    // There is data defined for this codepoint. Use it.
                    level1Data0.Add(value);
                }
                else
                {
                    // There is no data defined for this codepoint. Use the default value
                    // specified in the ctor.
                    level1Data0.Add(i);
                }

            }

            ushort ch = 0;
            ushort valueInHash;

            for (ushort i = 0; i < 256; i++)
            {
                // Generate level 1 indice

                    // Generate level 2 indice
                    string level3RowData = "";
                    for (ushort k = 0; k < 256; k++)
                    {
                        // Generate level 3 values by grouping 256 values together.
                        // each element of the 256 value group is seperated by ";"
                        //if (((ch > 0x05ff && ch < 0xD800) || ch >= 0xF900) && rawData.TryGetValue(ch, out ushort value))
                        if (rawData.TryGetValue(ch, out ushort value))
                        {
                            // There is data defined for this codepoint.  Use it.
                            level3RowData = level3RowData + value +  ";";
                        }
                        else
                        {
                            // There is no data defined for this codepoint.  Use the default value
                            // specified in the ctor.
                            level3RowData = level3RowData +  0 + ";";
                        }
                        ch++;
                    }

                    // Check if the pattern of these 256 values happens before.
                    if (!level3Hash.TryGetValue(level3RowData, out valueInHash))
                    {
                        // This is a new group in the data level values.
                        // Get the current count of level 3 group count for this plane.
                        valueInHash = (ushort)level3Data.Count;

                        // Store this count to the hash table, keyed by the pattern of these 16 values.
                        level3Hash[level3RowData] = valueInHash;

                        // Populate the 256 values into data level data table for this plane.
                        string [] values = level3RowData.Split(';');
                        foreach (string s in values)
                        {
                            if (s.Length > 0)
                            {
                                level3Data.Add(ushort.Parse(s));
                            }
                        }

                    }

                // Populate the index values into level 1 index table.
                level1Index.Add(valueInHash);
            }

            l0 = level1Data0.ToArray();
            l1 = level1Index.ToArray();
            l3 = level3Data.ToArray();
        }

        private static void DumpTable(ushort[] table, string name)
        {
            const int RawWidth = 16;

            Console.Write($"private static readonly ushort[] {name} =\n{{\n");

            Console.Write($"//");
            for (int i = 0; i < RawWidth; i++)
            {
                Console.Write($"{i,6:x}  ");
            }

            Console.Write($"\n    0x{table[0]:x4}, ");

            for (int i = 1; i < table.Length; i++)
            {
                Console.Write($"0x{table[i]:x4}, ");

                if ((i + 1) % RawWidth == 0)
                {
                    Console.WriteLine($" // {i - 15:x4} .. {i:x4}");
                    Console.Write($"    ");
                }

            }

            Console.WriteLine("\n};\n");
        }

        private static void DumpTable3(ushort[] table, string name)
        {
            const int RawWidth = 16;

            Console.Write($"private static readonly char[] {name} =\n{{\n");

            Console.Write($"//");
            for (int i = 0; i < RawWidth; i++)
            {
                Console.Write($"{i,6:x}  ");
            }

            Console.Write($"\n    (char)0x{table[0]:x4}, ");

            for (int i = 1; i < table.Length; i++)
            {
                Console.Write($"(char)0x{table[i]:x4}, ");

                if ((i + 1) % RawWidth == 0)
                {
                    Console.WriteLine($" // {i - 15:x4} .. {i:x4}");
                    Console.Write($"    ");
                }

            }

            Console.WriteLine("\n};\n");
        }
    }
}
