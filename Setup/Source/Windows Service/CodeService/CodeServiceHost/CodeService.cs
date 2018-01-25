using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace CodeServiceHost
{
    public partial class CodeService : ServiceBase
    {
        Process proc = new Process();
        public CodeService()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerAsync();
        }
        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            CodeCoverageStart();
        }

        private bool ServiceStop(string ServiceName)
        {
            ServiceController sc = new ServiceController(ServiceName);
            if (sc.Status.Equals(ServiceControllerStatus.Running))
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                return true;
            }
            else if (sc.Status.Equals(ServiceControllerStatus.StopPending))
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                return true;
            }
            else if (sc.Status.Equals(ServiceControllerStatus.Stopped))
            {
                return true;
            }
            else
                return false;
        }
        private bool ServiceStart(string ServiceName)
        {
            ServiceController sc = new ServiceController(ServiceName);
            if (sc.Status.Equals(ServiceControllerStatus.Stopped))
            {
                Thread.Sleep(5000);
                sc.Start();
                return true;
            }
            else if (sc.Status.Equals(ServiceControllerStatus.StopPending))
            {
                Thread.Sleep(5000);
                sc.Start();
                return true;
            }
            else
                return false;
        }

        private void CodeCoverageStart()
        {
            try
            {
                if (ServiceStop("W3SVC"))
                {
                    string w3wp = System.Configuration.ConfigurationManager.AppSettings["w3wp"];
                    string targetdir = System.Configuration.ConfigurationManager.AppSettings["targetdir"];
                    string output = System.Configuration.ConfigurationManager.AppSettings["output"];
                    string filter = System.Configuration.ConfigurationManager.AppSettings["filter"];
                    string debugmode = System.Configuration.ConfigurationManager.AppSettings["debugmode"];
                    string opencover = System.Configuration.ConfigurationManager.AppSettings["opencover"];
                    string parameters = System.Configuration.ConfigurationManager.AppSettings["parameters"];
                    output = output + "code" + DateTime.Now.ToString("MMddyyyyhhmmss") + ".xml";
                    string arguments = String.Format(" -register:user -target:\"{0}\" -targetargs:\"{1}\" -targetdir:\"{2}\" -filter:\"{3}\" -output:\"{4}\" {5}", w3wp, debugmode, targetdir, filter, output, parameters);
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.FileName = @opencover;
                    proc.StartInfo.Arguments = arguments;
                    proc.EnableRaisingEvents = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.OutputDataReceived += new DataReceivedEventHandler((s, e) => { Output(e.Data); });
                    proc.Start();
                    proc.BeginOutputReadLine();
                    //proc.WaitForExit();
                    //proc.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
        protected override void OnStop()
        {
            //System.Diagnostics.Debugger.Launch();
            //int pid = GetIISProcessID("DefaultAppPool");
            //if (pid != 0)
            //{
            //    Process p = Process.GetProcessById(pid);
            //    p.Kill();
            //}
            try
            {
                foreach (var process in Process.GetProcessesByName("w3wp"))
                {
                    process.Kill();
                }
                ServiceStart("W3SVC");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }
        //public static int GetIISProcessID(string appPoolName)
        //{
        //    ServerManager serverManager = new ServerManager();
        //    foreach (WorkerProcess workerProcess in serverManager.WorkerProcesses)
        //    {
        //        if (workerProcess.AppPoolName.Equals(appPoolName))
        //            return workerProcess.ProcessId;
        //    }

        //    return 0;
        //}
        private static void Output(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Trace.WriteLine(data.ToString());
            }
        }
    }
}
