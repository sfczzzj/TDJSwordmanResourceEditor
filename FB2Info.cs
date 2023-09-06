using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace SAFEditor
{
    class UnitData
    {
        public Byte[] Data = null;
    }

    class FB2Info : BaseUnitInfo
    {
        private String FB2File;
        private String MPLFile;
        private Int32 MapX;
        private Int32 MapY;
        private Int32 UnitCount;
        private List<UnitData> unitDataSet = new List<UnitData>();
        private Int16[] UnitIndex;
        private Byte[] FB2Head = { 0x43, 0x45, 0x4C, 0xD0, 0x07, 0x0F, 0x00, 0x1E, 0x00, 0x18, 0x00, 0x00, 0x00, 0x10, 0x00 };  //BLockNum OFFSET 11
        private Byte[] MPLHead = { 0x4D, 0x50, 0x4C, 0xD0, 0x07, 0x0B, 0x00, 0x00, 0x00, 0x00, 0x00 };  //X OFFSET 7;Y OFFSET 9

        public void Dispose()
        {
            unitDataSet.Clear();
        }

        public FB2Info(String fileName)
        {
            FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".mpl"), FileMode.Open);
            BinaryReader binReader = new BinaryReader(fs);
            Int32 i;
            byte[] bBuffer;
            Int32 p = 0x0F;
            Int32 offset;
            Int32 offset1;

            FB2File = fileName;
            MPLFile = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".MPL");
            bBuffer = new byte[fs.Length];
            binReader.Read(bBuffer, 0, (int)fs.Length);
            binReader.Close();
            fs.Close();
            fs.Dispose();

            MapX = Util.GetBEInt16(bBuffer, 7);
            MapY = Util.GetBEInt16(bBuffer, 9);

            UnitIndex = new Int16[MapX * MapY];
            p = 0x0B;
            for (i = 0; i < MapX * MapY; i++)
            {
                UnitIndex[i] = Util.GetBEInt16(bBuffer, p);
                p += 2;
            }

            fs = new FileStream(fileName, FileMode.Open);
            binReader = new BinaryReader(fs);
            bBuffer = new byte[fs.Length];
            binReader.Read(bBuffer, 0, (int)fs.Length);
            binReader.Close();
            fs.Close();
            fs.Dispose();

            UnitCount = Util.GetBEInt16(bBuffer, 0x0B);
            p = 0x0F;
            for (i = 0; i < UnitCount; i++)
            {
                if (i == UnitCount - 1)
                {
                    offset = Util.GetBEInt32(bBuffer, p);
                    UnitData ud = new UnitData();
                    ud.Data = GetSubArray(bBuffer, offset, bBuffer.Length - offset);
                    unitDataSet.Add(ud);
                }
                else
                {
                    offset = Util.GetBEInt32(bBuffer, p);
                    offset1 = Util.GetBEInt32(bBuffer, p + 4);
                    UnitData ud = new UnitData();
                    ud.Data = GetSubArray(bBuffer, offset, offset1 - offset);
                    unitDataSet.Add(ud);
                    p += 4;
                }
            }

        }

        private Byte[] GetMapDrawData()
        {
            Byte[] ret = new Byte[MapX * BlockXLimit * MapY * BlockYLimit * 2];
            Int32 p;
            Int32 i;

            p = 0;

            for (i = 0; i < UnitIndex.Length; i++)
            {
                Byte[] unitData = GetDrawData(unitDataSet[UnitIndex[i]].Data);
                CopyBuffer(unitData, 0, ret, p, unitData.Length);
                p += unitData.Length;
            }
            return ret;
        }

        public Bitmap GetMapBitmap()
        {
            return MakeBitmap(GetMapDrawData(), MapX * BlockXLimit, MapY * BlockYLimit);
        }

        private Int32 GetMPLCurrentFileSize()
        {
            Int32 ret = -1;

            if (FB2File == null)
            {
                return ret;
            }

            ret = MPLHead.Length + UnitIndex.Length * 2;

            return ret;
        }

        private Int32 GetFB2CurrentFileSize()
        {
            Int32 ret = -1;

            if (FB2File == null)
            {
                return ret;
            }

            ret = FB2Head.Length + unitDataSet.Count * 4;
            foreach (UnitData ud in unitDataSet)
            {
                ret += ud.Data.Length;
            }

            return ret;
        }

        public String SaveFB2ToFile()
        {
            String BackupFile = Path.Combine(Path.GetDirectoryName(FB2File), Path.GetFileNameWithoutExtension(FB2File) + "_Bak_" + DateTime.Now.Year.ToString().PadLeft(4, '0') + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0') + ".FB2");
            Int32 FileSize = GetFB2CurrentFileSize();
            if (FileSize == -1)
            {
                return "";
            }

            Byte[] FileData = new Byte[FileSize];
            Int32 pOffset = 0x0F;
            Int32 pData = pOffset + 4 * unitDataSet.Count;

            Array.Copy(FB2Head, FileData, FB2Head.Length);

            Util.SetBEUInt16(FileData, 11, (UInt16)unitDataSet.Count);

            foreach(UnitData ud in unitDataSet)
            {
                Util.SetBEUInt32(FileData, pOffset, (UInt32)pData);
                pOffset += 4;
                Array.Copy(ud.Data, 0, FileData, pData, ud.Data.Length);
                pData += ud.Data.Length;
            }

            File.Copy(FB2File, BackupFile);
            File.Delete(FB2File);

            File.WriteAllBytes(FB2File, FileData);

            return BackupFile;
        }

        public String SaveMPLToFile()
        {
            String BackupFile = Path.Combine(Path.GetDirectoryName(MPLFile), Path.GetFileNameWithoutExtension(MPLFile) + "_Bak_" + DateTime.Now.Year.ToString().PadLeft(4, '0') + DateTime.Now.Month.ToString().PadLeft(2, '0') + DateTime.Now.Day.ToString().PadLeft(2, '0') + DateTime.Now.Hour.ToString().PadLeft(2, '0') + DateTime.Now.Minute.ToString().PadLeft(2, '0') + DateTime.Now.Second.ToString().PadLeft(2, '0') + ".MPL");
            Int32 FileSize = GetMPLCurrentFileSize();
            if (FileSize == -1)
            {
                return "";
            }

            Byte[] FileData = new Byte[FileSize];
            Int32 pOffset = 0x0B;
            Int32 pData = pOffset + 4 * UnitIndex.Length;

            Array.Copy(MPLHead, FileData, MPLHead.Length);

            Util.SetBEUInt16(FileData, 7, (UInt16)MapX);
            Util.SetBEUInt16(FileData, 9, (UInt16)MapY);

            foreach (Int16 index in UnitIndex)
            {
                Util.SetBEUInt16(FileData, pOffset, (UInt16)index);
                pOffset += 2;
            }

            File.Copy(MPLFile, BackupFile);
            File.Delete(MPLFile);

            File.WriteAllBytes(MPLFile, FileData);

            return BackupFile;
        }

        public void SaveBitmapToFB2Info(Bitmap bm)
        {
            Int32 x = 0;
            Int32 y = 0;
            Byte[] originalUnitData = new Byte[BlockXLimit * BlockYLimit * 2];
            //Byte[] compressedUnitData;
            Int32 offset = 0;
            Int32 lastOffset = 0;
            Int32 p;
            UInt16 color;

            unitDataSet.Clear();
            UnitIndex = new Int16[MapX * MapY];

            for (p = 0; p < MapX * MapY; p++)
            {
                while (offset - lastOffset < originalUnitData.Length)
                {
                    SAFInfo.GetDrawCoordinate(offset, ref x, ref y, MapX * BlockXLimit, MapY * BlockYLimit);
                    Color c = bm.GetPixel(x, y);
                    color = (UInt16)((((UInt16)c.R & 0xF8) << 7) | (((UInt16)c.G & 0xF8) << 2) | (((UInt16)c.B & 0xF8) >> 3));

                    Util.SetBEUInt16(originalUnitData, offset % originalUnitData.Length, color);

                    offset += 2;
                }

                lastOffset = offset;
                UnitData ud = new UnitData();
                ud.Data = CompressUnitData(originalUnitData);
                unitDataSet.Add(ud);
                UnitIndex[p] = (Int16)p;
            }
        }
    }
}
