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
using System.Windows.Threading;

namespace JssxSeizouPC
{
    /// <summary>
    /// NGReason.xaml 的交互逻辑
    /// </summary>
    public partial class NGReason : Window
    {
        public string slines;
        public bool bIsok = false;
        public NGReason(string LabelNo, string sline)
        {
            InitializeComponent();
            Lb_LabelNo.Text = LabelNo;
            slines = sline;
            CreatButton();
            this.Title = "铭板号:" + LabelNo;
            
        }

        private void Btn_Submit_Click(object sender, RoutedEventArgs e)
        {
            string text = (sender as Button).Content.ToString();
            bIsok = true;
            sqlHelp.ExecuteSqlTran(sqlHelp.ConnectionStringLocalTransaction, "Insert into NGProductRecord(cReason,cMeiBan,cLine) values('" + text + "','" + Lb_LabelNo.Text + "','" + slines + "')");
            this.Close();
        }

        private void CreatButton()
        {
            DataTable dt = sqlHelp.ExecuteDataSet(sqlHelp.ConnectionStringLocalTransaction, CommandType.Text, "SELECT distinct CONVERT(nvarchar(20), id)+'.'+cReason as cReason,id FROM NGProductReason where cline= '" + slines + "' order by id asc ").Tables[0];
            //Cbx_Reason.ItemsSource = dt.DefaultView;
            //Cbx_Reason.SelectedValuePath = "cReason";
            //Cbx_Reason.DisplayMemberPath = "cReason";
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Button tb1 = new Button();
                tb1.Name = "myTextBox" + i.ToString();
                tb1.Content = dt.Rows[i]["cReason"].ToString();
                tb1.Margin = new Thickness(10, 10, 10, 10);
                tb1.FontSize = 20;
                tb1.SetValue(Grid.RowProperty, (i / 4) + 2); //设置按钮所在Grid控件的行
                tb1.SetValue(Grid.ColumnProperty, i % 4); //设置按钮所在Grid控件的列
                tb1.Click += new RoutedEventHandler(Btn_Submit_Click);
                grid1.Children.Add(tb1);
            }
        }

        private void Btn_cannel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        
    }
}
