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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JssxSeizouPC
{
    /// <summary>
    /// NumericKeyboard.xaml 的交互逻辑
    /// </summary>
    public partial class NumericKeyboard : UserControl
    {
        public delegate void RecvDataEventHandler(object sender, int nDig);
        public event RecvDataEventHandler RecvData;

        public NumericKeyboard()
        {
            InitializeComponent();
        }


        #region 属性
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericKeyboard), new UIPropertyMetadata(0));
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register("IsChecked", typeof(bool), typeof(NumericKeyboard), new UIPropertyMetadata(false));
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        #endregion

        //退格
        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x08);
        }

        public void SendNumber(int num)
        {
            if (RecvData != null)
            {
                RecvData(this, num);
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x32);
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x33);
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x34);
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x35);
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x36);
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x37);
        }

        private void button8_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x38);
        }

        private void button9_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x39);
        }

        private void button0_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x30);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x31);
        }

        //确认
        public   void btnClose_Click(object sender, RoutedEventArgs e)
        {
            SendNumber(0x13);
        }
    }
}
