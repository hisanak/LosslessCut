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
            TextBoxInput.AddHandler(TextBox.DragOverEvent, new DragEventHandler(InputFile_DragOver), true);
            TextBoxInput.AddHandler(TextBox.DropEvent, new DragEventHandler(InputFile_Drop), true);
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
        private void SaveConfig(string value)
        {
            string cfgFilePath = _cfg.GetPath();
            using (StreamWriter sw = new StreamWriter(cfgFilePath, false, Encoding.UTF8))
            {
                sw.WriteLine(value);
            }
        }

        /// <summary>
        /// Set FFmpeg Path
        /// </summary>
        private void SetPath(String path)
        {
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
                SaveConfig("Path\tffmpeg.exe");
                SetPath("ffmpeg.exe");
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
                if (item[0] == "Path")
                {
                    SetPath(item[1]);
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
                SaveConfig($"Path\t{dlg.FileName}");
                SetPath(dlg.FileName);
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
                    if (TextBoxInput.Text != fileNames[0])
                    {
                        TextBoxInput.Text = fileNames[0];
                        ShowLog(fileNames[0] + "を読み込みました");
                    }
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
            if (System.IO.File.Exists(TextBoxInput.Text))
            {
                var dir = System.IO.Path.GetDirectoryName(TextBoxInput.Text);
                var filename = System.IO.Path.GetFileName(TextBoxInput.Text);
                var ext = System.IO.Path.GetExtension(TextBoxInput.Text);
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
                    string strCmdText= @$"-y -ss {MaskedTextBoxStart.Text} -t {len.ToString()} -i {TextBoxInput.Text} -c:v copy -c:a copy -async 1 {dir}\{filename}";
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
