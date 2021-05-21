using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Media;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace JssxSeizouPC
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量
        TextBox SelTB;
        private static int iWorkType = 0;//工作模式

        private static int iRow = 0, iProductSelection = -1;
        private DispatcherTimer timer, timer2, OpenComTimer, SpeedTest, tButtonLocker, Schedule, DTStatus;
        private string sCom1 = "";
        private static string Resplus = "";
        private static string sTemp = "";
        private static string DayNight = "";
        private static string GunNo = "";
        private static string CustomerCode = "";
        private static string CustomerNo = "";
        private static string JssxCode = "";
        private static string JssxInsideCode = "";
        private static string Series = "";
        private static string UniqueID = "";
        private static string SN = "";
        private static string CarLabel = "";
        private static int Amount = 0, COM = 1, OpenedCom = 0, iAmount;
        private static int icarlabelLength = 0;
        private bool bisPress = true, isfinish, IsRebuild = false, Admin = false, TimerLock = false, OutStock = false, AllowPass = false, bIsNewVersion, bIsClickable, bIsNeedOK;
        private bool bDevice1, bDevice2, bDevice3, bPrinter;
        private static string EUCode = ""; 
        private static DataSet Mk = new DataSet();
        private static DataSet DS_Show = new DataSet();
        private static DataSet DS_EUCode = new DataSet();
        private static DataSet DS_30Days = new DataSet();
        private DataRow[] foundRows;
        public SerialPort myComPort;
        public SerialPort myComPort2;

        SoundPlayer player = new SoundPlayer("error.wav");
        SoundPlayer player2 = new SoundPlayer("Pass.wav");
        SoundPlayer CompletedSound = new SoundPlayer("CompletedSound.wav");
        SoundPlayer Back = new SoundPlayer("Back.wav");
        SoundPlayer GetNew = new SoundPlayer("GetNew.wav");
        SoundPlayer Okay = new SoundPlayer("Okay.wav");
        SerialPort port1;
        public string PlanNo, BigKanbanNo = "", SmallKanbanNo, LabelNo, UserName, Lines, FullCom, sPrintIP;
        public bool bisAllowScan = false;    //控制是否可以扫描。
                                             //量产品以外的，量产品HVPT 属于分类A，下机状态才可以扫描，
                                             //量产品不带HVPT的属于分类B。上机状态才可以扫描
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            BindPreviewMouseUpEvent();

        }

        #region 事件

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            System.Configuration.ConfigurationManager.RefreshSection("appSettings");
            Lines = System.Configuration.ConfigurationManager.AppSettings["Lines"];
            bIsNewVersion = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["bIsNewVersion"]);
            bIsClickable = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["bIsClickable"]);
            bIsNeedOK = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["bIsNeedOK"]);
            sPrintIP = System.Configuration.ConfigurationManager.AppSettings["PrintIP"];
            if (Lines == "0")
            {
                MessageBox.Show("获取线别失败，程序即将关闭。");
                Environment.Exit(0);
            }
            if (!bIsClickable)
            {
                if (Lines != "06")
                {
                    DP_Datatime.Visibility = Visibility.Hidden;
                }
                Btn_MSSubmit2.Visibility = Visibility.Hidden;
                Tb_MSSTART.Visibility = Visibility.Hidden;
                Tb_MSEND.Visibility = Visibility.Hidden;
                Btn_MSSubmit.Visibility = Visibility.Hidden;
            }
            if (bIsNeedOK)
            {
                Btn_SetOK.Visibility = Visibility.Visible;
            }
            FullCom = System.Configuration.ConfigurationManager.AppSettings["Devices"];
            GetOldData(Lines);
            Tb_Version.Content = " Ver " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Btn_OpenCom_Click(null, null);

            #region  上下机操作
            string LN = System.Configuration.ConfigurationManager.AppSettings["LinesName"];
            string sql_Working = "select *,CONVERT(varchar(20), cStartTime, 120) as cTime from JSSX_Line_Working_Status where cEndTime is  null and cLineCode = '" + LN + "'";
            DataSet ds_Working = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_Working);
            if (ds_Working.Tables[0].Rows.Count == 0)
            {
                //下机状态，显示上机文字
                Btn_Shutdown.Content = "▶上机";
                Btn_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                Lab_Shutdown.Content = "当前状态：停止中";
                Lab_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));

            }
            else
            {
                //上机状态，显示下机文字
                Btn_Shutdown.Content = "■下机";
                Btn_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));

                Lab_Shutdown.Content = "当前状态：运行中 " + ds_Working.Tables[0].Rows[0]["cTime"].ToString();
                Lab_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));

            }
            #endregion  

        }

        private void Btn_Shutdown_Click(object sender, RoutedEventArgs e)
        {
            #region  上下机操作

            string LN = System.Configuration.ConfigurationManager.AppSettings["LinesName"];
            if (Btn_Shutdown.Content.ToString() == "▶上机")
            {//按了之后上机


                string sql_Working = "insert into JSSX_Line_Working_Status(iStatusCode, cLineCode, cStartTime,cDate,cWorkShift) values(1, '" + LN + "', GETDATE(),iif(right(CONVERT(varchar(20),getdate(),120),8) <='07:45:00', CONVERT(varchar(10),DATEADD(day,-1,getdate()),120),CONVERT(varchar(10),getdate(),120)),iif(right( CONVERT(varchar(20), getdate(),120),8) between '07:45:00' and '19:45:00' ,'D','N'))";
                DataSet ds_Working = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_Working);
                Btn_Shutdown.Content = "■下机";
                Btn_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
                //下机改成获取最后一台的时间
                string sql_WorkingTime = "select *,CONVERT(varchar(20), cStartTime, 120) as cTime from JSSX_Line_Working_Status where cEndTime is  null and cLineCode = '" + LN + "'";
                DataSet ds_WorkingTime = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_WorkingTime);

                Lab_Shutdown.Content = "当前状态：运行中 " + ds_WorkingTime.Tables[0].Rows[0]["cTime"].ToString();
                Lab_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));

            }
            else
            {//按了之后下机
                string sql_Working = "update JSSX_Line_Working_Status set iStatusCode = 2, cEndTime = GETDATE() where cLineCode = '" + LN + "' and iStatusCode = 1";
                //string sql_Working = "update JSSX_Line_Working_Status set iStatusCode = 2, cEndTime = (select top 1 ScanTime from JSSX_ScanRecord_Seizou a  where a.Logger = '" + Lines + "' order by ID desc) where cLineCode = '" + LN + "' and iStatusCode = 1";
                DataSet ds_Working = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_Working);
                Btn_Shutdown.Content = "▶上机";
                Btn_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                Lab_Shutdown.Content = "当前状态：停止中";
                Lab_Shutdown.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));

                SqlParameter[] param = {
                 new SqlParameter("@InStockNumber", System.Data.SqlDbType.Char),
                 new SqlParameter("@cLine", System.Data.SqlDbType.Char),
             };
                param[0].Value = Tb_PlanNo.Content.ToString();
                param[1].Value = Lines;
                sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.StoredProcedure, "LineShutdown", param);   //下机时执行存储过程，功能1，刷新计划表里的完成条数，功能2，判断OK条数有没有缺漏


            }
            SetbisAllowScan();    //调用判断是否可以扫描的方法
            #endregion

        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {

                if (myComPort.IsOpen)
                {
                    Tb_Gun1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                    bDevice1 = true;
                    //Tb_Gun1.Text = "连接1";
                }
                else
                {
                    Tb_Gun1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
                    myComPort.Open();
                    bDevice1 = false;
                    // Tb_Gun1.Text = "连接1";
                }

            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                if (myComPort2.IsOpen)
                {
                    Tb_Gun2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                    // Tb_Gun2.Text = "连接2";
                    bDevice2 = true;
                }
                else
                {
                    Tb_Gun2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
                    myComPort2.Open();
                    bDevice2 = false;
                    //  Tb_Gun2.Text = "连接2";
                }
            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
            }

        }

        private void OpenComTimer_Tick(object sender, EventArgs e)
        {
            try
            {

                if (port1.IsOpen)
                {
                    Tb_Gun3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                    //  Tb_Gun1.Text = "连接1";
                    bDevice3 = true;
                }
                else
                {
                    if (port1 != null)
                    {
                        Tb_Gun3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
                        port1.Open();
                        bDevice3 = false;
                    }
                    // Tb_Gun1.Text = "连接1";
                }

            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
            }
        }

        private void ButtonLocker_Tick(object sender, EventArgs e)
        {
            Btn_MSSubmit.Visibility = Visibility.Visible;
            Btn_MSSubmit2.Visibility = Visibility.Visible;
            tButtonLocker.Stop();
        }

        private void SpeedTest_Tick(object sender, EventArgs e)
        {
            Tb_Timer.Content = DateTime.Now.ToString("HH:mm:ss");

        }

        private void DTStatus_Tick(object sender, EventArgs e)
        {

            //执行一次ping打印机
            bPrinter = ping(sPrintIP);

            sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "update JSSX_Line set " +
                "bDevice1='" + bDevice1 + "',bDevice2='" + bDevice2 + "',bDevice3='" + bDevice3 + "',bPrinter='" + bPrinter + "'" +
                "where LineNumber='" + Lines + "'");
        }

        private void Schedule_Tick(object sender, EventArgs e)
        {

            //在安排计划的按钮被点击后会hidden一次图片
            TimeSpan TSNow = DateTime.Now.TimeOfDay;
            TimeSpan TSDayS = DateTime.Parse("11:00").TimeOfDay;
            TimeSpan TSDayE = DateTime.Parse("19:00").TimeOfDay;
            TimeSpan TSNightS = DateTime.Parse("00:00").TimeOfDay;
            TimeSpan TSNightE = DateTime.Parse("07:00").TimeOfDay;
            if (((TSNow > TSDayS && TSNow < TSDayE) || (TSNow > TSNightS && TSNow < TSNightE)) && bIsNewVersion)
            {
                string LN = System.Configuration.ConfigurationManager.AppSettings["LinesName"];
                DataSet DS_Schedule = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select distinct aa.cPNumber,bb.cWorkShift from (select distinct 'JXPO' + right(left(a.Number, 14), 10) + b.WorkShift as cPNumber from JSSX_Stock_In a left join JSSX_Stock_In_Detailed b on a.Number = b.InStockNumber where a.Line =  '" + LN + "' and (b.WorkShift = 'D' or b.WorkShift = 'N') and CONVERT(varchar(10), a.PlanTime, 120) + b.WorkShift > iif(right(CONVERT(varchar(20), GETDATE(), 120),8) between '00:00:00' and '08:00:00',CONVERT(varchar(10), dateadd(day,-1,GETDATE()), 120),CONVERT(varchar(10), GETDATE(), 120) ) + iif(right(CONVERT(varchar(20), GETDATE(), 120), 8) between '08:00:00' and '19:59:59', 'D', 'N') and a.Isfinish = 0) aa left join JSSX_ProductionPlan bb on aa.cPNumber = bb.cPNumber and bb.isdelete = 0 order by aa.cPNumber");

                if (DS_Schedule != null && DS_Schedule.Tables[0].Rows.Count != 0 && DS_Schedule.Tables[0].Rows[0][1] != null && DS_Schedule.Tables[0].Rows[0][1].ToString() == "")
                {
                    Im_Notic.Visibility = Visibility.Visible;
                }
                else
                {
                    Im_Notic.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                Im_Notic.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// COM口触发接受数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceiveData(object sender, SerialDataReceivedEventArgs e)
        {
            if (TimerLock)
            {
                return;
            }
            int n = myComPort.BytesToRead;//获取缓存区内的字节长度
            byte[] buf = new byte[n];//建立一个对应长度的数组
            myComPort.Read(buf, 0, n);//读取数据到数组中
            string Res = Encoding.ASCII.GetString(buf);//把数组的数据赋值给STRING 
            Res = Res.Split(Environment.NewLine.ToCharArray())[0];//只取出第一次照合的数据
            if (Res == "")
            {
                MessageBox.Show("获取扫描枪的数据失败。");
                return;
            }
            else
            {
                DataAnalysis(Res);
            }
            Array.Clear(buf, 0, buf.Length);//数组清空
        }

        private void ReceiveData2(object sender, SerialDataReceivedEventArgs e)
        {
            if (TimerLock)
            {
                return;
            }
            int n = myComPort2.BytesToRead;
            byte[] buf = new byte[n];
            myComPort2.Read(buf, 0, n);
            string Res = Encoding.ASCII.GetString(buf);
            if (Res.IndexOf("\r") == -1)
            {
                Resplus += Res;
                return;
            }
            else
            {
                Res = Resplus + Res;
                Resplus = "";
            }
            Res = Res.Split(Environment.NewLine.ToCharArray())[0];
            if (Res == "")
            {
                MessageBox.Show("获取扫描枪的数据失败。");
                return;
            }
            else
            {
                DataAnalysis(Res);
            }
            Array.Clear(buf, 0, buf.Length);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "update JSSX_Line set " +
                 "bDevice1=0,bDevice2=0,bDevice3=0,bPrinter=0 where LineNumber='" + Lines + "'");
            Mylog.Error("退出程序");
            Environment.Exit(0);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //string Ckey = "";
            //if (e.Key == Key.Enter && Ckey.Length == 100)
            //{
            //    DataAnalysis_BigKanbanNoNew(Ckey);
            //}
            //else
            //{
            //    Ckey += e.Key.ToString().Substring(e.Key.ToString().Length - 1, 1);
            //}
        }

        private void Btn_Rebuild_Click(object sender, RoutedEventArgs e)
        {
            if (!Admin)
            {
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                ShowMessage("没有权限重置" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                return;
            }
            ProPass(Rec_Shape, 20, 1, "#FF0000FF");
            iWorkType = 1;
        }

        private void Btn_OutStock_Click(object sender, RoutedEventArgs e)
        {
            if (!Admin)
            {
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                ShowMessage("没有权限退库" + "-----" + DateTime.Now.ToString("HH:mm:ss"));

            }
            else
            {
                //DataView DV = (DataView)Dg_Show.ItemsSource;
                //SpecialHandling SH = new SpecialHandling(DV.ToTable(), BigKanbanNo, Lines);
                //SH.ShowDialog();
                ProPass(Rec_Shape, 20, 1, "#FFFFC0CB");
                ShowMessage("退库权限开启" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                iWorkType = 2;
            }

        }

        private void Btn_Unlock_Click(object sender, RoutedEventArgs e)
        {
            if (Btn_Unlock.Content.ToString() == "打开权限")
            {
                Change_Color(Color.FromArgb((byte)255, (byte)255, (byte)255, (byte)255));
                ShowMessage("请照合班组长的二维码打开权限" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                Admin = true;
                Btn_Unlock.Content = "关闭权限";
            }
            else
            {
                Change_Color(Color.FromArgb((byte)255, (byte)0, (byte)0, (byte)0));
                Admin = false;
                iWorkType = 0;
                Btn_Unlock.Content = "打开权限";
                Btn_Rebuild.Visibility = Visibility.Hidden;
                Btn_OutStock.Visibility = Visibility.Hidden;
                Btn_OutStockAll.Visibility = Visibility.Hidden;
                Btn_ChangeKanban.Visibility = Visibility.Hidden;
            }
        }

        private void Btn_DamageFeedback_Click(object sender, RoutedEventArgs e)
        {
            if (Tb_PlanNo.Content != null)
            {
                Feedback lo = new Feedback(Tb_PlanNo.Content.ToString(), Cbx_ProductSelection.Text, Lines);
                lo.ShowDialog();
            }
            else
            {
                ErrorSilen("先获取日计划", "1");
            }
        }

        private void Btn_OpenCom_Click(object sender, RoutedEventArgs e)
        {
            //枪A定时检测
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer1_Tick;

            //枪B定时检测
            timer2 = new DispatcherTimer();
            timer2.Interval = TimeSpan.FromMilliseconds(500);
            timer2.Tick += timer2_Tick;

            //继电器定时检测
            OpenComTimer = new DispatcherTimer();
            OpenComTimer.Interval = TimeSpan.FromMilliseconds(500);
            OpenComTimer.Tick += OpenComTimer_Tick;

            //网速测试
            SpeedTest = new DispatcherTimer();
            SpeedTest.Interval = TimeSpan.FromMilliseconds(1000);
            SpeedTest.Tick += SpeedTest_Tick;
            SpeedTest.Start();

            //检查计划有没有排
            Schedule = new DispatcherTimer();
            Schedule.Interval = TimeSpan.FromSeconds(10);
            Schedule.Tick += Schedule_Tick;
            Schedule.Start();


            //发送本机的设备状态到DB里
            DTStatus = new DispatcherTimer();
            DTStatus.Interval = TimeSpan.FromMinutes(5);
            DTStatus.Tick += DTStatus_Tick;
            DTStatus.Start();

            this._loading.Visibility = Visibility.Collapsed;

            OpenCom();
            if (OpenedCom < int.Parse(FullCom))
            {
                Mylog.Error("设备连接失败，本线应有【" + FullCom.ToString() + "】个设备，但是只连接了【" + OpenedCom.ToString() + "】个端口。\r\n程序即将推出，请检查设备连接情况。\r\n设备连接正常程序才可运行。");
                MessageBox.Show("设备连接失败，本线应有【" + FullCom.ToString() + "】个设备，但是只连接了【" + OpenedCom.ToString() + "】个端口。\r\n程序即将推出，请检查设备连接情况。\r\n设备连接正常程序才可运行。");
                Restart();
            }
        }

        private void Btn_Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetOldData(Lines);
        }

        private void Btn_MSSubmit_Click(object sender, RoutedEventArgs e)
        {

            string sCarLabel = CarLabel;

            if (DP_Datatime.Text == "")//如果DT有值就改掉sCarLabel的内容
            {
                sCarLabel = CarLabel + UniqueID + Lines + DateTime.Now.AddHours(-8).ToString("yyMMdd");
            }
            else
            {
                sCarLabel = CarLabel + UniqueID + Lines + Convert.ToDateTime(DP_Datatime.Text).ToString("yyMMdd");
            }
            //DP_Datatime.Text = "";
            if (int.Parse(Tb_MSSTART.Text) == 0 || Tb_MSSTART.Text == "" || Tb_MSEND.Text == "")
            {
                ShowMessage("不能是空数据或者是0" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;
            }
            if (BigKanbanNo == "")
            {
                ShowMessage("重新扫描大看板" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;

            }
            DataTable DS_Result = new DataTable();

            DS_Result.Columns.Add("ID", typeof(string));
            DS_Result.Columns.Add("ScanTime", typeof(string));
            DS_Result.Columns.Add("ScanResult", typeof(string));
            DS_Result.Columns.Add("ScanResult2", typeof(string));
            DS_Result.Columns.Add("KanbanNo", typeof(string));
            DS_Result.Columns.Add("InStocknumber", typeof(string));
            DS_Result.Columns.Add("Status", typeof(string));
            Thread Completed = new Thread(Show_completed);
            int iMSstart = int.Parse(Tb_MSSTART.Text);
            int iMSend = int.Parse(Tb_MSEND.Text) + 1;
            if (iMSstart == 0 && iMSend == 1)
            {
                iMSend = 0;
            }

            if (CarLabel.Length <= 5)
            {
                ShowMessage("必须先照一张铭板" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;
            }
            if (iMSend - iMSstart < 0)//起始小于结束
            {
                ShowMessage("起始必须大于结束" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;
            }
            if (iMSstart > 4500 || iMSend > 4500)
            {
                ShowMessage("不能大于3500的数" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;
            }
            Tb_MSSTART.Text = (iMSstart + 1).ToString();
            Tb_MSEND.Text = (iMSend).ToString();//自动+1
            int iCount = iMSend - iMSstart;
            string sSql = "";
            ButtonLocker();
            PLCUnlock();
            if (iCount == Amount)//正常生产
            {
                for (int i = iMSstart; i < iMSend; i++)
                {
                    sSql += " if not exists (select top 1 ScanResult from JSSX_ScanRecord_Seizou  where ScanResult= '" + sCarLabel + i.ToString("0000") + "' and Status='OK')insert into JSSX_ScanRecord_Seizou (ScanTime,ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) values((CONVERT([nvarchar](20),getdate(),(120))),'" + sCarLabel + i.ToString("0000") + "','','" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and   (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120));";
                }
                sSql = sSql + "update top(1) JSSX_Stock_In_Kanban set QuantityCompletion=(select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where KanbanNo='" + UniqueID + Series + "' and Status='OK')  where KanbanNo='" + UniqueID + Series + "' and Isfinish!=1;update JSSX_Stock_In_Detailed set  QuantityCompletion+= (select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where KanbanNo='" + UniqueID + Series + "' and Status='OK') where InStockNumber='" + PlanNo + "' and UniqueID ='" + UniqueID + "' and WorkShift=iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N')";
                bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, sSql);
                if (!bTran)
                {
                    Speaker("铭板照合失败。", 4);
                    ShowMessage("执行SQL语句失败！" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    return;
                }
                iAmount = iMSend - iMSstart;
                Tb_Completed.Text = iAmount.ToString() + "/" + Amount.ToString();

                Completed.Start();
                ProPass(Rec_Shape, 20, 0, "#FF00FF00");
            }
            else if (iCount + Dg_Show.Items.Count == Amount && Dg_Show.Items.Count > 0)//端数补满
            {
                foreach (DataRowView dr in Dg_Show.Items)
                {
                    // sSql += " if not exists (select ScanResult from JSSX_ScanRecord_Seizou  where ScanResult= '" + dr[2].ToString() + "' and Status='OK')insert into JSSX_ScanRecord_Seizou (ScanTime,ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift) values((CONVERT([nvarchar](20),getdate(),(120))),'" + dr[2].ToString() + "','','" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:30') and   (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<'20:00' ,'D','N'));";

                }
                for (int i = iMSstart; i < iMSend; i++)
                {
                    sSql += " if not exists (select top 1 ScanResult from JSSX_ScanRecord_Seizou  where ScanResult= '" + sCarLabel + i.ToString("0000") + "' and Status='OK')insert into JSSX_ScanRecord_Seizou (ScanTime,ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) values((CONVERT([nvarchar](20),getdate(),(120))),'" + sCarLabel + i.ToString("0000") + "','','" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and   (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120));";
                }
                sSql = sSql + "update top(1) JSSX_Stock_In_Kanban set QuantityCompletion=(select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where KanbanNo='" + UniqueID + Series + "' and Status='OK')  where KanbanNo='" + UniqueID + Series + "' and Isfinish!=1;update JSSX_Stock_In_Detailed set  QuantityCompletion= (select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where InStockNumber='" + PlanNo + "' and UniqueID ='" + UniqueID + "' and Status='OK') where InStockNumber='" + PlanNo + "' and UniqueID ='" + UniqueID + "' and WorkShift=iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N')";
                bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, sSql);
                if (!bTran)
                {
                    Speaker("铭板照合失败。", 4);
                    ShowMessage("执行SQL语句失败！" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    return;
                }
                iAmount = Dg_Show.Items.Count + iCount;

                Completed.Start();
                ProPass(Rec_Shape, 20, 0, "#FF00FF00");
            }
            else if (iCount + Dg_Show.Items.Count > Amount)//有异常取出
            {
                for (int i = iMSstart; i < iMSend; i++)
                {
                    DS_Result.Rows.Add(i.ToString("0000"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), sCarLabel + i.ToString("0000"), "", UniqueID + Series, PlanNo, "OK");
                    sSql += " if not exists (select top 1 ScanResult from JSSX_ScanRecord_Seizou  where ScanResult= '" + sCarLabel + i.ToString("0000") + "' and Status='OK')insert into JSSX_ScanRecord_Seizou (ScanTime,ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) values((CONVERT([nvarchar](20),getdate(),(120))),'" + sCarLabel + i.ToString("0000") + "','','" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and   (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120));";

                }
                sSql = sSql + "update top(1) JSSX_Stock_In_Kanban set QuantityCompletion=(select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where KanbanNo='" + UniqueID + Series + "' and Status='OK')  where KanbanNo='" + UniqueID + Series + "' and Isfinish!=1;update JSSX_Stock_In_Detailed set  QuantityCompletion= (select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where InStockNumber='" + PlanNo + "' and UniqueID ='" + UniqueID + "' and Status='OK') where InStockNumber='" + PlanNo + "' and UniqueID ='" + UniqueID + "' and WorkShift=iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N')";

                foreach (DataRowView dr in Dg_Show.Items)
                {
                    DS_Result.Rows.Add(dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString());

                }
                sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, sSql);

                Dg_Show.ItemsSource = DS_Result.DefaultView;
                Tb_Completed.Text = Dg_Show.Items.Count.ToString() + "/" + Amount.ToString();
                MessageBox.Show("跨度大于收容数，删除多余的数据之后重新点击提交");
                return;//超出实际收容数，返回，删除多余数据之后才可以提交
                //SpecialHandling SH = new SpecialHandling(DS_Result, BigKanbanNo, Lines);
                //SH.ShowDialog();
            }
            else if (iCount + Dg_Show.Items.Count < Amount)//端数
            {
                for (int i = iMSstart; i < iMSend; i++)
                {
                    sSql += " if not exists (select top 1 ScanResult from JSSX_ScanRecord_Seizou  where ScanResult= '" + sCarLabel + i.ToString("0000") + "' and Status='OK')insert into JSSX_ScanRecord_Seizou (ScanTime,ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) values((CONVERT([nvarchar](20),getdate(),(120))),'" + sCarLabel + i.ToString("0000") + "','','" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and   (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120));";
                }
                sSql = sSql + "update top(1) JSSX_Stock_In_Kanban set QuantityCompletion=(select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where KanbanNo='" + UniqueID + Series + "' and Status='OK')  where KanbanNo='" + UniqueID + Series + "' and Isfinish!=1;update JSSX_Stock_In_Detailed set  QuantityCompletion= (select count(*) from [JSSX_ScanRecord_Seizou] WITH(NOLOCK) where InStockNumber='" + PlanNo + "' and UniqueID ='" + UniqueID + "' and Status='OK') where InStockNumber='" + PlanNo + "' and UniqueID ='" + UniqueID + "' and WorkShift=iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N')";
                bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, sSql);
                if (!bTran)
                {
                    Speaker("铭板照合失败。", 4);
                    ShowMessage("执行SQL语句失败！" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    return;
                }
                iAmount = Dg_Show.Items.Count + iCount;
                Tb_Completed.Text = iAmount.ToString() + "/" + Amount.ToString();
                Dg_Show.ItemsSource = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select ROW_NUMBER() over(order by InStocknumber) as ID,ScanTime,ScanResult,ScanResult2,KanbanNo,Status from [dbo].[JSSX_ScanRecord_Seizou] WITH(NOLOCK) where KanbanNo='" + UniqueID + Series + "' AND Status='OK'").Tables[0].DefaultView;
                ProPass(Rec_Shape, 20, 0, "#FF00FF00");
            }
        }

        private void Btn_MSSubmit2_Click(object sender, RoutedEventArgs e)
        {
            if (BigKanbanNo == "")
            {
                ShowMessage("重新扫描大看板" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;
            }
            DP_Datatime.Text = "";
            if (Tb_MSSTART.Text == "" || Tb_MSEND.Text == "")
            {
                ShowMessage("不能是空数据" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;
            }
            string Lb = CarLabel + int.Parse(Tb_MSSTART.Text).ToString("0000");
            NGReason NG = new NGReason(Lb, Lines);
            NG.ShowDialog();
            if (NG.bIsok)
            {
                ButtonLocker();
                Tb_MSSTART.Text = (int.Parse(Tb_MSSTART.Text) + 1).ToString();
                Tb_MSEND.Text = (int.Parse(Tb_MSEND.Text)).ToString();//结束的+1在
                ShowMessage(Lb + "退库成功" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "update JSSX_ScanRecord_Seizou set Status='SC' where ScanResult='" + Lb + "';update top(1) JSSX_Stock_In_Kanban set QuantityCompletion-=1,Isfinish=0, CompleteTime=(CONVERT([nvarchar](20),getdate(),(120))),Status='SP' where KanbanNo= '" + UniqueID + Series + "'  ;update JSSX_Stock_ShippingDepartment set Status='SP' WHERE UniqueID='" + UniqueID + "' and Series='" + UniqueID + Series + "' ;insert into ErrorLog (sMessage,sLevel,sLogger,sDepartment,sException) values( '执行了【退库】操作','SC','" + UserName + "','" + Lines + "','" + Lb + "')");
                Dg_Show.ItemsSource = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select ROW_NUMBER() over(order by InStocknumber) as ID,ScanTime,ScanResult,ScanResult2,KanbanNo,InStocknumber,Status from [dbo].[JSSX_ScanRecord_Seizou] where KanbanNo='" + UniqueID + Series + "' AND Status='OK'").Tables[0].DefaultView;
                Tb_Completed.Text = Dg_Show.Items.Count.ToString() + "/" + Amount.ToString();
                Btn_Unlock.Content = "打开权限";
                Admin = false;//关闭权限 
                iWorkType = 0;
                Btn_Rebuild.Visibility = Visibility.Hidden;
                Btn_OutStock.Visibility = Visibility.Hidden;
                Btn_OutStockAll.Visibility = Visibility.Hidden;
            }
        }

        private void Btn_ChangeKanban_Click(object sender, RoutedEventArgs e)
        {
            Change_Color(Color.FromArgb((byte)255, (byte)0, (byte)0, (byte)0));
            Admin = false;
            iWorkType = 0;
            Btn_Unlock.Content = "打开权限";
            Btn_Rebuild.Visibility = Visibility.Hidden;
            Btn_OutStock.Visibility = Visibility.Hidden;
            Btn_OutStockAll.Visibility = Visibility.Hidden;
            Btn_ChangeKanban.Visibility = Visibility.Hidden;
            Tb_BigKanbanNo.Content = "";
            ShowMessage("看板切替完成，请扫描即将作业的看板" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
        }

        private void Tb_MSEND_LostFocus(object sender, RoutedEventArgs e)
        {
            NumericKeyboard lo = new NumericKeyboard();
            lo.btnClose_Click(null, null);
        }

        private void Dg_Show_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BigKanbanNo == "")
            {
                return;
            }
            var a = this.Dg_Show.SelectedItem;
            var b = a as DataRowView;
            DataTable Ds_ShowLabel = new DataTable();
            Ds_ShowLabel.Clear();
            Ds_ShowLabel.Columns.Add("ScanResult", typeof(string));
            Ds_ShowLabel.Columns.Add("Status", typeof(string));
            Ds_ShowLabel.Columns.Add("KanbanNo", typeof(string));
            Ds_ShowLabel.Columns.Add("InStockNumber", typeof(string));
            Ds_ShowLabel.Columns.Add("UniqueID", typeof(string));
            Thread Completed = new Thread(Show_completed);
            int iQuantityCompletion = 0;//此看板的数据 

            if (b == null)
            {
                return;
            }
            NGReason NG = new NGReason(b["ScanResult"].ToString(), Lines);
            NG.ShowDialog();
            if (NG.bIsok)
            {
                sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "update JSSX_ScanRecord_Seizou set Status='SC' where ScanResult='" + b["ScanResult"].ToString() + "';update top(1) JSSX_Stock_In_Kanban set QuantityCompletion-=1,Isfinish=0, CompleteTime=(CONVERT([nvarchar](20),getdate(),(120))),Status='SP' where KanbanNo= '" + b["KanbanNo"].ToString() + "'  ;update JSSX_Stock_ShippingDepartment set Status='SP' WHERE UniqueID='" + UniqueID + "' and Series='" + b["KanbanNo"].ToString() + "' ;insert into ErrorLog (sMessage,sLevel,sLogger,sDepartment,sException) values( '执行了【退库】操作','SC','" + UserName + "','" + Lines + "','" + b["ScanResult"].ToString() + "')");
                b.Delete();
                Tb_Completed.Text = Dg_Show.Items.Count.ToString() + "/" + Amount.ToString();
                string Res = b["ScanResult"].ToString();
                DataSet Os = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select top(1) KanbanNo from JSSX_ScanRecord_Seizou where ScanResult = '" + Res + "' and Status = 'OK' order by id desc");
                if (Os != null && Os.Tables.Count != 0)
                {
                    string sKanbanNo = Os.Tables[0].Rows[0]["KanbanNo"].ToString();
                    bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "update JSSX_ScanRecord_Seizou set Status='SC' where ScanResult='" + Res + "';update top(1) JSSX_Stock_In_Kanban set QuantityCompletion-=1,Isfinish=0, CompleteTime=(CONVERT([nvarchar](20),getdate(),(120))),Status='SP' where KanbanNo= '" + sKanbanNo + "'  ;update JSSX_Stock_ShippingDepartment set Status='SP' WHERE UniqueID='" + UniqueID + "' and Series='" + sKanbanNo + "' ;insert into ErrorLog (sMessage,sLevel,sLogger,sDepartment,sException) values( '执行了【退库】操作','SC','" + UserName + "','" + Lines + "','" + Res + "')");
                    if (!bTran)
                    {
                        Speaker("解除绑定失败！", 3);
                        ShowMessage("解除绑定失败！" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                        return;
                    }
                    //DS_30Days.Tables[0].Rows.Remove(DS_30Days.Tables[0].Rows.Find(Res));//删除表里的数据
                    DataRow[] arrRows = DS_30Days.Tables[0].Select("ScanResult='" + Res + "'");
                    foreach (DataRow row in arrRows)
                    {
                        row["Status"] = "SC";
                    }
                    Back.PlaySync();
                    ShowMessage(Res + "退库成功" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    iQuantityCompletion = DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK'").Length;//此看板的数据
                    Ds_ShowLabel.Clear();
                    foreach (DataRow DR in DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK' "))
                    {
                        Ds_ShowLabel.ImportRow(DR);
                    }
                    Dg_Show.ItemsSource = Ds_ShowLabel.DefaultView;
                    Tb_Completed.Text = iQuantityCompletion + "/" + Amount.ToString();

                    if (iQuantityCompletion == Amount)
                    {
                        Completed.Start();
                    }
                }
                else
                {
                    ErrorSilen("【铭板】已经解除绑定关系，或者根本没有绑定关系，请核对。", "3");
                }
            }
            else
            {
                ShowMessage("取消了退库操作" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
            }
        }

        private void Tb_MSSTART_GotFocus(object sender, RoutedEventArgs e)
        {
            Tb_MSSTART.Text = "";
        }

        private void Tb_MSEND_GotFocus(object sender, RoutedEventArgs e)
        {
            Tb_MSEND.Text = "";
        }

        private void Btn_Ending_Click(object sender, RoutedEventArgs e)
        {
            //DataAnalysis_CarLabel("D6CFKZC0001");
        }

        private void Btn_Schedule_Click(object sender, RoutedEventArgs e)
        {
            Im_Notic.Visibility = Visibility.Hidden;
            ProductionOrder lo = new ProductionOrder(System.Configuration.ConfigurationManager.AppSettings["LinesName"]);
            lo.ShowDialog();

        }

        private void Btn_SetOK_Click(object sender, RoutedEventArgs e)
        {
            Okay.PlaySync();
            bisPress = true;
            Btn_SetOK.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void Btn_OutStockAll_Click(object sender, RoutedEventArgs e)
        {
            if (!Admin)
            {
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                ShowMessage("没有权限" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                return;
            }
            else
            {
                ProPass(Rec_Shape, 20, 1, "#FFf20c00");
                iWorkType = 3;
                return;
            }


        }

        private void Tb_MSSTART_TextChanged(object sender, TextChangedEventArgs e)
        {
            Tb_MSEND.Text = Tb_MSSTART.Text;
        }

        #endregion

        #region 方法 

        /// <summary>
        /// 托盘扫描
        /// </summary>
        /// <param name="Res">托盘二维码的编号</param>
        public void ContainerBinding(string Res)
        {

            if (BigKanbanNo == "")
            {
                ErrorSilen("请先扫描看板", "Z0");
                return;
            }

            //update JSSX_Stock_ShippingDepartment set cTrayNo = '' where Series = ''
            string sSql_Tray = "update JSSX_Stock_ShippingDepartment set cTrayNo = '" + Res + "' where Series = '" + BigKanbanNo + "';";

            //Exec TrayInfoUpdate 'JXFS819B0003', '','','清洁人员','清洁扫描枪','Q';   
            sSql_Tray = sSql_Tray + "Exec TrayInfoUpdate '" + Res + "', '','" + BigKanbanNo + "','生产照合','" + Lines + "','Z' ";
            DataSet ds_Tray = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sSql_Tray);

            //bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.ConnectionStringLocalTransaction, sSql_Tray);
            if (ds_Tray.Tables[0].Rows.Count <= 0)
            {
                //Speaker("托盘照合失败。", 4);
                ShowMessage("执行SQL语句失败！" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                return;
            }
            if (ds_Tray.Tables[0].Rows[0][0].ToString() != "操作成功！")
            {
                //Speaker(ds_Tray.Tables[0].Rows[0][0].ToString(), 4);
                ErrorSilen(ds_Tray.Tables[0].Rows[0][0].ToString(), "Z0");
                return;
            }
            Lab_Container.Content = "托盘号：" + Res;   //把托盘号显示在界面上

        }



        /// <summary>
        /// 测试ping
        /// </summary>
        /// <param name="strDestIp"></param>
        /// <returns></returns>
        private bool ping(string strDestIp)
        {
            Ping myPing = new Ping();
            PingOptions myOptions = new PingOptions();
            myOptions.DontFragment = true;
            string data = "abcdefghijklmnopqrstuvwxy123456";
            byte[] buff = Encoding.ASCII.GetBytes(data);
            try
            {
                PingReply myPingReply = myPing.Send(strDestIp, 500, buff, myOptions);
                if (myPingReply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

        }

        /// <summary>
        /// 循环打开端口
        /// </summary>
        public void OpenCom()
        {
            foreach (string SpName in SerialPort.GetPortNames())
            {
                string str = MulGetHardwareInfo(HardwareEnum.Win32_PnPEntity, "Name", SpName);
                if (str.Contains("USB-SERIAL CH340"))
                {
                    if (port1 != null)
                    {
                        port1.Dispose();
                    }
                    port1 = new SerialPort(SpName, 9600, Parity.None);

                    if (port1 != null)
                    {
                        port1.Open();
                        OpenComTimer.Start();
                        Tb_Gun3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                        OpenedCom++;
                    }

                    // USCOK = true;

                    //无论如何，都必须先执行这一段下面才会执行。
                }

            }
            foreach (string SpName in SerialPort.GetPortNames())
            {

                string str = MulGetHardwareInfo(HardwareEnum.Win32_PnPEntity, "Name", SpName);
                if (COM == 1 && str.Contains("串行设备"))//过滤其他非扫描器设备只需要增加查询SpName是否有包含关键字
                {
                    if (myComPort != null)
                    {
                        myComPort.Dispose();
                    }
                    sCom1 = str;
                    myComPort = new SerialPort(SpName, 9600, Parity.None);
                    myComPort.DataReceived += ReceiveData;
                    myComPort.Open();
                    timer.Start();
                    Tb_Gun1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                    COM = 2;
                    OpenedCom++;
                }
                else if (COM == 2 && str.Contains("串行设备") && str != sCom1)
                {
                    if (myComPort2 != null)
                    {
                        myComPort2.Dispose();
                    }
                    myComPort2 = new SerialPort(SpName, 9600, Parity.None);
                    myComPort2.DataReceived += ReceiveData2;
                    myComPort2.Open();
                    timer2.Start();
                    Tb_Gun2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                    COM = 1;
                    OpenedCom++;
                }
            }
            foreach (string SpName in SerialPort.GetPortNames())
            {
                string str = MulGetHardwareInfo(HardwareEnum.Win32_PnPEntity, "Name", SpName);
                if (str.Contains("CDC USB Demonstration"))
                {
                    if (myComPort2 != null)
                    {
                        myComPort2.Dispose();
                    }
                    myComPort2 = new SerialPort(SpName, 9600, Parity.None);
                    myComPort2.DataReceived += ReceiveData2;
                    myComPort2.Open();
                    timer2.Start();
                    Tb_Gun2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00FF00"));
                    OpenedCom++;
                }
            }
        }

        private void Cbx_ProductSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetbisAllowScan();    //调用判断是否可以扫描的方法

        }

        public void SetbisAllowScan()
        {
            string sUID;
            if (Cbx_ProductSelection.SelectedIndex < 0)
            {
                sUID = "";
            }
            else
            {
                DataRowView aa = (DataRowView)Cbx_ProductSelection.SelectedItem;
                sUID = aa["UID"].ToString();
            }
            //string sUID = Cbx_ProductSelection.Text.ToString();
            //sUID = sUID.Substring(sUID.Length - 10);

            string sLineName = System.Configuration.ConfigurationManager.AppSettings["LinesName"];
            //select * from JSSX_Products a left join JSSX_DailyPlan_Category b on a.UniqueID = b.UniqueID where a.UniqueID = 'JX00000333'  and b.LineCategory = '四线' and a.TrayType = '量产' and  (CONVERT(varchar(10), GETDATE(), 120) > b.cHVPTTimeEnd  and b.cHVPTTimeEnd != '')
            //这条有返回行，则为标准量产品，没返回行，则为HVPT量产品，或者不是量产品
            //DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select * from JSSX_Products a left join JSSX_DailyPlan_Category b on a.UniqueID = b.UniqueID where a.UniqueID = '" + sUID + "'  and b.LineCategory = '" + sLineName + "' and a.TrayType = '量产' and  (not (CONVERT(varchar(10), GETDATE(), 120) between b.cHVPTTimeBegin and b.cHVPTTimeEnd) or cHVPTTimeBegin is null)");
            //DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select * from JSSX_Products a left join JSSX_DailyPlan_Category b on a.UniqueID = b.UniqueID where a.UniqueID = '" + sUID + "'  and b.LineCategory = '" + sLineName + "' and a.TrayType = '量产' and  (CONVERT(varchar(10), GETDATE(), 120) > b.cHVPTTimeEnd  and b.cHVPTTimeEnd != '')");
            //新改动，返回的HVPT列 = 0 则为标准量产品  HVPT=1 则为HVPT量产品，或者不是量产品

            //DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select *,iif(a.TrayType = '量产' and  (CONVERT(varchar(10), GETDATE(), 120) > b.cHVPTTimeEnd  and b.cHVPTTimeEnd != ''),0,1) as HVPT from JSSX_Products a left join JSSX_DailyPlan_Category b on a.UniqueID = b.UniqueID where a.UniqueID = '" + sUID + "'  and b.LineCategory =  '" + sLineName + "' and b.isdelete = 0");
            DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "exec Line_HVPTCheck '" + sUID + "','" + sLineName + "'");

            if (ds.Tables[0].Rows.Count <= 0)
            {
                //异常情况，无法扫描（应该不会出现这情况）
                Speaker("异常的UID，请联系管理员。", 3);
                bisAllowScan = false;
                return;
            }
            icarlabelLength = Int32.Parse(ds.Tables[0].Rows[0]["icarlabelLength"].ToString());

            if (ds.Tables[0].Rows[0]["HVPT"].ToString() == "0" & Btn_Shutdown.Content.ToString() == "■下机")
            {
                bisAllowScan = true;     //量产品，并且上机状态的情况。可以扫描
            }
            else if (ds.Tables[0].Rows[0]["HVPT"].ToString() == "1" & Btn_Shutdown.Content.ToString() == "▶上机")
            {
                bisAllowScan = true;     //HVPT 和非量产品，并且下机状态，可以扫描
            }
            else
            {
                bisAllowScan = false;
            }
 
        }

        public void DataAnalysis(string Res)
        {
            if (Res.Length <= 5)
            {
                return;
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                //Tb_MidMessage.Content += "\r\n" + Res;

                //if (!bisAllowScan)
                //{
                //    Speaker("请确认上下机状态。", 3);
                //    ShowMessage("请确认上下机状态。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                //    ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                //   // return;
                //}

                #region  //托盘扫描调用
                if (Res.Substring(0, 4) == "JXFS") //当扫的是托盘时进入托盘事件
                {
                    ContainerBinding(Res);
                    return;
                }
                #endregion


                if (Res.Length == 32)
                {
                    DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select UnlockKey as UnlockKey from JSSX_KeyManager where Department='ZZ' and UnlockKey='" + Res + "' ");
                    if (Admin && (ds != null && ds.Tables[0].Rows.Count != 0))
                    {
                        Change_Color(Color.FromArgb((byte)255, (byte)0, (byte)0, (byte)0));
                        ShowMessage("权限开启成功" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                        AllowPass = true;
                        UserName = Res;
                        Btn_Rebuild.Visibility = Visibility.Visible;
                        Btn_OutStock.Visibility = Visibility.Visible;
                        Btn_OutStockAll.Visibility = Visibility.Visible;
                        Btn_ChangeKanban.Visibility = Visibility.Visible;
                        Btn_Unlock.Content = "关闭权限";
                        Im_Notic.Visibility = Visibility.Hidden;//关闭提示框
                        return;
                    }
                }
                //根据iWorkType的情况执行不同的作业
                if (iWorkType == 1)
                {
                    WorkType1(Res);
                    Btn_Unlock.Content = "打开权限";
                    Admin = false;//关闭权限 
                    iWorkType = 0;
                    Btn_Rebuild.Visibility = Visibility.Hidden;
                    Btn_OutStock.Visibility = Visibility.Hidden;
                    Btn_OutStockAll.Visibility = Visibility.Hidden;
                    Btn_ChangeKanban.Visibility = Visibility.Hidden;

                }
                else if (iWorkType == 2)
                {
                    WorkType2(Res);
                    Btn_Unlock.Content = "打开权限";
                    Admin = false;//关闭权限 
                    iWorkType = 0;
                    Btn_Rebuild.Visibility = Visibility.Hidden;
                    Btn_OutStock.Visibility = Visibility.Hidden;
                    Btn_OutStockAll.Visibility = Visibility.Hidden;
                    Btn_ChangeKanban.Visibility = Visibility.Hidden;
                }
                else if (iWorkType == 3)
                {
                    if (Res.Length == 250 && Res.Substring(15, 3) == "B01")
                    {
                        WorkType3(Res.Substring(21, 10).Trim() + Res.Substring(77, 8).Trim());
                        Btn_Unlock.Content = "打开权限";
                        Admin = false;//关闭权限 
                        iWorkType = 0;
                        Btn_Rebuild.Visibility = Visibility.Hidden;
                        Btn_OutStock.Visibility = Visibility.Hidden;
                        Btn_OutStockAll.Visibility = Visibility.Hidden;
                        Btn_ChangeKanban.Visibility = Visibility.Hidden;

                        #region  //托盘号解除绑定部分
                        string sKanbanNo_Tray = Res.Substring(21, 10).Trim() + Res.Substring(77, 8).Trim();
                        string sSql_Tray = "declare @cTrayNo nvarchar(20);select top 1 @cTrayNo = cTrayNo from JSSX_Stock_ShippingDepartment where Series = '" + sKanbanNo_Tray + "';Exec TrayInfoUpdate @cTrayNo, '','','生产退库','" + Lines + "','Q';";
                        sSql_Tray = sSql_Tray + "update JSSX_Stock_ShippingDepartment set cTrayNo = '' where Series = '" + sKanbanNo_Tray + "';";
                        DataSet ds_Tray = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sSql_Tray);
                        Lab_Container.Content = "托盘号：";
                        #endregion
                    }
                    else
                    {
                        ErrorSilen("使用看板退库请扫描看板二维码", "3");
                    }
                }
                else
                {
                    if (Res.Substring(0, 4) == "JXDP")
                    {
                        Tb_PlanNo.Content = Res;
                        DS_Show = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select a.* from master..spt_values  b left join (select top 30 ROW_NUMBER() over(order by InStocknumber) as ID,a.JssxInsideCode as 背番,TrayType+'__'+CarType+'__'+WorkShift+'__'+CONVERT(nvarchar(10) ,Amount) as 车型, Amount as 总量,QuantityCompletion as 完成,a.JssxCode as 社番,b.CarLabel as 铭板信息,a.CustomerCode as 客番,WorkShift as 班次,b.UniqueID as UID  from JSSX_Stock_In_Detailed as a left join [dbo].[JSSX_Products] as b on a.UniqueID=b.UniqueID and b.Revoked=0  where InStocknumber='" + Res + "' and isfinish=0 order by 班次 asc)  a  on a.ID = b.number where b.type='p' and number between 1 and 30 order by number");
                        DS_30Days = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select distinct ScanResult,c.Status,KanbanNo,a.UniqueID,c.InStockNumber from [dbo].[JSSX_Stock_In_Detailed] a left join [dbo].[JSSX_Stock_In] b on a.InStockNumber = b.Number left join JSSX_ScanRecord_Seizou c on a.UniqueID = c.UniqueID  and c.ScanResult != 'EM' where a.instocknumber= '" + Res + "' and b.Isfinish is not null and c.ScanTime > dateadd(DAY, -15, getdate())");
                        if (DS_Show.Tables[0].Rows.Count == 0)
                        {
                            Tb_PlanNo.Content = "";
                            ErrorSilen("空的计划，可能是有发布了新数据，请联系日程。", "2");
                            return;
                        }
                        DayNight = DS_Show.Tables[0].Rows[0]["班次"].ToString();
                        Dg_Show.ItemsSource = DS_Show.Tables[0].DefaultView;
                        Dg_Show.Columns[4].Visibility = Visibility.Hidden;
                        iProductSelection = Cbx_ProductSelection.SelectedIndex;
                        Cbx_ProductSelection.SelectedValuePath = "社番";
                        Cbx_ProductSelection.DisplayMemberPath = "车型";
                        Cbx_ProductSelection.ItemsSource = DS_Show.Tables[0].DefaultView;
                        Cbx_ProductSelection.SelectedIndex = iProductSelection;
                        for (int i = 0; i < this.Dg_Show.Items.Count; i++)
                        {
                            DataGridRow row = (DataGridRow)Dg_Show.ItemContainerGenerator.ContainerFromIndex(i);
                            if (DayNight == "N")
                            {
                                if (row == null)
                                {
                                    Dg_Show.ScrollIntoView(Dg_Show.Items[i]);
                                    row = (DataGridRow)Dg_Show.ItemContainerGenerator.ContainerFromIndex(i);//这3句刷新了列表，这样才能获取倒图形界面上的数据
                                }
                                row.Background = new SolidColorBrush(Colors.Aqua);
                            }
                        }

                    }
                    else if (Res.Length == 250 && Res.Substring(15, 3) == "B01" && Tb_PlanNo.Content != null)
                    {
                        PlanNo = Tb_PlanNo.Content.ToString();
                        DataAnalysis_BigKanbanNoNew(Res);

                    }
                    else if ((Tb_PlanNo.Content != null || OutStock) && Tb_BigKanbanNo.Content != null && Res.Length < 100)
                    {
                        if (Admin)
                        {
                            ErrorSilen("权限开启的状态下无法进行扫描作业，请关闭权限。", "2");
                        }
                        else
                        {

                            if (!bisAllowScan)
                            {
                                Speaker("请确认上下机状态。", 3);
                                ShowMessage("请确认上下机状态。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                                sTemp = "";
                                Res = "";
                                return;
                            }

                            PlanNo = Tb_PlanNo.Content.ToString();
                            BigKanbanNo = Tb_BigKanbanNo.Content.ToString();
                            DataAnalysis_CarLabel(Res);//DC.Start();

                        }

                    }
                    else
                    {
                        ErrorSilen("扫描异常，可能是操作顺序的", "3");
                    }
                }

            }));
        }

        private void DataAnalysis_BigKanbanNoNew(string Res)
        {
            if (Tb_BigKanbanNo.Content != null && Tb_BigKanbanNo.Content.ToString() != "" && !Admin)
            {
                ShowMessage("当前生产未完结，没有权限切换看板。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                return;
            }
            Amount = Int32.Parse(Res.Substring(109, 4).Trim());
            CustomerNo = Res.Substring(118, 10).Trim();
            Series = Res.Substring(77, 8).Trim();
            UniqueID = Res.Substring(21, 10).Trim();
            BigKanbanNo = Res.Substring(21, 10).Trim() + Res.Substring(77, 8).Trim();
            if (Admin)
            {
                Tb_BigKanbanNo.Content = "背番:" + Res.Substring(85, 4) + "主键:" + Res.Substring(21, 10).Trim() + "流水号:" + Res.Substring(77, 8).Trim();
                ShowMessage("看板信息获取成功，执行下一步操作" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                return;
            }
            if (Cbx_ProductSelection.SelectedIndex < 0 || Cbx_ProductSelection.SelectedValue.ToString() != Res.Substring(89, 20).Trim())
            {
                ShowMessage("没有选对车型" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                return;
            }

            Thread Completed = new Thread(Show_completed);
            DataSet Ds_ShowLabel = new DataSet();
            string time = System.DateTime.Now.ToString("HH:mm:ssffff");

            JssxCode = Res.Substring(89, 20).Trim();
            if (Res.Substring(85, 4) != "SAMP")
            {
                JssxInsideCode = int.Parse(Res.Substring(85, 4)).ToString();
            }
            else
            {
                JssxInsideCode = Res.Substring(85, 4);
            }
            Tb_BigKanbanNo.Content = "背番:" + Res.Substring(85, 4) + "主键:" + Res.Substring(21, 10).Trim() + "流水号:" + Res.Substring(77, 8).Trim();
            foundRows = DS_Show.Tables[0].Select("社番 ='" + JssxCode + "' and 背番 ='" + JssxInsideCode + "'");
            if (foundRows.Length <= 0)
            {
                Tb_BigKanbanNo.Content = "";
                ErrorSilen("【大看板】错误的物品，请核对。", "3");
                return;
            }
            CustomerCode = foundRows[0]["客番"].ToString();
            DS_EUCode = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select EUCode,CarLabel from JSSX_Products where  UniqueID ='" + UniqueID + "'");
            if (DS_EUCode != null && DS_EUCode.Tables[0].Rows.Count != 0)
            {
                EUCode = DS_EUCode.Tables[0].Rows[0]["EUCode"].ToString();
                CarLabel = DS_EUCode.Tables[0].Rows[0]["CarLabel"].ToString();
            }
            else
            {
                Tb_BigKanbanNo.Content = "";
                ErrorSilen("【大看板】数据获取失败，请联系IT。", "2");
                return;
            }
            if (Amount == 1 && EUCode != "0" && EUCode != "9" && EUCode != "10" && Res.Substring(15, 4) != "B01B" && Res.Substring(15, 4) != "B01Y")
            {
                Tb_BigKanbanNo.Content = "";
                ErrorSilen("【大看板】收容数不对。这张看板的收容数是：" + Amount.ToString(), "3");
                return;
            } 
            DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select Series from JSSX_Stock_ShippingDepartment where Series='" + UniqueID + Series + "'");
            Ds_ShowLabel = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select ROW_NUMBER() over(order by InStocknumber) as ID,ScanTime,ScanResult,ScanResult2,KanbanNo,InStocknumber,Status from [dbo].[JSSX_ScanRecord_Seizou] where KanbanNo='" + UniqueID + Series + "' AND Status='OK'  ");
            Dg_Show.ItemsSource = Ds_ShowLabel.Tables[0].DefaultView;
            Tb_Completed.Text = Ds_ShowLabel.Tables[0].Rows.Count.ToString() + "/" + Amount.ToString();
            iAmount = Ds_ShowLabel.Tables[0].Rows.Count; 

            //照看板出全部小看板
            //for (int i = 0; i < 12; i++)
            //{
            //    IsRebuild = true;
            //    SN = Ds_ShowLabel.Tables[0].Rows[i]["ScanResult2"].ToString().Substring(10,8);
            //    DataAnalysis_CarLabel(Ds_ShowLabel.Tables[0].Rows[i]["ScanResult"].ToString());

            //}
            //return;
            if (JssxInsideCode == "928" || JssxInsideCode == "6520" || JssxInsideCode == "6005" || JssxInsideCode == "6197" || JssxInsideCode == "6196" || JssxInsideCode == "9013" || JssxInsideCode == "9184" || JssxInsideCode == "927" || JssxInsideCode == "9185" || JssxInsideCode == "9018")//ms线刻印，照看板直接完结
            {
                //CarLabel = CarLabel + DateTime.Now.AddHours(-8).ToString("yyMMdd");//扣掉8个小时，夜班的才会算到当天，而不会变成隔天的产品
            }
            if (ds == null || ds.Tables[0].Rows.Count == 0)
            {
                // Completed.Start();//获取已照合数量
                bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, " insert into JSSX_Stock_ShippingDepartment (UniqueID,Series,CustomerNo,JSSXInsideCode,JSSXCode,volume,ProducedTime,Status,EUCode,InStockNumber) select '" + UniqueID + "','" + UniqueID + Series + "','" + CustomerNo + "','" + JssxInsideCode + "','" + JssxCode + "','" + Amount + "',(CONVERT([nvarchar](20),getdate(),(120))),'SP','" + EUCode + "','" + Tb_PlanNo.Content.ToString() + "' WHERE NOT EXISTS(SELECT ID FROM JSSX_Stock_ShippingDepartment WHERE UniqueID='" + UniqueID + "' and Series='" + UniqueID + Series + "')");//iif('" + CustomerNo.Substring(0, 2) + "'='01' or '" + CustomerNo.Substring(0, 2) + "'='02' or '" + CustomerNo.Substring(0, 2) + "'='03','FP','SP')
                if (!bTran)
                {
                    Speaker("读取看板失败，重新照合。", 4);
                    ShowMessage("读取看板失败，重新照合。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    return;
                }

                Completed.Start();
                GetNew.PlaySync();
            }
            else
            {
                int iLabelCounts= DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK'").Length; 
                if (iLabelCounts<Amount)
                {
                    Completed.Start();//获取已照合数量
                    ErrorSilen("【大看板】发现未完结数据！", "Z0");
                }
                else if (Admin)
                {
                    Completed.Start();//获取已照合数量                   
                }

            }

        }

        private void DataAnalysis_CarLabel(string Res)
        {
            Thread Completed = new Thread(Show_completed);
            int iLabelCounts = DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK'").Length;
            if (iLabelCounts>= Amount)
            { 
                Completed.Start();
                Speaker("这张看板已经满了，需要换一张之后重新扫描。", 5);
                ShowMessage("这台产品需要重新扫描。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                return;
            }
            if (Res.Length!=icarlabelLength&& icarlabelLength!=0)
            {
                ShowMessage("长度不对.实物:"+ Res.Length.ToString() +",系统:"+ icarlabelLength.ToString() + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                sTemp = "";
                return;
            }
            if (!bisPress)
            {
                Speaker("请点击OK确认。", 5);
                ShowMessage("请点击OK确认。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                return;
            }
            if (sTemp == Res)
            {
                return;
            }
            if (bIsNeedOK)
            {
                bisPress = false;
            }

            Btn_SetOK.Foreground = new SolidColorBrush(Colors.Red);
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            string time = System.DateTime.Now.ToString("yyyyMMddHHmm");//日产伪流水号
                                                                       //Tb_MidMessage.Text += "\r\n" + "";
            SmallKanbanNo = "";
            
            Thread PLCLock = new Thread(PLCUnlock);


            DataSet Sc = new DataSet();
            bool Customer_Codecontain = Res.Contains(CarLabel);
            DataTable Ds_ShowLabel = new DataTable();
            Ds_ShowLabel.Clear();
            Ds_ShowLabel.Columns.Add("ScanResult", typeof(string));
            Ds_ShowLabel.Columns.Add("Status", typeof(string));
            Ds_ShowLabel.Columns.Add("KanbanNo", typeof(string));
            Ds_ShowLabel.Columns.Add("InStockNumber", typeof(string));
            Ds_ShowLabel.Columns.Add("UniqueID", typeof(string));
            int iQuantityCompletion = 0;//此看板的数据
            int iQuantityCompletion2 = 0;//整个计划的数据

            if (Customer_Codecontain && CarLabel.Length >= 3 && Res.Length > 4)
            {
                try
                {
                    //要屏蔽掉才能一次性出所有小看板
                    //if (sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select top 1 KanbanNo from  JSSX_Stock_In_Kanban  where KanbanNo = '" + UniqueID + Series + "' and QuantityCompletion<Volume").Tables[0].Rows.Count == 0 && UniqueID + Series != "")
                    //{
                    //    ResetKanban();
                    //    Speaker("此看板已经达到上限，请手动切换下一张看板。", 5);
                    //    ShowMessage("此看板已经达到上限，请手动切换下一张看板。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    //    ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                    //    return;
                    //}
                    if (!OutStock && Tb_BigKanbanNo.Content.ToString() == "" && !IsRebuild)
                    {
                        Speaker("没有照合大看板。", 3);
                        ShowMessage("没有照合大看板。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                        ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                        return;
                    }
                    #region  //托盘照合的部分
                    DataSet ds_Tray = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select * from JSSX_Line where LineNumber =  '" + Lines + "' and bisScanTray = 1");
                    if (ds_Tray.Tables[0].Rows.Count > 0)    //判断这条线是否已经开启的托盘照合
                    {
                        //加入一段判断UID是不是铁托盘，只有铁托盘需要照托盘码

                        DataSet ds_TrayType = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select * from JSSX_Products where UniqueID = '" + UniqueID + "' and TrayType = '量产'");

                        if (!OutStock && Lab_Container.Content.ToString() == "托盘号：" && ds_TrayType.Tables[0].Rows.Count > 0 && !IsRebuild)   //判断当前有没有照合托盘
                        {
                            Speaker("没有照合托盘。", 3);
                            ShowMessage("没有照合托盘。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                            ProPass(Rec_Shape, 20, 0, "#FFFF0000");
                            return;
                        }
                    }
                    #endregion

                    //ds = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select top 1 status from JSSX_ScanRecord_Seizou  where (ScanResult='" + Res + "' ) and (status='OK'or status='RM')");
                    if (DS_30Days.Tables[0].Select("ScanResult='" + Res + "' and Status <>'SC' and Status <>'TS'").Length == 0 || IsRebuild)
                    {
                        DataRow DRDaysNew = DS_30Days.Tables[0].NewRow();
                        if (isfinish == true)
                        {
                            ErrorSilen(BigKanbanNo + "看板扫描已经完成！请返回！", "1");
                            return;
                        }
                        if (CustomerNo.Substring(0, 2) == "14")//本田统一仓库
                        {
                            CustomerNo = "13" + CustomerNo.Substring(2, 2);
                        }
                        else if ((CustomerNo.Substring(0, 2) == "01" || CustomerNo.Substring(0, 2) == "02" || CustomerNo.Substring(0, 2) == "03") && Lines != "08" && Lines != "05" && Res.Length <= 15)//日产系列铭板无流水号的JssxInsideCode != "9097" && JssxInsideCode != "9098" && JssxInsideCode != "9099" && JssxInsideCode != "9251" && JssxInsideCode != "9252" && JssxInsideCode != "9253"
                        {
                            CarLabel = Res;
                            ShowMessage("获取铭板成功" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                            return;
                        }
                        ////照出所有小看板
                        //MakeKanban(UniqueID, PlanNo, DayNight, Lines, Res, SN, Lines);
                        //return;
                        Thread thread = new Thread(() => SmallKanbanNo = MakeKanban(UniqueID, PlanNo, DayNight, Lines, Res, SN, Lines));
                        if (EUCode != "0" && EUCode != "9" && EUCode != "10" && Amount != 1 && thread.ThreadState.ToString() != "Running")//EU箱
                        {
                            //新年来之后做一个1分钟刷新一次明细表的功能
                            //INKANBAN表可以把已经完结的都清掉
                            bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "insert into JSSX_Stock_In_SerialNoRecord (InStockNumber,UniqueID,SerialNo,Complement,Creator,DayNight) values('EU','" + UniqueID + "',(select right((select 100000000+(select max(SerialNo)+1 as 流水号 from JSSX_Stock_In_SerialNoRecord)),8)),'0','" + Lines + "','F'); " +
                                "insert into JSSX_ScanRecord_Seizou (ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate)" +
                                " values( '" + Res + "','" + UniqueID + "'+(select max(SerialNo) from JSSX_Stock_In_SerialNoRecord),'" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120))");
                            if (!bTran)
                            {
                                Speaker("铭板照合失败。", 3);
                                return;
                            }
                            DRDaysNew["ScanResult"] = Res;
                            DRDaysNew["Status"] = "OK";
                            DRDaysNew["KanbanNo"] = UniqueID + Series;
                            DRDaysNew["InStockNumber"] = PlanNo;
                            DRDaysNew["UniqueID"] = UniqueID;
                            DS_30Days.Tables[0].Rows.Add(DRDaysNew);
                            iQuantityCompletion = DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK'").Length;//此看板的数据
                            iQuantityCompletion2 = DS_30Days.Tables[0].Select("UniqueID='" + UniqueID + "' and Status ='OK' and InStockNumber like  '" + PlanNo.Substring(0, 14) + "%'").Length;//当天的总量
                            //写入IN_KANBAN是为了展示未完结的看板的数量，写入明细表是为了统计数量
                            //bTran = sqlHelp.ExecuteSqlTran(sqlHelp.ConnectionStringLocalTransaction, " ; update top(1) JSSX_Stock_In_Kanban set QuantityCompletion = '" + iQuantityCompletion + "'  where KanbanNo = '" + UniqueID + Series + "' and Isfinish!= 1; update JSSX_Stock_In_Detailed  set QuantityCompletion = '" + iQuantityCompletion2 + "' where InStockNumber = '" + PlanNo + "' and  UniqueID = '" + UniqueID + "' and  WorkShift = iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:30') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<'20:00' ,'D','N')");
                            //if (!bTran)
                            //{
                            //    Speaker("铭板照合失败。写入inkanban和明细表时出错", 3);
                            //    return;
                            //}

                            DataSet Ds_SerialNo = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select top 1 RIGHT( ScanResult2,8) as ScanResult2 from JSSX_ScanRecord_Seizou  where ScanResult='" + Res + "' and  status='OK'  ");
                            if (Ds_SerialNo == null || Ds_SerialNo.Tables[0].Rows.Count == 0)
                            {
                                Speaker("铭板照合失败。", 3);
                                return;
                            }
                            SN = Ds_SerialNo.Tables[0].Rows[0]["ScanResult2"].ToString();
                            PLCLock.Start();
                            thread.Start();
                        }
                        else
                        {
                            PLCLock.Start();//if not exists (select ScanResult from JSSX_ScanRecord_Seizou  where ScanResult= '" + Res + "' and Status='OK') 
                            DRDaysNew["ScanResult"] = Res;
                            DRDaysNew["Status"] = "OK";
                            DRDaysNew["KanbanNo"] = UniqueID + Series;
                            DRDaysNew["InStockNumber"] = PlanNo;
                            DRDaysNew["UniqueID"] = UniqueID;
                            DS_30Days.Tables[0].Rows.Add(DRDaysNew);
                            iQuantityCompletion = DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK'").Length;
                            iQuantityCompletion2 = DS_30Days.Tables[0].Select("UniqueID='" + UniqueID + "' and Status ='OK' and InStockNumber like '%" + PlanNo.Substring(0, 14) + "%'").Length;
                            //bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.ConnectionStringLocalTransaction, "insert into JSSX_ScanRecord_Seizou (ScanTime,ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) values((CONVERT([nvarchar](20),getdate(),(120))),'" + Res + "','','" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:30') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<'20:00' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-465, GETDATE()),120));");
                            bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "insert into JSSX_ScanRecord_Seizou (ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) values('" + Res + "','','" + PlanNo + "','" + UniqueID + Series + "','OK','" + UniqueID + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120))");
                            if (!bTran)
                            {
                                Speaker("铭板照合失败。", 3);
                                return;
                            }
                        }
                        Ds_ShowLabel.Clear();
                        foreach (DataRow DR in DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK' "))
                        {
                            Ds_ShowLabel.ImportRow(DR);
                        }
                        Dg_Show.ItemsSource = Ds_ShowLabel.DefaultView;
                        Tb_Completed.Text = iQuantityCompletion + "/" + Amount.ToString();
                        sTemp = Res;
                        if (iQuantityCompletion >= Amount)
                        {
                            iAmount = iQuantityCompletion;
                            Completed.Start();
                        }
                        ProPass(Rec_Shape, 20, 0, "#FF00FF00");
                        //Tb_MidMessage.Text += "\r\n" + "";
                    }
                    else
                    {
                        //ShowMessage("此铭板已照合。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Mylog.Error(ex.Message);
                }
            }
            else
            {
                ErrorSilen("【铭板】错误的物品，请核对。", "3");
                return;
            }
            Btn_Unlock.Content = "打开权限";
            Admin = false;//关闭权限 
            iWorkType = 0;
            Btn_Rebuild.Visibility = Visibility.Hidden;
            Btn_OutStock.Visibility = Visibility.Hidden;
            Btn_OutStockAll.Visibility = Visibility.Hidden;
            Btn_ChangeKanban.Visibility = Visibility.Hidden;
            // }));
        }

        /// <summary>
        /// EU重置
        /// </summary>
        /// <param name="sLN">铭板号</param>
        private void WorkType1(string sLN)
        {
            Admin = false;//关闭权限
            IsRebuild = false;
            Btn_Unlock.Content = "打开权限";
            DataSet DS_SKN = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select top 1 * from JSSX_ScanRecord_Seizou where (ScanResult='" + sLN + "' and status='OK' )");
            UniqueID = DS_SKN.Tables[0].Rows[0]["UniqueID"].ToString();
            PlanNo = DS_SKN.Tables[0].Rows[0]["InStockNumber"].ToString();
            DayNight = "R";
            Lines = DS_SKN.Tables[0].Rows[0]["InStockNumber"].ToString().Substring(12, 2);
            SN = (Int32.Parse(DS_SKN.Tables[0].Rows[0]["ScanResult2"].ToString().Substring(10, 8))).ToString("00000000");
            MakeKanban(UniqueID, PlanNo, DayNight, Lines, sLN, SN, Lines);
            sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "insert into ErrorLog (sMessage,sLevel,sLogger,sException) values( '执行了【重制】操作','RM','" + UserName + "','" + sLN + "')");//insert into JSSX_ScanRecord_Seizou (ScanTime,ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID) values('" + time + "','" + Res + "','" + SmallKanbanNo + "','" + PlanNo + "','" + UniqueID + Series + "','RM','" + UniqueID + "')//update JSSX_ScanRecord_Seizou Set Status='RM' where ScanResult='" + Res + "';
            Speaker("看板重制成功", 4);
            ShowMessage("看板重制成功。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
            SN = "";

        }

        /// <summary>
        /// 单台退库
        /// </summary>
        /// <param name="sLN">铭板号</param>
        private void WorkType2(string sLN)
        {
            DataTable Ds_ShowLabel = new DataTable();
            Ds_ShowLabel.Clear();
            Ds_ShowLabel.Columns.Add("ScanResult", typeof(string));
            Ds_ShowLabel.Columns.Add("Status", typeof(string));
            Ds_ShowLabel.Columns.Add("KanbanNo", typeof(string));
            Ds_ShowLabel.Columns.Add("InStockNumber", typeof(string));
            Ds_ShowLabel.Columns.Add("UniqueID", typeof(string));
            int iQC;
            if (UniqueID + Series == "")
            {
                ErrorSilen("必须先扫描看板。", "3");
                return;
            }
            OutStock = false;
            NGReason NG = new NGReason(sLN, Lines);
            NG.ShowDialog();
            if (NG.bIsok)
            {
                DataSet Os = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select top(1) KanbanNo from JSSX_ScanRecord_Seizou where ScanResult = '" + sLN + "' and KanbanNo='" + UniqueID + Series + "' and Status = 'OK' order by id desc");
                if (Os != null && Os.Tables[0].Rows.Count != 0)
                {
                    string sKanbanNo = Os.Tables[0].Rows[0]["KanbanNo"].ToString();
                    //
                    bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "update JSSX_ScanRecord_Seizou set Status='SC' where ScanResult='" + sLN + "';" +
                        "insert into JSSX_ScanRecord_Seizou (ScanResult,ScanResult2,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) select ScanResult,ScanResult2,InStockNumber,KanbanNo,'TS',UniqueID,Logger,WorkShift,convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120) from JSSX_ScanRecord_Seizou where ScanResult='" + sLN + "'  WAITFOR DELAY N'00:00:00.300';" +
                        "update JSSX_Stock_ShippingDepartment set Status='SP' WHERE UniqueID='" + UniqueID + "' and Series='" + sKanbanNo + "' ;" +
                        "insert into ErrorLog (sMessage,sLevel,sLogger,sDepartment,sException) values( '执行了【退库】操作','SC','" + UserName + "','" + Lines + "','" + sLN + "')");
                    if (!bTran)
                    {
                        Speaker("解除绑定失败！", 3);
                        ShowMessage("解除绑定失败！" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                        return;
                    }
                    //DS_30Days.Tables[0].Rows.Remove(DS_30Days.Tables[0].Rows.Find(Res));//删除表里的数据
                    DataRow[] arrRows = DS_30Days.Tables[0].Select("ScanResult='" + sLN + "'");
                    foreach (DataRow row in arrRows)
                    {
                        row["Status"] = "SC";
                    }

                    Back.PlaySync();
                    ShowMessage(sLN + "退库成功" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    iQC = DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK'").Length;//此看板的数据
                    Ds_ShowLabel.Clear();
                    foreach (DataRow DR in DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK' "))
                    {
                        Ds_ShowLabel.ImportRow(DR);
                    }
                    Dg_Show.ItemsSource = Ds_ShowLabel.DefaultView;
                    Tb_Completed.Text = iQC + "/" + Amount.ToString();
                }
                else
                {
                    ErrorSilen("此铭板不属于这张看板。", "3");
                }
            }
            else
            {
                ShowMessage("取消了退库操作" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
            }
        }

        /// <summary>
        /// 全部退库
        /// </summary>
        /// <param name="sBKN">UID+流水号</param>
        private void WorkType3(string sBKN)
        {
            if (sBKN == "")
            {
                ErrorSilen("请先扫描看板。", "3");
                return;
            }
            string sStatus = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select Status from  JSSX_Stock_ShippingDepartment where  Series='" + sBKN + "'").Tables[0].Rows[0]["Status"].ToString();
            if (sStatus == "IS" || sStatus == "OS")
            {
                ErrorSilen("此看板未经发送处理无法进行退库操作。", "3");
            }
            else
            {
                sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "KanbanSC '" + sBKN + "'");
                ShowMessage("此看板内的产品已经全部退库。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                Speaker("成功啦成功啦你是真的成功啦，整张都给你退掉了，你好棒棒。", 1);
            }
        }

        public void Show_completed()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                if (iAmount != Amount)
                {
                    ShowMessage(Dg_Show.Items.Count.ToString() + "铭板数,收容数:" + Amount.ToString() + "-----" + DateTime.Now.ToString("HH:mm:ss"));
                    return;
                }

                //对比服务器上的数据与本地的ds，差异的情况自动进行补正
                //EU箱小看板未解决
                DataSet Ds_SerialNo = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select * from JSSX_ScanRecord_Seizou  where  UniqueID='" + UniqueID + "' and KanbanNo='" + UniqueID + Series + "' and Status ='OK' ");
                foreach (DataRow DR in DS_30Days.Tables[0].Select("KanbanNo='" + UniqueID + Series + "' and Status ='OK' "))
                {
                    DataRow[] DR2 = Ds_SerialNo.Tables[0].Select("ScanResult='" + DR.ItemArray[0] + "' and Status ='OK' ");
                    if (DR2.Length == 0)
                    {
                        sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "insert into JSSX_ScanRecord_Seizou (ScanResult,InStockNumber,KanbanNo,Status,UniqueID,Logger,WorkShift,cDataDate) " +
                            " VALUES ('" + DR.ItemArray[0] + "','" + DR.ItemArray[4] + "','" + DR.ItemArray[2] + "','OK','" + DR.ItemArray[3] + "','" + Lines + "',iif((right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5)>='07:45') and (right(left(CONVERT([nvarchar](20),getdate(),(120)),16),5))<='19:45' ,'D','N'),convert(varchar(10),dateadd(MINUTE,-495, GETDATE()),120))");
                    }
                }
                try
                {

                    Tb_BigKanbanNo.Content = "";
                    Tb_Completed.Text = "";
                    if (EUCode != "0" && EUCode != "9" && EUCode != "10" && Amount != 1)//EU箱且不是补用品
                    {
                        sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, " update JSSX_Stock_ShippingDepartment set Status='EU' WHERE UniqueID='" + UniqueID + "' and Series='" + UniqueID + Series + "' and SPKanban is null ");
                    }
                    else
                    {
                        sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "update JSSX_Stock_ShippingDepartment set Status='FP' WHERE UniqueID='" + UniqueID + "' and Series='" + UniqueID + Series + "' ");
                    }
                    //ProPass(Rec_Shape, 20, 0, "#FF00FF00");
                    isfinish = false;
                    Tb_BigKanbanNo.Foreground = Brushes.White;
                    GetOldData(Lines);
                    ResetKanban();
                    CompletedSound.PlaySync();
                    CompleteWindow lo = new CompleteWindow();
                    lo.ShowDialog();

                }
                catch (Exception ex)
                {
                    Mylog.Error(ex.Message);

                }

            }));
        }

        private void ResetKanban()
        {
            BigKanbanNo = "";
            LabelNo = "";
            Tb_BigKanbanNo.Content = BigKanbanNo;
            UniqueID = "";
            Series = "";
            #region  托盘扫描相关初始化
            string sSql_Tray = "Exec TrayInfoUpdate '" + Lab_Container.Content.ToString().Substring(4) + "', '','" + BigKanbanNo + "','生产照合','" + Lines + "','M' ";
            bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, sSql_Tray);
            Lab_Container.Content = "托盘号：";
            #endregion

            GC.Collect();
        }

        private void ButtonLocker()
        {
            tButtonLocker = new DispatcherTimer();
            tButtonLocker.Interval = TimeSpan.FromMilliseconds(10000);
            tButtonLocker.Tick += ButtonLocker_Tick;
            Btn_MSSubmit.Visibility = Visibility.Hidden;
            Btn_MSSubmit2.Visibility = Visibility.Hidden;
            tButtonLocker.Start();
        }

        private void OpenLight()
        {
            try
            {
                if (!port1.IsOpen)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
                return;
            }

            byte[] sendBuffer = null;//发送数据缓冲区  
            string sendData = "A0 01 01 A2";//复制发送数据，以免发送过程中数据被手动改变  
            if (1 == 1)//16进制发送  
            {
                try //尝试将发送的数据转为16进制Hex  
                {
                    sendData = sendData.Replace(" ", "");//去除16进制数据中所有空格  
                    sendData = sendData.Replace("\r", "");//去除16进制数据中所有换行  
                    sendData = sendData.Replace("\n", "");//去除16进制数据中所有换行  
                    if (sendData.Length == 1)//数据长度为1的时候，在数据前补0  
                    {
                        sendData = "0" + sendData;
                    }
                    else if (sendData.Length % 2 != 0)//数据长度为奇数位时，去除最后一位数据  
                    {
                        sendData = sendData.Remove(sendData.Length - 1, 1);
                    }

                    List<string> sendData16 = new List<string>();//将发送的数据，2个合为1个，然后放在该缓存里 如：123456→12,34,56  
                    for (int i = 0; i < sendData.Length; i += 2)
                    {
                        sendData16.Add(sendData.Substring(i, 2));
                    }
                    sendBuffer = new byte[sendData16.Count];//sendBuffer的长度设置为：发送的数据2合1后的字节数  
                    for (int i = 0; i < sendData16.Count; i++)
                    {
                        sendBuffer[i] = (byte)(Convert.ToInt32(sendData16[i], 16));//发送数据改为16进制  
                    }
                }
                catch (Exception ex)  //无法转为16进制时，出现异常  
                {
                    Mylog.Error(ex.Message);
                    MessageBox.Show("请输入正确的16进制数据");
                    return;//输入的16进制数据错误，无法发送，提示后返回  
                }

            }

            try//尝试发送数据  
            {//如果发送字节数大于1000，则每1000字节发送一次  
                int sendTimes = (sendBuffer.Length / 1000);//发送次数  
                for (int i = 0; i < sendTimes; i++)//每次发生1000Bytes  
                {
                    port1.Write(sendBuffer, i * 1000, 1000);//发送sendBuffer中从第i * 1000字节开始的1000Bytes  
                                                            // sendCount.Text = (Convert.ToInt32(sendCount.Text) + 1000).ToString();//刷新发送字节数  
                }
                if (sendBuffer.Length % 1000 != 0)
                {
                    port1.Write(sendBuffer, sendTimes * 1000, sendBuffer.Length % 1000);//发送字节小于1000Bytes或上面发送剩余的数据  
                                                                                        //  sendCount.Text = (Convert.ToInt32(sendCount.Text) + sendBuffer.Length % 1000).ToString();//刷新发送字节数  
                }
            }
            catch (Exception ex)//如果无法发送，产生异常  
            {
                Mylog.Error(ex.Message);
                if (port1.IsOpen == false)//如果ComPort.IsOpen == false，说明串口已丢失  
                {
                    //SetComLose();//串口丢失后相关设置  
                }
                else
                {
                    MessageBox.Show("无法发送数据，原因未知！");
                }
            }
            //sendScrol.ScrollToBottom();//发送数据区滚动到底部   
        }

        private void CloseLight()
        {
            try
            {
                if (!port1.IsOpen)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
                return;
            }
            byte[] sendBuffer = null;//发送数据缓冲区  
            string sendData = "A0 01 00 A1";//复制发送数据，以免发送过程中数据被手动改变  
            if (1 == 1)//16进制发送  
            {
                try //尝试将发送的数据转为16进制Hex  
                {
                    sendData = sendData.Replace(" ", "");//去除16进制数据中所有空格  
                    sendData = sendData.Replace("\r", "");//去除16进制数据中所有换行  
                    sendData = sendData.Replace("\n", "");//去除16进制数据中所有换行  
                    if (sendData.Length == 1)//数据长度为1的时候，在数据前补0  
                    {
                        sendData = "0" + sendData;
                    }
                    else if (sendData.Length % 2 != 0)//数据长度为奇数位时，去除最后一位数据  
                    {
                        sendData = sendData.Remove(sendData.Length - 1, 1);
                    }

                    List<string> sendData16 = new List<string>();//将发送的数据，2个合为1个，然后放在该缓存里 如：123456→12,34,56  
                    for (int i = 0; i < sendData.Length; i += 2)
                    {
                        sendData16.Add(sendData.Substring(i, 2));
                    }
                    sendBuffer = new byte[sendData16.Count];//sendBuffer的长度设置为：发送的数据2合1后的字节数  
                    for (int i = 0; i < sendData16.Count; i++)
                    {
                        sendBuffer[i] = (byte)(Convert.ToInt32(sendData16[i], 16));//发送数据改为16进制  
                    }
                }
                catch (Exception ex) //无法转为16进制时，出现异常  
                {
                    Mylog.Error(ex.Message);
                    MessageBox.Show("请输入正确的16进制数据");
                    return;//输入的16进制数据错误，无法发送，提示后返回  
                }
            }
            try//尝试发送数据  
            {//如果发送字节数大于1000，则每1000字节发送一次  
                int sendTimes = (sendBuffer.Length / 1000);//发送次数  
                for (int i = 0; i < sendTimes; i++)//每次发生1000Bytes  
                {
                    port1.Write(sendBuffer, i * 1000, 1000);//发送sendBuffer中从第i * 1000字节开始的1000Bytes  
                                                            // sendCount.Text = (Convert.ToInt32(sendCount.Text) + 1000).ToString();//刷新发送字节数  
                }
                if (sendBuffer.Length % 1000 != 0)
                {
                    port1.Write(sendBuffer, sendTimes * 1000, sendBuffer.Length % 1000);//发送字节小于1000Bytes或上面发送剩余的数据  
                                                                                        //  sendCount.Text = (Convert.ToInt32(sendCount.Text) + sendBuffer.Length % 1000).ToString();//刷新发送字节数  
                }
            }
            catch (Exception ex)//如果无法发送，产生异常  
            {
                Mylog.Error(ex.Message);
                if (port1.IsOpen == false)//如果ComPort.IsOpen == false，说明串口已丢失  
                {
                    //SetComLose();//串口丢失后相关设置  
                }
                else
                {
                    MessageBox.Show("无法发送数据，原因未知！");
                }
            }
        }

        public void Change_Color(Color c)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.Resources.Remove("color");
                this.Resources.Add("color", new SolidColorBrush(c));
            }));
        }

        public void ErrorSilen(string Mes, string Lev)
        {
            Change_Color(Color.FromArgb((byte)255, (byte)255, (byte)0, (byte)0));
            ErrorWindow lo = new ErrorWindow(Mes);
            player.PlaySync();
            lo.ShowDialog();
            //MessageBox.Show(Mes);
            sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, "insert into ErrorLog (sMessage,sLevel,sLogger) values( '" + Mes + "','" + Lev + "','" + GunNo + "')");
            if (Lev == "Z0")
            {
                Change_Color(Color.FromArgb((byte)255, (byte)0, (byte)0, (byte)0));
                return;
            }
        }

        private void GetOldData(string LineNo)
        {
            try
            {
                DataSet DS_Plan = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select distinct top 1  instocknumber  from [dbo].[JSSX_Stock_In_Detailed] a left join [dbo].[JSSX_Stock_In] b on a.InStockNumber=b.Number " +
                    " where left(right(instocknumber,13),8)=iif(right(CONVERT([nvarchar](16),getdate(),(120)),5)>'00:00' and right(CONVERT([nvarchar](16),getdate(),(120)),5)<'07:45' ,CONVERT([nvarchar](8),getdate()-1,(112)),CONVERT([nvarchar](8),getdate(),(112))) and left(right(instocknumber,5),2)= '" + LineNo + "' and b.Isfinish is not null");
                //获取日计划
                if (DS_Plan.Tables[0].Rows.Count > 0)
                {
                    Tb_PlanNo.Content = DS_Plan.Tables[0].Rows[0]["instocknumber"].ToString();
                    DataAnalysis(Tb_PlanNo.Content.ToString());
                    return;
                }
                if (Tb_PlanNo.Content == null)
                {
                    return;
                }
                DataSet DS_Kanban = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select top(1) FullCode as FullCode from [dbo].[JSSX_Stock_In_Kanban]" +
                    " where InStockNumber='" + Tb_PlanNo.Content.ToString() + "' order by id desc");
                //获取看板
                if (DS_Kanban.Tables[0].Rows.Count > 0)
                {
                    Tb_BigKanbanNo.Content = DS_Kanban.Tables[0].Rows[0]["FullCode"].ToString(); ;
                    DataAnalysis(Tb_BigKanbanNo.Content.ToString());
                }


            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
            }


        }

        public void PLCUnlock()
        {
            Thread OP = new Thread(OpenLight);
            try
            {
                player2.PlaySync();
                OP.Start();
                Thread.Sleep(500);
                CloseLight();

            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
                ShowMessage("打开权限PLC失败，连接异常。" + "-----" + DateTime.Now.ToString("HH:mm:ss"));
            }

        }

        public string MakeKanban(string UniqueID, string InStockNumber, string WorkShift, string PrintNo, string label, string SN, string Lines)
        {
            try
            {
                return KanbanPrint.GetData(UniqueID, InStockNumber, WorkShift, PrintNo, label, SN, Lines);
            }
            catch (System.Exception ex)
            {
                Mylog.Error(ex.Message);
                //Tb_MidMessage.Content += "\r\n" + "打开权限PLC失败，连接异常。";
                return ex.ToString();
            }
        }

        public enum HardwareEnum
        {
            // 硬件，不一定要全部列出
            Win32_Processor, // CPU 处理器
            Win32_PhysicalMemory, // 物理内存条
            Win32_Keyboard, // 键盘
            Win32_PointingDevice, // 点输入设备，包括鼠标。
            Win32_FloppyDrive, // 软盘驱动器
            Win32_DiskDrive, // 硬盘驱动器
            Win32_CDROMDrive, // 光盘驱动器
            Win32_BaseBoard, // 主板
            Win32_BIOS, // BIOS 芯片
            Win32_ParallelPort, // 并口
            Win32_SerialPort, // 串口
            Win32_SerialPortConfiguration, // 串口配置
            Win32_SoundDevice, // 多媒体设置，一般指声卡。
            Win32_SystemSlot, // 主板插槽 (ISA & PCI & AGP)
            Win32_USBController, // USB 控制器
            Win32_NetworkAdapter, // 网络适配器
            Win32_NetworkAdapterConfiguration, // 网络适配器设置
            Win32_Printer, // 打印机
            Win32_PrinterConfiguration, // 打印机设置
            Win32_PrintJob, // 打印机任务
            Win32_TCPIPPrinterPort, // 打印机端口
            Win32_POTSModem, // MODEM
            Win32_POTSModemToSerialPort, // MODEM 端口
            Win32_DesktopMonitor, // 显示器
            Win32_DisplayConfiguration, // 显卡
            Win32_DisplayControllerConfiguration, // 显卡设置
            Win32_VideoController, // 显卡细节。
            Win32_VideoSettings, // 显卡支持的显示模式。

            // 操作系统
            Win32_TimeZone, // 时区
            Win32_SystemDriver, // 驱动程序
            Win32_DiskPartition, // 磁盘分区
            Win32_LogicalDisk, // 逻辑磁盘
            Win32_LogicalDiskToPartition, // 逻辑磁盘所在分区及始末位置。
            Win32_LogicalMemoryConfiguration, // 逻辑内存配置
            Win32_PageFile, // 系统页文件信息
            Win32_PageFileSetting, // 页文件设置
            Win32_BootConfiguration, // 系统启动配置
            Win32_ComputerSystem, // 计算机信息简要
            Win32_OperatingSystem, // 操作系统信息
            Win32_StartupCommand, // 系统自动启动程序
            Win32_Service, // 系统安装的服务
            Win32_Group, // 系统管理组
            Win32_GroupUser, // 系统组帐号
            Win32_UserAccount, // 用户帐号
            Win32_Process, // 系统进程
            Win32_Thread, // 系统线程
            Win32_Share, // 共享
            Win32_NetworkClient, // 已安装的网络客户端
            Win32_NetworkProtocol, // 已安装的网络协议
            Win32_PnPEntity,//all device
        }

        public static string MulGetHardwareInfo(HardwareEnum hardType, string propKey, string COMName)
        {

            string str = "";
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from " + hardType))
                {
                    var hardInfos = searcher.Get();
                    foreach (var hardInfo in hardInfos)
                    {
                        if (hardInfo.Properties[propKey].Value != null)
                        {
                            if (hardInfo.Properties[propKey].Value.ToString().Contains("(" + COMName + ")"))
                            {
                                str = hardInfo.Properties[propKey].Value.ToString();
                            }
                        }


                    }
                    searcher.Dispose();
                }
                return str;
            }
            catch (Exception ex)
            {
                Mylog.Error(ex.Message);
                return "";
            }
        }

        private void Restart()
        {
            System.Reflection.Assembly.GetEntryAssembly();
            string startpath = System.IO.Directory.GetCurrentDirectory();
            Process.Start(startpath + "\\JssxSeizouPC.exe");
            Application.Current.Shutdown();

        }

        /// <summary>
        /// 播放声音
        /// </summary>
        /// <param name="text">内容</param>
        /// <param name="rat">速率</param>
        private void Speaker(string text, int rat)
        {
            Type type = Type.GetTypeFromProgID("SAPI.SpVoice");
            dynamic spVoice = Activator.CreateInstance(type);
            spVoice.rate = rat;
            spVoice.Speak(text);
        }

        private void ShowMessage(string sMes)
        {

            this.Dispatcher.Invoke(new Action(() =>
            {
                string sLabName = "Lab" + iRow.ToString();
                Label label1 = new Label();
                if (iRow % 8 == 0)
                {
                    iRow = 0;
                    for (int i = 0; i <= 8; i++)
                    {
                        string sLabNameU = "Lab" + i.ToString();
                        Label btn = WP_Message.FindName(sLabNameU) as Label;//找到刚新添加的按钮   
                        if (btn != null)//判断是否找到，以免在未添加前就误点了   
                        {
                            WP_Message.Children.Remove(btn);//移除对应按钮控件   
                            WP_Message.UnregisterName(sLabNameU);//还需要把对用的名字注销掉，否则再次点击Button_Add会报错   
                        }
                    }

                }

                label1.Content = sMes;
                int isize = 1000 / sMes.Length;
                if (isize >= 37)
                {
                    isize = 37;
                }
                label1.FontSize = isize;
                Color color = (Color)ColorConverter.ConvertFromString("aqua");
                label1.Foreground = new SolidColorBrush(color);

                WP_Message.RegisterName(sLabName, label1);
                WP_Message.Children.Add(label1);

                iRow++;
                ThicknessAnimation marginAnimation = new ThicknessAnimation();
                marginAnimation.From = new Thickness(300, 0, 0, 0);//动画起始位置

                marginAnimation.To = new Thickness(Dg_Show.ActualWidth, 0, 0, 0);//动画结束位置
                marginAnimation.Duration = TimeSpan.FromSeconds(3);//动画执行时间
                label1.BeginAnimation(Grid.MarginProperty, marginAnimation);
                DoubleAnimation opacityAnimation = new DoubleAnimation();
                opacityAnimation.From = 1;//透明度初始值
                opacityAnimation.To = 0.2;//透明度值
                opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(6));
                label1.BeginAnimation(Grid.OpacityProperty, opacityAnimation);


            }));
        }

        /// <summary>
        /// 用户控件是的动画 缩放效果
        /// </summary>
        /// <param name="element">控件名</param>
        /// <param name="from">元素开始的大小</param>
        /// <param name="from">元素到达的大小</param>
        public void ProPass(UIElement element, double from, double to, string scolor)
        {
            Color color = (Color)ColorConverter.ConvertFromString(scolor);
            RotateTransform angle = new RotateTransform();  //旋转
            ScaleTransform scale = new ScaleTransform();   //缩放



            TransformGroup group = new TransformGroup();
            group.Children.Add(scale);
            group.Children.Add(angle);

            Rec_Shape.Fill = new SolidColorBrush(color);
            Rec_Shape.Visibility = Visibility.Visible;
            element.RenderTransform = group;
            element.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);//定义圆心位置        
            EasingFunctionBase easeFunction = new PowerEase()
            {
                EasingMode = EasingMode.EaseInOut,
                Power = 2
            };
            DoubleAnimation scaleAnimation = new DoubleAnimation()
            {
                From = from,                                   //起始值
                To = to,                                     //结束值
                EasingFunction = easeFunction,                    //缓动函数
                Duration = new TimeSpan(0, 0, 0, 2, 0),  //动画播放时间


            };

            DoubleAnimation angleAnimation = new DoubleAnimation()
            {
                From = 0,                                   //起始值
                To = 360,                                     //结束值
                EasingFunction = easeFunction,                    //缓动函数
                Duration = new TimeSpan(0, 0, 0, 2, 0),  //动画播放时间

            };

            //scaleAnimation.Completed += new EventHandler(scaleAnimation_Completed);
            //  AnimationClock clock = scaleAnimation.CreateClock();
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            angle.BeginAnimation(RotateTransform.AngleProperty, angleAnimation);

            //}
        }

        #endregion

        #region 参数输入

        private void BindPreviewMouseUpEvent()
        {
            grqNumKB.RecvData += new NumericKeyboard.RecvDataEventHandler(grqNumKB_RecvData);
        }
        private void textBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SelTB = sender as TextBox;
            popNumKeyboard.PlacementTarget = SelTB;   //表示Popup控件的放置的位置依赖的对象
            popNumKeyboard.IsOpen = true;
        }

        private void grqNumKB_RecvData(object sender, int nDig)
        {
            if (SelTB == null)
            {
                return;
            }

            if (nDig == 0x08)
            {
                //回退
                if (!string.IsNullOrEmpty(SelTB.Text))
                {
                    SelTB.Text = SelTB.Text.Substring(0, SelTB.Text.Length - 1);
                }
            }
            else if (nDig == 0x13)
            {
                popNumKeyboard.IsOpen = false;
            }
            else
            {
                int n = nDig - 0x30;
                if (string.IsNullOrEmpty(SelTB.Text))
                {
                    SelTB.Text = n.ToString();
                }
                else
                {
                    SelTB.Text += n.ToString();
                }
            }
        }
        #endregion
    }
}
