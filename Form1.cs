using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Media;

namespace SAFEditor
{
    public partial class Form1 : Form
    {
        private SAFInfo safInfo;
        private FB2Info fb2Info;
        private Int32 currentFrameIndex = -1;
        private Bitmap currentBitmap = null;
        private Int32 BitmapScale = 1;
        private String currentFileName = "";
        private String enumBasePath;
        private String exportBasePath;
        private SoundPlayer wavePlayer = new SoundPlayer();

        public Form1()
        {
            InitializeComponent();
            plFrames.Dock = DockStyle.Fill;
            plFrame.Dock = DockStyle.Fill;
            plElement.Dock = DockStyle.Fill;
            plWave.Dock = DockStyle.Fill;
            plUnknown.Dock = DockStyle.Fill;
            plFrames.Hide();
            plFrame.Hide();
            plElement.Hide();
            plWave.Hide();
            plUnknown.Hide();

            tabPage1.Dispose();
            tabPage3.Dispose();
        }

        private void btnOpenSAF_Click(object sender, EventArgs e)
        {
            Int32 i;

            ofd1.Filter = "SAF文件|*.saf|所有文件|*.*";
            ofd1.ShowDialog();

            if (ofd1.FileName.Trim() == "")
            {
                return;
            }

            if (safInfo != null)
            {
                safInfo.Dispose();
            }
            safInfo = GetSAFInfo(ofd1.FileName);

            lFilePath.Text = ofd1.FileName;
            lFileInfo.Text = "共含有 " + safInfo.GetFrameCount().ToString() + " 帧";
            currentFileName = ofd1.FileName;

            currentFrameIndex = 0;
            lCurrentFrame.Text = "当前绘制第 " + (currentFrameIndex + 1).ToString() + " 帧\t分辨率为 " + safInfo.GetFrameX(currentFrameIndex) + " * " + safInfo.GetFrameY(currentFrameIndex);
            DrawFrame(currentFrameIndex);

            cbUnitIndex.Items.Clear();
            for (i = 0; i < safInfo.GetUnitNumber(); i++)
            {
                cbUnitIndex.Items.Add(Convert.ToString(i, 16).PadLeft(4, '0').ToUpper());
            }

            if (safInfo.HasMultiplexUnit())
            {
                MessageBox.Show("此SAF中含有重复使用的图元，导入修改后有可能出现问题。");
            }
        }

        private void DrawUnit(Int32 unitIndex)
        {
            currentBitmap = safInfo.MakeUnitBitmap(unitIndex);

            Graphics gp = pictureBox1.CreateGraphics();
            gp.Clear(Color.LightGray);
            gp.InterpolationMode = InterpolationMode.NearestNeighbor;
            gp.DrawImage(currentBitmap, new Rectangle(0, 0, currentBitmap.Width * BitmapScale, currentBitmap.Height * BitmapScale), new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height), GraphicsUnit.Pixel);
        }

        private void DrawFrame(Int32 frameIndex)
        {
            currentBitmap = safInfo.GetFrameBitmap(frameIndex);
            //currentBitmap = safInfo.MakeBitmapByFrameParameter(frameIndex);

            Graphics gp = pictureBox1.CreateGraphics();
            gp.Clear(Color.LightGray);
            gp.InterpolationMode = InterpolationMode.NearestNeighbor;
            gp.DrawImage(currentBitmap, new Rectangle(0, 0, currentBitmap.Width * BitmapScale, currentBitmap.Height * BitmapScale), new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height), GraphicsUnit.Pixel);
        }

        private SAFInfo GetSAFInfo(String fileName)
        {
            return new SAFInfo(fileName);
        }

        private FB2Info GetFB2Info(String fileName)
        {
            return new FB2Info(fileName);
        }
        
        private void btnPrevFrame_Click(object sender, EventArgs e)
        {
            if (currentFrameIndex == 0)
            {
                return;
            }

            currentFrameIndex--;

            if (currentFrameIndex < 0)
            {
                currentFrameIndex = 0;
            }
            lCurrentFrame.Text = "当前绘制第 " + (currentFrameIndex + 1).ToString() + " 帧\t分辨率为 " + safInfo.GetFrameX(currentFrameIndex) + " * " + safInfo.GetFrameY(currentFrameIndex);
            DrawFrame(currentFrameIndex);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (currentFrameIndex >= safInfo.GetFrameCount() - 1)
            {
                return;
            }

            currentFrameIndex++;

            if (currentFrameIndex > safInfo.GetFrameCount())
            {
                currentFrameIndex = safInfo.GetFrameCount() - 1;
            }
            lCurrentFrame.Text = "当前绘制第 " + (currentFrameIndex + 1).ToString() + " 帧\t分辨率为 " + safInfo.GetFrameX(currentFrameIndex) + " * " + safInfo.GetFrameY(currentFrameIndex);
            DrawFrame(currentFrameIndex);
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            ofd1.Filter = "24位色PNG文件|*.png|所有文件|*.*";
            ofd1.ShowDialog();

            if (ofd1.FileName.Trim() == "")
            {
                return;
            }

            Image tempBitmap = Bitmap.FromFile(ofd1.FileName);
            if ((tempBitmap.Width != currentBitmap.Width) || (tempBitmap.Height != currentBitmap.Height))
            {
                MessageBox.Show("导入图像的尺寸必须与当前帧一致");
                return;
            }
            
            currentBitmap = new Bitmap(tempBitmap);
            safInfo.SaveBitmapToFrame(currentBitmap, currentFrameIndex);
            DrawFrame(currentFrameIndex);

            tempBitmap.Dispose();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            sfd1.Filter = "24位色PNG文件|*.png";
            sfd1.ShowDialog();

            currentBitmap.Save(sfd1.FileName);
        }

        private void cbScale_SelectedIndexChanged(object sender, EventArgs e)
        {
            BitmapScale = Int32.Parse(cbScale.Text);
            DrawFrame(currentFrameIndex);
        }
        
        private void btnRedraw_Click(object sender, EventArgs e)
        {
            //if (currentFrameIndex != -1)
            //{
            //    DrawFrame(currentFrameIndex);
            //}
            Bitmap bm = safInfo.MakeBitmapByFrameParameter(4);
            Graphics gp = pictureBox1.CreateGraphics();
            gp.Clear(Color.LightGray);
            gp.InterpolationMode = InterpolationMode.NearestNeighbor;
            gp.DrawImage(bm, new Rectangle(0, 0, bm.Width * BitmapScale, bm.Height * BitmapScale), new Rectangle(0, 0, bm.Width, bm.Height), GraphicsUnit.Pixel);
        }

        private void btnSaveSAF_Click(object sender, EventArgs e)
        {
            String BackupFile = safInfo.SaveSAFToFile(cbDeleteWave.Checked);
            if (BackupFile == "")
            {
                return;
            }
            MessageBox.Show("当前SAF文件已保存为原文件，原文件备份为" + BackupFile);
        }

        private void btnDrawUnit_Click(object sender, EventArgs e)
        {
            DrawUnit(Convert.ToInt32(cbUnitIndex.SelectedItem.ToString(), 16));
        }

        private void btnBatchExport_Click(object sender, EventArgs e)
        {
            Int32 i;

            fbd1.ShowDialog();

            if (fbd1.SelectedPath == "")
            {
                return;
            }

            String strExport = Path.Combine(fbd1.SelectedPath, Path.GetFileName(currentFileName)) + "\\";
            Bitmap frame;
            if (!Directory.Exists(strExport))
            {
                Directory.CreateDirectory(strExport);
            }

            for (i = 0; i < safInfo.GetFrameCount(); i++)
            {
                frame = safInfo.GetFrameBitmap(i);
                String savePath = strExport + String.Format("{0:D4}", i) + ".png";
                frame.Save(savePath);
                frame.Dispose();
            }

            MessageBox.Show("已批量导出到" + strExport);
        }

        private void btnBatchImport_Click(object sender, EventArgs e)
        {
            fbd1.ShowDialog();

            if (fbd1.SelectedPath == "")
            {
                return;
            }

            String[] PNGFiles = Directory.GetFiles(fbd1.SelectedPath, "*.png");
            UInt16 unitCount = 0;
            foreach (String PNGFile in PNGFiles)
            {
                Int32 frameIndex = Convert.ToInt32(Path.GetFileNameWithoutExtension(PNGFile), 16);
                Image tempBitmap = Bitmap.FromFile(PNGFile);
                if ((tempBitmap.Width % BaseUnitInfo.BlockXLimit != 0) || (tempBitmap.Height % BaseUnitInfo.BlockYLimit != 0))
                {
                    Bitmap tempBitmap2 = new Bitmap((tempBitmap.Width / BaseUnitInfo.BlockXLimit + 1 * (tempBitmap.Width % BaseUnitInfo.BlockXLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockXLimit, (tempBitmap.Height / BaseUnitInfo.BlockYLimit + 1 * (tempBitmap.Height % BaseUnitInfo.BlockYLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockYLimit);
                    Graphics g = Graphics.FromImage(tempBitmap2);
                    g.Clear(Color.Black);
                    Rectangle rect = new Rectangle(0, 0, tempBitmap2.Width, tempBitmap2.Height);
                    g.DrawImage(tempBitmap, 0, 0, rect, GraphicsUnit.Pixel);
                    tempBitmap.Dispose();
                    tempBitmap = tempBitmap2;
                }

                safInfo.ReconstructFrame(frameIndex, (UInt16)(tempBitmap.Width / BaseUnitInfo.BlockXLimit), (UInt16)(tempBitmap.Height / BaseUnitInfo.BlockYLimit), ref unitCount);

                tempBitmap.Dispose();
            }
            safInfo.ExpandUnitData(unitCount);
            foreach(String PNGFile in PNGFiles)
            {
                Int32 frameIndex = Convert.ToInt32(Path.GetFileNameWithoutExtension(PNGFile), 16);
                Image tempBitmap = Bitmap.FromFile(PNGFile);

                if ((tempBitmap.Width % BaseUnitInfo.BlockXLimit != 0) || (tempBitmap.Height % BaseUnitInfo.BlockYLimit != 0))
                {
                    Bitmap tempBitmap2 = new Bitmap((tempBitmap.Width / BaseUnitInfo.BlockXLimit + 1 * (tempBitmap.Width % BaseUnitInfo.BlockXLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockXLimit, (tempBitmap.Height / BaseUnitInfo.BlockYLimit + 1 * (tempBitmap.Height % BaseUnitInfo.BlockYLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockYLimit);
                    Graphics g = Graphics.FromImage(tempBitmap2);
                    g.Clear(Color.Black);
                    Rectangle rect = new Rectangle(0, 0, tempBitmap2.Width, tempBitmap2.Height);
                    g.DrawImage(tempBitmap, 0, 0, rect, GraphicsUnit.Pixel);
                    tempBitmap.Dispose();
                    tempBitmap = tempBitmap2;
                }

                Bitmap importBitmap = new Bitmap(tempBitmap);
                safInfo.SaveBitmapToFrame(importBitmap, frameIndex);

                tempBitmap.Dispose();
            }

            MessageBox.Show("批量导入完毕");
        }

        private void btnOpenFB2_Click(object sender, EventArgs e)
        {
            ofd1.Filter = "FB2文件|*.fb2|所有文件|*.*";
            ofd1.ShowDialog();

            if (ofd1.FileName.Trim() == "")
            {
                return;
            }

            if (fb2Info != null)
            {
                fb2Info.Dispose();
            }
            fb2Info = GetFB2Info(ofd1.FileName);

            DrawMap();
        }

        private void DrawMap()
        {
            currentBitmap = fb2Info.GetMapBitmap();

            Graphics gp = pbFB2.CreateGraphics();
            gp.Clear(Color.LightGray);
            gp.InterpolationMode = InterpolationMode.NearestNeighbor;
            gp.DrawImage(currentBitmap, new Rectangle(0, 0, currentBitmap.Width * BitmapScale, currentBitmap.Height * BitmapScale), new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height), GraphicsUnit.Pixel);
        }

        private void btnRedrawMap_Click(object sender, EventArgs e)
        {
            DrawMap();
        }

        private void btnExportMap_Click(object sender, EventArgs e)
        {
            sfd1.Filter = "24位色PNG文件|*.png";
            sfd1.ShowDialog();

            if (sfd1.FileName.Trim() == "")
            {
                return;
            }

            currentBitmap.Save(sfd1.FileName);
        }

        private void btnImportFB2_Click(object sender, EventArgs e)
        {
            ofd1.Filter = "24位色PNG文件|*.png|所有文件|*.*";
            ofd1.ShowDialog();

            if (ofd1.FileName.Trim() == "")
            {
                return;
            }

            Image tempBitmap = Bitmap.FromFile(ofd1.FileName);
            if ((tempBitmap.Width != currentBitmap.Width) || (tempBitmap.Height != currentBitmap.Height))
            {
                MessageBox.Show("导入图像的尺寸必须与当前帧一致");
                return;
            }

            currentBitmap = new Bitmap(tempBitmap);
            fb2Info.SaveBitmapToFB2Info(currentBitmap);
            DrawMap();

            tempBitmap.Dispose();
        }

        private void btnSaveMap_Click(object sender, EventArgs e)
        {
            String BackupFile = fb2Info.SaveFB2ToFile();
            if (BackupFile == "")
            {
                return;
            }
            MessageBox.Show("当前FB2文件已保存为原文件，原文件备份为" + BackupFile);

            BackupFile = fb2Info.SaveMPLToFile();
            if (BackupFile == "")
            {
                return;
            }
            MessageBox.Show("当前MPL文件已保存为原文件，原文件备份为" + BackupFile);
        }

        private void btnExportAll_Click(object sender, EventArgs e)
        {
            fbd1.Description = "选择检索起始目录";
            fbd1.ShowDialog();

            if (fbd1.SelectedPath == "")
            {
                return;
            }

            enumBasePath = fbd1.SelectedPath;

            fbd1.Description = "选择保存目录";
            fbd1.ShowDialog();

            if (fbd1.SelectedPath == "")
            {
                return;
            }
            exportBasePath = fbd1.SelectedPath;

            EnumAllFiles(enumBasePath);
        }

        String GetSavePath(String FilePath)
        {
            String tempPath = FilePath.Substring(FilePath.IndexOf(enumBasePath) + enumBasePath.Length);

            return exportBasePath + tempPath;
        }

        void EnumAllFiles(String BasePath)
        {
            String[] ChildDirs = Directory.GetDirectories(BasePath);
            String[] ChildFiles = Directory.GetFiles(BasePath);
            Int32 i;

            foreach (String ChildDir in ChildDirs)
            {
                EnumAllFiles(ChildDir);
            }

            foreach (String ChildFile in ChildFiles)
            {
                if (Path.GetExtension(ChildFile).ToUpper() == ".SAF")
                {
                    SAFInfo si = new SAFInfo(ChildFile);
                    Int32 count = si.GetFrameParaCount();

                    for (i = 0; i < count; i++)
                    {
                        Bitmap bm = si.MakeBitmapByFrameParameter(i);
                        if (bm != null)
                        {
                            String tFile = GetSavePath(ChildFile) + "\\" + String.Format("{0:D4}", i) + ".png";
                            Directory.CreateDirectory(GetSavePath(ChildFile));
                            bm.Save(tFile);
                        }
                    }
                }

                if (Path.GetExtension(ChildFile).ToUpper() == ".FB2")
                {
                }
            }
        }

        private void btnSaveTxt_Click(object sender, EventArgs e)
        {
            safInfo.OutputTXT(Path.GetFileNameWithoutExtension(safInfo.SAFFile) + ".Txt");
        }

        // 以下为新版工具
        private void btnOpenSAF2_Click(object sender, EventArgs e)
        {
            ofd1.Filter = "SAF文件|*.saf|所有文件|*.*";
            DialogResult dr = ofd1.ShowDialog();

            if (dr != DialogResult.OK)
            {
                return;
            }

            if (safInfo != null)
            {
                safInfo.Dispose();
            }
            safInfo = GetSAFInfo(ofd1.FileName);

            lFilePath2.Text = ofd1.FileName;

            if (safInfo.HasMultiplexUnit())
            {
                MessageBox.Show("此SAF中含有重复使用的图元，导入修改后有可能出现问题。");
            }
            SafInfoToTreeView();
        }

        private void SafInfoToTreeView()
        {
            int i;
            int j;

            tvSAFInfo.Nodes.Clear();

            TreeNode root = tvSAFInfo.Nodes.Add(Path.GetFileName(lFilePath2.Text));
            root.Tag = "root";
            TreeNode frames = root.Nodes.Add("帧");
            frames.Tag = "frames";

            i = 1;
            foreach (FrameParameter fp in safInfo.frameParameter)
            {
                TreeNode OneFrame = frames.Nodes.Add(i.ToString());
                OneFrame.Tag = "frame";
                j = 1;
                if (fp.Params != null)
                {
                    foreach (ParameterUnit pu in fp.Params)
                    {
                        TreeNode Element = OneFrame.Nodes.Add(j.ToString());
                        Element.Tag = "element";
                        j++;
                    }
                }
                i++;
            }

            i = 1;
            if ((safInfo.WaveData != null) && ((safInfo.WaveData.Count != 0)))
            {
                TreeNode Waves = root.Nodes.Add("音效");
                Waves.Tag = "Waves";
                foreach(WaveData wd in safInfo.WaveData)
                {
                    TreeNode OneWave = Waves.Nodes.Add(i.ToString());
                    OneWave.Tag = "Wave";
                    i++;
                }
            }
        }
        
        private void tvSAFInfo_AfterSelect(object sender, TreeViewEventArgs e)
        {
            switch ((String)tvSAFInfo.SelectedNode.Tag)
            {
                case "root":
                case "frames":
                    ShowFrames();
                    break;
                case "frame":
                    //ShowFrame(safInfo.frameParameter[Int32.Parse(tvSAFInfo.SelectedNode.Text) - 1]);
                    ShowFrame(Int32.Parse(tvSAFInfo.SelectedNode.Text) - 1);
                    break;
                case "element":
                    ShowElement(safInfo.frameParameter[Int32.Parse(tvSAFInfo.SelectedNode.Parent.Text) - 1].Params[Int32.Parse(tvSAFInfo.SelectedNode.Text) - 1]);
                    break;
                case "Wave":
                    ShowWave(safInfo.WaveData[Int32.Parse(tvSAFInfo.SelectedNode.Text) - 1]);
                    break;
            }
        }

        private void ShowFrames()
        {
            plFrames.Show();
            plFrame.Hide();
            plElement.Hide();
            plWave.Hide();
            plUnknown.Hide();

            lbFramesInfo.Text = "共有" + safInfo.GetFrameCount() + "帧";
        }

        private void ShowFrame(Int32 frameIndex)
        {
            plFrames.Hide();
            plFrame.Show();
            plElement.Hide();
            plWave.Hide();
            plUnknown.Hide();

            pbFrame.Image = safInfo.MakeBitmapByFrameParameter(frameIndex, true);
        }

        private void ShowElement(ParameterUnit pu)
        {
            plFrames.Hide();
            plFrame.Hide();
            plElement.Show();
            plWave.Hide();
            plUnknown.Hide();

            dgvElementData.Rows.Clear();

            if (pu.FrameIndex < 0)
            {
                dgvElementData.Rows.Add(new String[] { "对象动作帧", "" });
            }
            else
            {
                dgvElementData.Rows.Add(new String[] { "宽度", (safInfo.GetFrameX(pu.FrameIndex)).ToString() });
                dgvElementData.Rows.Add(new String[] { "高度", (safInfo.GetFrameY(pu.FrameIndex)).ToString() });
                dgvElementData.Rows.Add(new String[] { "元素编号", (pu.FrameIndex).ToString("D4") });
                dgvElementData.Rows.Add(new String[] { "坐标X", pu.DrawX.ToString() });
                dgvElementData.Rows.Add(new String[] { "坐标Y", pu.DrawY.ToString() });
                dgvElementData.Rows.Add(new String[] { "透明度", pu.Alpha.ToString() });
                dgvElementData.Rows.Add(new String[] { "未知1", pu.Red.ToString() });
                dgvElementData.Rows.Add(new String[] { "未知2", pu.Green.ToString() });
                dgvElementData.Rows.Add(new String[] { "未知3", pu.Blue.ToString() });
                dgvElementData[1, 0].ReadOnly = true;
                dgvElementData[1, 1].ReadOnly = true;
                dgvElementData[1, 2].ReadOnly = true;
                pbElement.Image = safInfo.MakeBitmapByParameterUnit(pu, false);
            }
        }

        private void ShowWave(WaveData wave)
        {
            plFrames.Hide();
            plFrame.Hide();
            plElement.Hide();
            plWave.Show();
            plUnknown.Hide();

            Byte[] WAVHead = { 0x52, 0x49, 0x46, 0x46, 0xAC, 0x61, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20, 0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x22, 0x56, 0x00, 0x00, 0x44, 0xAC, 0x00, 0x00, 0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61, 0x88, 0x61, 0x00, 0x00 };
            Byte[] fileData = new Byte[WAVHead.Length + wave.Data.Length - 8];
            Array.Copy(WAVHead, fileData, WAVHead.Length);
            fileData[0x16] = wave.Channels;
            Util.SetBEUInt16(fileData, 0x18, (ushort)wave.SampleRate);
            fileData[0x20] = (Byte)(wave.Bits / 8 * wave.Channels);
            fileData[0x22] = wave.Bits;
            Array.Copy(wave.Data, 8, fileData, WAVHead.Length, wave.Data.Length - 8);
            Util.SetBEUInt32(fileData, 0x28, (uint)wave.Data.Length - 8);
            Util.SetBEUInt32(fileData, 0x4, (uint)wave.Data.Length + 0x1C);
            File.WriteAllBytes("temp.wav", fileData);

            float playMS = (float)wave.DataLength / (float)wave.Channels / (float)(wave.Bits / 8) / (float)wave.SampleRate;

            lbWaveInfo.Text = "声道数:" + wave.Channels.ToString() + "\r\n" + "采样率:" + wave.SampleRate + "\r\n" + "位数:" + wave.Bits + "\r\n" + "时长:" + playMS.ToString("F3") + "秒";
        }

        // 多帧Panel相关代码
        private void cbPlayAll_CheckedChanged(object sender, EventArgs e)
        {
            if (cbPlayAll.Checked)
            {
                tbPlayEnd.Enabled = false;
                tbPlayStart.Enabled = false;
            }
            else
            {
                tbPlayStart.Enabled = true;
                tbPlayEnd.Enabled = true;
            }
        }

        private void btnPlayFrames_Click(object sender, EventArgs e)
        {
            Int32 startIndex;
            Int32 endIndex;

            try
            {
                if (cbPlayAll.Checked)
                {
                    startIndex = 0;
                    endIndex = safInfo.GetFrameCount() - 1;
                }
                else
                {
                    startIndex = Int32.Parse(tbPlayStart.Text) - 1;
                    endIndex = Int32.Parse(tbPlayEnd.Text) - 1;
                }

                if ((startIndex > endIndex) || (endIndex > safInfo.GetFrameCount()))
                {
                    return;
                }

                for (Int32 i = startIndex; i <= endIndex; i++)
                {
                    pbFrames.Image = safInfo.MakeBitmapByFrameParameter(i, true);
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //单元素Panel相关代码
        private void SyncElemetData()
        {
            if (dgvElementData.RowCount <= 0)
            {
                return;
            }

            Int32 index = Int32.Parse(dgvElementData[1, 2].Value.ToString());
            Int32 x = Int32.Parse(dgvElementData[1, 3].Value.ToString());
            Int32 y = Int32.Parse(dgvElementData[1, 4].Value.ToString());
            Int32 alpha = Int32.Parse(dgvElementData[1, 5].Value.ToString());
            Int32 r = Int32.Parse(dgvElementData[1, 6].Value.ToString());
            Int32 g = Int32.Parse(dgvElementData[1, 7].Value.ToString());
            Int32 b = Int32.Parse(dgvElementData[1, 8].Value.ToString());

            ParameterUnit pu = safInfo.frameParameter[Int32.Parse(tvSAFInfo.SelectedNode.Parent.Text) - 1].Params[Int32.Parse(tvSAFInfo.SelectedNode.Text) - 1];

            pu.DrawX = (Int16)x;
            pu.DrawY = (Int16)y;
            pu.Alpha = (Byte)alpha;
            pu.Red = (Int16)r;
            pu.Green = (Int16)g;
            pu.Blue = (Int16)b;

            safInfo.frameParameter[Int32.Parse(tvSAFInfo.SelectedNode.Parent.Text) - 1].SyncParamToData();
        }

        private void dgvElementData_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            SyncElemetData();
        }

        private void dgvElementData_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex == 5)
            {
                Int32 alpha = Int32.Parse(e.FormattedValue.ToString());
                if ((alpha != 0) && (alpha != 1) && (alpha != 2) && (alpha != 7))
                {
                    MessageBox.Show("透明度仅可设置为0/1/2/7");
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void btnPlayWave_Click(object sender, EventArgs e)
        {
            wavePlayer.SoundLocation = "temp.wav";
            wavePlayer.Play();
        }

        private void btnSaveSAF2_Click(object sender, EventArgs e)
        {
            String BackupFile = safInfo.SaveSAFToFile(false);
            if (BackupFile == "")
            {
                return;
            }
            MessageBox.Show("当前SAF文件已保存为原文件，原文件备份为" + BackupFile);
        }

        private void btnExportWave_Click(object sender, EventArgs e)
        {
            sfd1.Filter = "Wav文件|*.wav";
            DialogResult dr = sfd1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                File.Copy("temp.wav", sfd1.FileName);
            }
        }

        private void btnExportAll2_Click(object sender, EventArgs e)
        {
            Int32 i;

            DialogResult dr = fbd1.ShowDialog();

            if (dr != DialogResult.OK)
            {
                return;
            }

            String strExport = Path.Combine(fbd1.SelectedPath, Path.GetFileName(currentFileName)) + "\\";
            Bitmap frame;
            if (!Directory.Exists(strExport))
            {
                Directory.CreateDirectory(strExport);
            }

            for (i = 0; i < safInfo.GetElementCount(); i++)
            {
                frame = safInfo.MakeBitmapByElementIndex(i);
                String savePath = strExport + String.Format("{0:D4}", i) + ".png";
                frame.Save(savePath);
                frame.Dispose();
            }

            MessageBox.Show("已批量导出到" + strExport);
        }

        private void btnImportAll_Click(object sender, EventArgs e)
        {
            DialogResult dr = fbd1.ShowDialog();

            if (dr != DialogResult.OK)
            {
                return;
            }

            String[] PNGFiles = Directory.GetFiles(fbd1.SelectedPath, "*.png");
            UInt16 unitCount = 0;
            foreach (String PNGFile in PNGFiles)
            {
                Int32 frameIndex = Convert.ToInt32(Path.GetFileNameWithoutExtension(PNGFile), 10);
                Image tempBitmap = Bitmap.FromFile(PNGFile);
                if ((tempBitmap.Width % BaseUnitInfo.BlockXLimit != 0) || (tempBitmap.Height % BaseUnitInfo.BlockYLimit != 0))
                {
                    Bitmap tempBitmap2 = new Bitmap((tempBitmap.Width / BaseUnitInfo.BlockXLimit + 1 * (tempBitmap.Width % BaseUnitInfo.BlockXLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockXLimit, (tempBitmap.Height / BaseUnitInfo.BlockYLimit + 1 * (tempBitmap.Height % BaseUnitInfo.BlockYLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockYLimit);
                    Graphics g = Graphics.FromImage(tempBitmap2);
                    g.Clear(Color.Black);
                    Rectangle rect = new Rectangle(0, 0, tempBitmap2.Width, tempBitmap2.Height);
                    g.DrawImage(tempBitmap, 0, 0, rect, GraphicsUnit.Pixel);
                    tempBitmap.Dispose();
                    tempBitmap = tempBitmap2;
                }

                safInfo.ReconstructFrame(frameIndex, (UInt16)(tempBitmap.Width / BaseUnitInfo.BlockXLimit), (UInt16)(tempBitmap.Height / BaseUnitInfo.BlockYLimit), ref unitCount);

                tempBitmap.Dispose();
            }
            safInfo.ExpandUnitData(unitCount);
            foreach (String PNGFile in PNGFiles)
            {
                Int32 frameIndex = Convert.ToInt32(Path.GetFileNameWithoutExtension(PNGFile), 10);
                Image tempBitmap = Bitmap.FromFile(PNGFile);

                if ((tempBitmap.Width % BaseUnitInfo.BlockXLimit != 0) || (tempBitmap.Height % BaseUnitInfo.BlockYLimit != 0))
                {
                    Bitmap tempBitmap2 = new Bitmap((tempBitmap.Width / BaseUnitInfo.BlockXLimit + 1 * (tempBitmap.Width % BaseUnitInfo.BlockXLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockXLimit, (tempBitmap.Height / BaseUnitInfo.BlockYLimit + 1 * (tempBitmap.Height % BaseUnitInfo.BlockYLimit == 0 ? 0 : 1)) * BaseUnitInfo.BlockYLimit);
                    Graphics g = Graphics.FromImage(tempBitmap2);
                    g.Clear(Color.Black);
                    Rectangle rect = new Rectangle(0, 0, tempBitmap2.Width, tempBitmap2.Height);
                    g.DrawImage(tempBitmap, 0, 0, rect, GraphicsUnit.Pixel);
                    tempBitmap.Dispose();
                    tempBitmap = tempBitmap2;
                }

                Bitmap importBitmap = new Bitmap(tempBitmap);
                safInfo.SaveBitmapToFrame(importBitmap, frameIndex);

                tempBitmap.Dispose();
            }

            MessageBox.Show("批量导入完毕");
        }

        /// <summary>
        /// 返回指定字节数组包含的Wave头部信息
        /// </summary>
        public WaveHeader GetWaveHeaderFromBytes(byte[] data)
        {
            WaveHeader header = new WaveHeader();
            ushort tempIndex = 0;
            header.RIFF = Convert.ToString(System.Text.Encoding.ASCII.GetChars(data, 0, 4));
            header.FileSize = System.BitConverter.ToUInt32(data, 4);
            header.WAVE = Convert.ToString(System.Text.Encoding.ASCII.GetChars(data, 8, 4));
            //FormatChunk
            header.FORMAT = Convert.ToString(System.Text.Encoding.ASCII.GetChars(data, 12, 4));
            header.FormatSize = System.BitConverter.ToUInt32(data, 16);
            header.FilePadding = System.BitConverter.ToUInt16(data, 20);
            header.FormatChannels = System.BitConverter.ToUInt16(data, 22);
            header.SamplesPerSecond = System.BitConverter.ToUInt32(data, 24);
            header.AverageBytesPerSecond = System.BitConverter.ToUInt32(data, 28);
            header.BytesPerSample = System.BitConverter.ToUInt16(data, 32);
            header.BitsPerSample = System.BitConverter.ToUInt16(data, 34);
            if (header.FormatSize == 18)
            {
                header.FormatExtra = System.BitConverter.ToUInt16(data, 36);
            }
            else
            {
                header.FormatExtra = 0;
            }
            tempIndex = (UInt16)(20 + header.FormatSize);
            //FactChunk
            header.FACT = Convert.ToString(System.Text.Encoding.ASCII.GetChars(data, tempIndex, 4));
            if (header.FACT == "fact")
            {
                header.FactSize = System.BitConverter.ToUInt32(data, tempIndex + 4);
                header.FactInf = (header.FactSize == 2 ? System.BitConverter.ToUInt16(data, tempIndex + 8) : System.BitConverter.ToUInt32(data, tempIndex + 8));
                tempIndex = (UInt16)(tempIndex + header.FactSize + 8);
            }
            else
            {
                header.FACT = "NULL";
                header.FactSize = 0;
                header.FactInf = 0;
            }
            //DataChunk
            header.DataSize = System.BitConverter.ToUInt32(data, tempIndex + 4);
            header.DATA = new Byte[header.DataSize];//Convert.ToString(System.Text.Encoding.ASCII.GetChars(data, tempIndex, 4));
            Array.Copy(data, tempIndex, header.DATA, 0, header.DataSize);
            return header;
        }

        private void btnImportWave_Click(object sender, EventArgs e)
        {
            ofd1.Filter = "Wav文件|*.wav";
            DialogResult dr = ofd1.ShowDialog();

            if (dr != DialogResult.OK)
            {
                return;
            }

            FileStream fs = new FileStream(ofd1.FileName, FileMode.Open);
            BinaryReader binReader = new BinaryReader(fs);
            byte[] bBuffer;
            bBuffer = new byte[fs.Length];
            binReader.Read(bBuffer, 0, (int)fs.Length);
            binReader.Close();
            fs.Close();

            WaveHeader wh = GetWaveHeaderFromBytes(bBuffer);

            WaveData wd = safInfo.WaveData[Int32.Parse(tvSAFInfo.SelectedNode.Text) - 1];

            wd.Channels = (Byte)wh.FormatChannels;
            wd.Bits = (Byte)wh.BitsPerSample;
            wd.SampleRate = (UInt16)wh.SamplesPerSecond;
            wd.DataLength = (Int32)wh.DataSize;

            wd.Data = new Byte[wd.DataLength + 8];
            wd.Data[0] = wd.Channels;
            wd.Data[1] = wd.Bits;
            Util.SetBEUInt16(wd.Data, 2, wd.SampleRate);
            Util.SetBEUInt32(wd.Data, 4, wh.DataSize);
            Array.Copy(wh.DATA, 0, wd.Data, 8, wh.DataSize);
        }

    }
}
