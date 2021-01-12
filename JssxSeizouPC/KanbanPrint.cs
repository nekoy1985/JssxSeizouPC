using System;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using System.IO;
using System.Drawing;
using System.Data;
using ZXing.Common;
using ZXing.QrCode;
using ZXing;
using System.Windows.Media.Imaging;
using System.Windows;
using NPOI.HSSF.Util;

namespace JssxSeizouPC
{
    public class KanbanPrint
    {
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);
        public static bool Newpage = true;
        public static int Pag;
        public static int SheetNo;
        public string CustomerName, Username;
        class Externs
        {
            [DllImport("winspool.drv")]
            public static extern bool SetDefaultPrinter(String Name);
        }

        public static string GetData(string UniqueID, string InStockNumber, string WorkShift, string PrintNo, string Label, string SN, string Lines)
        {
            DataTable Dt = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select distinct TrayVolume as 托收容,EuCode as 包装,IsPrinted^1 as 打印,line as 生产线,CONVERT(varchar(10),PlanTime, 23) as 计划日期,a.JSSXInsideCode as 背番,iif(EUCode<>0 and EUCode<>9 and EUCode<>10,KanbanAmount*TrayVolume,KanbanAmount) as 数量,a.JSSXCode as 社番,a.CustomerCode as 客番,c.CustomerInsideCode as 客户背番,InStockNumber as 指示单号,b.CreateTime as 创建时间,Creator as 创建者,[Version] as 版本,KanbanAmount as 需求看板,TrayVolume as 收容数,(select max(SerialNo) from JSSX_Stock_In_SerialNoRecord) as 流水号,c.UniqueID as 主键,iif(IsRepeated=1,1301,iif(IsRepeated=2,0101,d.CategoryNumber)) as 客户编码, (case c.TrayType when '量产' then 'B01L' when '补用品' then 'B01B' when '单纳品' then 'B01D' when '样品' then 'B01Y' when '特殊' then 'B01T' else 'ERROR' end) as 看板类型,CarType as 车型,ProName as 品名,iif(f.Name='广本' or f.Name='东本' ,'本田',f.Name) as 客户名称,Amount% Volume as 端数,EuCode as 箱种,EuName as 容器名称,iif(FreeOfCharge='True',1,0) as 有无偿,a.Note as 备注,g.WHCode as 中间仓编号,g.WHName as 仓库名称,h.WHCode as 中继仓编号,h.WHName as 中继仓名,e.Redistribute as 出货便名,e.CustomerCode as JCC客户代码,iif(LabelType='客户',1,0) as 销售模式 from [dbo].[JSSX_Stock_In_Detailed] as a left join [dbo].[JSSX_Stock_in] as b on a.instocknumber=b.Number  left join JSSX_Products c on a.UniqueID=c.UniqueID left join JSSX_Products_Category_Detailed d on d.UniqueID=a.UniqueID left join JSSX_Products_Category e on d.CategoryNumber=e.Number left join JSSX_Custom f on left(e.Number,2)=f.Number left join JSSX_WareHouse g on e.Warehouse = g.WHCode left join JSSX_WareHouse h on g.IntermediateWarehouse = h.WHCode where  InStockNumber= '" + InStockNumber + "' and a.UniqueID ='" + UniqueID + "'  and d.State is null and c.Revoked=0 and isdelete =0 ").Tables[0];
            string SalesMode, EuName, RelayStation, WHLocation, Redistribute, Companynumber, WHCodeb, CustomerCode, Warehouse, WHName, EuCode, CustomerInsideCode, JCCCustomerNo, TrayVolume, JssxCode, JssxInsideCode, Amount, SerialNo, Volume, CustomerNo, UniqueNo, KanbanNo, TrayType, CData, CarType, ProName, Line, PlanTime, CustomerName, Complement, KanbanAoumt, FOC;
            TrayVolume = Dt.Rows[0]["托收容"].ToString();
            EuCode = Dt.Rows[0]["包装"].ToString();
            TrayType = Dt.Rows[0]["看板类型"].ToString();
            UniqueNo = Dt.Rows[0]["主键"].ToString();
            Volume = Int32.Parse(Dt.Rows[0]["收容数"].ToString()).ToString("0000");
            CustomerNo = Dt.Rows[0]["客户编码"].ToString();
            SalesMode = Dt.Rows[0]["销售模式"].ToString();
            if (SN == "")
            {
                SerialNo = Int32.Parse(Dt.Rows[0]["流水号"].ToString()).ToString("00000000");
            }
            else
            {
                SerialNo = SN;
            }

            //JCC
            WHCodeb = "";
            Warehouse = Dt.Rows[0]["中间仓编号"].ToString();
            CustomerInsideCode = Dt.Rows[0]["客户背番"].ToString();
            JCCCustomerNo = Dt.Rows[0]["JCC客户代码"].ToString();
            Companynumber = "K0000004";//JSSX固定
            Redistribute = Dt.Rows[0]["出货便名"].ToString();
            RelayStation = "";
            WHLocation = "";
            EuName = Dt.Rows[0]["容器名称"].ToString();
            WHName = Dt.Rows[0]["仓库名称"].ToString();
            //JCC

            JssxCode = Dt.Rows[0]["社番"].ToString();
            CustomerCode = Dt.Rows[0]["客番"].ToString();
            JssxInsideCode = Dt.Rows[0]["背番"].ToString();
            InStockNumber = Dt.Rows[0]["指示单号"].ToString();
            Amount = Dt.Rows[0]["数量"].ToString();
            CarType = Dt.Rows[0]["车型"].ToString();
            ProName = Dt.Rows[0]["品名"].ToString();
            Line = Dt.Rows[0]["生产线"].ToString();
            PlanTime = Dt.Rows[0]["计划日期"].ToString();
            CustomerName = Dt.Rows[0]["客户名称"].ToString();
            Complement = Dt.Rows[0]["端数"].ToString();
            KanbanAoumt = Dt.Rows[0]["需求看板"].ToString();
            FOC = Dt.Rows[0]["有无偿"].ToString();

            try
            {
                KanbanNo = "EU";
                CData = QRData(SalesMode, Companynumber, WHCodeb, "0", "0", Warehouse, EuCode, CustomerCode, CustomerInsideCode, JCCCustomerNo, TrayType, JssxCode, CustomerNo, JssxInsideCode, "1", SerialNo, UniqueNo, InStockNumber, "E", FOC);
                createQRCode(InStockNumber + SerialNo, CData);
                CExcel(WHName, RelayStation, WHLocation, Redistribute, WHCodeb, Warehouse, EuName, TrayType, JssxCode, CustomerCode, JssxInsideCode, "1", SerialNo, UniqueNo, InStockNumber, KanbanNo, CarType, ProName, Line, PlanTime, CustomerName, PrintNo, FOC, Label);
                KanbanPrint_Web(InStockNumber, JssxInsideCode, PrintNo);
                SheetNo = 0;
                //sqlHelp.ExecuteSqlTran(sqlHelp.ConnectionStringLocalTransaction,  "update JSSX_Stock_In_Detailed set isprinted=1,EndNo= '" + SerialNo + "' where instocknumber='" + InStockNumber + "' and  UniqueID='" + UniqueNo + "' and  WorkShift='" + WorkShift + "'");
                return UniqueNo + SerialNo;
            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
                return ex.ToString();
            }

        }

        public static bool Isnull(string str, string StrN)
        {
            if (str == "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string DelChinese(string str)
        {
            string retValue = str;
            if (System.Text.RegularExpressions.Regex.IsMatch(str, @"[\u4e00-\u9fa5]"))
            {
                retValue = string.Empty;
                var strsStrings = str.ToCharArray();
                for (int index = 0; index < strsStrings.Length; index++)
                {
                    if (strsStrings[index] >= 0x4e00 && strsStrings[index] <= 0x9fa5)
                    {
                        continue;
                    }
                    retValue += strsStrings[index];
                }
            }
            return retValue;
        }

        private static void createQRCode(string path, String content)
        {
            EncodingOptions options;
            //包含一些编码、大小等的设置
            //BarcodeWriter :一个智能类来编码一些内容的条形码图像
            BarcodeWriter write = null;
            options = new QrCodeEncodingOptions
            {
                DisableECI = true,
                CharacterSet = "UTF-8",
                Width = 180,
                Height = 180,
                Margin = 0
            };
            write = new BarcodeWriter();
            //设置条形码格式
            write.Format = BarcodeFormat.QR_CODE;
            //获取或设置选项容器的编码和渲染过程。
            write.Options = options;
            //对指定的内容进行编码，并返回该条码的呈现实例。渲染属性渲染实例使用，必须设置方法调用之前。
            Bitmap bitmap = write.Write(content);
            IntPtr ip = bitmap.GetHbitmap();//从GDI+ Bitmap创建GDI位图对象
                                            //Imaging.CreateBitmapSourceFromHBitmap方法，基于所提供的非托管位图和调色板信息的指针，返回一个托管的BitmapSource
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty,
            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(ip);
            BitmapEncoder encoder = null;
            encoder = new PngBitmapEncoder();
            FileStream stream = new FileStream("QRCode\\" + path + ".png", FileMode.Create);
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(stream);
            stream.Dispose();
        }

        /// <summary>
        /// 生产二维码的数据
        /// </summary>
        /// <param name="Companynumber">公司号</param>
        /// <param name="WHCodeb">仓库代码</param>
        /// <param name="Numerator">分子</param>
        /// <param name="Denominator">分母</param>
        /// <param name="CustomerCode">客户代码</param>
        /// <param name="Warehouse">仓库名</param>
        /// <param name="EuCode">EU代码</param>
        /// <param name="CustomerInsideCode">客番</param>
        /// <param name="CustomerInsideCode">客户背番</param>
        /// <param name="JCCCustomerNo">JCC客户号</param>
        /// <param name="Types">类型</param>
        /// <param name="TrayType">托盘类型</param>
        /// <param name="JssxCode">社番</param>
        /// <param name="CustomerNo">客户号</param>
        /// <param name="JssxInsideCode">背番</param>
        /// <param name="Volume">收容数</param>
        /// <param name="SerialNo">流水号</param>
        /// <param name="UniqueNo">UID</param>
        /// <param name="InStockNumber">入库单号</param>
        /// <param name="WorkShift">白夜班</param>
        /// <param name="FOC">有无偿</param>
        /// <returns></returns>
        public static string QRData(string SalesMode, string Companynumber, string WHCodeb, string Numerator, string Denominator, string Warehouse, string EuCode, string CustomerCode, string CustomerInsideCode, string JCCCustomerNo, string TrayType, string JssxCode, String CustomerNo, String JssxInsideCode, String Volume, String SerialNo, String UniqueNo, String InStockNumber, string WorkShift, string FOC)
        {
            string sType = "", space = "";
            if (TrayType == "ERROR")
            {
                MessageBox.Show("获取看板类型失败！");
                return "";
            }
            if (Isnull(TrayType, "看板类型") | Isnull(JssxCode, "社番") | Isnull(CustomerNo, "客码") | Isnull(JssxInsideCode, "背番") | Isnull(Volume, "收容数") | Isnull(SerialNo, "流水号") | Isnull(UniqueNo, "主键") | Isnull(InStockNumber, "指示单号"))
            {
                return "";
            }
            if (TrayType == "ERROR")
            {
                MessageBox.Show("获取看板类型失败！");
                return "";
            }
            else if (TrayType.Substring(3, 1) == "L")
            {
                sType = "1";
            }
            else if (TrayType.Substring(3, 1) == "B")
            {
                sType = "2";
            }
            else if (TrayType.Substring(3, 1) == "D")
            {
                sType = "3";
            }
            else if (TrayType.Substring(3, 1) == "T")
            {
                sType = "4";
            }
            else if (TrayType.Substring(3, 1) == "Y")
            {
                sType = "5";
            }
            else
            {
                sType = "0";
            }
            return "F03JSSX" + Companynumber.PadRight(8, ' ') + TrayType.PadRight(6, ' ') + UniqueNo.PadRight(10, ' ') + "0001" + WorkShift + FOC + WHCodeb.PadRight(5, ' ') + space.PadRight(16, ' ') + space.PadRight(11, ' ') + Numerator.PadLeft(4, '0') + Denominator.PadLeft(4, '0') + SerialNo.PadLeft(8, '0') + JssxInsideCode.PadRight(4, ' ') + JssxCode.PadRight(20, ' ') + Volume.PadLeft(4, '0') + Warehouse.PadRight(5, ' ') + CustomerNo.PadLeft(10, ' ') + EuCode.PadLeft(2, '0') + space.PadRight(11, ' ') + CustomerCode.PadRight(20, ' ') + CustomerInsideCode.PadRight(4, ' ') + JCCCustomerNo.PadRight(8, ' ') + sType + SalesMode + space.PadRight(55, ' ') + InStockNumber.PadRight(20, ' ');
        }

        public static void CExcel(string WHName, string RelayStation, string WHLocation, string Redistribute, string WHCodeb, string Warehouse, string EuName, string TrayType, string JssxCode, String CustomerCode, String JssxInsideCode, String Volume, String SerialNo, String UniqueNo, String InStockNumber, String KanbanNo, String CarType, String ProName, string Line, string PlanTime, string CustomerName, string PrintNo, string FOC, string label)
        {
            int pictureIdx = 0;
            int pictureIdx2 = 0;
            int pictureIdx3 = 0;
            string Str_SheetNo = (++SheetNo).ToString();
            string ImagePath = "";
            string Logo = "Type\\Logo.png";
            string QrPath = "QRCode\\" + InStockNumber + SerialNo + ".png";
            if (TrayType.Substring(3, 1) == "L")
            {
                ImagePath = "Type\\L.png";
            }
            else if (TrayType.Substring(3, 1) == "B")
            {
                ImagePath = "Type\\B.png";
            }
            else if (TrayType.Substring(3, 1) == "D")
            {
                ImagePath = "Type\\D.png";
            }
            else if (TrayType.Substring(3, 1) == "Y")
            {
                ImagePath = "Type\\Y.png";
            }
            else if (TrayType.Substring(3, 1) == "T")
            {
                ImagePath = "Type\\T.png";
            }
            else
            {
                MessageBox.Show("没有获取到印章!");
                return;
            }
            byte[] bytes = System.IO.File.ReadAllBytes(ImagePath);
            byte[] bytes2 = System.IO.File.ReadAllBytes(QrPath);
            byte[] bytes3 = System.IO.File.ReadAllBytes(Logo);
            DateTime DT = System.DateTime.Now;
            string dt = System.DateTime.Now.ToString();
            System.Data.DataTable getrow = new System.Data.DataTable();
            System.Data.DataTable getdata = new System.Data.DataTable();
            if (SheetNo == 1 || SheetNo % 35 == 0)
            {
                if (SheetNo % 35 == 0)
                {
                    KanbanPrint_Web(InStockNumber, JssxInsideCode, PrintNo);
                    Pag++;
                    Newpage = true;
                }

                System.IO.File.Copy("module\\kanban.xls", "Normal\\" + InStockNumber + "_" + JssxInsideCode + "_" + Pag.ToString("000") + ".xls", true);//JssxInsideCode+"_" + SerialNo+".xls"


            }

            HSSFWorkbook workbook = null;//创建Workbook
            using (FileStream fs = new FileStream("Normal\\" + InStockNumber + "_" + JssxInsideCode + "_" + Pag.ToString("000") + ".xls", FileMode.Open, FileAccess.Read))
            {
                workbook = new HSSFWorkbook(fs);
                var sheet2 = workbook.GetSheetAt(0) as HSSFSheet;
                var fs2 = new FileStream("Normal\\" + InStockNumber + "_" + JssxInsideCode + "_" + Pag.ToString("000") + ".xls", FileMode.Create);
                sheet2.CopyTo(workbook, Str_SheetNo, true, true);
                workbook.Write(fs2);
                fs2.Close();

                pictureIdx = workbook.AddPicture(bytes, NPOI.SS.UserModel.PictureType.PNG);
                pictureIdx2 = workbook.AddPicture(bytes2, NPOI.SS.UserModel.PictureType.PNG);
                pictureIdx3 = workbook.AddPicture(bytes3, NPOI.SS.UserModel.PictureType.PNG);
                fs.Close();
            }
            ICellStyle style = workbook.CreateCellStyle();
            style.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            style.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            style.ShrinkToFit = true;
            //style.BorderBottom = BorderStyle.Thin;
            //style.BorderLeft = BorderStyle.Thin;
            //style.BorderRight = BorderStyle.Thin;
            //style.BorderTop = BorderStyle.Thin; 
            IFont Font = workbook.CreateFont();
            Font.FontHeightInPoints = 13;
            style.SetFont(Font);

            ICellStyle style2 = workbook.CreateCellStyle();
            style2.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            style2.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            style2.FillForegroundColor = HSSFColor.Black.Index;
            style2.FillPattern = FillPattern.SolidForeground;
            style2.ShrinkToFit = true;
            IFont Font2 = workbook.CreateFont();
            Font2.Boldweight = (short)FontBoldWeight.Bold;
            Font2.FontHeightInPoints = 60;
            Font2.Color = HSSFColor.White.Index;
            style2.SetFont(Font2);

            ICellStyle style3 = workbook.CreateCellStyle();
            style3.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            style3.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            style3.ShrinkToFit = true;
            IFont Font3 = workbook.CreateFont();
            Font3.Boldweight = (short)FontBoldWeight.Bold;
            Font3.FontHeightInPoints = 40;
            style3.SetFont(Font3);

            ICellStyle style4 = workbook.CreateCellStyle();
            style4.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            style4.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            style4.ShrinkToFit = true;
            IFont Font4 = workbook.CreateFont();
            Font4.Boldweight = (short)FontBoldWeight.Bold;
            Font4.FontHeightInPoints = 20;
            style4.SetFont(Font4);


            ICellStyle style5 = workbook.CreateCellStyle();
            style5.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            style5.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            style5.BorderLeft = BorderStyle.Thin;
            style5.ShrinkToFit = true;
            IFont Font5 = workbook.CreateFont();
            Font5.Boldweight = (short)FontBoldWeight.Bold;
            Font5.FontHeightInPoints = 20;
            style5.SetFont(Font5);

            ICellStyle style6 = workbook.CreateCellStyle();
            style6.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
            style6.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
            style6.BorderTop = BorderStyle.Thin;
            style6.WrapText = true;
            style6.ShrinkToFit = true;
            IFont Font6 = workbook.CreateFont();
            Font6.Boldweight = (short)FontBoldWeight.Bold;
            Font6.FontHeightInPoints = 10;
            style6.SetFont(Font6);



            ISheet sheet = workbook.GetSheet(Str_SheetNo);
            HSSFPatriarch patriarch = (HSSFPatriarch)sheet.CreateDrawingPatriarch();
            HSSFClientAnchor anchor = new HSSFClientAnchor(0, 0, 0, 0, 61, 18, 66, 22);
            HSSFPicture pict = (HSSFPicture)patriarch.CreatePicture(anchor, pictureIdx);

            anchor = new HSSFClientAnchor(0, 0, 0, 0, 29, 8, 39, 15);
            pict = (HSSFPicture)patriarch.CreatePicture(anchor, pictureIdx2);//QR

            anchor = new HSSFClientAnchor(200, 70, 50, 50, 2, 1, 9, 3);
            pict = (HSSFPicture)patriarch.CreatePicture(anchor, pictureIdx3);//LOGO

            //W3
            sheet.GetRow(3).CreateCell(23).SetCellValue(JssxInsideCode);
            sheet.GetRow(3).GetCell(23).CellStyle = style2;
            //W20
            sheet.GetRow(20).CreateCell(23).SetCellValue(Int32.Parse(Volume).ToString());
            sheet.GetRow(20).GetCell(23).CellStyle = style;
            //A20
            sheet.GetRow(20).CreateCell(1).SetCellValue(KanbanNo);
            sheet.GetRow(20).GetCell(1).CellStyle = style5;
            //W16
            sheet.GetRow(16).CreateCell(23).SetCellValue(JssxCode);
            sheet.GetRow(16).GetCell(23).CellStyle = style;
            //F12
            sheet.GetRow(12).CreateCell(6).SetCellValue(ProName);
            sheet.GetRow(12).GetCell(6).CellStyle = style4;
            //F15
            sheet.GetRow(15).CreateCell(6).SetCellValue(CarType);
            if (CarType.Length >= 6)
            {
                sheet.GetRow(15).GetCell(6).CellStyle = style6;
            }
            else
            {
                sheet.GetRow(15).GetCell(6).CellStyle = style5;
            }

            //L20
            sheet.GetRow(20).CreateCell(12).SetCellValue(InStockNumber.Substring(4, 13));
            sheet.GetRow(20).GetCell(12).CellStyle = style;
            //L18
            sheet.GetRow(18).CreateCell(12).SetCellValue(SerialNo);
            sheet.GetRow(18).GetCell(12).CellStyle = style;
            //F8
            sheet.GetRow(8).CreateCell(6).SetCellValue(Line);
            sheet.GetRow(8).GetCell(6).CellStyle = style3;
            //F5
            sheet.GetRow(5).CreateCell(6).SetCellValue(PlanTime);
            sheet.GetRow(5).GetCell(6).CellStyle = style;


            //AR7
            sheet.GetRow(7).CreateCell(44).SetCellValue(CustomerName);
            sheet.GetRow(7).GetCell(44).CellStyle = style;
            //AR10
            sheet.GetRow(10).CreateCell(44).SetCellValue(CustomerCode);
            sheet.GetRow(10).GetCell(44).CellStyle = style;
            //AD20
            sheet.GetRow(20).CreateCell(30).SetCellValue(EuName);
            sheet.GetRow(20).GetCell(30).CellStyle = style;
            //AX1
            sheet.GetRow(1).CreateCell(50).SetCellValue(WHName);
            sheet.GetRow(1).GetCell(50).CellStyle = style6;
            //AX3
            sheet.GetRow(3).CreateCell(50).SetCellValue(WHLocation);
            sheet.GetRow(3).GetCell(50).CellStyle = style;
            //AX12
            sheet.GetRow(12).CreateCell(50).SetCellValue(Redistribute);
            sheet.GetRow(12).GetCell(50).CellStyle = style;
            //AX15
            sheet.GetRow(18).CreateCell(44).SetCellValue(label);
            sheet.GetRow(18).GetCell(44).CellStyle = style6;

            using (FileStream fileStream = File.Open("Normal\\" + InStockNumber + "_" + JssxInsideCode + "_" + Pag.ToString("000") + ".xls", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                workbook.Write(fileStream);
                fileStream.Close();
            }

        }

        public static void KanbanPrint_Web(string OutStockNumber, string JssxInsideCode, string PrintNo)
        {
            PrintDocument prtdoc = new PrintDocument();
            string strDefaultPrinter = prtdoc.PrinterSettings.PrinterName;
            //根据OutStockNumber的line信息选择打印机
            if (Externs.SetDefaultPrinter("line" + PrintNo)) //设置默认打印机
            {

            }
            else
            {
                //插入数据库或者返回异常提示
                return;
            }

            Microsoft.Office.Interop.Excel.Application xlsApp = new Microsoft.Office.Interop.Excel.Application();

            Microsoft.Office.Interop.Excel._Workbook xlsBook = xlsApp.Workbooks.Open(Environment.CurrentDirectory + "\\Normal\\" + OutStockNumber + "_" + JssxInsideCode + "_" + Pag.ToString("000") + ".xls");

            Microsoft.Office.Interop.Excel._Worksheet xlsSheet = (Microsoft.Office.Interop.Excel._Worksheet)xlsBook.Worksheets[1];

            xlsSheet.Activate();
            xlsSheet.PageSetup.PrintGridlines = false;
            xlsSheet.PageSetup.CenterHorizontally = true;
            xlsApp.DisplayAlerts = false;
            xlsSheet.Delete();
            xlsApp.DisplayAlerts = true;

            xlsBook.PrintOut();

            xlsBook.Saved = true;

            xlsApp.Workbooks.Close();
            xlsApp.Visible = false;

            xlsApp.Quit();
            Externs.SetDefaultPrinter(strDefaultPrinter);

        }

    }
}