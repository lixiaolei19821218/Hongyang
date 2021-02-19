using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Hongyang
{
    /// <summary>
    /// PINominal.xaml 的交互逻辑
    /// </summary>
    public partial class PINominal : Page
    {
        public PINominal()
        {
            InitializeComponent();
            
            //读取后处理文件
            foreach(string file in System.IO.Directory.GetFiles(@"Pmoptz"))
            {
                string opt = System.IO.Path.GetFileNameWithoutExtension(file);
                if (opt != ConfigurationManager.AppSettings["totalPmoptz"])
                {
                    cbxOPT.Items.Add(opt);
                }                
            }
            cbxOPT.SelectedIndex = 0;
        }
    }
}
