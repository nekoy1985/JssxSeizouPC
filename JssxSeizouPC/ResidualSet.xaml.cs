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
	/// ResidualSet.xaml 的交互逻辑
	/// </summary>
	public partial class ResidualSet : Window
	{
		public static string ssPuid;
		public static string ssLine;
		TextBox SelTB;
		bool bIsMove = false;
		public static DataSet ds_Residual;

		public ResidualSet(string sLine, string sPuid)
		{
			InitializeComponent();
			BindPreviewMouseUpEvent();

			ssPuid = sPuid;
			ssLine = sLine;
			string sql = @"
			declare @cLineNumber nvarchar(20)  
			select @cLineNumber = LineNumber from JSSX_Line a where a.LineCode = @cLine

			select JP.cPartsUniqueID,JP.cPInsideCode,JP.cPJSSXCode,JP.cPChineseName,isnull(JBLF.cPutRack,0) as cPutRack,JP.iPVolume  + JP.iVolumeDiff + JBLF.iDifference as iVolume,R.iQTYSPend as iQTYSP,'' as 端数修改值
			from (select row_number() over (partition by JBR.iSerialNum,a.UniqueID order by JBR.cRelationID desc) as iJPRowNum, UniqueID, CarType, a.iP2R,JBR.iSerialNum as iSerialNum,JBR.iQty as iQty,JBP2.cPartsUniqueID,JBP2.cPJSSXCode,JBP2.cPChineseName,JBP2.cPInsideCode,JBP2.iPVolume,JBP2.iVolumeDiff,JBP2.cPPlaceOfProduction,JBR.iGreaseCount
				from JSSX_Products a 
                 LEFT join JSSX_BOM_Product AS JBP ON a.iP2R=JBP.iP2R and  (a.JSSXCode = left(JBP.cProJSSXCode,len(a.JSSXCode)) or a.bisEntireBOM = 0 or (a.TrayType = '补用品' and JBP.cProJSSXCode = (select max(JBP1.cProJSSXCode)  from JSSX_Products JPS1 LEFT join JSSX_BOM_Product AS JBP1 ON JPS1.iP2R=JBP1.iP2R left join JSSX_BOM_Relation_Produce as JBR1 on JBR1.cRelationID=JBP1.cRelationID and bIsNowUseParts = 1 and JBR1.bIsDelete = 0 and JBR1.cLineNumber = @cLine  where JPS1.UniqueID = a.UniqueID and JBR1.cPartsUniqueID is not null)) )
				 LEFT JOIN JSSX_BOM_Relation_Produce  AS JBR ON JBR.cRelationID=JBP.cRelationID and bIsNowUseParts = 1 and JBR.bIsDelete = 0 and JBR.cLineNumber = @cLineNumber --and JBR.dEndDate >= GETDATE()
				 LEFT JOIN  JSSX_BOM_Parts AS JBP3 ON JBR.cPartsUniqueID= JBP3.cPartsUniqueID    --转成不带小设变位的过度表
				 LEFT JOIN  JSSX_BOM_Parts AS JBP2 ON JBP3.cPSJSSXCode= JBP2.cPartsUniqueID
				 where  a.UniqueID = @cUniqueID and  JBR.cPartsUniqueID is not null and JBP2.cProcessType is null
					) AS JP      --  JBP2.cProcessType is null 代表不是机械场的东西，机械场的东西会有不同的值
					left join JSSX_BOM_LogisticsFillPro JBLF on JP.UniqueID = JBLF.cUniqueID and JP.iSerialNum = JBLF.iSerialNum  and JP.cPartsUniqueID = JBLF.cPartsUniqueID and JBLF.bisdelete = 0 and JBLF.cLine = @cLineNumber
					left join (	
						SELECT aa.Id,aa.cPartsUniqueID,aa.cPutRack,isnull(aa.iQTYSPend,0) as iQTYSPend,isnull(aa.iLineAmount,0) as iLineAmount FROM (
						select row_number() over (partition by a1.cPartsUniqueID,a1.cPutRack order by a1.Id desc) as iRow, a1.* from JSSX_Logistics_Manifest a1 where a1.bIsEnabled = 0 and a1.cPartsUniqueID != 'PJX0000000' and a1.cWorkSpace = @cLine 
						) aa where aa.iRow = 1   
					) R on jp.cPartsUniqueID = r.cPartsUniqueID and JBLF.cPutRack = r.cPutRack
	   
			where JP.iJPRowNum = 1 and  JBLF.cPutRack != '999' and JBLF.iPartsType = 0
			order by jp.cPInsideCode, JBLF.cPutRack
";

			SqlParameter[] param = {
				 new SqlParameter("@cUniqueID", System.Data.SqlDbType.Char),
				 new SqlParameter("@cLine", System.Data.SqlDbType.Char),
			 };
			param[0].Value = ssPuid;
			param[1].Value = ssLine;

			if (ssPuid != null)
			{
				ds_Residual = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql, param);

				DG_Residual.ItemsSource = ds_Residual.Tables[0].DefaultView;


			}



		}

		/// <summary>
		/// 加载虚拟键盘按钮单击事件
		/// </summary>
		private void BindPreviewMouseUpEvent()
		{
			grqNumKB.RecvData += new NumericKeyboard.RecvDataEventHandler(grqNumKB_RecvData);
		}

		/// <summary>
		/// 虚拟键盘按钮单击事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void textBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			SelTB = sender as TextBox;
			popNumKeyboard.PlacementTarget = SelTB;   //表示Popup控件的放置的位置依赖的对象
			popNumKeyboard.IsOpen = true;
		}

		private void Tb_MSSTART_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!bIsMove)   //没有经过弹框关闭移动选项的时候，不执行此操作
			{
				int iRow = DG_Residual.SelectedIndex;
				if (iRow >= 0)
				{
					if (SelTB != null)
					{
						if (SelTB.Text != "")
						{
							SelTB = sender as TextBox;
							string a = SelTB.Text;
							ds_Residual.Tables[0].Rows[iRow]["端数修改值"] = SelTB.Text;
						}
					}
				}

			}
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

		private void Btn_B_Click(object sender, RoutedEventArgs e)
		{
			SetDG(0);    //0是大部品，对应False

		}

		private void SetDG(int iPartType)
		{
			string sql = @"
					declare @cLineNumber nvarchar(20)  
					select @cLineNumber = LineNumber from JSSX_Line a where a.LineCode = @cLine

					select distinct a.cPartsUniqueID,b.cPInsideCode,b.cPJSSXCode,b.cPChineseName,a.cPutRack,b.iPVolume  + b.iVolumeDiff + a.iDifference as iVolume,isnull(r.iQTYSPend,0) as iQTYSP,'' as 端数修改值 from JSSX_BOM_LogisticsFillPro a
					left join JSSX_BOM_Parts b on a.cPartsUniqueID = b.cPartsUniqueID
					left join (
							SELECT aa.Id,aa.cPartsUniqueID,aa.cPutRack,isnull(aa.iQTYSPend,0) as iQTYSPend,isnull(aa.iLineAmount,0) as iLineAmount FROM (
							select row_number() over (partition by a1.cPartsUniqueID,a1.cPutRack order by a1.Id desc) as iRow, a1.* from JSSX_Logistics_Manifest a1 where a1.bIsEnabled = 0 and a1.cPartsUniqueID != 'PJX0000000' and a1.cWorkSpace = @cLine 
							) aa where aa.iRow = 1   
					) R on a.cPartsUniqueID = r.cPartsUniqueID and a.cPutRack = r.cPutRack
					where a.cLine = @cLineNumber and a.bisdelete = 0 and a.cPutRack != '999' and ((a.iPartsType = 0 and @bPartType = 0) or (@bPartType = 1 and a.iPartsType != 0))
					order by cPInsideCode,cPutRack

";

			SqlParameter[] param = {
				 new SqlParameter("@cLine", System.Data.SqlDbType.Char),
				 new SqlParameter("@bPartType", System.Data.SqlDbType.Char),
			 };
			param[0].Value = ssLine;
			param[1].Value = iPartType;


			try
			{
				ds_Residual = sqlHelp.ExecuteDataSet(sqlHelp.SQLCon, CommandType.Text, sql, param);

				DG_Residual.ItemsSource = ds_Residual.Tables[0].DefaultView;

			}
			catch (Exception ex)
			{
				MessageBox.Show("获取全部部品失败，请重试，多次失败请联络IT");
				throw;
			}


		}



		private void Btn_S_Click(object sender, RoutedEventArgs e)
		{
			SetDG(1);    //1是小部品，对应True

		}

		private void Btn_SubMit_Click(object sender, RoutedEventArgs e)
		{
			string sql = "insert into JSSX_Logistics_Manifest(cBrNumber, cWorkSpace, cJICode, iAmount, cPartsUniqueID, iQTYSP, iSNP, iQTYSPend, cPutRack, cNote) values ";

			int i = 0;
			foreach (DataRow item in ds_Residual.Tables[0].Rows)
			{
				if (item["端数修改值"].ToString() != "" && item["cPartsUniqueID"].ToString() != "")
				{
					i++;
					sql = sql + "('JBR' + CONVERT(nvarchar(8), GETDATE(), 112), @cLine, '" + item["cPInsideCode"].ToString() + "', 0, '" + item["cPartsUniqueID"].ToString() + "', '" + item["iQTYSP"].ToString() + "', '" + item["iVolume"].ToString() + "', '" + item["端数修改值"].ToString() + "', '" + item["cPutRack"].ToString() + "', '生产线端数调整'),";

				}
				//select JP.cPartsUniqueID,JP.cPInsideCode,JP.cPJSSXCode,JP.cPChineseName,isnull(JBLF.cPutRack,0) as cPutRack,R.iQTYSPend as iQTYSP,'' as 端数修改值

				//('JBR' + CONVERT(nvarchar(8), GETDATE(), 112), @cLine, cJICode, iAmount, cPartsUniqueID, iQTYSP, iSNP, iQTYSPend, cPutRack, cNote)

			}
			sql = sql.Substring(0, sql.Length - 1);


			SqlParameter[] param = {
				 new SqlParameter("@cLine", System.Data.SqlDbType.Char),
			 };
			param[0].Value = ssLine;

			if (i > 0)   //有明细信息才写入数据，不然SQL语句会报错
			{
                try
                {
					int iCount = sqlHelp.ExecuteNonQuery(sqlHelp.SQLCon, CommandType.Text, sql, param);

					if (iCount == 0)
					{
						MessageBox.Show("注意：提交失败，请联络相关人士。");
					}
					else
					{
						MessageBox.Show(i.ToString() + "个背番，端数提交成功。");

					}
				}
                catch (Exception ex)
                {
					MessageBox.Show("写入数据发生异常，请重试，多次失败请联络IT");

					return;
                }

			}
			else
			{
				MessageBox.Show("没有修改任何行，提交无效。");

			}

		}
	}
}
