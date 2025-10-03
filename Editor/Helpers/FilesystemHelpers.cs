using System.IO;

namespace Indiecat.TerrainGraph.Editor
{
    public static class FilesystemHelpers
    {
        public static string GetIndexedFilePath(string filePath, int index)
        {
            return GetSuffixedFilePath(filePath, index.ToString());
        }
        public static string GetIndexedFileName(string fileName, int index)
        {
            return GetSuffixedFileName(fileName, index.ToString());
        }

        public static string GetSuffixedFilePath(string filePath, string suffix)
        {
            return Path.Combine(
                Path.GetDirectoryName(filePath),
                GetSuffixedFileName(Path.GetFileName(filePath), suffix));
        }

        public static string GetSuffixedFileName(string fileName, string suffix)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            return fileNameWithoutExtension + suffix + Path.GetExtension(fileName);
        }
    }
}
