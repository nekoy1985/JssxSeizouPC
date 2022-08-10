using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace JssxSeizouPC
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public static string key = "";
        public static DataSet Mk = new DataSet();
        public static DataSet Ver = new DataSet();
        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        public Login()
        {
            TimeSpan nowDt = DateTime.Now.TimeOfDay;
            TimeSpan workStartDT = DateTime.Parse("14:00").TimeOfDay;
            TimeSpan workEndDT = DateTime.Parse("15:00").TimeOfDay;
            if (nowDt > workStartDT && nowDt < workEndDT)
            {
            }
            Mylog.Error("打开");
            System.Diagnostics.Process[] myProcesses = System.Diagnostics.Process.GetProcessesByName("JssxSeizouPC");
            if (myProcesses.Length > 1)
            {

                foreach (System.Diagnostics.Process p in myProcesses)
                {
                    p.Kill();
                }
            }
            InitializeComponent();

        }



        private void Btn_Line_Click(object sender, RoutedEventArgs e)
        {
            Button Bt = (Button)sender;
            Mylog.Error("登陆成功");
            string sLine = int.Parse(Bt.ToolTip.ToString()).ToString("00");
            config.AppSettings.Settings["Lines"].Value = sLine;
            DataSet LineInfo = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text,
                "select LineCode,bIsNewVersion,Devices,bIsClickable,bIsNeedOK,cIP1 from [dbo].[JSSX_Line] where LineNumber='" + sLine + "' ");
            config.AppSettings.Settings["LinesName"].Value = LineInfo.Tables[0].Rows[0]["LineCode"].ToString();
            config.AppSettings.Settings["Devices"].Value = LineInfo.Tables[0].Rows[0]["Devices"].ToString();
            config.AppSettings.Settings["bIsNewVersion"].Value = LineInfo.Tables[0].Rows[0]["bIsNewVersion"].ToString();
            config.AppSettings.Settings["bIsClickable"].Value = LineInfo.Tables[0].Rows[0]["bIsClickable"].ToString();
            config.AppSettings.Settings["bIsNeedOK"].Value = LineInfo.Tables[0].Rows[0]["bIsNeedOK"].ToString();
            config.AppSettings.Settings["PrintIP"].Value = LineInfo.Tables[0].Rows[0]["cIP1"].ToString();
            config.Save();
            this.Hide();
            MainWindow lo = new MainWindow();
            lo.Show();
        }

        private void Btn_Close_Click(object sender, RoutedEventArgs e)
        {
            Mylog.Error("退出程序");
            Environment.Exit(0);
        }
    }
}
