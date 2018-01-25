using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareHelper
{
    class CompareHelper
    {
        private const string CMD = "cmd.exe";
        private const string cmdFormat = "/C \"\"{0}\"  /SILENT \"@{1}\" \"{2}\" \"{3}\" \"{4}\"\"";
        public const string reportPostfix = "_report.xml";

        private const string scriptName = "BCompScript.txt";
        private const string BCompScript = "file-report layout:xml options:ignore-unimportant,display-mismatches,line-numbers output-to:\"%3\" \"%1\" \"%2\"";
        public static string ApplicationPath { get; set; }
        public static string ScriptPath
        {
            get
            {
                string tempPath = Path.Combine(Path.GetTempPath(), scriptName);
                if (!File.Exists(tempPath))
                    File.WriteAllText(tempPath, BCompScript);

                return tempPath;
            }
        }

        static CompareHelper()
        {
            try
            {
                if (System.Environment.Is64BitOperatingSystem)
                    ApplicationPath = @"C:\Program Files (x86)\Beyond Compare 3\BComp.exe";
                else
                    ApplicationPath = @"C:\Program Files\Beyond Compare 3\BComp.exe";
            }

            catch (Exception ex)
            {
                return;
            }

        }
        public static string connectfile { get; set; }

        public static string CompareFiles(string leftFile, string rightFile)
        {
            try
            {

                //using (UNCAccessWithCredentials UNC = new UNCAccessWithCredentials())
                //{
                //    if (UNC.NetUseWithCredentials(Connect.NetworkPath, "zDEV-APPLorenz206", "cscidp", "DEF4#IUf"))
                //    {

                //        return CompareApp(leftFile, rightFile);

                //    }
                //    else
                //    {
                return CompareApp(leftFile, rightFile);
                //    }
                //}

            }
            catch (Exception ex)
            {
                return null;
            }
            //return null;
        }
        public static string MakeUnique(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string fileExt = Path.GetExtension(path);

            for (int i = 1; ; ++i)
            {
                if (!File.Exists(path))
                    return path;

                path = Path.Combine(dir, fileName + " " + i + fileExt);
            }
        }
        private static string CompareApp(string leftFile, string rightFile)
        {
            if (!File.Exists(leftFile) || !File.Exists(rightFile))
                return string.Empty;


            connectfile = Path.GetFileName(rightFile);
            string a = "\\" + Path.GetFileName(rightFile);
            string[] fname = rightFile.Split('\\');
            string temp = string.Empty;
            int len = fname.Length;
            temp = "\\" + fname[len - 1] + "_"; //+ fname[len - 3] +"_"+ fname[len - 2] + "_" 


            string reportPath = @System.Configuration.ConfigurationManager.AppSettings["reportPath"] + temp + reportPostfix;//"d:\\test2" + a + reportPostfix;
            //reportPath = MakeUnique(reportPath);
            string startPgm = String.Format(cmdFormat, ApplicationPath, ScriptPath, leftFile, rightFile, reportPath);
            ProcessStartInfo psi = new ProcessStartInfo(CMD, startPgm)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            using (Process proc = Process.Start(psi))
            {
                proc.WaitForExit();
                int exitCode = proc.ExitCode;
            }
            return reportPath;
        }
        static bool FileEquals(string path1, string path2)
        {
            FileInfo f1 = new FileInfo(path1);
            FileInfo f2 = new FileInfo(path2);
            if (f1.Length != f2.Length)
            {
                return false;
            }
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        static void Main(string[] args)
        {
            try
            {
                string[] leftfilePaths = Directory.GetFiles(@System.Configuration.ConfigurationManager.AppSettings["leftfilePath"], "*.cs", System.IO.SearchOption.AllDirectories);
                string[] rightfilePaths = Directory.GetFiles(@System.Configuration.ConfigurationManager.AppSettings["rightfilePath"], "*.cs", System.IO.SearchOption.AllDirectories);
                foreach (var file in leftfilePaths)
                {

                    string[] fname = file.Split('\\');
                    string temp = string.Empty;
                    int len = fname.Length;
                    temp = "\\" + fname[len - 1];//+ fname[len - 3] + "\\" + fname[len - 2] + "\\"
                    int pos = Array.FindIndex(rightfilePaths, x => x.Contains(temp));
                    string path = string.Empty;
                    if (pos > -1)
                    {
                        path = rightfilePaths[pos];
                        if (!FileEquals(file, path))
                            CompareFiles(file, path);
                    }

                }
                //CompareFiles(file, rightfilePaths[pos]);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
