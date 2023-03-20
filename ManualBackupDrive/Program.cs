using Axata.BackupDrive;
using System;
using System.Windows.Forms;

namespace ManualBackupDrive
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            BackupFiles frm = new BackupFiles();
            frm.ManualBackup();
            Application.Run(frm);
        }
    }
}
