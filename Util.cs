using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml;

class Util
{
    public static void Packup(string outputfullpath)
    {
        if (File.Exists(outputfullpath))
        {
            File.Delete(outputfullpath);
        }
        ZipFile.CreateFromDirectory("temp", outputfullpath);
        Log.log("Saved:" + outputfullpath);
    }
    public static void Unzip(string archive_path, string output_dir)
    {
        if (Directory.Exists(output_dir))
        {
            Directory.CreateDirectory(output_dir);
        }
        ZipArchive archive = ZipFile.OpenRead(archive_path);
        archive.ExtractToDirectory(output_dir);
    }
    public static void DeleteDir(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (string p in Directory.GetFiles(path)) File.Delete(p);
        foreach (string p in Directory.GetDirectories(path)) DeleteDir(p);
        Directory.Delete(path);
    }
    public static void DeleteEmptyDir(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (string p in Directory.GetDirectories(path)) DeleteEmptyDir(p);
        if (Directory.GetDirectories(path).Length == 0 && Directory.GetFiles(path).Length == 0)
            Directory.Delete(path);
    }
    public delegate void FileProc(string filename);
    public static void ForeachFile(string dir, FileProc m)
    {
        foreach (string filename in Directory.GetFiles(dir))
            m(filename);

        foreach (string dirname in Directory.GetDirectories(dir))
        {
            ForeachFile(dirname, m);
        }
    }

    public static string ReferPath(string filename, string refPath)
    {
        string r = Path.GetDirectoryName(filename);
        string[] parts = refPath.Replace('/', '\\').Split('\\');
        foreach (string p in parts)
        {
            if (p == "") continue;

            if (p == "..") { r = Path.GetDirectoryName(r); continue; }
            r = Path.Combine(r, p);

        }
        return r;
    }
}
