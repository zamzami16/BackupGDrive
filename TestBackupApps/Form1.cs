using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace TestBackupApps
{
    public partial class Form1 : Form
    {
        private string FileName = null;
        private readonly string iniFiles = @"Backup\";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                ManualBackup();
            }
            catch (Exception)
            {
                MessageBox.Show("Error");
            }
        }
        private void ManualBackup()
        {
            try
            {
                var filename = $"Axata_{DateTime.Now:yyyy-MM-dd}.axt";
                FileName = @"Backup\" + filename;
                /*CopyForm frm = new CopyForm(isAuto: false);
                frm.OriginFile = "Axata_WSS.axt";
                frm.DestinationFile = FileName;
                frm.ShowDialog();*/

                if (!File.Exists(FileName))
                {
                    var worker = new BackgroundWorker();
                    worker.DoWork += (s, args) =>
                    {
                        File.Copy("Axata_WSS.axt", FileName, true);
                    };
                    worker.RunWorkerCompleted += (s, args) =>
                    {
                        IniFile ini = new IniFile(@"Backup\BackupInfo.ini");
                        ini.Write("FileName", filename, "main");

                        // Execute the external executable.
                        //Process.Start(@"Backup\ManualBackupDrive.exe");
                        var processStartInfo = new ProcessStartInfo();
                        processStartInfo.FileName = @"ManualBackupDrive.exe";
                        processStartInfo.WorkingDirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Backup");
                        Process.Start(processStartInfo);
                    };
                    worker.RunWorkerAsync();
                }
                else
                {
                    IniFile ini = new IniFile(@"Backup\BackupInfo.ini");
                    ini.Write("FileName", filename, "main");

                    var processStartInfo = new ProcessStartInfo();
                    processStartInfo.FileName = @"ManualBackupDrive.exe";
                    processStartInfo.WorkingDirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Backup");
                    Process.Start(processStartInfo);
                }
                
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
