using System.Collections.Generic;
using System.IO;

namespace AITSFChsPatchCreate
{
    public class TypeTree
    {
        public List<TypeTreeNode> m_Nodes = new List<TypeTreeNode>();
        public byte[] m_StringBuffer;

        // For Dump
        public int NumberOfNodes;
        public int StringBufferSize;

        public void Load(BinaryReaderExtended reader)
        {
            NumberOfNodes = reader.ReadInt32();
            StringBufferSize = reader.ReadInt32();
            for (int i = 0; i < NumberOfNodes; i++)
            {
                var typeTreeNode = new TypeTreeNode();
                m_Nodes.Add(typeTreeNode);
                typeTreeNode.m_Version = reader.ReadUInt16();
                typeTreeNode.m_Level = reader.ReadByte();
                typeTreeNode.m_TypeFlags = reader.ReadByte();
                typeTreeNode.m_TypeStrOffset = reader.ReadUInt32();
                typeTreeNode.m_NameStrOffset = reader.ReadUInt32();
                typeTreeNode.m_ByteSize = reader.ReadInt32();
                typeTreeNode.m_Index = reader.ReadInt32();
                typeTreeNode.m_MetaFlag = reader.ReadInt32();
            }
            m_StringBuffer = reader.ReadBytes(StringBufferSize);

            using (var stringBufferReader = new BinaryReaderExtended(new MemoryStream(m_StringBuffer)))
            {
                for (int i = 0; i < NumberOfNodes; i++)
                {
                    var m_Node = m_Nodes[i];
                    m_Node.m_Type = ReadString(stringBufferReader, m_Node.m_TypeStrOffset);
                    m_Node.m_Name = ReadString(stringBufferReader, m_Node.m_NameStrOffset);
                }
            }

            string ReadString(BinaryReaderExtended stringBufferReader, uint value)
            {
                var isOffset = (value & 0x80000000) == 0;
                if (isOffset)
                {
                    stringBufferReader.BaseStream.Position = value;
                    return stringBufferReader.ReadStringToNull();
                }
                var offset = value & 0x7FFFFFFF;
                if (CommonString.StringBuffer.TryGetValue(offset, out var str))
                {
                    return str;
                }
                return offset.ToString();
            }
        }
    }

    public class TypeTreeNode
    {
        public string m_Type;
        public string m_Name;
        public int m_ByteSize;
        public int m_Index;
        public byte m_TypeFlags; //m_IsArray
        public ushort m_Version;
        public int m_MetaFlag;
        public byte m_Level;
        public uint m_TypeStrOffset;
        public uint m_NameStrOffset;
        public ulong m_RefTypeHash;

        public TypeTreeNode() { }

        public TypeTreeNode(string type, string name, byte level, bool align)
        {
            m_Type = type;
            m_Name = name;
            m_Level = level;
            m_MetaFlag = align ? 0x4000 : 0;
        }
    }
}
