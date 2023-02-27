using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AITSFChsPatchCreate
{
    internal class TextHelper
    {
        public readonly static Dictionary<string, string> ReplaceTable = File.ReadAllLines("Bin/ReplaceTable.txt").Select(x => x.Split('\t')).Where(x => x.Length == 2).ToDictionary(x => x[0], x => x[1]);

        public void ReplaceText(string path, string name, string output)
        {
            foreach (string store in new string[] { "MicrosoftStore", "Steam" })
            {
                foreach (string fullPath in Directory.GetFiles(Path.Combine(path, store), "*.dat"))
                {
                    string fileName = Path.GetFileName(fullPath);
                    string fileNameWithoutExtension = fileName.Split('-')[0];
                    long pathId = Convert.ToInt64(Path.GetFileNameWithoutExtension(fileName.Substring(fileName.IndexOf('-', fileName.IndexOf('-') + 1) + 1)));

                    byte[] header;
                    byte[] luaBuffer;
                    using (var fs = File.OpenRead(fullPath))
                    {
                        var br = new BinaryReaderExtended(fs);
                        int nameLength = br.ReadInt32();
                        byte[] nameBytes = br.ReadBytes(nameLength);
                        br.AlignStream(4);
                        var position = br.Position;
                        br.Position = 0;
                        header = br.ReadBytes((int)position);
                        int luaLength = br.ReadInt32();
                        using (var reader = new PositionXorReader(fs, br.Position, 4))
                        {
                            luaBuffer = new byte[luaLength];
                            reader.Read(luaBuffer, 0, luaLength);
                        }
                    }
                    LuaFile lua;
                    using (var ms = new MemoryStream(luaBuffer))
                    {
                        lua = new LuaFile(ms);
                    }
                    if (lua.StringTable.Count == 0)
                    {
                        continue;
                    }
                    bool replaced = false;
#if DEBUG
                    Directory.CreateDirectory(Path.Combine(path, $"{store}TextExtract"));
                    var originalStream = File.CreateText(Path.Combine(path, $"{store}TextExtract", $"{fileNameWithoutExtension}.txt"));
                    Directory.CreateDirectory(Path.Combine(path, $"{store}TextReplaced"));
                    var replacedStream = File.CreateText(Path.Combine(path, $"{store}TextReplaced", $"{fileNameWithoutExtension}.txt"));
                    Directory.CreateDirectory(Path.Combine(path, $"{store}TextChs"));
                    var chsStream = File.CreateText(Path.Combine(path, $"{store}TextChs", $"{fileNameWithoutExtension}.txt"));
#endif
                    foreach (var entry in lua.StringTable)
                    {
#if DEBUG
                        originalStream.WriteLine($"{entry.Value.ID}\t{entry.Value.Text.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n")}");
#endif
                        foreach (var pair in ReplaceTable)
                        {
                            if (entry.Value.Text.Contains(pair.Key))
                            {
                                entry.Value.Text = entry.Value.Text.Replace(pair.Key, pair.Value);
                                replaced = true;
                            }
                        }
#if DEBUG
                        replacedStream.WriteLine($"{entry.Value.ID}\t{entry.Value.Text.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n")}");
                        chsStream.WriteLine($"{entry.Value.ID}\t{ChtConverter.Convert(entry.Value.Text).Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n")}");
#endif
                    }
#if DEBUG
                    originalStream.Close();
                    replacedStream.Close();
                    chsStream.Close();
#endif
                    if (!replaced)
                    {
                        if (File.Exists(Path.Combine(output, name, $"TextAsset/{pathId:x016}.asset")))
                        {
                            File.Delete(Path.Combine(output, name, $"TextAsset/{pathId:x016}.asset"));
                        }
                        continue;
                    }
                    using (var ms = new MemoryStream())
                    {
                        lua.SaveStream(ms);
                        luaBuffer = ms.ToArray();
                    }
                    Directory.CreateDirectory(Path.Combine(output, name, "TextAsset"));
                    using (var fs = File.OpenWrite(Path.Combine(output, name, $"TextAsset/{pathId:x016}.asset")))
                    {
                        var bw = new BinaryWriterExtended(fs);
                        bw.Write(header);
                        bw.Write(luaBuffer.Length);
                        using (var writer = new PositionXorWriter(fs, bw.Position, 4))
                        {
                            writer.Write(luaBuffer, 0, luaBuffer.Length);
                        }
                    }
                    Console.WriteLine($"Text saved: {store} {fileNameWithoutExtension}");
                }
            }
        }
    }
}
