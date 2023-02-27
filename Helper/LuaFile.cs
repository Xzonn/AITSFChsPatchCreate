using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AITSFChsPatchCreate
{
    public class LuaFile
    {
        public class TextEntry
        {
            public int Index;
            public string ID;
            public string Text;
        }

        public class TableEntry
        {
            public byte type;
            public object data;
        }

        public byte[] Header;
        public string Source;
        public Encoding Encoding = Encoding.UTF8;
        public byte[] FuncHeader;
        public uint[] ByteCode;
        public List<TableEntry> ConstantTable = new List<TableEntry>();
        public Dictionary<int, TextEntry> StringTable = new Dictionary<int, TextEntry>();
        public byte[] Tail;

        public LuaFile(Stream stream)
        {
            LoadStream(stream);
        }

        public void LoadStream(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                Header = br.ReadBytes(0xc);
                Source = ReadString(br);
                FuncHeader = br.ReadBytes(0xc);
                int count = br.ReadInt32();
                ByteCode = new uint[count];
                for (int i = 0; i < count; i++)
                {
                    ByteCode[i] = br.ReadUInt32();
                }
                count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    TableEntry t = new TableEntry
                    {
                        type = br.ReadByte()
                    };
                    switch (t.type)
                    {
                        case 1:
                        case 2:
                            break;
                        case 3:
                            t.data = br.ReadDouble();
                            break;
                        case 4:
                            t.data = ReadString(br);
                            break;
                    }
                    ConstantTable.Add(t);
                }
                Tail = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
            }
            int[] reg = new int[256];
            foreach (uint bc in ByteCode)
            {
                uint opcode = bc & 0x3f;
                int b, c;
                switch (opcode)
                {
                    case 1: //LOADK
                        int bx = (int)((bc & 0xFFFFC000) >> 14);
                        int a = (int)((bc & 0x3FC0) >> 6);
                        reg[a] = bx;
                        continue;
                    case 9: //SETTABLE
                        if ((bc & 0x80000000) != 0)
                        {
                            b = (int)((bc & 0x7F800000) >> 23);
                        }
                        else
                        {
                            b = reg[(int)((bc & 0x7F800000) >> 23)];
                        }
                        if ((bc & 0x400000) != 0)
                        {
                            c = (int)((bc & 0x3FC000) >> 14);
                        }
                        else
                        {
                            c = reg[(int)((bc & 0x3FC000) >> 14)];
                        }
                        break;
                    default:
                        continue;
                }
                if (ConstantTable[c].type == 4)
                {
                    TextEntry sc = new TextEntry
                    {
                        Index = c,
                        ID = (string)ConstantTable[b].data,
                        Text = (string)ConstantTable[c].data
                    };
                    if (!StringTable.ContainsKey(c))
                    {
                        StringTable.Add(c, sc);
                    }
                    else
                    {
                        Debug.Assert(StringTable[c].Text == (string)ConstantTable[c].data);
                    }
                }
            }
        }

        public void SaveStream(Stream stream)
        {
            int[] reg = new int[256];
            foreach (uint bc in ByteCode)
            {
                uint opcode = bc & 0x3f;
                int b, c;
                switch (opcode)
                {
                    case 1: //LOADK
                        int bx = (int)((bc & 0xFFFFC000) >> 14);
                        int a = (int)((bc & 0x3FC0) >> 6);
                        reg[a] = bx;
                        continue;
                    case 9: //SETTABLE
                        if ((bc & 0x80000000) != 0)
                        {
                            b = (int)((bc & 0x7F800000) >> 23);
                        }
                        else
                        {
                            b = reg[(int)((bc & 0x7F800000) >> 23)];
                        }
                        if ((bc & 0x400000) != 0)
                        {
                            c = (int)((bc & 0x3FC000) >> 14);
                        }
                        else
                        {
                            c = reg[(int)((bc & 0x3FC000) >> 14)];
                        }
                        break;
                    default:
                        continue;
                }
                if (ConstantTable[c].type == 4)
                {
                    TextEntry sc = StringTable[c];
                    ConstantTable[c].data = sc.Text;
                }
            }
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write(Header);
                WriteString(bw, Source);
                bw.Write(FuncHeader);
                bw.Write(ByteCode.Length);
                foreach (uint bc in ByteCode)
                {
                    bw.Write(bc);
                }
                bw.Write(ConstantTable.Count);
                foreach (var value in StringTable.Values)
                {
                    if (value.Text != (string)ConstantTable[value.Index].data)
                    {
                        ConstantTable[value.Index].data = value.Text;
                    }
                }
                foreach (var t in ConstantTable)
                {
                    bw.Write(t.type);
                    switch (t.type)
                    {
                        case 1:
                        case 2:
                        case 3:
                            bw.Write((double)t.data);
                            break;
                        case 4:
                            string str = (string)t.data;
                            WriteString(bw, str);
                            break;
                    }
                }
                bw.Write(Tail);
            }
        }

        private string ReadString(BinaryReader br)
        {
            int size;
            if (Header[8] == 8)
            {
                size = (int)br.ReadInt64();
            }
            else
            {
                size = br.ReadInt32();
            }
            string str = Encoding.GetString(br.ReadBytes(size - 1));
            br.ReadByte();
            return str;
        }

        private void WriteString(BinaryWriter bw, string str)
        {
            int size = Encoding.GetByteCount(str) + 1;

            if (Header[8] == 8)
            {
                bw.Write((long)size);
            }
            else
            {
                bw.Write(size);
            }
            bw.Write(Encoding.GetBytes(str));
            bw.Write((byte)0);
        }
    }
}
