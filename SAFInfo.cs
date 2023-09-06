using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
//using OpenCvSharp.Extensions;
//using OpenCvSharp;

namespace SAFEditor
{
    public struct WaveHeader
    {
        #region "RiffChunk"
        /// <summary>
        /// RIFF标志
        /// </summary>
        public string RIFF;
        /// <summary>
        /// 文件长度
        /// </summary>
        public uint FileSize;
        /// <summary>
        /// WAVE标志
        /// </summary>
        #endregion
        public string WAVE;
        #region "FormatChunk"
        /// <summary>
        /// FORMAT标志
        /// </summary>
        public string FORMAT;
        /// <summary>
        /// Format长度
        /// </summary>
        public uint FormatSize;
        /// <summary>
        /// 编码方式
        /// </summary>
        public ushort FilePadding;
        /// <summary>
        /// 声道数目
        /// </summary>
        public ushort FormatChannels;
        /// <summary>
        /// 采样频率
        /// </summary>
        public uint SamplesPerSecond;
        /// <summary>
        /// 每秒所需字节数
        /// </summary>
        public uint AverageBytesPerSecond;
        /// <summary>
        /// 数据块对齐单位
        /// </summary>
        public ushort BytesPerSample;
        /// <summary>
        /// 单个采样所需Bit数
        /// </summary>
        public ushort BitsPerSample;
        /// <summary>
        /// 附加信息
        /// </summary>
        #endregion
        public ushort FormatExtra;
        #region "FactChunk"
        /// <summary>
        /// FACT标志
        /// </summary>
        public string FACT;
        /// <summary>
        /// Fact长度
        /// </summary>
        public uint FactSize;
        /// <summary>
        /// Fact信息
        /// </summary>
        #endregion
        public uint FactInf;
        #region "DataChunk"
        /// <summary>
        /// DATA标志
        /// </summary>
        public Byte[] DATA;
        /// <summary>
        /// Data长度
        /// </summary>
        #endregion
        public uint DataSize;
    }

    class UnitDataSet
    {
        public Byte[] Data = null;
    }
    
    class FrameConstruct
    {
        public Byte[] Data = null;
        public Int32 x;
        public Int32 y;
        public Bitmap bitmap;
    }

    class ParameterUnit
    {
        public Int16 FrameIndex = 0;
        public Int16 DrawX = 0;
        public Int16 DrawY = 0;
        public Byte Alpha = 0;
        public Int16 Red = 0;
        public Int16 Green = 0;
        public Int16 Blue = 0;
    }
    class FrameParameter
    {
        public Byte[] Data = null;
        public List<ParameterUnit> Params = null;
        public Int16 WaveIndex;

        public void SyncParamToData()
        {
            if (Params != null)
            {
                Int32 tp = 10;
                foreach(ParameterUnit pu in Params)
                {
                    Util.SetBEUInt16(Data, tp, (UInt16)pu.FrameIndex);
                    tp += 2;
                    Util.SetBEUInt16(Data, tp, (UInt16)pu.DrawX);
                    tp += 2;
                    Util.SetBEUInt16(Data, tp, (UInt16)pu.DrawY);
                    tp += 2;
                    Data[tp] = (Byte)pu.Alpha;
                    tp += 1;
                    Util.SetBEUInt16(Data, tp, (UInt16)pu.Red);
                    tp += 2;
                    Util.SetBEUInt16(Data, tp, (UInt16)pu.Green);
                    tp += 2;
                    Util.SetBEUInt16(Data, tp, (UInt16)pu.Blue);
                    tp += 2;
                }
            }
        }
    }
    
    class WaveData
    {
        public Byte Channels;
        public Byte Bits;
        public UInt16 SampleRate;
        public Int32 DataLength;
        public Byte[] Data = null;
    }

    class UnknownData
    {
        public Byte[] Data = null;
    }
    class SAFInfo : BaseUnitInfo
    {
        public List<FrameParameter> frameParameter = new List<FrameParameter>();
        public List<FrameConstruct> frameConstruct = new List<FrameConstruct>();
        public List<UnitDataSet> unitDataSet = new List<UnitDataSet>();
        public List<WaveData> WaveData = new List<WaveData>();
        public List<UnknownData> unknownData1 = new List<UnknownData>();
        public String SAFFile = null;
        private Byte[] SAFHead = { 0x53, 0x41, 0x46, 0x05, 0x02, 0x74, 0x00, 0x1e, 0x00, 0x18, 0x00, 0x00 };
        private const Int32 SIZE_SECTOR = 10 * (2 + 4 + 4) + 4; //2字节个数+4字节起始地址+4字节结束地址
        private const Int32 OFFSET_FRAME_PARAMETOR_BEGIN = 0x74;

        public Byte[] GetFrameConstructData(Int32 frameIndex)
        {
            return GetSubArray(frameConstruct[frameIndex].Data, 4, frameConstruct[frameIndex].Data.Length - 4);
        }

        public void Dispose()
        {
            frameParameter.Clear();
            frameConstruct.Clear();
            unitDataSet.Clear();
            WaveData.Clear();
            unknownData1.Clear();
        }

        public void ReconstructFrame(Int32 frameIndex, UInt16 x, UInt16 y, ref UInt16 unitCount)
        {
            Int32 i = x * y;
            Int32 k;
            Byte[] NewData = new Byte[i * 2 + 4];
            Util.SetBEUInt16(NewData, 0, x);
            Util.SetBEUInt16(NewData, 2, y);
            for (k = 0; k < i; k++)
            {
                Util.SetBEUInt16(NewData, k * 2 + 4, unitCount);
                unitCount++;
            }

            frameConstruct[frameIndex].Data = NewData;
            frameConstruct[frameIndex].x = x * BlockXLimit;
            frameConstruct[frameIndex].y = y * BlockYLimit;
        }

        public void ExpandUnitData(UInt16 unitCount)
        {
            while (unitDataSet.Count < unitCount)
            {
                UnitDataSet uds = new UnitDataSet();
                uds.Data = null;
                unitDataSet.Add(uds);
            }
        }

        public void ReconstructAllFrame()
        {
            Int32 i;
            UInt16 j = 0;
            Int32 k;

            foreach(FrameConstruct fc in frameConstruct)
            {
                Byte[] NewData = new Byte[fc.Data.Length];
                i = Util.GetBEInt16(fc.Data, 0) * Util.GetBEInt16(fc.Data, 2);
                Array.Copy(fc.Data, NewData, fc.Data.Length);
                for (k = 0; k < i; k++)
                {
                    Util.SetBEUInt16(NewData, k * 2 + 4, j);
                    j++;
                }

                fc.Data = NewData;
            }

            while (unitDataSet.Count < j)
            {
                UnitDataSet uds = new UnitDataSet();
                uds.Data = null;
                unitDataSet.Add(uds);
            }
        }

        public void ReplaceUnitData(Int32 unitIndex, Byte[] unitData)
        {
            unitDataSet[unitIndex].Data = unitData;
        }

        public Int32 GetFrameCount()
        {
            return frameParameter.Count;
            //return frameConstruct.Count;
        }

        public Int32 GetElementCount()
        {
            return frameConstruct.Count;
        }
        
        public Int32 GetFrameParaCount()
        {
            return frameParameter.Count;
        }

        public Boolean HasMultiplexUnit()
        {
            String tempIndexList = "";
            Int32 i;
            String tempIndex;

            foreach (FrameConstruct fc in frameConstruct)
            {
                for (i = 4; i < fc.Data.Length; i += 2)
                {
                    tempIndex = "," + Util.GetBEInt16(fc.Data, i) + ",";
                    if (tempIndexList.LastIndexOf(tempIndex) != -1)
                    {
                        return true;
                    }
                    tempIndexList += tempIndex;
                }
            }

            return false;
        }

        private Byte[] GetUnitDrawData(Int32 unitIndex)
        {
            return GetDrawData(unitDataSet[unitIndex].Data);
        }

        private Byte[] GetFrameDrawData(Int32 FrameIndex)
        {
            Byte[] ret = new Byte[frameConstruct[FrameIndex].x * frameConstruct[FrameIndex].y * 2];
            Int32 i;
            Int32 p;
            Int32 unitIndex;
            Byte[] unitData;

            p = 0;

            for (i = 4; i < frameConstruct[FrameIndex].Data.Length; i += 2)
            {
                unitIndex = Util.GetBEInt16(frameConstruct[FrameIndex].Data, i);
                unitData = GetUnitDrawData(unitIndex);
                CopyBuffer(unitData, 0, ret, p, unitData.Length);
                p += unitData.Length;
            }
            return ret;
        }

        public Int32 GetFrameX(Int32 FrameIndex)
        {
            return frameConstruct[FrameIndex].x;
        }

        public Int32 GetFrameY(Int32 FrameIndex)
        {
            return frameConstruct[FrameIndex].y;
        }

        public Bitmap GetFrameBitmap(Int32 frameIndex)
        {
            //return frameConstruct[frameIndex].bitmap;
            return MakeFrameBitmap(frameIndex);
        }

        public Int32 GetUnitNumber()
        {
            return unitDataSet.Count;
        }
        
        public Bitmap MakeUnitBitmap(Int32 unitIndex)
        {
            return MakeBitmap(GetUnitDrawData(unitIndex), 30, 24);
        }

        private Bitmap MakeFrameBitmap(Int32 frameIndex)
        {
            return MakeBitmap(GetFrameDrawData(frameIndex), GetFrameX(frameIndex), GetFrameY(frameIndex));
        }

        public Bitmap MakeBitmapByElementIndex(Int32 elementIndex)
        {
            Bitmap ret = null;
            Bitmap OriginalBitmap = null;

            OriginalBitmap = MakeBitmap(GetFrameDrawData(elementIndex), GetFrameX(elementIndex), GetFrameY(elementIndex), 0, 0, 0, 0);
            ret = new Bitmap(OriginalBitmap.Width, OriginalBitmap.Height);
            Graphics gp = Graphics.FromImage(ret);
            gp.DrawImage(OriginalBitmap, 0, 0);
            gp.Dispose();

            return ret;
        }

        public Bitmap MakeBitmapByParameterUnit(ParameterUnit pu, Boolean isCoordinateEffective)
        {
            Bitmap ret = null;
            Bitmap OriginalBitmap = null;

            OriginalBitmap = MakeBitmap(GetFrameDrawData(pu.FrameIndex), GetFrameX(pu.FrameIndex), GetFrameY(pu.FrameIndex), pu.Alpha, pu.Red, pu.Green, pu.Blue);
            if (isCoordinateEffective)
            {
                ret = new Bitmap(pu.DrawX + OriginalBitmap.Width, pu.DrawY + OriginalBitmap.Height);
                Graphics gp = Graphics.FromImage(ret);
                gp.DrawImage(OriginalBitmap, pu.DrawX, pu.DrawY);
                gp.Dispose();
            }
            else
            {
                ret = new Bitmap(OriginalBitmap.Width, OriginalBitmap.Height);
                Graphics gp = Graphics.FromImage(ret);
                gp.DrawImage(OriginalBitmap, 0, 0);
                gp.Dispose();
            }

            return ret;
        }

        public Bitmap MakeBitmapByFrameParameter(Int32 frameParamIndex, Boolean isTranspartent = false)
        {
            Bitmap ret = null;
            Bitmap OriginalBitmap = null;
            Bitmap temp = null;
            Int32 MaxWidth;
            Int32 MaxHeight;

            FrameParameter fp = frameParameter[frameParamIndex];

            if (fp.Params != null)
            {
                foreach (ParameterUnit pu in fp.Params)
                {
                    if (pu.FrameIndex < 0)
                    {
                        continue;
                    }
                    OriginalBitmap = MakeBitmap(GetFrameDrawData(pu.FrameIndex), GetFrameX(pu.FrameIndex), GetFrameY(pu.FrameIndex), pu.Alpha, pu.Red, pu.Green, pu.Blue, isTranspartent);
                    temp = new Bitmap(pu.DrawX + OriginalBitmap.Width, pu.DrawY + OriginalBitmap.Height);
                    if (isTranspartent)
                    {
                        temp.MakeTransparent();
                    }

                    Graphics gp = Graphics.FromImage(temp);
                    gp.DrawImage(OriginalBitmap, pu.DrawX, pu.DrawY);
                    gp.Dispose();
                    if (ret == null)
                    {
                        ret = temp;
                    }
                    else
                    {
                        MaxWidth = ret.Width;
                        MaxHeight = ret.Height;
                        if (ret.Width < temp.Width)
                        {
                            MaxWidth = temp.Width;
                        }
                        if (ret.Height < temp.Height)
                        {
                            MaxHeight = temp.Height;
                        }

                        Bitmap tbm = new Bitmap(MaxWidth, MaxHeight);

                        //Mat mat1 = BitmapConverter.ToMat(tbm);
                        //Mat mat2 = BitmapConverter.ToMat(ret);
                        //Mat mat3 = BitmapConverter.ToMat(temp);
                        //Cv2.AddWeighted(mat2, 1, mat3, 1, 0, mat1);
                        //tbm = mat1.ToBitmap();

                        if (isTranspartent)
                        {
                            tbm.MakeTransparent();
                        }
                        gp = Graphics.FromImage(tbm);
                        gp.DrawImage(ret, 0, 0);
                        if (pu.Alpha != 0)
                        {
                            int i;
                            int j;
                            for (i = 0; i < temp.Width; i++)
                            {
                                for (j = 0; j < temp.Height; j++)
                                {
                                    Color b = tbm.GetPixel(i, j);
                                    Color f = temp.GetPixel(i, j);
                                    int newA = b.A > f.A ? b.A : f.A;
                                    int newR = b.R > f.R ? b.R : f.R;
                                    int newG = b.G > f.G ? b.G : f.G;
                                    int newB = b.B > f.B ? b.B : f.B;
                                    Color c = Color.FromArgb(newA, newR, newG, newB);
                                    tbm.SetPixel(i, j, c);
                                }
                            }
                        }
                        else
                        {
                            gp.DrawImage(temp, 0, 0);
                        }

                        gp.Dispose();
                        ret.Dispose();
                        temp.Dispose();
                        ret = tbm;
                    }
                }
            }
            
            return ret;
        }

        public void SaveBitmapToFrame(Bitmap bm, Int32 frameIndex)
        {
            Int32 x = 0;
            Int32 y = 0;
            Int32 unitIndex;
            Byte[] originalUnitData = new Byte[BlockXLimit * BlockYLimit * 2];
            Byte[] compressedUnitData;
            Byte[] frameConstructData = GetFrameConstructData(frameIndex);
            Int32 offset = 0;
            Int32 lastOffset = 0;
            Int32 p;
            UInt16 color;

            for (p = 0; p < frameConstructData.Length; p += 2)
            {
                unitIndex = Util.GetBEInt16(frameConstructData, p);

                while (offset - lastOffset < originalUnitData.Length)
                {
                    SAFInfo.GetDrawCoordinate(offset, ref x, ref y, GetFrameX(frameIndex), GetFrameY(frameIndex));
                    Color c = bm.GetPixel(x, y);
                    color = (UInt16)((((UInt16)c.R & 0xF8) << 7) | (((UInt16)c.G & 0xF8) << 2) | (((UInt16)c.B & 0xF8) >> 3));

                    Util.SetBEUInt16(originalUnitData, offset % originalUnitData.Length, color);

                    offset += 2;
                }

                lastOffset = offset;
                compressedUnitData = CompressUnitData(originalUnitData);
                ReplaceUnitData(unitIndex, compressedUnitData);
            }

            Bitmap frame = MakeFrameBitmap(frameIndex);
            frameConstruct[frameIndex].bitmap = frame;
        }
        public SAFInfo(String fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            BinaryReader binReader = new BinaryReader(fs);
            SAFFile = fileName;
            byte[] bBuffer;
            int p = 12;
            int i = 1;
            int j = 1;
            int itemcount;
            int itemstart;
            int lastItemEnd;
            int itemlength;
            int subitemstart;
            int subitemend;
            byte[] subdata = null;

            bBuffer = new byte[fs.Length];
            binReader.Read(bBuffer, 0, (int)fs.Length);
            binReader.Close();
            fs.Close();

            itemcount = Util.GetBEInt16(bBuffer, p);
            while (itemcount != 0)
            {
                itemstart = Util.GetBEInt32(bBuffer, p + 2);
                itemlength = Util.GetBEInt32(bBuffer, p + 6);
	            lastItemEnd = itemstart + itemlength;
	            while (j <= itemcount)
                {
                    if (i > 5)
                    {
                        throw new Exception("出现未知Chunk");
                    }
                    if (i != 5)
                    {
                        subitemstart = Util.GetBEInt32(bBuffer, itemstart);
                        if (j < itemcount)
                        {
                            subitemend = Util.GetBEInt32(bBuffer, itemstart + 4);
                        }
                        else
                        {
                            subitemend = lastItemEnd;
                        }

                        subdata = GetSubArray(bBuffer, subitemstart, subitemend - subitemstart);
                    }
                    else
                    {
                        subdata = GetSubArray(bBuffer, p + 2, itemcount * 2);
                    }
                    switch (i)
                    {
                        case 1:
                            FrameParameter fp = new FrameParameter();
                            fp.Data = subdata;
                            frameParameter.Add(fp);
                            break;
                        case 2:
                            FrameConstruct fc = new FrameConstruct();
                            fc.Data = subdata;
                            fc.x = Util.GetBEInt16(subdata, 0) * 30;
                            fc.y = Util.GetBEInt16(subdata, 2) * 24;
                            frameConstruct.Add(fc);
                            break;
                        case 3:
                            UnitDataSet fd = new UnitDataSet();
                            fd.Data = subdata;
                            unitDataSet.Add(fd);
                            break;
                        case 4:
                            WaveData ud = new WaveData();
                            ud.Channels = subdata[0];
                            ud.Bits = subdata[1];
                            ud.SampleRate = Util.GetBEUInt16(subdata, 2);
                            ud.DataLength = Util.GetBEInt32(subdata, 4);
                            ud.Data = subdata;
                            WaveData.Add(ud);
                            break;
                        case 5:
                            UnknownData ud1 = new UnknownData();
                            ud1.Data = subdata;
                            unknownData1.Add(ud1);
                            break;
                    }

		            itemstart = itemstart + 4;
		            j = j + 1;
                }
	            p = p + 10;
	            i = i + 1;
	            j = 1;

                itemcount = Util.GetBEInt16(bBuffer, p);
            }

            for (i = 0; i < frameParameter.Count; i++)
            {
                Int16 paramCount = Util.GetBEInt16(frameParameter[i].Data, 8);
                frameParameter[i].WaveIndex = Util.GetBEInt16(frameParameter[i].Data, 0);
                Int16 tp = 10;
                if (paramCount != 0)
                {
                    if (frameParameter[i].Params == null)
                    {
                        frameParameter[i].Params = new List<ParameterUnit>();
                    }

                    for (j = 0; j < paramCount; j++)
                    {
                        ParameterUnit pu = new ParameterUnit();
                        pu.FrameIndex = Util.GetBEInt16(frameParameter[i].Data, tp);
                        tp += 2;
                        pu.DrawX = Util.GetBEInt16(frameParameter[i].Data, tp);
                        tp += 2;
                        pu.DrawY = Util.GetBEInt16(frameParameter[i].Data, tp);
                        tp += 2;
                        pu.Alpha = frameParameter[i].Data[tp];
                        tp += 1;
                        pu.Red = Util.GetBEInt16(frameParameter[i].Data, tp);
                        tp += 2;
                        pu.Green = Util.GetBEInt16(frameParameter[i].Data, tp);
                        tp += 2;
                        pu.Blue = Util.GetBEInt16(frameParameter[i].Data, tp);
                        tp += 2;

                        frameParameter[i].Params.Add(pu);
                    }
                }
            }

            //for (i = 0; i < frameConstruct.Count; i++)
            //{
            //    Bitmap frame = MakeFrameBitmap(i);
            //    frameConstruct[i].bitmap = frame;
            //}
        }

        private Int32 GetSAFCurrentFileSize(Boolean isDeleteWave)
        {
            Int32 ret = -1;

            if (SAFFile == null)
            {
                return ret;
            }

            ret = SAFHead.Length + SIZE_SECTOR + frameParameter.Count * 4 + frameConstruct.Count * 4 + unitDataSet.Count * 4;
            foreach(FrameParameter fp in frameParameter)
            {
                ret += fp.Data.Length;
            }
            foreach (FrameConstruct fc in frameConstruct)
            {
                ret += fc.Data.Length;
            }
            foreach (UnitDataSet ud in unitDataSet)
            {
                ret += ud.Data.Length;
            }

            if ((WaveData.Count != 0) && (!isDeleteWave))
            {
                ret += WaveData.Count * 4;

                foreach (WaveData ud in WaveData)
                {
                    ret += ud.Data.Length;
                }
            }

            return ret;
        }

        public String SaveSAFToFile(Boolean isDeleteWave)
        {
            String BackupFile = Path.Combine(Path.GetDirectoryName(SAFFile), Path.GetFileNameWithoutExtension(SAFFile) + "_Bak_" + DateTime.Now.Year.ToString().PadLeft(4, '0') + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0') + ".SAF");
            Int32 FileSize = GetSAFCurrentFileSize(isDeleteWave);
            if (FileSize == -1)
            {
                return "";
            }

            Byte[] FileData = new Byte[FileSize];
            Int32 p = 0x0C;
            Int32 pOffset = OFFSET_FRAME_PARAMETOR_BEGIN;
            Int32 pOffsetBegin = pOffset;
            Int32 pData = pOffset + 4 * frameParameter.Count;

            Array.Copy(SAFHead, FileData, SAFHead.Length);

            Util.SetBEUInt16(FileData, p, (UInt16)frameParameter.Count);
            p += 2;
            Util.SetBEUInt32(FileData, p, (UInt32)pOffset);
            p += 4;
            foreach(FrameParameter fp in frameParameter)
            {
                Util.SetBEUInt32(FileData, pOffset, (UInt32)pData);
                pOffset += 4;
                Array.Copy(fp.Data, 0, FileData, pData, fp.Data.Length);
                pData += fp.Data.Length;
            }
            Util.SetBEUInt32(FileData, p, (UInt32)(pData - pOffsetBegin));
            p += 4;

            pOffset = pData;
            pOffsetBegin = pOffset;
            pData = pOffset + 4 * frameConstruct.Count;
            Util.SetBEUInt16(FileData, p, (UInt16)frameConstruct.Count);
            p += 2;
            Util.SetBEUInt32(FileData, p, (UInt32)pOffset);
            p += 4;
            foreach (FrameConstruct fc in frameConstruct)
            {
                Util.SetBEUInt32(FileData, pOffset, (UInt32)pData);
                pOffset += 4;
                Array.Copy(fc.Data, 0, FileData, pData, fc.Data.Length);
                pData += fc.Data.Length;
            }
            Util.SetBEUInt32(FileData, p, (UInt32)(pData - pOffsetBegin));
            p += 4;

            pOffset = pData;
            pOffsetBegin = pOffset;
            pData = pOffset + 4 * unitDataSet.Count;
            Util.SetBEUInt16(FileData, p, (UInt16)unitDataSet.Count);
            p += 2;
            Util.SetBEUInt32(FileData, p, (UInt32)pOffset);
            p += 4;
            foreach (UnitDataSet ud in unitDataSet)
            {
                Util.SetBEUInt32(FileData, pOffset, (UInt32)pData);
                pOffset += 4;
                Array.Copy(ud.Data, 0, FileData, pData, ud.Data.Length);
                pData += ud.Data.Length;
            }
            Util.SetBEUInt32(FileData, p, (UInt32)(pData - pOffsetBegin));
            p += 4;

            if ((WaveData.Count != 0) && (!isDeleteWave))
            {
                pOffset = pData;
                pOffsetBegin = pOffset;
                pData = pOffset + 4 * WaveData.Count;
                Util.SetBEUInt16(FileData, p, (UInt16)WaveData.Count);
                p += 2;
                Util.SetBEUInt32(FileData, p, (UInt32)pOffset);
                p += 4;
                foreach (WaveData ud in WaveData)
                {
                    Util.SetBEUInt32(FileData, pOffset, (UInt32)pData);
                    pOffset += 4;
                    Array.Copy(ud.Data, 0, FileData, pData, ud.Data.Length);
                    pData += ud.Data.Length;
                }
                Util.SetBEUInt32(FileData, p, (UInt32)(pData - pOffsetBegin));
                p += 4;

                if (unknownData1.Count != 0)
                {
                    pOffset = pData;
                    pOffsetBegin = pOffset;
                    pData = pOffset + 4 * unknownData1.Count;
                    Util.SetBEUInt16(FileData, p, (UInt16)unknownData1.Count);
                    p += 2;
                    Array.Copy(unknownData1[0].Data, 0, FileData, p, unknownData1[0].Data.Length);
                    p += unknownData1[0].Data.Length;
                }
            }
            else
            {
                Util.SetBEUInt16(FileData, p, (UInt16)0);
                p += 2;
                Util.SetBEUInt32(FileData, p, (UInt32)pData);
                p += 4;
            }

            File.Copy(SAFFile, BackupFile);
            File.Delete(SAFFile);

            File.WriteAllBytes(SAFFile, FileData);

            return BackupFile;
        }

        public String OutputTXT(String path)
        {
            String ret = "";
            int i;

            ret = "----------------- frameParameter -----------------\r\n";
            i = 0;
            foreach (FrameParameter fp in frameParameter)
            {
                ret += Convert.ToString(i++, 16).PadLeft(2, '0').ToUpper() + " : " + Util.ConvBytesToHexString(fp.Data, 0, fp.Data.Length).ToUpper() +"\r\n";
            }

            ret += "----------------- frameConstruct -----------------\r\n";
            i = 0;
            foreach (FrameConstruct fc in frameConstruct)
            {
                ret += Convert.ToString(i++, 16).PadLeft(2, '0').ToUpper() + " : " + Util.ConvBytesToHexString(fc.Data, 0, fc.Data.Length).ToUpper() + "\r\n";
            }

            ret += "----------------- unitDataSet -----------------\r\n";
            i = 0;
            foreach (UnitDataSet ud in unitDataSet)
            {
                ret += Convert.ToString(i++, 16).PadLeft(2, '0').ToUpper() + " : " + Util.ConvBytesToHexString(ud.Data, 0, ud.Data.Length).ToUpper() + "\r\n";
            }

            ret += "----------------- WaveData -----------------\r\n";
            i = 0;
            foreach (WaveData wd in WaveData)
            {
                ret += Convert.ToString(i++, 16).PadLeft(2, '0').ToUpper() + " : " + Util.ConvBytesToHexString(wd.Data, 0, wd.Data.Length).ToUpper() + "\r\n";
            }

            ret += "----------------- unknownData1 -----------------\r\n";
            i = 0;
            foreach (UnknownData ud in unknownData1)
            {
                ret += Convert.ToString(i++, 16).PadLeft(2, '0').ToUpper() + " : " + Util.ConvBytesToHexString(ud.Data, 0, ud.Data.Length).ToUpper() + "\r\n";
            }

            //FileStream fs = File.Open(path, FileMode.OpenOrCreate);
            File.WriteAllText(path, ret);

            return ret;
        }
    }
}
