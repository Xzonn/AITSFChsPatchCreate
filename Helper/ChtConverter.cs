using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AITSFChsPatchCreate
{

    internal class ChtConverter
    {
        public readonly static Dictionary<char, char> ChtToChsTable = File.ReadAllLines("Bin/ChtToChsTable.txt").Where(x => x.Length == 3 && x[1] == '\t').ToDictionary(x => x[0], x => x[2]);

        public static char Convert(char chtChar)
        {
            if (ChtToChsTable.ContainsKey(chtChar))
            {
                return ChtToChsTable[chtChar];
            }
            else
            {
                return chtChar;
            }
        }

        public static string Convert(string chtString)
        {
            string chsString = "";
            foreach (var chtChar in chtString)
            {
                chsString += Convert(chtChar);
            }
            return chsString;
        }
    }
}
