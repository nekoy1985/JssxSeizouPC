using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

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
           
            GetLine();
        }

        
    private void GetLine()
        {
            DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select LineCode as  line,LineNumber as LineNumber from [dbo].[JSSX_Line] order by LineNumber ");
            Cbx_Line.ItemsSource = ds.Tables[0].DefaultView;
            Cbx_Line.DisplayMemberPath = "line";
            Cbx_Line.SelectedValuePath = "LineNumber";

        }
        private void Cbx_Line_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cbx_Line.SelectedIndex < 0)
            {
                MessageBox.Show("请先选择客户！");
                return;
            }
            else
            {
                Mylog.Error("登陆成功");
                config.AppSettings.Settings["Lines"].Value = Cbx_Line.SelectedValue.ToString();
                config.AppSettings.Settings["Devices"].Value = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select Devices as  Devices from [dbo].[JSSX_Line] where LineNumber='" + Cbx_Line.SelectedValue.ToString() + "' ").Tables[0].Rows[0]["Devices"].ToString();
                config.Save();
                this.Hide();
                MainWindow lo = new MainWindow();
                lo.Show();
            }
        }
    }
}
