using Ionic.Zip;
using System.IO;

namespace AITSFChsPatchCreate
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (Directory.GetFiles("Fonts", "*.ttf").Length == 0)
            {
                foreach (var file in Directory.GetFiles("Fonts", "*.zip"))
                {
                    using (ZipFile archive = new ZipFile(file))
                    {
                        archive.Password = "E1A1F6FB";
                        archive.Encryption = EncryptionAlgorithm.PkzipWeak;
                        archive.StatusMessageTextWriter = System.Console.Out;
                        archive.ExtractAll(@"Fonts", ExtractExistingFileAction.OverwriteSilently);
                    }
                }
            }

            FontHelper font = new FontHelper(@"Fonts");
            font.CreateFont(@"MainFont", "CAB-f862fe844235967e981e152eea7ad062", @"Patch");
            font.CreateFont(@"ResourcesFont", "resources.assets", @"Patch");

            TextHelper text = new TextHelper();
            text.ReplaceText(@"Text", "CAB-0670e8eb4b419284c6de5d2d82066179", @"Patch");
        }
    }
}
