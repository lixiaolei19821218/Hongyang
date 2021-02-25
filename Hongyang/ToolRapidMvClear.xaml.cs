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

            PowerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            Session = PowerMILL.ActiveProject;

            //Refresh();
        }

        public void Apply()
        {
            PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK MOVE_DIR {(cbxMoveDir.SelectedItem as ComboBoxItem).Tag}");
            PowerMILL.Execute($"EDIT TOOLPATH LEADS RETRACTDIST \"{tbxRetract.Text}\"");
            PowerMILL.Execute($"EDIT TOOLPATH LEADS APPROACHDIST \"{tbxApproach.Text}\"" );
            if (cbxExtend.IsChecked ?? false)
            {
                PowerMILL.Execute("EDIT TOOLPATH LEADS LINK EXTEND_MOVE Y");
                PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK MAX_EXTENSION \"{txtMaxExtension.Text}\"");
            }
            else
            {
                PowerMILL.Execute("EDIT TOOLPATH LEADS LINK EXTEND_MOVE N");
            }
            PowerMILL.Execute($"EDIT TOOLPATH LEADS SKIMPLANE TYPE {(cbxSkimPlane.SelectedItem as ComboBoxItem).Tag}");
            if (cbxArcFit.IsChecked ?? false)
            {
                PowerMILL.Execute("EDIT TOOLPATH LEADS LINK ARCFIT Y");
                PowerMILL.Execute($"EDIT TOOLPATH LEADS LINK ARCFIT_RAD \"{txtArcFit.Text}\"");
            }
            else
            {
                PowerMILL.Execute("EDIT TOOLPATH LEADS LINK ARCFIT N");
            }
            PowerMILL.Execute($"EDIT TOOLPATH LEADS SKIMDIST \"{txtSkimDir.Text}\"");
            PowerMILL.Execute($"EDIT TOOLPATH LEADS RADIAL_CLEARANCE {txtClearance.Text}");
        }

        public void Refresh()
        {
            string direction = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.MoveDirection\"").ToString().Replace("_", "");
            for (int i = 0; i < cbxMoveDir.Items.Count; i++)
            {
                ComboBoxItem item = cbxMoveDir.Items[i] as ComboBoxItem;
                if (direction.ToLower() == item.ToString().ToLower())
                {
                    cbxMoveDir.SelectedIndex = i;
                    break;
                }
            }

            tbxRetract.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.RetractDistance\"").ToString();
            tbxApproach.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.ApproachDistance\"").ToString();

            if (PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.ExtendMove\"").ToString() == "1")
            {
                cbxExtend.IsChecked = true;
            }
            else
            {
                cbxExtend.IsChecked = false;
            }
            txtMaxExtension.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.MaxMoveExtension\"").ToString();

            string type = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.SkimPlane.type\"").ToString();
            for (int i = 0; i < cbxSkimPlane.Items.Count; i++)
            {
                ComboBoxItem item = cbxSkimPlane.Items[i] as ComboBoxItem;
                if (item.Tag.ToString().ToLower() == type.ToLower())
                {
                    cbxSkimPlane.SelectedIndex = i;
                    break;
                }
            }

            if (PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.ArcFittingRadius.active\"").ToString() == "1")
            {
                cbxArcFit.IsChecked = true;
            }
            else
            {
                cbxArcFit.IsChecked = false;
            }
            txtArcFit.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.ArcFittingRadius.value\"").ToString();

            txtSkimDir.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.SkimDistance\"").ToString();
            txtClearance.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.RadialClearance\"").ToString();
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
