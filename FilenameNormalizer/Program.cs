using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

namespace FilenameNormalizer
{
    static class Program
    {
        private static bool CreateShortcut(string shortcut, string exe, string dir)
        {
            bool created = false;
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
            object shell = Activator.CreateInstance(t);
            try
            {
                object lnk = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcut });
                try
                {
                    string targetPath = (string)t.InvokeMember("TargetPath", BindingFlags.GetProperty, null, lnk, null);
                    if (targetPath != exe)
                    {
                        t.InvokeMember("TargetPath", BindingFlags.SetProperty, null, lnk, new object[] { exe });
                        t.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, lnk, new object[] { dir });
                        t.InvokeMember("IconLocation", BindingFlags.SetProperty, null, lnk, new object[] { String.Format("{0}, 1", exe) });
                        t.InvokeMember("Save", BindingFlags.InvokeMethod, null, lnk, null);
                        created = true;
                    }
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
            return created;
        }

        private static bool InstallSendToShortcut()
        {
            String shortcutPath = String.Format("{0}\\FilenameNormalizer.lnk", Environment.GetFolderPath(Environment.SpecialFolder.SendTo));
            string exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            string exePath = Path.GetDirectoryName(Application.ExecutablePath);
            return CreateShortcut(shortcutPath, string.Format("{0}\\{1}.exe", exePath, exeName), exePath);
        }

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (InstallSendToShortcut())
            {
                MessageBox.Show("Acceso directo creado correctamente");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var frmPrincipal = new FrmFilenameNormalizer();
            if (args.Count() > 0)
            {
                String path = args[0];
                if (!Directory.Exists(path))
                {
                    if (File.Exists(path))
                    {
                        path = Path.GetDirectoryName(path);
                        frmPrincipal.InitialPath = path;
                    }
                }
                else
                {
                    frmPrincipal.InitialPath = path;
                }
            }
            Application.Run(frmPrincipal);
        }
    }
}
