using System;

namespace AITSFChsPatchCreate
{
    public class SerializedType
    {
        public int classID;
        public bool m_IsStrippedType;
        public short m_ScriptTypeIndex = -1;
        public TypeTree m_Type;
        public byte[] m_ScriptID; //Hash128
        public byte[] m_OldTypeHash; //Hash128
        public int[] m_TypeDependencies;
        public string m_KlassName;
        public string m_NameSpace;
        public string m_AsmName;
    }

    public class ObjectInfo
    {
        public long byteStart;
        public uint byteSize;
        public int typeID;
        public int classID;
        public ushort isDestroyed;
        public byte stripped;

        public long m_PathID;
        public SerializedType serializedType;
    }

    public class LocalSerializedObjectIdentifier
    {
        public int localSerializedFileIndex;
        public long localIdentifierInFile;
    }

    public class FileIdentifier
    {
        public Guid guid;
        public int type; //enum { kNonAssetType = 0, kDeprecatedCachedAssetType = 1, kSerializedAssetType = 2, kMetaAssetType = 3 };
        public string pathName;

        //custom
        public string fileName;

        // For Dump
        public string tempEmpty;
    }
}
