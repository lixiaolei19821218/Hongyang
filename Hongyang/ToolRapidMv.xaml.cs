using System;
using System.Collections.Generic;
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
    /// ToolRapidMv.xaml 的交互逻辑
    /// </summary>
    public partial class ToolRapidMv : Page
    {
        public ToolRapidMv()
        {
            InitializeComponent();
            //print par  "entity('toolpath', 'CAO1').Rapid.CalculateDimensions.RapidClearance 快进间隙
            //print par  "entity('toolpath', 'CAO1').Rapid.Plane.Distance" 快进高度
        }
    }
}
