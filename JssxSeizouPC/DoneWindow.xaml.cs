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

namespace JssxSeizouPC
{

    /// <summary>
    /// DoneWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DoneWindow : Window
    {
        private static System.Windows.Threading.DispatcherTimer readDataTimer = new System.Windows.Threading.DispatcherTimer();
        public DoneWindow()
        {
            InitializeComponent();
            readDataTimer.Tick += new EventHandler(timeCycle);
            readDataTimer.Interval = new TimeSpan(0, 0, 0, 1);
            readDataTimer.Start();
            Tb_Messagebox.Text = "扫描成功。";
        }
        public void timeCycle(object sender, EventArgs e)
        {
            readDataTimer.Stop();
            this.Close();
        }
    }
}
