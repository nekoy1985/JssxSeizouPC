using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JssxSeizouPC;
namespace UnitTestProject2
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            MainWindow com = new MainWindow();
            com.DataAnalysis("fds");
        }
    }
}
