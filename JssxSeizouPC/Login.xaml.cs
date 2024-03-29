﻿using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            DataSet ds_Line = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "select LineName,LineNumber from JSSX_Line where cArea = '后工段' or  cArea = '后虚拟' order by iLineSequence");
            int LineCount = ds_Line.Tables[0].Rows.Count;

            double dGridLine = Math.Sqrt(LineCount + 1);
            int iGridBtn = int.Parse(Math.Ceiling(dGridLine).ToString());   //每一行，每一列放几个按钮 （总行数加一个退出按钮，开根号后向上取整）


            for (int i = 0; i < iGridBtn; i++)      //画纵向格子（4个格子循环写4次）
            {
                var Cell1 = new RowDefinition();
                Cell1.Height = new GridLength(1, GridUnitType.Star);
                WinGrid.RowDefinitions.Add(Cell1);
            }

            for (int i = 0; i < iGridBtn; i++)         //画横向格子
            {
                var Cell1 = new ColumnDefinition();
                Cell1.Width = new GridLength(1, GridUnitType.Star);
                WinGrid.ColumnDefinitions.Add(Cell1);
            }

            int iRow = 0, iCol = 0;

            //循环绘制按钮
            foreach (DataRow rows in ds_Line.Tables[0].Rows)
            {
                Button NewBtn = new Button();
                NewBtn.Name = "Btn_Line" + rows["LineNumber"].ToString();
                NewBtn.Content = rows["LineName"].ToString();
                NewBtn.ToolTip = rows["LineNumber"].ToString();
                NewBtn.FontSize = 20;
                NewBtn.Foreground = new SolidColorBrush(Colors.Aqua);
                NewBtn.Margin = new Thickness(10, 10, 10, 10);
                NewBtn.Click += Btn_Line_Click;

                WinGrid.Children.Add(NewBtn);

                Grid.SetRow(NewBtn, iRow);
                Grid.SetColumn(NewBtn, iCol);
                iCol++;
                if (iCol >= iGridBtn)  //换行
                {
                    iCol = 0;
                    iRow++;
                }


            }
            //绘制退出按钮
            Button NewBtn2 = new Button();
            NewBtn2.Name = "Btn_Close";
            NewBtn2.Content = "退出";
            NewBtn2.ToolTip = 999;
            NewBtn2.FontSize = 20;
            NewBtn2.Foreground = new SolidColorBrush(Colors.Red);
            NewBtn2.Margin = new Thickness(10, 10, 10, 10);
            NewBtn2.Click += Btn_Close_Click;

            WinGrid.Children.Add(NewBtn2);

            Grid.SetRow(NewBtn2, iGridBtn - 1);
            Grid.SetColumn(NewBtn2, iGridBtn - 1);

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
