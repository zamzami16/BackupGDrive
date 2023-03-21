using Axata.BackupDrive.Utils;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Axata.BackupDrive
{
    public partial class BackupFiles : Form
    {
        private delegate void UpdateProgressDelegate(int progress);
        private CancellationTokenSource _cancellationTokenSource;
        private UserCredential credential;
        private bool _Authed = false;
        private bool Cancelled = false;
        private readonly string BaseFolder = @"AxataBackup";
        private string IdBaseFolder = null;
        private bool UploadComplete = false;
        private readonly string TokenFileName = "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user";
        private string FilePathToUploaded = null;
        private string FileName = null;
        private bool SudahDiupload = false;
        public bool IsAutoBackup { get; set; }
        private IniFile iniFile;

        public bool Authed => _Authed;
        public BackupFiles()
        {
            InitializeComponent();
            IsAutoBackup = false;
        }
        private static string GetFolderId(DriveService service, string folderName)
        {
            // This method will get the ID of a folder with the given name
            var request = service.Files.List();
            request.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
            var result = request.Execute();
            foreach (var file in result.Files)
            {
                return file.Id;
            }
            return null;
        }
        private static string GetFileExist(DriveService service, string fileName, string folderName)
        {
            // This method will get the ID of a folder with the given name
            var folderId = GetFolderId(service, folderName);
            if (folderId == null)
                return null;

            var fileListRequest = service.Files.List();
            fileListRequest.Q = "mimeType!='application/vnd.google-apps.folder' and trashed = false and name = '" + fileName + "' and parents in '" + folderId + "'";
            fileListRequest.Fields = "files(id)";
            var fileListResult = fileListRequest.Execute();
            foreach (var file in fileListResult.Files)
            {
                return file.Id;
            }
            return null;
        }

        private static string CreateFolder(DriveService service, string folderName)
        {
            // This method will create a new folder with the given name
            var folderMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };

            var request = service.Files.Create(folderMetadata);
            request.Fields = "id";
            var file = request.Execute();
            return file.Id;
        }

        private void UpdateProgress(int progress)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new UpdateProgressDelegate(this.UpdateProgress), progress);
            }
            else
            {
                progressBar1.Value = progress;
                if (UploadComplete)
                    UploadHasComplete();
            }
        }

        private void CreateCredentials()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Axata.BackupDrive.client_secret_axata.json"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string jsonString = reader.ReadToEnd();
                        // Use the JSON string as needed
                        using (var streams = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var cancel_token = new CancellationTokenSource();
                            // Add event listener to cancel token when user navigates away or closes the tab
                            this.FormClosing += new FormClosingEventHandler((sender, e) =>
                            {
                                if (e.CloseReason == CloseReason.UserClosing)
                                {
                                    // Cancel the authorization task if the user closes the form
                                    cancel_token.Cancel();
                                }
                            });
                            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                GoogleClientSecrets.FromStream(streams).Secrets,
                                new[] { DriveService.Scope.Drive },
                                "user",
                                cancel_token.Token,
                                new FileDataStore(".AxataPOS.Data")).Result;
                            if (cancel_token.Token.IsCancellationRequested)
                            {
                                if (MessageBox.Show("Gagal autentikasi ke google drive...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                                    Application.Exit();
                            }
                        }
                    }
                }
                _Authed = true;
            }
            catch (Exception)
            {
                _Authed = false;
                throw new Exception("Authorisasi dengan google gagal.");
            }
        }

        private void BackupFile(string filePath)
        {
            try { _GetAuth(); }
            catch (OperationCanceledException) { LblStatus.Text = "cancelled"; Cancelled = true; }
            catch (Exception) { _Authed = false; }
            finally { if (_Authed && !Cancelled) { Upload(filePath); } else { MessageBox.Show("Autentikasi gagal."); Application.Exit(); } }
        }

        private void Upload(string filePath)
        {
            // Start the file upload
            _cancellationTokenSource = new CancellationTokenSource();

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "BackupGDrive",
            });

            // get id or create basefolder first
            IdBaseFolder = GetFolderId(service, BaseFolder) ?? CreateFolder(service, BaseFolder);

            // Upload file
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(filePath),
                Parents = new[] { IdBaseFolder }
            };

            var existFileId = GetFileExist(service, filePath, BaseFolder);
            var uploadStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open);
            if (existFileId == null)
            {
                var request = service.Files.Create(fileMetadata, uploadStream, "application/octet-stream");
                request.Fields = "id";
                request.ChunkSize = ResumableUpload.MinimumChunkSize;
                request.ProgressChanged += (IUploadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case UploadStatus.Starting:
                            Console.WriteLine("Upload starting");
                            LblStatus.Text = "Upload Files ...";
                            btnStart.Enabled = false;
                            break;
                        case UploadStatus.Uploading:
                            Console.WriteLine("{0} bytes sent", progress.BytesSent);
                            UpdateProgress((int)(progress.BytesSent * 100 / uploadStream.Length));
                            break;
                        case UploadStatus.Completed:
                            Console.WriteLine("Upload completed");
                            LblStatus.Text = "Upload selesai ...";
                            UpdateProgress(100);
                            btnCancel.Enabled = false;
                            UploadComplete = true;
                            uploadStream.Dispose();
                            break;
                        case UploadStatus.Failed:
                            Console.WriteLine("Upload failed");
                            Application.Exit();
                            break;
                    }
                };
                request.UploadAsync(_cancellationTokenSource.Token);
            }
            else
            { 
                MessageBox.Show("File sudah ada di google drive.", "File Exist", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                btnStart.Enabled = SudahDiupload = true;
                uploadStream.Dispose();
            }
        }

        private void UploadHasComplete()
        {
            File.Delete(FilePathToUploaded);
            MessageBox.Show("Database berhasil di upload ke Google Divre");
            Application.Exit();
        }

        private string GetHomeDirectory()
        {
            string environmentVariable = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(environmentVariable))
            {
                return environmentVariable;
            }

            string environmentVariable2 = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(environmentVariable2))
            {
                string text = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (string.IsNullOrEmpty(text))
                {
                    text = Path.Combine(environmentVariable2, ".local", "share");
                }
                return Path.Combine(text, "google-filedatastore");
            }
            throw new PlatformNotSupportedException("Relative FileDataStore paths not supported on this platform.");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            LblStatus.Text = "Menyiapkan File ...";
            FileName = iniFile.Read("FileName", "main");
            if (!File.Exists(FileName))
            {
                if (MessageBox.Show("File tidak ditemukan.", "error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                    Application.Exit();
            }

            FileInfo fileInfo = new FileInfo(FileName);
            FilePathToUploaded = Path.ChangeExtension(FileName, ".zip");
            var thread = new Thread(() =>
            {
                Compression.Compress(fileInfo, FilePathToUploaded);
                BackupFile(FilePathToUploaded);
            });
            thread.Start();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            Application.Exit();
        }

        private void BackupFiles_Load(object sender, EventArgs e)
        {
            if (IsAutoBackup) btnStart_Click(sender, e);
            else this.WindowState = FormWindowState.Normal;

            if (!File.Exists("BackupInfo.ini"))
            {
                if (MessageBox.Show("'BackupInfo.ini' tidak ditemukan.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                {
                    Application.Exit();
                }
            }
            else
            {
                try
                {
                    iniFile = new IniFile("BackupInfo.ini");
                    if (!iniFile.KeyExists("FileName", "main"))
                        throw new InvalidOperationException();
                }
                catch (InvalidOperationException)
                {
                    if (MessageBox.Show("Terjadi kesalahan saat menyiapkan file,\nUlangi lagi.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
                        Application.Exit();
                }
            }
        }

        private void UpdateIniFile()
        {
            IniFile iniFile = new IniFile("BackupInfo.ini");
            iniFile.Write("LastBackup", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "main");
            iniFile.DeleteKey("FileName", "main");
        }

        private void _GetAuth(int timewait = 300)
        {
            try
            {
                Task task = Task.Run(() => { CreateCredentials(); });
                TimeSpan ts = TimeSpan.FromSeconds(timewait);
                if (!task.Wait(ts)) { _Authed = false; throw new Exception("Autentikasi gagal."); }
            }
            catch (Exception) { throw; }
        }

        public void GetAuth(int timewait = 300)
        {
            try { _GetAuth(timewait); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
        }
        public void AuthoBackup()
        {
            IsAutoBackup = true;
            this.Show();
        }
        public void ManualBackup()
        {
            IsAutoBackup = false;
            this.Show();
        }

        private void BackupFiles_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (UploadComplete)
            {
                UpdateIniFile();
                File.Delete(FileName);
                File.Delete(FilePathToUploaded);
            }
            if (SudahDiupload) {
                File.Delete(FileName);
                File.Delete(FilePathToUploaded);
            }
        }
    }
}