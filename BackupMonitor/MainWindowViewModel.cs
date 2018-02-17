using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BackupMonitor
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public BindingList<BackupItem> BackupItems { get; set; }
        public string RefreshFrequency { get; set; }
        public List<string> RefreshFrequencyList { get; set; }
        public BackupItem SelectedBackupItem { get; set; }
        private BackgroundWorker _bgLoadStatus;
        private List<BackgroundWorker> _bgCopyFiles;

        private const string STATUS_UNKNOWN = "LOADING...";
        private const string STATUS_OK = "GOOD";
        private const string STATUS_NEEDSUPDATE_FILECOUNT = "MISSING FILES!";
        private const string STATUS_NEEDSUPDATE_DATE = "UPDATE!";
        private const string STATUS_BACKINGUP = "UPDATING...";

        private Repository _repostitory;

        private string _statusText;
        public string StatusText
        {
            get { return _statusText; }
            set { _statusText = value; OnPropertyChanged(); }
        }

        public MainWindowViewModel()
        {
            //InitializeFrequencyList();

            _repostitory = new Repository();

            _copyFilesCommand = new RelayCommand(obj => CopyFiles((BackupItem)obj));
            _refreshScreenCommand = new RelayCommand(obj => RefreshScreen());


            BackupItems = _repostitory.ReadBackupItems();

            _bgLoadStatus = new BackgroundWorker();
            _bgLoadStatus.WorkerSupportsCancellation = true;
            _bgLoadStatus.WorkerReportsProgress = true;
            _bgLoadStatus.DoWork += _bgLoadStatus_DoWork;
            _bgLoadStatus.RunWorkerCompleted += _bgLoadStatus_RunWorkerCompleted;
            _bgLoadStatus.RunWorkerAsync();

            _bgCopyFiles = new List<BackgroundWorker>();
            //_bgCopyFiles = new BackgroundWorker();
            //_bgCopyFiles.DoWork += CopyFilesBackground;
        }

        private void _bgLoadStatus_DoWork(object sender, DoWorkEventArgs e)
        {
            Parallel.ForEach(BackupItems, new Action<BackupItem>(UpdateItemStatus));
        }
        private void _bgLoadStatus_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StatusText = "Refreshed all statuses " + DateTime.Today.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
        }

        private readonly RelayCommand _copyFilesCommand;
        public ICommand CopyFilesCommand
        {
            get { return _copyFilesCommand; }
        }
        private void CopyFiles(BackupItem item)
        {
            //if (_bgCopyFiles.IsBusy)
            //{
            //    StatusText = "Worker is busy. Try again later";
            //}
            //else
            //{
            //    item.Status = STATUS_BACKINGUP;
            //    //item.Note = CopyFiles(item.SourcePath, item.TargetPath, commandOptions);
            //    //UpdateItemStatus(item);
            //    _bgCopyFiles.RunWorkerAsync(item);
            //}
            var bgCopyFiles = new BackgroundWorker();
            bgCopyFiles.DoWork += CopyFilesBackground;
            bgCopyFiles.RunWorkerCompleted += UpdateBackgroundWorkerStatus;
            _bgCopyFiles.Add(bgCopyFiles);
            StatusText = String.Format("Working on files for {0} folders", _bgCopyFiles.Count);
            item.Status = STATUS_BACKINGUP;
            bgCopyFiles.RunWorkerAsync(item);
        }
        private void CopyFilesBackground(object sender, DoWorkEventArgs e)
        {
            BackupItem item = (BackupItem)e.Argument;
            item.Note = CopyFiles(item.SourcePath, item.TargetPath);
            UpdateItemStatus(item);
        }


        private void UpdateBackgroundWorkerStatus(object sender, RunWorkerCompletedEventArgs e)
        {
            for (int i = _bgCopyFiles.Count - 1; i >= 0; i--)
            {
                var bw = _bgCopyFiles[i];
                if (!bw.IsBusy)
                {
                    _bgCopyFiles.Remove(bw);
                }
            }
            StatusText = string.Empty;
            if (_bgCopyFiles.Count > 0)
            {
                StatusText = String.Format("Working on files for {0} folders", _bgCopyFiles.Count);
            }
        }

        private readonly RelayCommand _refreshScreenCommand;
        public ICommand RefreshScreenCommand
        {
            get { return _refreshScreenCommand; }
        }
        private void RefreshScreen()
        {
            if (_bgLoadStatus.IsBusy)
            {
                StatusText = "System is busy updating statuses. Try again once all statuses are loaded.";
            }
            else
            {
                StatusText = "Refreshing statuses...";
                foreach (var item in BackupItems)
                {
                    item.Status = STATUS_UNKNOWN;
                }
                _bgLoadStatus.RunWorkerAsync();
            }
        }


        //private void InitializeFrequencyList()
        //{
        //    RefreshFrequencyList = new List<string>();
        //    RefreshFrequencyList.Add("Never");
        //    RefreshFrequencyList.Add("30s");
        //    RefreshFrequencyList.Add("60s");
        //    RefreshFrequencyList.Add("15m");
        //    RefreshFrequencyList.Add("30m");
        //    RefreshFrequencyList.Add("60m");
        //    RefreshFrequencyList.Add("2h");
        //    RefreshFrequencyList.Add("4h");
        //    RefreshFrequencyList.Add("12h");
        //    RefreshFrequencyList.Add("24h");
        //    RefreshFrequencyList.Add("2d");
        //    RefreshFrequencyList.Add("3d");

        //    RefreshFrequency = "30s";
        //}


        private void UpdateItemStatus(BackupItem item)
        {
            //item.OriginalSourceDirText = GetFolderContents(item.SourcePath);
            //item.NewSourceDirText = GetFolderContents(item.TargetPath);
            //if (item.OriginalSourceDirText.Equals(item.NewSourceDirText))
            //{
            //    item.Status = "SAME";
            //}
            //else
            //{
            //    item.Status = "DIFFERENT";
            //}
            int sourceFileCount = GetFolderFileCount(item.SourcePath);
            int targetFileCount = GetFolderFileCount(item.TargetPath);
            if (sourceFileCount > targetFileCount)
            {
                item.Status = STATUS_NEEDSUPDATE_FILECOUNT;
            }
            else
            {
                DateTime sourceUpdatedDate = GetFolderMostRecentDate(item.SourcePath);
                DateTime targetUpdatedDate = GetFolderMostRecentDate(item.TargetPath);
                if (sourceUpdatedDate > targetUpdatedDate)
                {
                    item.Status = STATUS_NEEDSUPDATE_DATE;
                }
                else
                {
                    item.Status = STATUS_OK;
                }
            }
        }

        private int GetFolderFileCount(string path)
        {
            var fils = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            return fils.Length;
        }
        private DateTime GetFolderMostRecentDate(string path)
        {
            DateTime mostRecentDate = new DateTime();
            var fils = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (var fil in fils)
            {
                DateTime d = File.GetLastWriteTime(fil);
                if (d > mostRecentDate)
                {
                    mostRecentDate = d;
                }
            }
            return mostRecentDate;
        }

        //private string GetFolderContents(string path)
        //{
        //    string output = string.Empty;

        //    try
        //    {
        //        var fils = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        //        foreach (var fil in fils)
        //        {
        //            DateTime d = File.GetLastWriteTime(fil);
        //            //output += d.ToShortDateString() + " " + d.ToLongTimeString() + " ";
        //            //output += " ";
        //            //output += d.ToLongTimeString();
        //            //output += " ";
        //            output += fil.Substring(fil.LastIndexOf("\\") + 1);
        //            output += Environment.NewLine;
        //        }
        //        //output = String.Join(Environment.NewLine, fils);
        //    }
        //    catch (DirectoryNotFoundException ex)
        //    {
        //        // assume there are no files
        //    }

        //    return output;
        //}

        //private string GetFolderContents(string path)
        //{
        //    string output = null;

        //    if (!Directory.Exists(path))
        //    {
        //        Directory.CreateDirectory(path);
        //    }

        //    Process p = new Process();
        //    p.StartInfo.UseShellExecute = false;
        //    p.StartInfo.RedirectStandardOutput = true;
        //    p.StartInfo.FileName = "cmd.exe";
        //    p.StartInfo.WorkingDirectory = path;
        //    p.StartInfo.Arguments = "/s /o dir";
        //    p.Start();
        //    output = p.StandardOutput.ReadToEnd();
        //    p.WaitForExit();

        //    int pos = output.IndexOf("Directory of");
        //    pos = output.IndexOf(Environment.NewLine, pos);
        //    pos = output.IndexOf("..", pos);
        //    output = output.Substring(pos + 2);
        //    pos = output.IndexOf("File(s)");
        //    output = output.Substring(0, pos);

        //    return output;
        //}



        private string CopyFiles(string SourceDir, string TargetDir)
        {
            string output = string.Empty;

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "robocopy.exe";
            p.StartInfo.Arguments = "/E /R:5 \"" + SourceDir + "\" \"" + TargetDir + "\"";
            p.Start();
            output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            int pos = output.LastIndexOf("--------------------------------------------------------------------------");
            pos = output.IndexOf(Environment.NewLine, pos);
            output = output.Substring(pos + 1);

            return output;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string memberName = "")
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(memberName));
            }
        }
    }

}
