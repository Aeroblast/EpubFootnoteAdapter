using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace EpubFootnoteAdapter
{
    public class ProcOPF
    {
        public ProcOPF(string root)
        {
            string container=Path.Combine(root,"META-INF/container.xml");
            if(!File.Exists(container)){Log.log("Error:Cannot find meta-inf");return;}
            string metainf=File.ReadAllText(container);
            Regex reg=new Regex("<rootfile .*?>");
            XTag tag=new XTag(reg.Match(metainf).Value);
            string opf_path=tag.GetAttribute("full-path");
            opf_path=Path.Combine(root,opf_path);

            string opf=File.ReadAllText(opf_path);
            Regex item=new Regex("<item .*?>");
            Match m=item.Match(opf);
            while(m.Success)
            {
                XTag t=new XTag(m.Value);
                if(Path.GetFileName(t.GetAttribute("href"))=="notereplace.js")
                {
                    opf=opf.Remove(m.Index,m.Length);break;
                }
                m=m.NextMatch();
            }

            File.WriteAllText(opf_path,opf);

        }
    }
}