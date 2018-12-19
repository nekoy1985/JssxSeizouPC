using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JssxSeizouPC
{
  
    /// <summary>
    /// ScrapRecord.xaml 的交互逻辑
    /// </summary>
    public partial class ScrapRecord : Window
    {
        TextBox SelTB;

        public ScrapRecord()
        {
            InitializeComponent();
            BindPreviewMouseUpEvent();
            GetData();
        }

        protected void GetData()
        {
            DataSet Ds_Result = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "SELECT distinct b.CarType FROM JSSX_Stock_In_Detailed  a left join JSSX_Products b on a.UniqueID=b.UniqueID WHERE   (InStockNumber = N'JXDP2018121903V04')");

            DG_Inputwindows.ItemsSource = Ds_Result.Tables[0].DefaultView;
        }

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

        private void DG_Inputwindows_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void Btn_Submit_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
