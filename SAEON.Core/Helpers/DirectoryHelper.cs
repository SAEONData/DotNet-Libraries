using System.IO;

namespace SAEON.Core
{
    public static class DirectoryHelper
    {
        public static void DirectoryCopy(string sourceDirectory, string destDirectory, bool recursive = true, bool overwriteFiles = true)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirectory);
            if (!dir.Exists) throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirectory);
            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirectory)) Directory.CreateDirectory(destDirectory);
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirectory, file.Name);
                file.CopyTo(temppath, overwriteFiles);
            }
            if (recursive)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirectory, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, recursive, overwriteFiles);
                }
            }
        }
    }
}
