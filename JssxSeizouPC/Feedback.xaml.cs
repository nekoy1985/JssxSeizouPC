using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JssxSeizouPC
{
    /// <summary>
    /// Feedback.xaml 的交互逻辑
    /// </summary>
    public partial class Feedback : Window
    {
        TextBox SelTB;
        public string sPlanNo, sLineNo;
        public Feedback(string sPlanNo, string sCartype, string sLineNo)
        {
            InitializeComponent();
            BindPreviewMouseUpEvent();
            DataSet dsCarTypes = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select ROW_NUMBER() over(order by InStocknumber) as ID,a.JssxInsideCode as 背番,TrayType+'__'+CarType+'__'+WorkShift as 车型, Amount as 总量,QuantityCompletion as 完成,a.JssxCode as 社番,b.CarLabel as 铭板信息,a.CustomerCode as 客番,WorkShift as 班次,b.UniqueID as UID  from JSSX_Stock_In_Detailed as a left join [dbo].[JSSX_Products] as b on a.UniqueID=b.UniqueID and b.Revoked=0 where InStocknumber='" + sPlanNo + "' and isfinish=0 order by 班次 asc");
            Cbx_ProductSelection.SelectedValuePath = "UID";
            Cbx_ProductSelection.DisplayMemberPath = "车型";
            Cbx_ProductSelection.ItemsSource = dsCarTypes.Tables[0].DefaultView;
            this.sPlanNo = sPlanNo;
            this.sLineNo = sLineNo;
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

        private void Btn_Up_Click(object sender, RoutedEventArgs e)
        {
            ScrollViewer sv = FV<ScrollViewer>(this.Lbx_Results);
            sv.PageUp();//向右滚动
        }

        private void Btn_Down_Click(object sender, RoutedEventArgs e)
        {
            ScrollViewer sv = FV<ScrollViewer>(this.Lbx_Results);
            sv.PageDown();//向右滚动
        }

        public static ci FV<ci>(DependencyObject o)
        where ci : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                DependencyObject c = VisualTreeHelper.GetChild(o, i);

                if (c != null && c is ci)
                    return (ci)c;
                else
                {
                    ci cc = FV<ci>(c);

                    if (cc != null)
                        return cc;
                }
            }
            return null;
        }

        private void Cbx_ProductSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataSet dsCarTypes = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "select distinct cPJSSXCode as 社番,cPInsideCode as 背番,cPChineseName as 部品名称,'\\QRCode\\123.PNG' as FullPath,JBR.cPartsUniqueID AS UID from (select cRelationID from [dbo].[JSSX_BOM_Product]  WHERE  cProductUID='" + Cbx_ProductSelection.SelectedValue.ToString() + "')  AS JBP  LEFT JOIN (SELECT cRelationID,cPartsUniqueID, SUM(iQty) AS iQty FROM JSSX_BOM_Relation where  bIsNowUseParts = 0 and cLvl!='001' group by cRelationID,cPartsUniqueID)  AS JBR ON  JBR.cRelationID=JBP.cRelationID  LEFT JOIN [dbo].[JSSX_BOM_Parts] AS JBP2 ON  JBR.cPartsUniqueID=JBP2.cPartsUniqueID");
            Lbx_Results.DataContext = dsCarTypes;
        }

        private void Lbx_Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView selectedText = (DataRowView)(sender as ListBox).SelectedItem;
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();//初始化
            bmp.UriSource = new Uri(Environment.CurrentDirectory + "\\" + selectedText.Row.ItemArray[3].ToString());//LIST是数组，数字代表的是第几列
            bmp.EndInit();//结束初始化
            Im_PartImg.Source = bmp;//设置显示图片 

        }

        private void Btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            if (Lbx_Results.SelectedIndex <= 0 && Tb_quantity.Text != "")
            {
                MessageBox.Show("请选中部品并输入数量。");
            }
            else
            {
                DataRowView selectedText = (DataRowView)Lbx_Results.SelectedItem;
                string sUID = selectedText.Row.ItemArray[4].ToString();
                bool bTran = sqlHelp.ExecuteSqlTran(sqlHelp.ConnectionStringLocalTransaction, "insert into JSSX_Logistics_Supplement (cPartsUniqueID, iQuantity,cLogger,cInStockNumber) values( '" + sUID + "','" + Tb_quantity.Text + "','" + sLineNo + "','" + sPlanNo + "')");
                if (bTran)
                {
                    this.Close();
                }
                else
                {
                    MessageBox.Show("执行部品异常消减失败。");
                }
            }
        }
    }
}
