using System;
using System.IO;
using System.IO.Compression;

namespace Axata.BackupDrive.Utils
{
    public class Compression
    {
        public static void Compress(FileInfo fi, String filePath)
        {
            // Get the stream of the source file.
            using (FileStream inFile = fi.OpenRead())
            {
                // Prevent compressing hidden and 
                // already compressed files.
                if ((File.GetAttributes(fi.FullName)
                    & FileAttributes.Hidden)
                    != FileAttributes.Hidden & fi.Extension != ".gz")
                {
                    // Create the compressed file.
                    using (FileStream outFile = File.Create(filePath))
                    {
                        using (GZipStream Compress = new GZipStream(outFile, CompressionMode.Compress))
                        {
                            // Copy the source file into 
                            // the compression stream.
                            inFile.CopyTo(Compress);

                            //MessageBox.Show(String.Format("Compressed {0} from {1} to {2} bytes.",
                            //    fi.Name, fi.Length.ToString(), outFile.Length.ToString()));
                        }
                    }
                }
            }
        }
        public static void Decompress(FileInfo fileToDecompress, String targetPath)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(targetPath))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        //MessageBox.Show($"Decompressed: {fileToDecompress.Name}");
                    }
                }
            }
        }
    }
}
