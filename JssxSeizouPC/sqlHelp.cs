using System.Data;
using System.Data.SqlClient;
using JssxSeizouPC;
using System;
using System.Threading;
using System.Windows;





/// <summary>
///sqlHelp 的摘要说明
/// </summary>
public class sqlHelp
{
    public static int iRedoTimes=0;

   
    //获取数据库连接字符串，其属于静态变量且只读，项目中所有文档可以直接使用，但不能修改
    //stdInfoConnectionString为连接字符串的名称 ，在后面添加连接字符串后进行修改
    public static readonly string ConnectionStringLocalTransaction = "Data Source=10.91.91.245;Initial Catalog=JSSX;Persist Security Info=True;User ID=Staff;Password=66666666";
    /// <summary>
    ///执行一个不需要返回值的SqlCommand命令，通过指定专用的连接字符串。
    /// 使用参数数组形式提供参数列表 
    /// </summary>
    /// <remarks>
    /// 使用示例：
    ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
    /// </remarks>
    /// <param name="connectionString">一个有效的数据库连接字符串</param>
    /// <param name="commandType">SqlCommand命令类型 (存储过程， T-SQL语句， 等等。)</param>
    /// <param name="commandText">存储过程的名字或者 T-SQL 语句</param>
    /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
    /// <returns>返回一个数值表示此SqlCommand命令执行后影响的行数</returns>
    public static int ExecuteNonQuery(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
    {
        try
        {
            iRedoTimes++;
            Mylog.Info(cmdText);
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                //通过PrePareCommand方法将参数逐个加入到SqlCommand的参数集合中
                PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                int val = cmd.ExecuteNonQuery();
                //清空SqlCommand中的参数列表
                cmd.Parameters.Clear();
                return val;
            }
        }
        catch (Exception ex)
        {
            Mylog.Error(ex.Message);
            if (ex.ToString().Contains("死锁") || ex.ToString().Contains("无法访问") || ex.ToString().Contains("超时") || ex.ToString().Contains("传输级"))
            {
                if (iRedoTimes % 10!=0)
                {
                    return ExecuteNonQuery(connectionString, cmdType, cmdText, commandParameters);
                }
                // MessageBox.Show("网络异常，点击确定后系统自动重试。如果此异常长时间没有解除，请检查设备或者联系13164846310。\r\b" + ex.ToString().Substring(0, 200));
               
            }
            else
            {
                Mylog.Error(ex.ToString());
            }
            return 0;
        }

    }

    /// <summary>
    /// 执行一条返回结果集的SqlCommand命令，通过专用的连接字符串。
    /// 使用参数数组提供参数
    /// </summary>
    /// <remarks>
    /// 使用示例：  
    ///  SqlDataReader r = ExecuteReader(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
    /// </remarks>
    /// <param name="connectionString">一个有效的数据库连接字符串</param>
    /// <param name="commandType">SqlCommand命令类型 (存储过程， T-SQL语句， 等等。)</param>
    /// <param name="commandText">存储过程的名字或者 T-SQL 语句</param>
    /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
    /// <returns>返回一个包含结果的SqlDataReader</returns>
    public static SqlDataReader ExecuteReader(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
    {
        SqlCommand cmd = new SqlCommand();
        SqlConnection conn = new SqlConnection(connectionString);
        // 在这里使用try/catch处理是因为如果方法出现异常，则SqlDataReader就不存在，
        //CommandBehavior.CloseConnection的语句就不会执行，触发的异常由catch捕获。
        //关闭数据库连接，并通过throw再次引发捕捉到的异常。
        try
        {
            PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
            SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            cmd.Parameters.Clear();
            return rdr;
        }
        catch (Exception ex)
        {
            Mylog.Error(ex.Message);
            conn.Close();
            throw;
        }
    }

    /// <summary>
    /// 执行一条返回第一条记录第一列的SqlCommand命令，通过专用的连接字符串。 
    /// 使用参数数组提供参数
    /// </summary>
    /// <remarks>
    /// 使用示例：  
    ///  Object obj = ExecuteScalar(connString, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
    /// </remarks>
    /// <param name="connectionString">一个有效的数据库连接字符串</param>
    /// <param name="commandType">SqlCommand命令类型 (存储过程， T-SQL语句， 等等。)</param>
    /// <param name="commandText">存储过程的名字或者 T-SQL 语句</param>
    /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
    /// <returns>返回一个object类型的数据，可以通过 Convert.To{Type}方法转换类型</returns>
    public static object ExecuteScalar(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
    {
        SqlCommand cmd = new SqlCommand();
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
            object val = cmd.ExecuteScalar();
            cmd.Parameters.Clear();
            return val;
        }
    }
    /// <summary>
    /// 为执行命令准备参数
    /// </summary>
    /// <param name="cmd">SqlCommand 命令</param>
    /// <param name="conn">已经存在的数据库连接</param>
    /// <param name="trans">数据库事物处理</param>
    /// <param name="cmdType">SqlCommand命令类型 (存储过程， T-SQL语句， 等等。)</param>
    /// <param name="cmdText">Command text，T-SQL语句 例如 Select * from Products</param>
    /// <param name="cmdParms">返回带参数的命令</param>
    private static void PrepareCommand(SqlCommand cmd, SqlConnection conn, SqlTransaction trans, CommandType cmdType, string cmdText, SqlParameter[] cmdParms)
    {
        //Mylog.Info(cmdText);
        //判断数据库连接状态
        if (conn.State != ConnectionState.Open)
            conn.Open();
        cmd.Connection = conn;
        cmd.CommandText = cmdText;
        //判断是否需要事物处理
        if (trans != null)
            cmd.Transaction = trans;
        cmd.CommandType = cmdType;
        if (cmdParms != null)
        {
            foreach (SqlParameter parm in cmdParms)
                cmd.Parameters.Add(parm);
        }
    }

    /// <summary>
    /// return a dataset
    /// </summary>
    /// <param name="connectionString">一个有效的数据库连接字符串</param>
    /// <param name="cmdType">SqlCommand命令类型 (存储过程， T-SQL语句， 等等。)</param>
    /// <param name="cmdText">存储过程的名字或者 T-SQL 语句</param>
    /// <param name="commandParameters">以数组形式提供SqlCommand命令中用到的参数列表</param>
    /// <returns>return a dataset</returns>

    public static DataSet ExecuteDataSet(string connectionString, CommandType cmdType, string cmdText, params SqlParameter[] commandParameters)
    {
        SqlConnection conn = new SqlConnection(connectionString);
        SqlCommand cmd = new SqlCommand();
        Mylog.Info(cmdText);
        try
        {
            PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
            SqlDataAdapter da = new SqlDataAdapter();
            DataSet ds = new DataSet();
            da.SelectCommand = cmd;
            da.Fill(ds);
            return ds;
        }
        catch (Exception ex)
        {
            Mylog.Error(ex.Message);
            if (ex.ToString().Contains("死锁") || ex.ToString().Contains("无法访问") || ex.ToString().Contains("超时") || ex.ToString().Contains("传输级"))
            {
                if (iRedoTimes % 10!=0)
                {
                    return ExecuteDataSet(connectionString, cmdType, cmdText, commandParameters);
                }
                // MessageBox.Show("网络异常，点击确定后系统自动重试。如果此异常长时间没有解除，请检查设备或者联系13164846310。\r\b" + ex.ToString().Substring(0, 200));
               
            }
            else
            {
                Mylog.Error(ex.ToString());
            }
            return null;

        }
        finally
        {
            conn.Close();

        }
    }

    /// <summary>
    /// 执行事务
    /// </summary>
    /// <param name="connectionString">一个有效的数据库连接字符串</param>
    /// <param name="cmdType">SqlCommand命令类型 (存储过程， T-SQL语句， 等等。)</param> 
    /// <returns>return a bool</returns>
    public static bool ExecuteSqlTran(string connectionString, string strSQL)
    {

        char[] de = { ';' };
        string strChildSQL;
        int i;
        string[] strSQLArr = strSQL.Split(de);
        bool IsControll = true;
        SqlConnection conn = new SqlConnection(connectionString);
        SqlCommand myCommand = new SqlCommand();
        SqlTransaction myTrans=null;
        Mylog.Info(strSQL);
        try
        {
            conn.Open();
            
            myTrans = conn.BeginTransaction();
            myCommand.Connection = conn; 
            myCommand.Transaction = myTrans;
            for (i = 0; i < strSQLArr.Length; i++)
            {
                strChildSQL = strSQLArr[i];
                myCommand.CommandText = strChildSQL;
                myCommand.ExecuteNonQuery();
            }
            myTrans.Commit();
            IsControll =true;
        }
        catch (Exception ex)
        {
            Mylog.Error(ex.Message);
            try
            {
                IsControll = false;
                if (myTrans!=null)
                {
                    myTrans.Rollback();
                }
               
                //ExecuteSqlTran(connectionString, strSQL);
            }
            catch (SqlException sex)
            {
                Mylog.Error(sex.Message);
            }
        }
        finally
        {
            conn.Close();
        }
        return IsControll;
    }


}
