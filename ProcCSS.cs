using System.IO;
using System.Collections.Generic;
//using System.Text.RegularExpressions;
namespace EpubFootnoteAdapter
{
    public class ProcCSS
    {
        string text;
        public ProcCSS(string path)
        {
            text=File.ReadAllText(path);
            int media_i=text.IndexOf("@media");
            if(media_i<0)
            {
                text+="\n@media amzn-kf8{\naside{display:none;}\n.duokan-footnote-item{page-break-after:always;}\n}";
            }
            File.WriteAllText(path,text);

        }

    }

}