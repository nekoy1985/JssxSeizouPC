using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
    /// ProductionOrder.xaml 的交互逻辑
    /// </summary>
    public partial class ProductionOrder : Window
    {
        public static string sLine, JXPONumber;
        public static DataSet ds_p, ds_Next;
        TextBox SelTB;
        bool bIsMove = false;
        public ProductionOrder(string Line)
        {
            sLine = Line;
            InitializeComponent();
            BindPreviewMouseUpEvent();
            string sql = @"select top 7 Number,CONVERT(varchar(10),PlanTime,120) as PlanTime from JSSX_Stock_In where Line = @sLine and PlanTime >= CONVERT(varchar(10),getdate(),120) and Isfinish is not null  
                            union all
                            select* from (select top 2 Number, CONVERT(varchar(10), PlanTime, 120) as PlanTime from JSSX_Stock_In where Line = @sLine and PlanTime < CONVERT(varchar(10), getdate(), 120) and Isfinish is not null order by PlanTime desc) a order by PlanTime asc";
            SqlParameter[] param = {
                 new SqlParameter("@sLine", System.Data.SqlDbType.Char),
             };
            param[0].Value = sLine;


            DataSet ds = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql, param);
            Cbx_Date.ItemsSource = ds.Tables[0].DefaultView;  // 设置下拉数据源
            Cbx_Date.DisplayMemberPath = "PlanTime";
            Cbx_Date.SelectedValuePath = "PlanTime";

            string sql2 = @"select top 7 Number,CONVERT(varchar(10),PlanTime,120) as PlanTime from JSSX_Stock_In where Line = @sLine and PlanTime >= CONVERT(varchar(10),getdate(),120) and Isfinish is not null 
                            union all
                            select* from (select top 7 Number, CONVERT(varchar(10), PlanTime, 120) as PlanTime from JSSX_Stock_In where Line = @sLine and PlanTime < CONVERT(varchar(10), getdate(), 120) and Isfinish is not null order by PlanTime desc) a order by PlanTime asc";
            SqlParameter[] param2 = {
                 new SqlParameter("@sLine", System.Data.SqlDbType.Char),
             };
            param2[0].Value = sLine;


            DataSet ds2 = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql2, param2);
            Cbx_Date2.ItemsSource = ds2.Tables[0].DefaultView;  // 设置下拉数据源
            Cbx_Date2.DisplayMemberPath = "PlanTime";
            Cbx_Date2.SelectedValuePath = "PlanTime";


            string sql_DN = "select 'D' as cWorkShift union select 'N' as cWorkShift";

            DataSet ds_DN = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_DN);
            Cbx_WorkShift.ItemsSource = ds_DN.Tables[0].DefaultView;  // 设置下拉数据源
            Cbx_WorkShift.DisplayMemberPath = "cWorkShift";
            Cbx_WorkShift.SelectedValuePath = "cWorkShift";


        }

        private void Cbx_WorkShift_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cbx_Date.SelectedIndex != -1 && Cbx_WorkShift.SelectedIndex != -1)
            {
                Gvw_Set();
            }
        }
        private void Gvw_Set()
        {
            string sql_Now = "select b.UniqueID,b.JSSXInsideCode as 背番,c.TrayType as 类型,c.CarType as 车型,sum(b.Amount) as 计划数,iif(c.ismanufactured = 1,'是','否') as 照合,b.iSequence as 排序,a.Number from JSSX_Stock_In a left join JSSX_Stock_In_Detailed b on a.Number = b.InStockNumber left join JSSX_Products c on b.UniqueID = c.UniqueID where a.Line = @Line and a.PlanTime =@PlanTime and a.Isfinish is not null  and b.Amount != 0 and b.WorkShift = @WorkShift  group by b.UniqueID,b.JSSXInsideCode,c.TrayType,c.CarType,b.iSequence,a.Number,c.ismanufactured  order by b.iSequence";
            SqlParameter[] param_Now = {
                 new SqlParameter("@Line",    System.Data.SqlDbType.Char),
                 new SqlParameter("@PlanTime", System.Data.SqlDbType.Char),
                 new SqlParameter("@WorkShift", System.Data.SqlDbType.Char),
             };
            param_Now[0].Value = sLine;
            param_Now[1].Value = Cbx_Date.SelectedValue.ToString();
            param_Now[2].Value = Cbx_WorkShift.SelectedValue.ToString();


            DataSet ds_Now = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_Now, param_Now);
            if (ds_Now.Tables[0].Rows.Count == 0)
            {
                MessageBox.Show("没有找到生产计划，请确认日期班次");
                return;
            }

            DG_Now.ItemsSource = ds_Now.Tables[0].DefaultView;
            JXPONumber = "JXPO" + ds_Now.Tables[0].Rows[0]["Number"].ToString().Substring(4, 10) + Cbx_WorkShift.SelectedValue.ToString();

            string sql_Next = "select b.UniqueID,b.JSSXInsideCode as 背番,c.TrayType as 类型,c.CarType as 车型,sum(b.Amount) as 计划数,iif(c.ismanufactured = 1,'是','否') as 照合,CONVERT(varchar(10),a.PlanTime,120) as 计划日期,b.WorkShift as 班次 from JSSX_Stock_In a left join JSSX_Stock_In_Detailed b on a.Number = b.InStockNumber left join JSSX_Products c on b.UniqueID = c.UniqueID where a.Line = @Line  and a.Isfinish is not null  and b.Amount != 0 and (( a.PlanTime >= @PlanTime and @WorkShift = 'D') or (a.PlanTime > @PlanTime and @WorkShift = 'N'))  group by b.UniqueID,b.JSSXInsideCode,c.TrayType,c.CarType,a.PlanTime,b.WorkShift,c.ismanufactured order by a.PlanTime,b.WorkShift";
            SqlParameter[] param_Next = {
                 new SqlParameter("@Line", System.Data.SqlDbType.Char),
                 new SqlParameter("@PlanTime", System.Data.SqlDbType.Char),
                 new SqlParameter("@WorkShift", System.Data.SqlDbType.Char),
             };
            param_Next[0].Value = sLine;
            param_Next[1].Value = Cbx_Date.SelectedValue.ToString();
            param_Next[2].Value = Cbx_WorkShift.SelectedValue.ToString();
            ds_Next = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_Next, param_Next);
            if (ds_Next.Tables[0].Rows.Count != 0)
            {
                DG_Next.Visibility = Visibility.Visible;
                DG_Next.ItemsSource = ds_Next.Tables[0].DefaultView;
                DG_Next.Columns[0].Visibility = Visibility.Hidden;

            }
            else
            {
                DG_Next.Visibility = Visibility.Hidden;
            }



            string sql_p = "select ROW_NUMBER() OVER (ORDER BY a.iSequence ASC) as Nid,a.cUniqueID as UniqueID,a.cJSSXInsideCode as 背番,c.TrayType as 类型,c.CarType as 车型,a.iAmount as 生产数,isnull((select sum(iAmount) as iAmount from (select distinct cBrNumber,cUniqueID,iAmount from JSSX_Logistics_Manifest where cProductionSequenceNo = a.cPNumber and cUniqueID = a.cUniqueID) aa group by cUniqueID),0) as 物流已送,a.iSequence as 排序 from JSSX_ProductionPlan a left join JSSX_Products c on a.cUniqueID = c.UniqueID where a.cLine = @Line and a.cWorkShift = @WorkShift and a.cPlanTime = @PlanTime and a.isdelete = 0 and ismanufactured = 1 order by a.iSequence";
            SqlParameter[] param_p = {
                 new SqlParameter("@Line", System.Data.SqlDbType.Char),
                 new SqlParameter("@PlanTime", System.Data.SqlDbType.Char),
                 new SqlParameter("@WorkShift", System.Data.SqlDbType.Char),
             };
            param_p[0].Value = sLine;
            param_p[1].Value = Cbx_Date.SelectedValue.ToString();
            param_p[2].Value = Cbx_WorkShift.SelectedValue.ToString();
            ds_p = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_p, param_p);
            Lab_Label.Content = "本  次  排  程";

            if (ds_p.Tables[0].Rows.Count == 0)
            {
                Lab_Label.Content = "本  次  排  程 (未排)";
                string sql_p2 = "select ROW_NUMBER() OVER (ORDER BY b.iSequence ASC) as Nid,b.UniqueID,b.JSSXInsideCode as 背番,c.TrayType as 类型,c.CarType as 车型,sum(b.Amount) as 生产数,0 as 物流已送,b.iSequence as 排序 from JSSX_Stock_In a left join JSSX_Stock_In_Detailed b on a.Number = b.InStockNumber left join JSSX_Products c on b.UniqueID = c.UniqueID where a.Line = @Line and a.PlanTime =@PlanTime and a.Isfinish is not null  and b.WorkShift = @WorkShift and b.Amount != 0  and ismanufactured = 1 group by b.UniqueID,b.JSSXInsideCode,c.TrayType ,c.CarType ,b.iSequence  order by b.iSequence";
                SqlParameter[] param_p2 = {
                 new SqlParameter("@Line", System.Data.SqlDbType.Char),
                 new SqlParameter("@PlanTime", System.Data.SqlDbType.Char),
                 new SqlParameter("@WorkShift", System.Data.SqlDbType.Char),
                     };
                param_p2[0].Value = sLine;
                param_p2[1].Value = Cbx_Date.SelectedValue.ToString();
                param_p2[2].Value = Cbx_WorkShift.SelectedValue.ToString();
                ds_p = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_p2, param_p2);

            }
            DG_Production.ItemsSource = ds_p.Tables[0].DefaultView;



            DG_Now.Columns[0].Visibility = Visibility.Hidden;
            DG_Now.Columns[6].Visibility = Visibility.Hidden;
            //DG_Production.Columns[0].Visibility = Visibility.Hidden;
            //DG_Production.Columns[0].IsReadOnly = true;
        }

        private void Cbx_Date_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cbx_Date.SelectedIndex != -1 && Cbx_WorkShift.SelectedIndex != -1)
            {
                Gvw_Set();
            }
        }

        private void Btn_Up_Click(object sender, RoutedEventArgs e)
        {
            bIsMove = true;
            if (DG_Production.SelectedIndex <= 0)
            {
                return;   //最顶行不移动
            }
            int irow = DG_Production.SelectedIndex;

            ds_p.Tables[0].Rows[irow]["Nid"] = int.Parse(ds_p.Tables[0].Rows[irow]["Nid"].ToString()) - 1;
            ds_p.Tables[0].Rows[irow - 1]["Nid"] = int.Parse(ds_p.Tables[0].Rows[irow - 1]["Nid"].ToString()) + 1;

            DataView dv = ds_p.Tables[0].DefaultView;
            dv.Sort = "Nid";
            ds_p.Tables.Clear();
            ds_p.Tables.Add(dv.ToTable());
            // 对DS排序

            DG_Production.ItemsSource = ds_p.Tables[0].DefaultView;
            DG_Production.SelectedIndex = irow - 1;

        }

        private void Btn_Down_Click(object sender, RoutedEventArgs e)
        {
            bIsMove = true;
            if (DG_Production.SelectedIndex >= ds_p.Tables[0].Rows.Count - 1)
            {
                return;   //最底行不移动
            }
            int irow = DG_Production.SelectedIndex;

            ds_p.Tables[0].Rows[irow]["Nid"] = int.Parse(ds_p.Tables[0].Rows[irow]["Nid"].ToString()) + 1;
            ds_p.Tables[0].Rows[irow + 1]["Nid"] = int.Parse(ds_p.Tables[0].Rows[irow + 1]["Nid"].ToString()) - 1;

            DataView dv = ds_p.Tables[0].DefaultView;
            dv.Sort = "Nid";
            ds_p.Tables.Clear();
            ds_p.Tables.Add(dv.ToTable());
            // 对DS排序

            DG_Production.ItemsSource = ds_p.Tables[0].DefaultView;
            DG_Production.SelectedIndex = irow + 1;

        }

        private void Cbx_Date2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            string sql_Next = "select b.UniqueID,b.JSSXInsideCode as 背番,c.TrayType as 类型,c.CarType as 车型,sum(b.Amount) as 计划数,iif(c.ismanufactured = 1,'是','否') as 照合,CONVERT(varchar(10),a.PlanTime,120) as 计划日期,b.WorkShift as 班次 from JSSX_Stock_In a left join JSSX_Stock_In_Detailed b on a.Number = b.InStockNumber left join JSSX_Products c on b.UniqueID = c.UniqueID where a.Line = @Line  and a.Isfinish is not null and b.Amount != 0 and  a.PlanTime >= @PlanTime                                                                              group by b.UniqueID,b.JSSXInsideCode,c.TrayType,c.CarType,a.PlanTime,b.WorkShift,c.ismanufactured order by a.PlanTime,b.WorkShift";
            SqlParameter[] param_Next = {
                 new SqlParameter("@Line", System.Data.SqlDbType.Char),
                 new SqlParameter("@PlanTime", System.Data.SqlDbType.Char),
             };
            param_Next[0].Value = sLine;
            param_Next[1].Value = Cbx_Date2.SelectedValue.ToString();
            ds_Next = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql_Next, param_Next);
            if (ds_Next.Tables[0].Rows.Count != 0)
            {
                DG_Next.Visibility = Visibility.Visible;
                DG_Next.ItemsSource = ds_Next.Tables[0].DefaultView;
                DG_Next.Columns[0].Visibility = Visibility.Hidden;

            }
            else
            {
                DG_Next.Visibility = Visibility.Hidden;
            }

        }

        private void Btn_Del_Click(object sender, RoutedEventArgs e)
        {
            int irow = DG_Production.SelectedIndex;
            ds_p.Tables[0].Rows.RemoveAt(irow);
            DG_Production.ItemsSource = ds_p.Tables[0].DefaultView;
        }

        private void Btn_Add_Click(object sender, RoutedEventArgs e)
        {


            if (DG_Next.SelectedIndex < 0)
            {
                MessageBox.Show("先选中下方表格的一行，才能添加");
                return;
            }
            int irow = DG_Next.SelectedIndex;
            try
            {
                string sismanufactured = ds_Next.Tables[0].Rows[irow]["照合"].ToString();
                if (sismanufactured == "否")
                {
                    MessageBox.Show("不扫描铭板的东西不需要排班，如果此项产品确实需要扫描铭板，设置有误的情况，请联系日程课");
                    return;
                }

                string sUid = ds_Next.Tables[0].Rows[irow][0].ToString();


                string sJSSXInsideCode = ds_Next.Tables[0].Rows[irow][1].ToString();

                ds_p.Tables[0].Rows.Add();
                int iDsRC = ds_p.Tables[0].Rows.Count;
                ds_p.Tables[0].Rows[iDsRC - 1]["UniqueID"] = sUid;
                ds_p.Tables[0].Rows[iDsRC - 1]["背番"] = sJSSXInsideCode;
                ds_p.Tables[0].Rows[iDsRC - 1]["类型"] = ds_Next.Tables[0].Rows[irow]["类型"].ToString();
                ds_p.Tables[0].Rows[iDsRC - 1]["车型"] = ds_Next.Tables[0].Rows[irow]["车型"].ToString();
                ds_p.Tables[0].Rows[iDsRC - 1]["生产数"] = ds_Next.Tables[0].Rows[irow]["计划数"].ToString();
                ds_p.Tables[0].Rows[iDsRC - 1]["Nid"] = (int.Parse(ds_p.Tables[0].Rows[iDsRC - 2]["Nid"].ToString()) + 1);



                DG_Production.ItemsSource = ds_p.Tables[0].DefaultView;

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }

        }

        private void BindPreviewMouseUpEvent()
        {
            grqNumKB.RecvData += new NumericKeyboard.RecvDataEventHandler(grqNumKB_RecvData);
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

        private void Tb_MSSTART_GotFocus(object sender, RoutedEventArgs e)
        {
            bIsMove = false;
            SelTB = sender as TextBox;
            SelTB.Text = "";
        }



        private void Tb_MSSTART_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!bIsMove)   //没有经过弹框关闭移动选项的时候，不执行此操作
            {
                int iRow = DG_Production.SelectedIndex;
                if (iRow >= 0)
                {
                    if (SelTB != null)
                    {
                        if (SelTB.Text != "")
                        {
                            SelTB = sender as TextBox;
                            string a = SelTB.Text;
                            ds_p.Tables[0].Rows[iRow]["生产数"] = SelTB.Text;
                        }
                    }
                }

            }
        }


        private void DG_Production_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            popNumKeyboard.IsOpen = false;

        }

        private void textBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SelTB = sender as TextBox;
            popNumKeyboard.PlacementTarget = SelTB;   //表示Popup控件的放置的位置依赖的对象
            popNumKeyboard.IsOpen = true;
        }

        private void Btn_UpDate_Click(object sender, RoutedEventArgs e)
        {

            if (ds_p.Tables[0].Rows.Count == 0)
            {
                MessageBox.Show("没有数据");
                return;
            }
            try
            {

                string sql = "insert into JSSX_ProductionPlan(cPNumber, cLine, cWorkShift, cPlanTime, cUniqueID, cJSSXInsideCode, iAmount, iSequence) values ";
                int iDsRow = 0;

                foreach (DataRow rows in ds_p.Tables[0].Rows)
                {
                    iDsRow++;
                    sql = sql + "('" + JXPONumber + "','" + sLine + "','" + Cbx_WorkShift.SelectedValue.ToString() + "','" + Cbx_Date.SelectedValue.ToString() + "','" + rows["UniqueID"].ToString() + "','" + rows["背番"].ToString() + "','" + rows["生产数"].ToString() + "','" + iDsRow + "'),";

                }
                sql = sql.Substring(0, sql.Length - 1);

                //insert into JSSX_ProductionPlan(cProductionSequenceNo, cLine, cWorkShift, cPlanTime, cUniqueID, cJSSXInsideCode, iAmount, iSequence) values()
                sqlHelp.ExecuteNonQuery(sqlHelp.SQLCon, CommandType.Text, "update JSSX_ProductionPlan set isdelete = 1,dDeleteTime = getdate(),cLine = cLine + REPLACE( REPLACE( CONVERT(varchar(100), GETDATE(), 120),'-',''),':','') where cPNumber = '" + JXPONumber + "' and isdelete = 0");
                int irow = sqlHelp.ExecuteNonQuery(sqlHelp.SQLCon, CommandType.Text, sql);
                if (irow == 0)
                {
                    MessageBox.Show("提交失败，请确认是否输入了数字");
                }
                else
                {
                    SqlParameter[] param = {
                 new SqlParameter("@cPNumber", System.Data.SqlDbType.Char),
                 new SqlParameter("@cPlanTime", System.Data.SqlDbType.Char),
             };
                    param[0].Value = JXPONumber;
                    param[1].Value = Cbx_Date.SelectedValue.ToString();
                    sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.StoredProcedure, "Logistics_Plan_Parts", param);

                    MessageBox.Show("提交成功");
                    this.Hide();


                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
        }

    }
}
