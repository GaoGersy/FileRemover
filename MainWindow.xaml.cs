using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace FileRemover
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private int count = 0;
        private Thread _currentThread = null;
        private string _pattern = "*.*";
        private bool isEarlier = true;
        private DateTime _selectedDate;
        private List<FileInfo> files = null;

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            DatePicker.SelectedDate = DateTime.Now;
            DatePicker.SelectedDateFormat = DatePickerFormat.Long;
        }

        private void SelectDir(object sender, RoutedEventArgs routedEventArgs)
        {
            FolderBrowserDialog openFileDialog = new FolderBrowserDialog(); //选择文件夹
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
            ) //注意，此处一定要手动引入System.Window.Forms空间，否则你如果使用默认的DialogResult会发现没有OK属性
            {
                FilePathTextBox.Text = openFileDialog.SelectedPath;
            }
        }

        private void ScanFile(string path)
        {
            if (files != null)
            {
                files.Clear();
                files = null;
            }
            var directoryInfo = new DirectoryInfo(path);
            if (FilePathTextBox.Text.Trim().Equals("")||!directoryInfo.Exists)
            {
                MessageBox.Show("请选择正确的文件路径");
                StartButton.Text = "开始";
                return;
            }
            files = new List<FileInfo>();
            GetPattern();
            FileNameTextBlock.Text = "正在扫描...";
            count = 0;
            CountButton.Text = "文件总数：0";
            if (TimeCheckBox.IsChecked != null) isEarlier = (bool) TimeCheckBox.IsChecked;
            if (DatePicker.SelectedDate != null) _selectedDate = (DateTime) DatePicker.SelectedDate;
            StopScan();
            _currentThread = new Thread(ScanDirectory);
            _currentThread.Start(directoryInfo);
        }

        private void StopScan()
        {
            if (_currentThread != null)
            {
                if (_currentThread.IsAlive)
                {
                    _currentThread.Abort();
                    _currentThread = null;
                }
            }
        }

        private void ScanDirectory(object obj)
        {
            DirectoryInfo directory = (DirectoryInfo) obj;
            GetDirectory(directory);
            if (Dispatcher != null)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    StartButton.Text = "开始";
                    FileNameTextBlock.Text = "扫描完成";
                    MessageBoxResult dialogResult =
                        MessageBox.Show("扫描完成，是否要删除扫描到的这些文件？", "警告", MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        // DeleteFile();
                        MessageBox.Show("删除成功", "警告");
                    }
                }));
            }
        }

        private void DeleteFile()
        {
            if (files!=null&&files.Count>0)
            {
                foreach (var fileInfo in files)
                {
                    fileInfo.Delete();
                }
            }
        }

        /// <summary>
        /// 获得指定路径下所有文件名
        /// </summary>
        private void GetFileName(DirectoryInfo root)
        {
            foreach (FileInfo f in root.GetFiles(_pattern))
            {
                var lastWriteTime = f.CreationTime;
                var compare = DateTime.Compare(lastWriteTime, _selectedDate);
                if ((isEarlier && compare > 0) || (!isEarlier && compare < 0))
                {
                    continue;
                }

                files.Add(f);
                // Thread.Sleep(50);
                if (Dispatcher != null)
                    Dispatcher.Invoke(new Action(delegate
                    {
                        FileNameTextBlock.Text = f.Name;
                        count++;
                        CountButton.Text = $"文件总数：{count}";
                    }));
                // Console.WriteLine(f.Name);
            }
        }

        /// <summary>
        /// 获得指定路径下所有子目录名
        /// </summary>
        private void GetDirectory(DirectoryInfo root)
        {
            if (!root.Exists)
            {
                return;
            }

            GetFileName(root);
            foreach (DirectoryInfo d in root.GetDirectories())
            {
                GetDirectory(d);
            }
        }

        private void GetPattern()
        {
            var text = FileTypeTextBox.Text.Trim();
            if (text.Length == 0)
            {
                this._pattern = "*.*";
            }

            var strings = text.Split(',');
            StringBuilder sb = new StringBuilder();
            foreach (var s in strings)
            {
                var trim = s.Trim();
                if (trim.Length == 0)
                {
                    continue;
                }

                if (!trim.Contains("."))
                {
                    sb.Append("*").Append(".").Append(trim).Append("|");
                }
                else
                {
                    if (trim.StartsWith("."))
                    {
                        sb.Append("*");
                    }

                    sb.Append(trim).Append("|");
                }
            }

            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            this._pattern = sb.Length > 0 ? sb.ToString() : "*.*";
        }

        private void OnFileTypeChange(object sender, TextChangedEventArgs e)
        {
        }

        private void StartScan(object sender, RoutedEventArgs e)
        {
            var b = StartButton.Text.Equals("开始");
            if (b)
            {
                StartButton.Text = "停止";
                ScanFile(FilePathTextBox.Text);
            }
            else
            {
                StartButton.Text = "开始";
                StopScan();
            }
        }
    }
}