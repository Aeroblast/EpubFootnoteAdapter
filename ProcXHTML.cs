using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace EpubFootnoteAdapter
{
    public class ProcXHTML
    {
        string text;
        string filename;
        public bool contain_footnote = false;
        public List<string> css = new List<string>();
        public ProcXHTML(string filename)
        {
            Log.log("-------"+Path.GetFileName(filename)+"---------");
            this.filename = filename;
            text = File.ReadAllText(filename);
            CheckFootnotes();
            if (contain_footnote) CheckNamespace();
            CheckHead();
            File.WriteAllText(filename, text);
        }
        void CheckHead()
        {
            Regex reg = new Regex("<head>[\\w\\W]*?</head>");
            Match m = reg.Match(text);
            if (!m.Success) { Log.log("Warn:No head tag in xhtml"); return; }
            Regex reg_link = new Regex("<link .*?>");
            Regex reg_script = new Regex("<script .*?>");
            var ms = reg_link.Matches(m.Value);
            foreach (Match link in ms)
            {
                XTag tag = new XTag(link.Value);
                if (tag.GetAttribute("type").ToLower() == "text/css")
                {
                    string url = tag.GetAttribute("href");
                    url = Util.ReferPath(filename, url);
                    css.Add(url);
                }
            }
            int pos = m.Index;
            Match scpt = reg_script.Match(text, pos);
            while (scpt.Success)
            {
                XTag tag = new XTag(scpt.Value);
                if (tag.GetAttribute("src").Contains("notereplace.js"))
                {
                    string scpt_end="</script>";
                    int sei=text.IndexOf(scpt_end,scpt.Index);
                    if(sei<0){Log.log("Error:Unclosed script tag.");break;}
                    text = text.Remove(scpt.Index, sei-scpt.Index+scpt_end.Length);break;
                }
                else
                {
                    pos = scpt.Index + scpt.Length;
                }
                scpt = reg_script.Match(text, pos);
            }


        }
        void CheckNamespace()
        {
            Regex html_tag = new Regex("<html.*?>");
            MatchCollection ms = html_tag.Matches(text);
            if (ms.Count != 1)
            {
                Log.log("Warn:None or multiple html tag found.");
            }
            else
            {
                string s = ms[0].Groups[0].Value;
                if (!s.Contains("xmlns:epub"))
                {
                    text=text.Replace(s, s.Insert(s.Length - 1, " xmlns:epub=\"http://www.idpf.org/2007/ops\""));
                    Log.log( "Added xmlns:epub to "+filename);
                }
            }
        }

        void CheckFootnotes()
        {
            Regex reg_link = new Regex("<a .*?>");
            int pos = 0;
            Match link = reg_link.Match(text);
            while (link.Success)
            {
                XTag tag = new XTag(link.Value);
                if (Contains(tag.GetClassNames(), "duokan-footnote")
                || tag.GetAttribute("epub:type") == "noteref")
                {
                    ProcNote(link, tag);
                }
                pos = link.Index + 1;//假定注释本体都在链接后面
                link = reg_link.Match(text, pos);
            }
        }
        void ProcNote(Match m, XTag tag)
        {
            string note_id="", ref_id;
            //Link tag solve
            {
                var a = tag.GetClassNames();
                if (!Contains(a, "duokan-footnote"))
                {
                    string added = "duokan-footnote";
                    if (a.Length != 0)
                    {
                        added = " " + added;
                    }
                    tag.SetAttribute("class", tag.GetAttribute("class") + added);
                }
            }

            if (tag.GetAttribute("epub:type") != "noteref")
            {
                tag.SetAttribute("epub:type", "noteref");
            }
            {
                string href = tag.GetAttribute("href");
                if (href == "") 
                { Log.log("Error:Cannot find href. id="+note_id); return; }
                if (href[0] != '#')
                 { Log.log("Error:href cannot solve:"+href); return; }
                note_id = href.Substring(1);
            }
            ref_id = tag.GetAttribute("id");
            if (ref_id == "")
            {
                ref_id = note_id + "_ref";
                tag.SetAttribute("id", ref_id);
            }
            text = text.Remove(m.Index, m.Length);
            text = text.Insert(m.Index, tag.ToString());


            //Note content
            ProcNoteContent(note_id, ref_id);

        }
        void ProcNoteContent(string note_id, string ref_id)
        {

            Regex reg_tag = new Regex("<.*?>");
            Regex reg_duokan = new Regex("<ol .*?>");
            Regex reg_aside = new Regex("<aside .*?>");
            int index = -1, length = 0;
            string note_content = null; string list_value = "1";

            Match m = reg_aside.Match(text);
            while (m.Success)
            {
                XTag tag = new XTag(m.Value);
                if (tag.GetAttribute("id") == note_id)
                {
                    index = m.Index;
                    XFragment frag = new XFragment(text, index);
                    if (frag.root != null)
                    {
                        var dk = frag.root.GetElementById(note_id);
                        if (dk != null)
                        {
                            //做过兼容，aside里套多看li
                            note_content = dk.innerXHTML;
                            list_value = dk.tag.GetAttribute("value");
                        }
                        else
                        {
                            note_content = frag.root.innerXHTML;
                        }
                        length = frag.Length;

                    }
                    else
                    {
                        Log.log("Error:Found note but failure on parsing. id="+note_id); return;
                    }
                    break;
                }
                m = m.NextMatch();
            }

            if (index < 0)//如果只对多看适配，没有aside 
            {
                m = reg_duokan.Match(text);
                while (m.Success)
                {
                    XFragment frg = new XFragment(text, m.Index);
                    if (frg.root != null)
                    {
                        if (Contains(frg.root.tag.GetClassNames(), "duokan-footnote-content"))
                        {
                            var a = frg.root.GetElementById(note_id);
                            if (a != null)
                            {
                                index = m.Index;
                                note_content = a.innerXHTML;
                                length = frg.Length;
                                break;
                            }
                        }

                    }
                    m = m.NextMatch();

                }
            }

            if (note_content == null) { Log.log("Error:cannot find note"); return; }
            string template = "<aside epub:type=\"footnote\" id=\"{0}\"><a href=\"#{1}\"></a><ol class=\"duokan-footnote-content\" style=\"list-style:none\"><li class=\"duokan-footnote-item\" id=\"{0}\">{2}</li></ol></aside>";
            string note_full = string.Format(template, note_id, ref_id, note_content);
            text = text.Remove(index, length);
            text = text.Insert(index, note_full);

            Log.log("Formated:" + note_content);
            contain_footnote = true;
        }


        bool Contains(string[] c, string s) { if (c != null) foreach (string x in c) if (x == s) return true; return false; }
    }


    //System.Xml太抠规范了，还是自己简易糊个吧。
    public class XFragment
    {
        public List<XPart> parts = new List<XPart>();
        public XELement root;
        public int Length;
        public XFragment(string text, int start)
        {
            Regex reg_tag = new Regex("<.*?>");
            int count = 0, pos = start;
            Match m;
            do
            {
                m = reg_tag.Match(text, pos);
                if (!m.Success) { Log.log("Error:Unexpect end."); return; }
                XTag tag = new XTag(m.Value);
                if (tag.type == PartType.tag_start) count++;
                if (tag.type == PartType.tag_end) count--;
                if (m.Index > pos) { parts.Add(new XText(text.Substring(pos, m.Index - pos))); }
                parts.Add(tag);
                pos = m.Index + m.Value.Length;
            }
            while (count > 0);
            Length = m.Index - start + m.Value.Length;
            root = new XELement(this, 0);


        }

    }

    public class XELement
    {
        //不包括自己
        public XELement GetElementById(string id)
        {
            foreach (var x in childs)
            {
                if (x.tag.GetAttribute("id") == id) { return x; }
                var r = x.GetElementById(id);
                if (r != null) return r;
            }
            return null;
        }
        public XTag tag { get { return (XTag)doc.parts[start]; } }
        string tagname { get { return tag.tagname; } }

        List<XAttribute> attributes { get { return tag.attributes; } }
        XFragment doc;
        int start, end = -1;
        public List<XELement> childs = new List<XELement>();
        public XELement parent;
        public XELement(XFragment frag, int start)
        {
            doc = frag;
            this.start = start;
            for (int i = start + 1; i < doc.parts.Count; i++)
            {
                if (doc.parts[i].type == PartType.tag_start)
                {
                    XELement ele = new XELement(doc, i);
                    ele.parent = this;
                    childs.Add(ele);
                    i = ele.end + 1;
                }
                if (doc.parts[i].type == PartType.tag_end)
                {
                    if (((XTag)doc.parts[i]).tagname == ((XTag)doc.parts[start]).tagname)
                    {
                        end = i; break;
                    }
                    else
                    {
                        Log.log("Error:dismatched end tag");
                    }
                }
            }
            if (end == -1) Log.log("Error:Closing Tag Failure.");
        }
        public string innerXHTML
        {
            get
            {
                string r = "";
                for (int i = start + 1; i < end; i++)
                {
                    r += doc.parts[i].ToString();
                }
                return r;
            }
        }

    }

    public class XPart
    {
        public PartType type;
    }

    public enum PartType
    {
        text, tag_start, tag_end, tag_single
    }
    public class XText : XPart
    {
        string text;
        public XText(string s)
        {
            text = s;
            type = PartType.text;
        }
        override public string ToString()
        {
            return text;
        }
    }
    public class XTag : XPart
    {

        public string text;
        public string tagname;
        public List<XAttribute> attributes;
        public string GetAttribute(string name)
        {
            foreach (var att in attributes)
                if (att.name == name)
                {
                    return att.value;
                }
            return "";
        }
        public void SetAttribute(string name, string value)
        {

            foreach (var att in attributes)
                if (att.name == name)
                {
                    att.value = value;
                    return;
                }
            attributes.Add(new XAttribute(name, value));
        }

        public string[] GetClassNames()
        {
            foreach (var att in attributes)
                if (att.name == "class")
                {
                    return att.value.Split(' ');
                }
            return null;
        }

        public XTag(string text)
        {

            this.text = text;
            attributes = new List<XAttribute>();
            string intag = text.Substring(1, text.Length - 2);
            if (intag[intag.Length - 1] == '/')
            {
                type = PartType.tag_single;
                intag = intag.Substring(0, intag.Length - 1);
            }
            string[] x = intag.Split(" ");
            tagname = x[0];
            string t = "";
            for (int i = 1; i < x.Length; i++)
            {
                if (x[i].Length == 0) continue;
                t += x[i];
                if (x[i][x[i].Length - 1] != '\"') { t += ' '; continue; }
                attributes.Add(new XAttribute(t));
                t = "";
            }
            if (tagname[0] == '/') { type = PartType.tag_end; tagname = tagname.Substring(1); }
            else type = PartType.tag_start;

        }
        public override string ToString()
        {
            string r = "<";
            if (type == PartType.tag_end) r += "/";
            r += tagname;
            if (attributes != null)
                foreach (var att in attributes)
                {
                    r += string.Format(" {0}=\"{1}\"", att.name, att.value);
                }
            if (type == PartType.tag_single) r += "/";
            r += ">";
            return r;
        }


    }
    public class XAttribute
    {
        public string name, value;
        public XAttribute(string n, string v) { name = n; value = v; }
        public XAttribute(string s)
        {
            int e = s.IndexOf("=");
            name = s.Substring(0, e);
            value = s.Substring(e + 2, s.Length - name.Length - 3);
        }
    }
}