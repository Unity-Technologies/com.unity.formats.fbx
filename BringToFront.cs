using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices; 
using System.Diagnostics;

namespace FbxExporters
{
    class Program
    {

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        const int SW_RESTORE = 9;
        public static void bringToFront(string title)
        {
            // Get a handle to the application.
            IntPtr handle = FindWindow(null, title);

            // Verify that this is a running process.
            if (handle == IntPtr.Zero)
            {
                return;
            }
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }

        static void Main(string[] args)
        {
            Process[] processlist = Process.GetProcessesByName("Unity");

            bool found = false;
            foreach (Process process in processlist)
            {
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    if(args.Length > 0 && !process.MainWindowTitle.Contains(args[0]))
                    {
                        continue;
                    }
                    bringToFront(process.MainWindowTitle);
                    found = true;
                }
            }
            
            if(!found && args.Length > 1){
                Process myProcess = new Process();
                myProcess.StartInfo.FileName = args[1];
                if(args.Length > 2){
                    myProcess.StartInfo.Arguments = args[2]; 
                }
                myProcess.Start();
            }
        }
    }
}