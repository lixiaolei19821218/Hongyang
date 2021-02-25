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

            //Refresh();
        }

        public void Apply(string tpName)
        {
            PowerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            Session = PowerMILL.ActiveProject;

            PowerMILL.Execute($"EDIT PAR 'Connections.Link[0].ProbingType' '{(cbxType0.SelectedItem as ComboBoxItem).Tag}'");
            PowerMILL.Execute("EDIT PAR 'Connections.Link[0].ApplyConstraints' '1'");
            PowerMILL.Execute("EDIT PAR 'Connections.Link[0].Constraint[0].Type' 'distance'");
            PowerMILL.Execute("EDIT PAR 'Toolpath.Connections.Link.First.Constraint[0].Distance.Function' 'less_than");
            PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.First.Constraint[0].Distance.Value' \"{tbx11S.Text}\"");
            
            return;
            //下面暂时不用
            if (chx1stConstraint.IsChecked ?? false)
            {
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[0].ApplyConstraints' '1'");
                string tag = (cbx1stLink1stConstraint.SelectedItem as ComboBoxItem).Tag.ToString();
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[0].Constraint[0].Type' '{tag}'");
                if (tag != "unspecified")
                {
                    PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.First.Constraint[0].{tag.Replace("_", "")}.Function' '{(cbx11S.SelectedItem as ComboBoxItem).Tag}'");
                    PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.First.Constraint[0].{tag.Replace("_", "")}.Value' \"{tbx11S.Text}\"");
                    tag = (cbx1stLink2ndConstraint.SelectedItem as ComboBoxItem).Tag.ToString();
                    PowerMILL.Execute($"EDIT PAR 'Connections.Link[0].Constraint[1].Type' '{tag}'");
                    if (tag != "unspecified")
                    {
                        PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.First.Constraint[1].{tag.Replace("_", "")}.Function' '{(cbx12S.SelectedItem as ComboBoxItem).Tag}'");
                        PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.First.Constraint[1].{tag.Replace("_", "")}.Value' \"{tbx12S.Text}\"");
                    }
                }                 
            }
            else
            {
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[0].ApplyConstraints' '0'");
            }

            PowerMILL.Execute($"EDIT PAR 'Connections.Link[1].ProbingType' '{(cbxType1.SelectedItem as ComboBoxItem).Tag}'");
            if (chx2ndConstraint.IsChecked ?? false)
            {
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[1].ApplyConstraints' '1'");
                string tag = (cbx2ndLink1stConstraint.SelectedItem as ComboBoxItem).Tag.ToString();
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[1].Constraint[0].Type' '{tag}'");
                if (tag != "unspecified")
                {
                    PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.Second.Constraint[0].{tag.Replace("_", "")}.Function' '{(cbx21S.SelectedItem as ComboBoxItem).Tag}'");
                    PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.Second.Constraint[0].{tag.Replace("_", "")}.Value' \"{tbx21S.Text}\"");
                    tag = (cbx2ndLink2ndConstraint.SelectedItem as ComboBoxItem).Tag.ToString();
                    PowerMILL.Execute($"EDIT PAR 'Connections.Link[1].Constraint[1].Type' '{tag}'");
                    if (tag != "unspecified")
                    {
                        PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.Second.Constraint[1].{tag.Replace("_", "")}.Function' '{(cbx22S.SelectedItem as ComboBoxItem).Tag}'");
                        PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.Second.Constraint[1].{tag.Replace("_", "")}.Value' \"{tbx22S.Text}\"");
                    }
                }
            }
            else
            {
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[1].ApplyConstraints' '0'");
            }

            PowerMILL.Execute($"EDIT PAR 'Connections.DefaultLink[0].ProbingType' '{(cbxTypeDef.SelectedItem as ComboBoxItem).Tag}'");

            if (chxGouge.IsChecked ?? false)
            {
                PowerMILL.Execute("EDIT TOOLPATH LEADS GOUGECHECK Y");
            }
            else
            {
                PowerMILL.Execute("EDIT TOOLPATH LEADS GOUGECHECK N");
            }
        }

        public void Refresh(string tpName)
        {  
            if (string.IsNullOrEmpty(tpName))
            {
                return;
            }

            string type;

            type = PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.Link[0].ProbingType\"").ToString();
            for (int i = 0; i < cbxType0.Items.Count; i++)
            {
                ComboBoxItem item = cbxType0.Items[i] as ComboBoxItem;
                if (item.Tag.ToString() == type)
                {
                    cbxType0.SelectedIndex = i;
                    break;
                }
            }
            tbx11S.Text = PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.Link.First.Constraint[0].Distance.Value\"").ToString();

            return;
            //下面的暂时不用；
            if (PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.Link[0].ApplyConstraints\"").ToString() == "1")
            {
                chx1stConstraint.IsChecked = true;
            }
            else
            {
                chx1stConstraint.IsChecked = false;
            }

            type = PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.Link[0].Constraint[0].Type\"").ToString();
            for (int i = 0; i < cbx1stLink1stConstraint.Items.Count; i++)
            {
                ComboBoxItem item = cbx1stLink1stConstraint.Items[i] as ComboBoxItem;
                if (item.Tag.ToString() == type)
                {
                    cbx1stLink1stConstraint.SelectedIndex = i;
                    break;
                }
            }

            type = PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.Link[1].ProbingType\"").ToString();
            for (int i = 0; i < cbxType1.Items.Count; i++)
            {
                ComboBoxItem item = cbxType1.Items[i] as ComboBoxItem;
                if (item.Tag.ToString() == type)
                {
                    cbxType1.SelectedIndex = i;
                    break;
                }
            }

            if (PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.Link[1].ApplyConstraints\"").ToString() == "1")
            {
                chx2ndConstraint.IsChecked = true;
            }
            else
            {
                chx2ndConstraint.IsChecked = false;
            }

            type = PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.Link[1].Constraint[0].Type\"").ToString();
            for (int i = 0; i < cbx2ndLink1stConstraint.Items.Count; i++)
            {
                ComboBoxItem item = cbx2ndLink1stConstraint.Items[i] as ComboBoxItem;
                if (item.Tag.ToString() == type)
                {
                    cbx2ndLink1stConstraint.SelectedIndex = i;
                    break;
                }
            }

            type = PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.DefaultLink[0].ProbingType\"").ToString();
            for(int i = 0; i < cbxType1.Items.Count; i++)
            {
                ComboBoxItem item = cbxTypeDef.Items[i] as ComboBoxItem;
                if (item.Tag.ToString() == type)
                {
                    cbxTypeDef.SelectedIndex = i;
                    break;
                }
            }

            if (PowerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Connections.gougecheck\"").ToString() == "1")
            {
                chxGouge.IsChecked = true;
            }
            else
            {
                chxGouge.IsChecked = false;
            }            
        }

        private void CbxProbing_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                PowerMILL.Execute($"EDIT PAR 'Connections.Link[{comboBox.Tag}].ProbingType' '{comboBoxItem.Tag}'");
            }            
        }

        private void CbxConstraint_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                if (comboBox.Tag.ToString() == "Default")
                {
                    PowerMILL.Execute($"EDIT PAR 'Connections.DefaultLink[0].ProbingType' '{comboBoxItem.Tag}'");
                }
                else
                {
                    CheckBox checkBox = ((comboBox.Parent as Grid).Parent as GroupBox).Header as CheckBox;
                    PowerMILL.Execute($"EDIT PAR 'Connections.Link[{checkBox.Tag}].Constraint[{comboBox.Tag}].Type' '{comboBoxItem.Tag}'");
                }
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (PowerMILL != null)
            {
                CheckBox checkBox = sender as CheckBox;
                if (checkBox.Tag.ToString() == "GOUGECHECK")
                {
                    string v = checkBox.IsChecked ?? false ? "Y" : "N";
                    PowerMILL.Execute($"EDIT TOOLPATH LEADS GOUGECHECK {v}");
                }
                else
                {
                    string v = checkBox.IsChecked ?? false ? "1" : "0";
                    PowerMILL.Execute($"EDIT PAR 'Connections.Link[{checkBox.Tag}].ApplyConstraints' '{v}'");
                }
            }
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (PowerMILL != null)
            {
                TextBox textBox = sender as TextBox;               
                CheckBox checkBox = ((textBox.Parent as Grid).Parent as GroupBox).Header as CheckBox;
                string seq = checkBox.Tag.ToString() == "0" ? "First" : "Second";                
                int count = VisualTreeHelper.GetChildrenCount(textBox.Parent);
                for (int i = 0; i < count; i++)
                {
                    object child = VisualTreeHelper.GetChild(textBox.Parent, i);
                    ComboBox comboBox = child as ComboBox;
                    if (comboBox != null && comboBox.Name.Contains("Constraint") && comboBox.Tag.ToString() == textBox.Tag.ToString())
                    {
                        ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                        string type = comboBoxItem.Tag.ToString().Replace("_", "");
                        PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.{seq}.Constraint[{textBox.Tag}].{type}.Value' \"{textBox.Text}\"");
                        break;
                    }
                }               
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                ComboBox f = sender as ComboBox;
                CheckBox checkBox = ((f.Parent as Grid).Parent as GroupBox).Header as CheckBox;
                string seq = checkBox.Tag.ToString() == "0" ? "First" : "Second";
                int count = VisualTreeHelper.GetChildrenCount(f.Parent);
                for (int i = 0; i < count; i++)
                {
                    object child = VisualTreeHelper.GetChild(f.Parent, i);
                    ComboBox comboBox = child as ComboBox;
                    if (comboBox != null && comboBox.Name.Contains("Constraint") && comboBox.Tag.ToString() == f.Tag.ToString())
                    {
                        ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                        string type = comboBoxItem.Tag.ToString().Replace("_", "");
                        PowerMILL.Execute($"EDIT PAR 'Toolpath.Connections.Link.{seq}.Constraint[{f.Tag}].{type}.Function' \"{(f.SelectedItem as ComboBoxItem).Tag}\"");
                        break;
                    }
                }
            }
        }
    }
}
