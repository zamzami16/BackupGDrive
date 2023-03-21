﻿using System;
using System.Windows.Forms;

namespace Axata.BackupDrive
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
            //Application.Run(new CreateZipFile(isAuto:false));
            BackupFiles frm = new BackupFiles();
            frm.ManualBackup();
            Application.Run(frm);
        }
    }
}
