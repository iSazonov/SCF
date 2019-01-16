using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CaseFolding
{
    class Program
    {
        private static ushort[] l1;
        private static int[] l3;

        static void Main(string[] args)
        {
            Dictionary<ushort, int> simpleFoldingMapping = ReadCaseFolding(@"CaseFolding.txt");
            GenerateTable8_4_4(simpleFoldingMapping, out l1, out l3);

            DumpTable(l1, "MapSurrogateLevel1");
            DumpTable3(l3, "MapSurrogateData");

            var sizel1 = l1.Length * sizeof(ushort);
            var sizel3 = l3.Length * sizeof(char) * 2;

            Console.WriteLine($"MapSurrogateLevel1 Length     = {l1.Length}");
            Console.WriteLine($"MapSurrogateLevel1 Size       = {sizel1}");
            Console.WriteLine($"MapSurrogateData Length   = {l3.Length}");
            Console.WriteLine($"MapSurrogateData Size     = {sizel3}");
             Console.WriteLine($"Total size               = {sizel1 + sizel3}");

            // Validate the generated tables

            foreach (ushort kv in simpleFoldingMapping.Keys)
            {
                var c = GetFoldCase(kv);
                if (c != simpleFoldingMapping[kv])
                {
                    Console.WriteLine($"... {kv:x4}:  {c:x4} != {simpleFoldingMapping[kv]:x4}");
                }
            }

            Console.ReadLine();
        }

        private static Dictionary<ushort, int> ReadCaseFolding(string CaseFoldingFilePath)
        {
            Dictionary<ushort, int> simpleFoldingMapping = new Dictionary<ushort, int>();

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

                    if (n1 <= 0xFFFF)
                    {
                        continue;
                    }

                    n1 -= 0x010000;

                    simpleFoldingMapping[(ushort)n1] = n2;
                }
            }

            return simpleFoldingMapping;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetFoldCase(int c)
        {
            var v = l1[c >> 8];
            var v1 = l3[v + (c & 0xFF)];

            return v1 == 0 ? c : v1;
        }

        private static void GenerateTable8_4_4(Dictionary<ushort, int> rawData, out ushort [] l1, out int [] l3)
        {
            Dictionary<string, ushort> level3Hash = new Dictionary<string, ushort>();

            List<ushort> level1Index = new List<ushort>();
            List<int> level3Data  = new List<int>();

            ushort ch = 0;
            ushort valueInHash;

            for (ushort i = 0; i < 256; i++)
            {
                // Generate level 1 indice
                string level3RowData = "";
                for (ushort k = 0; k < 256; k++)
                {
                    // Generate data level values by grouping 256 values together.
                    // Each element of the 256 value group is seperated by ";".
                    if (rawData.TryGetValue(ch, out int value))
                    {
                        // There is data defined for this codepoint. Use it.
                        level3RowData = level3RowData + value +  ";";
                    }
                    else
                    {
                        // There is no data defined for this codepoint. Use the default value
                        // specified in the ctor.
                        level3RowData = level3RowData +  0 + ";";
                    }

                    ch++;
                }

                // Check if the pattern of these 256 values happens before.
                if (!level3Hash.TryGetValue(level3RowData, out valueInHash))
                {
                    // This is a new group in the data level values.
                    // Get the current count of data level group count for this plane.
                    valueInHash = (ushort)level3Data.Count;

                    // Store this count to the hash table, keyed by the pattern of these 16 values.
                    level3Hash[level3RowData] = valueInHash;

                    // Populate the 256 values into data level data table for this plane.
                    string [] values = level3RowData.Split(';');
                    foreach (string s in values)
                    {
                        if (s.Length > 0)
                        {
                            level3Data.Add(int.Parse(s));
                        }
                    }

                }

                // Populate the index values into level 1 index table.
                level1Index.Add(valueInHash);
            }

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

        private static void DumpTable3(int[] table, string name)
        {
            const int RawWidth = 16;

            Console.Write($"private static readonly (char, char)[] {name} =\n{{\n");

            Console.Write($"//");
            for (int i = 0; i < RawWidth; i++)
            {
                Console.Write($"           {i,6:x}             ");
            }

            var empty =  new ushort[] {(ushort)0, (ushort)0};
            var surrogates = Char.ConvertFromUtf32(table[0]);
            ushort[] s;

            if (surrogates.Length == 1)
            {
                s = empty;
            }
            else
            {
                // Output low surrogate,then high surrogate.
                // This allow to compare as uint.
                s = new ushort[2] {surrogates[0], surrogates[1]};
            }

            Console.Write($"\n    ((char)0x{s[0]:x4}, (char)0x{s[1]:x4}), ");

            for (int i = 1; i < table.Length; i++)
            {
                surrogates = Char.ConvertFromUtf32(table[i]);
                if (surrogates.Length == 1)
                {
                    s = empty;
                }
                else
                {
                    s = new ushort[2] {surrogates[0], surrogates[1]};
                }

                Console.Write($"((char)0x{s[0]:x4}, (char)0x{s[1]:x4}), ");

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
