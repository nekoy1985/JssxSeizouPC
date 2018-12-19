using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace JssxSeizouPC
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex mutex;


        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Application.Current.ApplyTheme("ExpressionDark");
            bool ret;
            mutex = new System.Threading.Mutex(true, "JssxSeizouPC", out ret);

            if (!ret)
            {
                MessageBox.Show("不要重复打开多个程序。");
                Environment.Exit(0);
            }


        }

        protected override void OnStartup(StartupEventArgs e)
        {

            SplashScreen s = new SplashScreen("Strat.jpg");
            s.Show(false);
            s.Close(new TimeSpan(0, 0, 5));
            System.Threading.Thread.Sleep(3000);
            base.OnStartup(e);
        }
    }
}
