using Accord.Video.DirectShow;
using RazorEnhanced;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Assistant
{
    public partial class MainForm : System.Windows.Forms.Form
    {
        internal TextBox VideoPathTextBox { get { return videoPathTextBox; } }

        public void DisableRecorder()
        {
            videoTab.Enabled = Assistant.Engine.CDepPresent = false;
            removeVideoTab();
        }

        private void videoFPSTextBox_TextChanged(object sender, EventArgs e)
        {
            if (videoFPSTextBox.Focused)
            {

                if (!Int32.TryParse(videoFPSTextBox.Text, out int fps))
                    videoFPSTextBox.Text = "25";

                Settings.General.WriteInt("VideoFPS", fps);
            }
        }

        private void videoPathButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folder = new()
            {
                Description = "Select a folder to store Razor video file",
                SelectedPath = RazorEnhanced.Settings.General.ReadString("VideoPath"),
                ShowNewFolderButton = true
            };

            if (folder.ShowDialog(this) == DialogResult.OK)
            {
                RazorEnhanced.Settings.General.WriteString("VideoPath", folder.SelectedPath);
                videoPathTextBox.Text = folder.SelectedPath;
                ReloadVideoList();
            }
        }

        internal void ReloadVideoList()
        {
            CloseCurrentVideoSource();
            VideoCapture.DisplayTo(videolistBox);
        }

        private void videoList_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (videolistBox.Focused)
            {
                CloseCurrentVideoSource();

                if (videolistBox.SelectedIndex == -1)
                    return;

                string file = Path.Combine(RazorEnhanced.Settings.General.ReadString("VideoPath"), videolistBox.SelectedItem.ToString());
                if (!File.Exists(file))
                {
                    RazorEnhanced.UI.RE_MessageBox.Show("File Not Found",
                        Language.Format(LocString.FileNotFoundA1, file),
                        ok: "Ok", no: null, cancel: null, backColor: null);
                    videolistBox.Items.RemoveAt(videolistBox.SelectedIndex);
                    videolistBox.SelectedIndex = -1;
                    return;
                }
                OpenVideoSource(file);
            }
        }

        private void videoList_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.Clicks == 1)
            {
                ContextMenu menu = new();
                menu.MenuItems.Add("Delete", new EventHandler(DeleteVideoFile));
                if (videolistBox.SelectedIndex == -1)
                    menu.MenuItems[menu.MenuItems.Count - 1].Enabled = false;
                menu.MenuItems.Add("Delete ALL", new EventHandler(ClearVideoDirectory));
                menu.Show(videolistBox, new Point(e.X, e.Y));
            }
        }

        private void DeleteVideoFile(object sender, System.EventArgs e)
        {
            CloseCurrentVideoSource();
            int sel = videolistBox.SelectedIndex;
            if (sel == -1)
                return;

            string file = Path.Combine(RazorEnhanced.Settings.General.ReadString("VideoPath"), (string)videolistBox.SelectedItem);
            var dialogResult = RazorEnhanced.UI.RE_MessageBox.Show("Delete Confirmation",
                Language.Format(LocString.DelConf, file),
                ok: "Ok", no: "No", cancel: null, backColor: null);
            if (dialogResult == DialogResult.No)
                return;

            videolistBox.SelectedIndex = -1;

            try
            {
                File.Delete(file);
                videolistBox.Items.RemoveAt(sel);
            }
            catch (Exception ex)
            {
                RazorEnhanced.UI.RE_MessageBox.Show("Unable to Delete",
                    $"Unable to delete file:\r\n{file}\r\nError: {ex}",
                    ok: "Ok", no: null, cancel: null, backColor: null);
                return;
            }
            ReloadVideoList();
        }

        private void ClearVideoDirectory(object sender, System.EventArgs e)
        {
            string dir = RazorEnhanced.Settings.General.ReadString("VideoPath");
            var dialogResult = RazorEnhanced.UI.RE_MessageBox.Show("Delete Confirmation",
                Language.Format(LocString.Confirm, dir),
                ok: "Ok", no: "No", cancel: null, backColor: null);
            if (dialogResult == DialogResult.No)
                return;

            string[] files = Directory.GetFiles(dir, "*.avi");
            StringBuilder sb = new();
            int failed = 0;
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    File.Delete(files[i]);
                }
                catch
                {
                    sb.AppendFormat("{0}\n", files[i]);
                    failed++;
                }
            }

            if (failed > 0)
                RazorEnhanced.UI.RE_MessageBox.Show("Warning",
                    Language.Format(LocString.FileDelError, failed, failed != 1 ? "s" : String.Empty, sb.ToString()),
                    ok: "Ok", no: null, cancel: null, backColor: null);
            ReloadVideoList();
        }

        private void videorecbutton_Click(object sender, EventArgs e)
        {
            StartVideoRecorder();
        }

        private void videostopbutton_Click(object sender, EventArgs e)
        {
            StopVideoRecorder();
        }

        internal static void StartVideoRecorder()
        {
            if (!Assistant.Engine.CDepPresent) // DIsable mancanza librerie c++
                return;

            if (VideoCapture.Recording) // already on record
            {
                RazorEnhanced.Misc.SendMessage("Already on Record", false);
                return;
            }
            RazorEnhanced.Misc.SendMessage("Start Video Record", false);
            Engine.MainWindow.videoRecStatuslabel.Text = "Recording";
            Engine.MainWindow.videoRecStatuslabel.ForeColor = Color.Red;

            Engine.MainWindow.videosettinggroupBox.Enabled = false;
            int fps = 30;
            if (Settings.General.ReadInt("VideoFPS") < 30)
                fps = Settings.General.ReadInt("VideoFPS");

            VideoCapture.Record(fps, Engine.MainWindow.videoCodecComboBox.SelectedIndex);
        }

        internal static void StopVideoRecorder()
        {
            if (!Assistant.Engine.CDepPresent) // DIsable mancanza librerie c++
                return;

            RazorEnhanced.Misc.SendMessage("Stop Video Record", false);
            Engine.MainWindow.videoRecStatuslabel.Text = "Idle";
            Engine.MainWindow.videoRecStatuslabel.ForeColor = Color.Green;
            VideoCapture.Stop();
            Engine.MainWindow.ReloadVideoList();
            Engine.MainWindow.videosettinggroupBox.Enabled = true;
        }

        private void videoCodecComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (videoCodecComboBox.Focused)
            {
                Settings.General.WriteInt("VideoFormat", videoCodecComboBox.SelectedIndex);
            }
        }


        private void CloseCurrentVideoSource()
        {
            if (videoSourcePlayer.VideoSource != null)
            {
                videoSourcePlayer.SignalToStop();
                videoSourcePlayer.WaitForStop();
                videoSourcePlayer.VideoSource = null;
            }
        }
        // Open video source
        private void OpenVideoSource(string file)
        {
            FileVideoSource source = new(file);
            // set busy cursor
            this.Cursor = Cursors.WaitCursor;

            // stop current video source
            CloseCurrentVideoSource();

            // start new video source
            videoSourcePlayer.VideoSource = source;
            videoSourcePlayer.Start();

            this.Cursor = Cursors.Default;
        }
    }
}
