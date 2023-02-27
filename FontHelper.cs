using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.IO.Hashing;
using System.Linq;

namespace AITSFChsPatchCreate
{
    public struct FontInfo
    {
        public string Name;
        public int Size;
        public int Width;
        public int Height;
        public string Characters;
        public uint FontHash;
        public uint InfoHash;
    }

    public struct FontData
    {
        public FT_FaceInfo FaceInfo;
        public byte[] GlyphData;
        public byte[] FontBuffer;
    }

    internal class FontHelper
    {
        readonly TypeTree FontTree = new TypeTree();
        readonly string FontPath;
        readonly Dictionary<string, FontInfo> FontInfoDict;
        readonly Dictionary<string, uint> FontHashDict;
        readonly Dictionary<uint, FontData> FontDataDict;

        public FontHelper(string fontPath)
        {
            FontTree = new TypeTree();
            using (var fs = File.OpenRead("Bin/MonoBehaviour.bin"))
            {
                FontTree.Load(new BinaryReaderExtended(fs));
            }
            FontPath = fontPath;
            if (File.Exists(Path.Combine(fontPath, "FontInfo.json")))
            {
                try
                {
                    FontInfoDict = JsonConvert.DeserializeObject<Dictionary<string, FontInfo>>(File.ReadAllText(Path.Combine(fontPath, "FontInfo.json")));
                }
                catch
                {
                    FontInfoDict = new Dictionary<string, FontInfo>();
                }
            }
            else
            {
                FontInfoDict = new Dictionary<string, FontInfo>();
            }
            FontHashDict = new Dictionary<string, uint>();
            FontDataDict = new Dictionary<uint, FontData>();
        }

        public void CreateFont(string path, string name, string output)
        {
            foreach (string fullPath in Directory.GetFiles(path, "*.dat"))
            {
                string fileName = Path.GetFileName(fullPath);
                long pathId = Convert.ToInt64(Path.GetFileNameWithoutExtension(fileName.Substring(fileName.IndexOf('-', fileName.IndexOf('-') + 1) + 1)));
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName.Split('-')[0]) + $"-{pathId:x016}";

                OrderedDictionary dictionary;
                using (var fs = File.OpenRead(fullPath))
                {
                    var binaryReader = new BinaryReaderExtended(fs);
                    dictionary = TypeTreeHelper.ReadType(FontTree, binaryReader);
                }

                var fontInfo = (OrderedDictionary)dictionary["m_fontInfo"];
                var fontName = (string)fontInfo["Name"];
                var fontSize = (int)(float)fontInfo["PointSize"];
                var fontPath = Path.Combine(FontPath, $"{fontName}.ttf");
                var fontHash = FontHashDict.ContainsKey(fontName) ? FontHashDict[fontName] : BitConverter.ToUInt32(Crc32.Hash(File.ReadAllBytes(fontPath)), 0);

                var chtCharacters = ((List<object>)dictionary["m_glyphInfoList"]).Select(x => (char)(int)((OrderedDictionary)x)["id"]).ToList();
                foreach (var pair in TextHelper.ReplaceTable)
                {
                    if (pair.Key.All(origChar => chtCharacters.Contains(origChar)))
                    {
                        foreach (var replChar in pair.Value)
                        {
                            if (!chtCharacters.Contains(replChar))
                            {
                                chtCharacters.Add(replChar);
                            }
                        }
                    }
                }
                chtCharacters.Sort();
                var chsCharacters = chtCharacters.Select(x => ChtConverter.Convert(x)).ToList();
                if (string.Join("", chtCharacters) == "中文繁體")
                {
                    chsCharacters = "中文简体".ToList();
                }

#if DEBUG
                Directory.CreateDirectory(Path.Combine(path, "CharacterTable"));
                File.WriteAllText(Path.Combine(path, "CharacterTable", $"{fileNameWithoutExtension}.txt"), string.Join("", chtCharacters).Trim());
                Directory.CreateDirectory(Path.Combine(path, "CharacterTableChs"));
                File.WriteAllText(Path.Combine(path, "CharacterTableChs", $"{fileNameWithoutExtension}.txt"), string.Join("", chsCharacters).Trim());
#endif

                var atlasWidth = (int)(float)fontInfo["AtlasWidth"];
                var atlasHeight = (int)(float)fontInfo["AtlasHeight"];
                if (FontInfoDict.ContainsKey(fileNameWithoutExtension))
                {
                    atlasWidth = FontInfoDict[fileNameWithoutExtension].Width;
                    atlasHeight = FontInfoDict[fileNameWithoutExtension].Height;
                }

                uint infoHash;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter br = new BinaryWriter(ms);
                    br.Write(fontName);
                    br.Write(fontSize);
                    br.Write(atlasWidth);
                    br.Write(atlasHeight);
                    br.Write(string.Join("", chsCharacters).Trim());
                    br.Write(fontHash);
                    infoHash = BitConverter.ToUInt32(Crc32.Hash(ms.ToArray()), 0);
                }
                if (FontInfoDict.ContainsKey(fileNameWithoutExtension) && infoHash == FontInfoDict[fileNameWithoutExtension].InfoHash)
                {
                    Console.WriteLine($"Nothing changed: {fileNameWithoutExtension}, hash: {infoHash:x08}");
                    continue;
                }
                else
                {
                    FontInfo info = new FontInfo
                    {
                        Name = fontName,
                        Size = fontSize,
                        Width = atlasWidth,
                        Height = atlasHeight,
                        Characters = string.Join("", chsCharacters).Trim(),
                        FontHash = fontHash,
                        InfoHash = infoHash
                    };
                    FontInfoDict[fileNameWithoutExtension] = info;
                }

                byte[] fontBuffer;
                FT_FaceInfo faceInfo;
                byte[] glyphDataRaw;

                if (FontDataDict.ContainsKey(infoHash))
                {
                    faceInfo = FontDataDict[infoHash].FaceInfo;
                    glyphDataRaw = FontDataDict[infoHash].GlyphData;
                    fontBuffer = FontDataDict[infoHash].FontBuffer;
                    Console.WriteLine($"Copying from memory: {fileNameWithoutExtension}, hash: {infoHash:x08}");
                }
                else
                {
                    int tries = 3;
                    faceInfo = new FT_FaceInfo();
                    glyphDataRaw = new byte[chtCharacters.Count * 32];
                    fontBuffer = new byte[atlasWidth * atlasHeight];
                    while (tries > 0 && fontBuffer.All(x => x == 0))
                    {
                        Console.WriteLine($"Generating: {fileNameWithoutExtension}, hash: {infoHash:x08}");
                        TMPro_FontPlugin.Initialize_FontEngine();
                        TMPro_FontPlugin.Load_TrueType_Font(fontPath);
                        TMPro_FontPlugin.FT_Size_Font(fontSize);
                        TMPro_FontPlugin.Render_Characters(fontBuffer, atlasWidth, atlasHeight, 5, chsCharacters.Select(x => Convert.ToInt32(x)).ToArray(), chsCharacters.Count, FaceStyles.Normal, 0F, false, RenderModes.DistanceField16, 0, ref faceInfo, glyphDataRaw);
                        TMPro_FontPlugin.Destroy_FontEngine();
                    }
                    FontDataDict[infoHash] = new FontData
                    {
                        FaceInfo = faceInfo,
                        GlyphData = glyphDataRaw,
                        FontBuffer = fontBuffer
                    };
                }

                long atlasPathId = (long)((OrderedDictionary)dictionary["atlas"])["m_PathID"];
                Directory.CreateDirectory(Path.Combine(output, name, "Texture2D"));
                File.WriteAllBytes(Path.Combine(output, name, $"Texture2D/{atlasPathId:x016}.res"), fontBuffer);

#if DEBUG
                Bitmap bitmap = new Bitmap(atlasWidth, atlasHeight);
                for (int y = 0; y < atlasHeight; y++)
                {
                    for (int x = 0; x < atlasWidth; x++)
                    {
                        var alpha = fontBuffer[y * atlasWidth + x];
                        bitmap.SetPixel(x, atlasHeight - y - 1, Color.FromArgb(255, alpha, alpha, alpha));
                    }
                }
                Directory.CreateDirectory(Path.Combine(path, "NewResourcesPng"));
                bitmap.Save(Path.Combine(path, "NewResourcesPng", $"{fileNameWithoutExtension}.png"));
#endif

                var glyphDict = new Dictionary<int, FT_GlyphInfo>();
                using (var br = new BinaryReader(new MemoryStream(glyphDataRaw)))
                {
                    for (int i = 0; i < chtCharacters.Count; i++)
                    {
                        var glyphId = br.ReadInt32();
                        glyphDict[glyphId] = new FT_GlyphInfo
                        {
                            id = glyphId,
                            x = br.ReadSingle(),
                            y = br.ReadSingle(),
                            width = br.ReadSingle(),
                            height = br.ReadSingle(),
                            xOffset = br.ReadSingle(),
                            yOffset = br.ReadSingle(),
                            xAdvance = br.ReadSingle()
                        };
                    }
                }
                var nonExistsCharList = new List<char>();
                var newGlyphData = new List<object>();
                foreach (var chtChar in chtCharacters)
                {
                    char chsChar = ChtConverter.Convert(chtChar);
                    if (string.Join("", chtCharacters) == "中文繁體" && chsChar == '繁')
                    {
                        chsChar = '简';
                    }
                    if (!glyphDict.ContainsKey(Convert.ToInt32(chsChar)))
                    {
                        nonExistsCharList.Add(chtChar);
                        continue;
                    }
                    var chsGlyph = glyphDict[Convert.ToInt32(chsChar)];
                    if (chsGlyph.x < 0)
                    {
                        nonExistsCharList.Add(chtChar);
                    }
                    var newGlyphDict = new OrderedDictionary
                    {
                        ["id"] = Convert.ToInt32(chtChar),
                        ["x"] = chsGlyph.x,
                        ["y"] = chsGlyph.y,
                        ["width"] = chsGlyph.width,
                        ["height"] = chsGlyph.height,
                        ["xOffset"] = chsGlyph.xOffset,
                        ["yOffset"] = chsGlyph.yOffset,
                        ["xAdvance"] = chsGlyph.xAdvance,
                        ["scale"] = 1F
                    };
                    newGlyphData.Add(newGlyphDict);
                }
                dictionary["m_glyphInfoList"] = newGlyphData;

                fontInfo["CharacterCount"] = newGlyphData.Count;
                fontInfo["LineHeight"] = faceInfo.lineHeight;
                fontInfo["Baseline"] = 0F;
                fontInfo["Ascender"] = faceInfo.ascender;
                fontInfo["CapHeight"] = 0F;
                fontInfo["Descender"] = faceInfo.descender;
                fontInfo["CenterLine"] = faceInfo.centerLine;
                fontInfo["SuperscriptOffset"] = faceInfo.ascender;
                fontInfo["SubscriptOffset"] = faceInfo.underline;
                fontInfo["SubSize"] = 0.5F;
                fontInfo["Underline"] = faceInfo.underline;
                fontInfo["UnderlineThickness"] = faceInfo.underlineThickness == 0F ? 5F : faceInfo.underlineThickness;
                fontInfo["strikethrough"] = (faceInfo.ascender + faceInfo.descender) / 2.75F;
                fontInfo["strikethroughThickness"] = faceInfo.underlineThickness;
                if (glyphDict.ContainsKey(32))
                {
                    fontInfo["TabWidth"] = glyphDict[32].xAdvance;
                }
                fontInfo["Padding"] = (float)faceInfo.padding;
                fontInfo["AtlasWidth"] = (float)atlasWidth;
                fontInfo["AtlasHeight"] = (float)atlasHeight;

                Directory.CreateDirectory(Path.Combine(output, name, "MonoBehaviour"));
                using (var fs = File.OpenWrite(Path.Combine(output, name, $"MonoBehaviour/{pathId:x016}.asset")))
                {
                    var binaryWriter = new BinaryWriterExtended(fs);
                    TypeTreeHelper.WriteType(dictionary, FontTree, binaryWriter);
                }

                Console.WriteLine($"New Font: {fileNameWithoutExtension}, hash: {infoHash:x08}");
                var nonExistsCharacters = string.Join("", nonExistsCharList).Trim();
                if (nonExistsCharacters.Length > 0)
                {
                    Console.WriteLine($"Characters not included: {nonExistsCharacters}");
                }
                File.WriteAllText(Path.Combine(FontPath, "FontInfo.json"), JsonConvert.SerializeObject(FontInfoDict, Formatting.Indented));
            }
        }
    }
}