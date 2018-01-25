
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Palmmedia.ReportGenerator.Common
{
    class CompareHelper
    {
        public static SortedDictionary<int, string> Report(string path)
        {
            string filename;
            string checkfile;
            int lineNo;

            
            SortedDictionary<int, string> diffDict = new SortedDictionary<int, string>();

          

            XmlDocument xDoc = null;
            try
            {
                xDoc = new XmlDocument();
                xDoc.Load(path);
                filename = xDoc.SelectSingleNode("//filename1").InnerText;
                checkfile = filename.Substring(filename.LastIndexOf('\\') + 1);
                if (File.Exists(filename))
                {
                    XmlNodeList diffNodes = xDoc.SelectNodes("//linecomp[@status='rightorphan']/text[1]");
                    foreach (XmlNode node in diffNodes)
                    {
                        if (node.Attributes["rtid"] == null)
                            continue;
                        lineNo = Convert.ToInt32(node.Attributes["rtid"].Value);
                        diffDict.Add(lineNo, node.InnerText);
                    }
                    XmlNodeList diffNodes1 = xDoc.SelectNodes("//linecomp[@status='different']/text[2]");
                    foreach (XmlNode node in diffNodes1)
                    {
                        if (node.Attributes["rtid"] == null)
                            continue;
                        lineNo = Convert.ToInt32(node.Attributes["rtid"].Value);
                        diffDict.Add(lineNo, node.InnerText);
                    }

                    return diffDict;
                }
            }
            catch (Exception e)
            {
                return diffDict;
            }
            return diffDict;
        }
        public class Asmbly
        {
            public int ID;
            public string name;
            public List<BuildChanges> buildchanges = new List<BuildChanges>();
            public List<Classes> classes = new List<Classes>();
            public int coveredLines;
            public int uncoveredLines;
            public int coverableLines;
            public int totalLines;
            public int newlines;
            public int testednewlines;
        }
        public class BuildChanges
        {
            public string name;
            public List<string> buildlines = new List<string>();
            public List<string> buildtestedlines = new List<string>();
        }
        public class Classes
        {
            public string name;
            public int coveredLines;
            public int uncoveredLines;
            public int coverableLines;
            public int totalLines;
            public int newlines;
            public int testednewlines;
            public string testcoverage;
            public string coverageType;
            public string coverage;
            public string methodCoverage;
            public string branchCoverage;
            public int coveredBranches;
            public int totalBranches;
            public List<string> buildlines = new List<string>();
            public List<string> lineCoverageHistory = new List<string>();
            public List<string> branchCoverageHistory = new List<string>();
        }
        public static List<Asmbly> ReadJson()
        {
            List<Asmbly> buildlines = new List<Asmbly>();
            try
            {
                Assembly asm = Assembly.LoadFrom("ReportHelper.dll");
                if (asm != null)
                {
                    Type type = asm.GetType("Report.ReportHelper");
                    object ob = new List<Asmbly>();
                    object[] methodData = null;
                    methodData = new object[1];
                    object obj = Activator.CreateInstance(type);
                    ob = type.InvokeMember("ReadJson", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, null);
                    //string st = JsonConvert.SerializeObject(ob);
                    buildlines = ob as List<Asmbly>;// JsonConvert.DeserializeObject<List<Asmbly>>(st);
                    return buildlines;
                }
                return buildlines;
            }
            catch (Exception e)
            {
                return buildlines;
            }
        }
        public static List<Asmbly> ReadAsmbly(string asmblyname)
        {
            List<Asmbly> buildlines = new List<Asmbly>();
            try
            {
                Assembly asm = Assembly.LoadFrom("ReportHelper.dll");
                if (asm != null)
                {
                    Type type = asm.GetType("Report.ReportHelper");
                    object ob = new List<Asmbly>();
                    object[] methodData = null;
                    methodData = new object[1];
                    methodData[0] = asmblyname;
                    object obj = Activator.CreateInstance(type);
                    ob = type.InvokeMember("ReadAsmbly", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, methodData);
                    string st = JsonConvert.SerializeObject(ob);
                    buildlines = JsonConvert.DeserializeObject<List<Asmbly>>(st);
                    return buildlines;
                }
                return buildlines;
            }
            catch (Exception e)
            {
                return buildlines;
            }
        }
    }
}
