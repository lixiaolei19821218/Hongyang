using Autodesk.ProductInterface.PowerMILL;
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
    /// Link.xaml 的交互逻辑
    /// </summary>
    public partial class Link : Page
    {
        public PMAutomation PowerMILL;
        public PMProject Session;

        public Link()
        {
            InitializeComponent();

            PowerMILL = (Application.Current.MainWindow as MainWindow).PowerMILL;
            Session = (Application.Current.MainWindow as MainWindow).Session;
        }

        private void CbxProbing_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[{comboBox.Tag}].ProbingType' '{comboBox.Text}'");
            }            
        }

        private void CbxConstraint_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                
            }
        }
    }
}
