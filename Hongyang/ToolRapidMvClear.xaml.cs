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
    /// ToolRapidMvClear.xaml 的交互逻辑
    /// </summary>
    public partial class ToolRapidMvClear : Page
    {
        public PMAutomation PowerMILL;
        public PMProject Session;

        public ToolRapidMvClear()
        {
            InitializeComponent();

            PowerMILL = (Application.Current.MainWindow as MainWindow).PowerMILL;
            Session = (Application.Current.MainWindow as MainWindow).Session;
        }

        private void CbxMoveDir_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
        }

        private void CbxMoveDir_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;            
            PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK MOVE_DIR {(comboBox.SelectedItem as ComboBoxItem).Tag}");
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                TextBox textBox = sender as TextBox;
                PowerMILL.Execute($"EDIT TOOLPATH LEADS {textBox.Tag}  \"{textBox.Text}\"");
            }
        }

        private void CbxSkimPlane_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                PowerMILL.Execute($"EDIT TOOLPATH LEADS SKIMPLANE TYPE {(comboBox.SelectedItem as ComboBoxItem).Tag}");
            }
        }

        private void CbxExtend_Click(object sender, RoutedEventArgs e)
        {
            string v = cbxExtend.IsChecked ?? false ? "Y" : "N";
            PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK EXTEND_MOVE {v}");
        }

        private void TxtMaxExtension_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK MAX_EXTENSION \"{txtMaxExtension.Text}\"");
            }
        }

        private void CbxArcFit_Click(object sender, RoutedEventArgs e)
        {
            string v = cbxArcFit.IsChecked ?? false ? "Y" : "N";
            PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK ARCFIT {v}");
        }

        private void TxtArcFit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK ARCFIT_RAD \"{txtArcFit.Text}\"");
            }
        }
    }
}
