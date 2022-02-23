using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LosslessCut
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ConfigFile _cfg = new ConfigFile();

        public MainWindow()
        {
            InitializeComponent();
            ButtonInput.AddHandler(TextBox.DragOverEvent, new DragEventHandler(InputFile_DragOver), true);
            ButtonInput.AddHandler(TextBox.DropEvent, new DragEventHandler(InputFile_Drop), true);
            AddHotKeys();
        }

        private void AddHotKeys()
        {
            try
            {
                RoutedCommand cutSetting = new RoutedCommand();
                cutSetting.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control));
                CommandBindings.Add(new CommandBinding(cutSetting , CutMovie));
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        /// <summary>
        /// Initialize Window
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadConfig();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        /// <summary>
        /// Save Config into a File
        /// </summary>
        private void SaveConfig()
        {
            string cfgFilePath = _cfg.GetPath();
            using (StreamWriter sw = new StreamWriter(cfgFilePath, false, Encoding.UTF8))
            {
                sw.WriteLine($"FFmpegPath\t{_cfg.FFmpeg}");
                sw.WriteLine($"OldPath\t{_cfg.Old}");
            }
        }

        /// <summary>
        /// Set FFmpeg Path
        /// </summary>
        private void SetPath(string path)
        {
            _cfg.FFmpeg = path;
            TextBoxFfmpeg.Text = path;
        }

        /// <summary>
        /// Load Config from a File
        /// </summary>
        private void LoadConfig()
        {
            string cfgFilePath = _cfg.GetPath();

            // Create Setting File if NOT Exists
            if (!System.IO.File.Exists(cfgFilePath))
            {
                SetPath("ffmpeg.exe");
                SaveConfig();
                return;
            }

            using var reader = new StreamReader(cfgFilePath);
            while (!reader.EndOfStream)
            {
                var item = reader.ReadLine()?.Split(ConfigFile.Delimiter);
                if (item is null || item.Length < 1)
                {
                    // Ignore
                    continue;
                }
                if (item[0] == "FFmpegPath")
                {
                    SetPath(item[1]);
                }
                else if (item[0] == "OldPath")
                {
                    _cfg.Old = item[1];
                }
            }
        }

        /// <summary>
        /// Browse FFmpeg EXE File
        /// </summary>
        private void BrowseFfmpeg(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.FileName = "ffmpeg.exe";
            dlg.Filter = "実行ファイル(*.exe)|*.*";
            dlg.Title = "FFmpegのパスを設定";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            dlg.FilterIndex = 0;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SetPath(dlg.FileName);
                SaveConfig();
            }
        }

        private void SetInput(string filename)
        {
            if (TextBlockInput.Text == filename)
            {
                return;
            }
            TextBlockInput.Text = filename;
            ShowLog(filename + "を読み込みました");
            var str = System.IO.Path.GetDirectoryName(filename);
            if (str is not null)
            {
                _cfg.Old = str;
            }
            SaveConfig();
        }

        /// <summary>
        /// Browse Input File
        /// </summary>
        private void BrowseInput(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.FileName = "";
            dlg.Filter = "すべてのファイル (*.*)|*.*";
            dlg.Title = "入力ファイルを開く";
            if (System.IO.File.Exists(_cfg.Old)) {
                dlg.InitialDirectory = _cfg.Old;
            }
            dlg.FilterIndex = 0;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SetInput(dlg.FileName);
            }
        }

        /// <summary>
        /// Get Full Path from D&D
        /// </summary>
        private void InputFile_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (fileNames is not null)
                {
                    SetInput(fileNames[0]);
                }
                LosslessMainWindow.Activate();
            }
        }

        /// <summary>
        /// Set Mouse Cursor
        /// </summary>
        private void InputFile_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Just Call FFmpeg with Appropriate Arguments
        /// </summary>
        private void CutMovie(object sender, RoutedEventArgs e)
        {
            string inputFile = TextBlockInput.Text;
            if (System.IO.File.Exists(inputFile))
            {
                var dir = System.IO.Path.GetDirectoryName(inputFile);
                var filename = System.IO.Path.GetFileName(inputFile);
                var ext = System.IO.Path.GetExtension(inputFile);
                if (ext is not null)
                {
                    int place = filename.LastIndexOf(ext);
                    if (place >= 0)
                    {
                        filename = filename.Remove(place, ext.Length) + $"_Trim{ext}";
                    }
                }
                else {
                    filename += "_Trim";
                }

                var dlg = new System.Windows.Forms.SaveFileDialog();
                dlg.FileName = filename;
                dlg.Filter = "すべてのファイル (*.*)|*.*";
                dlg.Title = "ファイルを保存";
                dlg.InitialDirectory = dir;
                dlg.FilterIndex = 0;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TimeSpan len = TimeSpan.Parse(MaskedTextBoxEnd.Text + "0") - TimeSpan.Parse(MaskedTextBoxStart.Text + "0");
                    string strCmdText= @$"-y -ss {MaskedTextBoxStart.Text} -t {len.ToString()} -i {TextBlockInput.Text} -c:v copy -c:a copy -async 1 {dir}\{filename}";
                    System.Diagnostics.Process.Start(TextBoxFfmpeg.Text, strCmdText);
                    ShowLog($"{filename}に保存しました");
                }
            }
            else {
                ShowLog("動画ファイルを選択して下さい。");
            }
        }

        /// <summary>
        /// Show Error Message
        /// </summary>
        private void ShowException(Exception ex)
        {
            ShowLog(ex.ToString());
        }

        /// <summary>
        /// Show Log Message
        /// </summary>
        private void ShowLog(string text)
        {
            TextBoxLog.Text += $"{text}\n";
        }
    }
}
