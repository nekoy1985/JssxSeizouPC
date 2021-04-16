using System;
using System.Collections.Generic;
using System.Data;
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

namespace JssxSeizouPC
{
    /// <summary>
    /// SpecialHandling.xaml 的交互逻辑
    /// </summary>
    public partial class SpecialHandling : Window
    {
        public string slines;
        public SpecialHandling(DataTable Ds_Result,String sBigKanbanNo, string sline)
        {
            InitializeComponent();
            slines = sline;
            if (sBigKanbanNo==""|| Ds_Result.Rows.Count==0)
            {
                MessageBox.Show("没有获取到看板号或者看板内没有铭板");
                this.Close();
            }
            Lb_kanbanNo.Content = sBigKanbanNo;
           
            DG_DataList.ItemsSource = Ds_Result.DefaultView;
        }

        private void GetReasonData(DataGrid DG)
        {
            for (int k = 0; k < DG.Items.Count; k++)
            {
                DataGridTemplateColumn tempColumn = DG.Columns[1] as DataGridTemplateColumn;
                FrameworkElement element = DG.Columns[1].GetCellContent(DG.Items[k]);

                if (element != null)
                {
                    ComboBox DgCb_Reason = tempColumn.CellTemplate.FindName("Cbx_Reason", element) as ComboBox; 
                    DataTable dt = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, "SELECT distinct cReason FROM NGProductReason ").Tables[0];
                    DgCb_Reason.ItemsSource = dt.DefaultView;
                    DgCb_Reason.DisplayMemberPath = "cReason";
                    DgCb_Reason.SelectedValuePath = "cReason";
                }
            } 

        } 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DataRowView rowSelected = (DataRowView)DG_DataList.SelectedItem;
            if (rowSelected != null)
            {
                rowSelected.Delete();
            }
        }

        private void Cbx_Reason_Loaded(object sender, RoutedEventArgs e)
        {
            GetReasonData(DG_DataList);
        }

        private void Btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            string sSql = "";
            MainWindow Mw = new MainWindow();
            foreach (DataRowView dr in DG_DataList.Items)
            {
                string sScanResult = dr[0].ToString();
                string sReason = dr[1].ToString();
                if (sReason!="")
                {
                    sSql += "Insert into NGProductRecord(cReason,cMeiBan,cLine)values('" + sReason + "','" + sScanResult + "','" + slines + "');";
                    dr.Delete();
                }
            }
            sqlHelp.ExecuteSqlTran(sqlHelp.SQLCon, sSql);
            Mw.Dg_Show.ItemsSource = DG_DataList.ItemsSource;
        }
    }
}
