using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace TestBackupApps
{
    public partial class CopyForm : Form
    {
        public string OriginFile { get; set; }
        public string DestinationFile { get; set; }

        public CopyForm(bool isAuto = true)
        {
            InitializeComponent();
        }
        public CopyForm(string origin, string destination, bool isAuto=true)
        {
            InitializeComponent();
            if (isAuto ) { this.WindowState = FormWindowState.Minimized; }
            else { this.WindowState = FormWindowState.Normal;}
            OriginFile = origin;
            DestinationFile = destination;
        }

        private void CopyFile(string origin, string destination)
        {
            // Start the BackgroundWorker to perform the file copy.
            var worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync(new CopyFileArgs(origin, destination));
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = (CopyFileArgs)e.Argument;
            var sourceFilePath = args.SourceFilePath;
            var destinationFilePath = args.DestinationFilePath;

            // Get the size of the source file.
            var fileSize = new FileInfo(sourceFilePath).Length;

            // Create the source and destination streams.
            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
            {
                // Copy the file in chunks, reporting progress as each chunk is copied.
                var buffer = new byte[4096];
                int bytesRead;
                long totalBytesRead = 0;
                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destinationStream.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    var progress = (int)(totalBytesRead * 100 / fileSize);
                    ((BackgroundWorker)sender).ReportProgress(progress);
                    if (((BackgroundWorker)sender).CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }
        }
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Update the progress bar.
            progressBar1.Value = e.ProgressPercentage;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // If the copy was cancelled, show a message indicating that the operation was cancelled.
            if (e.Cancelled)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }

            // If there was an error during the copy, show an error message.
            if (e.Error != null)
            {
                this.DialogResult = DialogResult.None;
                this.Close();
            }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CopyForm_Load(object sender, System.EventArgs e)
        {
            if (OriginFile == null || DestinationFile == null) { this.DialogResult = DialogResult.None; throw new System.Exception("File harus diberikan."); }
            else CopyFile(OriginFile, DestinationFile);
        }
    }

    public class CopyFileArgs
    {
        public string SourceFilePath { get; set; }
        public string DestinationFilePath { get; set; }

        public CopyFileArgs(string sourceFilePath, string destinationFilePath)
        {
            SourceFilePath = sourceFilePath;
            DestinationFilePath = destinationFilePath;
        }
    }
}
