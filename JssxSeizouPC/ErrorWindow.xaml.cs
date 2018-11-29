using System;
using System.Collections.Generic;
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
using System.Configuration;

namespace JssxSeizouPC
{
    /// <summary>
    /// ErrorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorWindow : Window
    {
        private static System.Windows.Threading.DispatcherTimer readDataTimer = new System.Windows.Threading.DispatcherTimer();
        private int co = 0;
        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        public ErrorWindow(string Mes)
        {
            InitializeComponent();
            Tb_Messagebox.Text = Mes;
            readDataTimer.Tick += new EventHandler(timeCycle);
            readDataTimer.Interval = new TimeSpan(0, 0, 0, 1);
            readDataTimer.Start();
        }
        public void timeCycle(object sender, EventArgs e)
        {
            if (co%2==0)
            {
                Tb_Messagebox.Foreground = System.Windows.Media.Brushes.Black;
            }
            else  
            {
                Tb_Messagebox.Foreground = System.Windows.Media.Brushes.Red;
            }
            if(co == 4)
            {
                readDataTimer.Stop();
                this.Close();
                 
            }
            co++;
            
          
        }
    }
}
