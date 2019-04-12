using System;
using System.Collections.Generic;
using System.IO;
namespace EpubFootnoteAdapter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (Path.GetExtension(args[0]).ToLower() == ".epub" && File.Exists(args[0]))
                {
                    Util.DeleteDir("temp");
                    Util.Unzip(args[0], "temp");

                    List<string> css = new List<string>();
                    Util.ForeachFile("temp",
                    (path) =>
                    {
                        if (Path.GetFileName(path).ToLower() == "notereplace.js")
                        {
                            File.Delete(path);
                            Log.log("Removed notereplace.js");

                        }
                        if (Path.GetExtension(path).ToLower() == ".xhtml")
                        {
                            var x = new ProcXHTML(path);
                            if (x.contain_footnote)
                            {
                                if (x.css.Count > 0)
                                {
                                    bool exi = false;
                                    foreach (var a in css) if (a == x.css[0]) exi = true;
                                    if (!exi) css.Add(x.css[0]);
                                }
                            }
                        }
                    }
                    );
                    foreach (string p in css)
                    {
                        new ProcCSS(p);
                    }
                    new ProcOPF("temp");
                    Util.DeleteEmptyDir("temp");
                    string outname = Path.GetFileNameWithoutExtension(args[0]) + " [FootnoteAdapted].epub";
                    Util.Packup(Path.Combine(Path.GetDirectoryName(args[0]), outname));
                    Util.DeleteDir("temp");
                }
                else
                    Console.WriteLine("Invalid Input File");
            }
            else
                Console.WriteLine("Usage: <epub file> [<output name>]");

        }


    }
}
