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
    /// Interaction logic for LinkFilter.xaml
    /// </summary>
    public partial class LinkFilter : Page
    {
        private PMAutomation powerMILL;
        private PMProject session;

        public LinkFilter()
        {
            InitializeComponent();

            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;

            //Refresh();
        }

        public void Apply()
        {
            if (chxUseSetting.IsChecked ?? false)
            {
                powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.UseToolpathSettings' 1");
            }
            else
            {
                powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.UseToolpathSettings' 0");
            }
            if (chxDist.IsChecked ?? false)
            {
                powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.Rapid.Active' 1");
                powerMILL.Execute($"EDIT PAR 'Connections.PointDistribution.Rapid.Filter.Type' '{(cbxType.SelectedItem as ComboBoxItem).Tag}'");
                powerMILL.Execute($"EDIT PAR 'Connections.PointDistribution.Rapid.Filter.Factor' \"{tbxFactor.Text}\"");
                if (chxDistance.IsChecked ?? false)
                {
                    powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.Rapid.MaxDistanceBetweenPoints.Active' 1");
                    powerMILL.Execute($"EDIT PAR 'Connections.PointDistribution.Rapid.MaxDistanceBetweenPoints.Value' \"{tbxDistance.Text}\"");
                }
                else
                {
                    powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.Rapid.MaxDistanceBetweenPoints.Active' 0");
                }
                if (chxAngle.IsChecked ?? false)
                {
                    powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.Rapid.MaxAngleBetweenPoints.Active' 1");
                    powerMILL.Execute($"EDIT PAR 'Connections.PointDistribution.Rapid.MaxAngleBetweenPoints.Value' \"{tbxAngle.Text}\"");
                }
                else
                {
                    powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.Rapid.MaxAngleBetweenPoints.Active' 0");
                }
            }
            else
            {
                powerMILL.Execute("EDIT PAR 'Connections.PointDistribution.Rapid.Active' 0");
            }
            if (chxGouge.IsChecked ?? false)
            {
                powerMILL.Execute("EDIT TOOLPATH LEADS GOUGECHECK Y");
            }
            else
            {
                powerMILL.Execute("EDIT TOOLPATH LEADS GOUGECHECK N");
            }
        }

        public void Refresh()
        {
            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.UseToolpathSettings\"").ToString() == "1")
            {
                chxUseSetting.IsChecked = true;
            }
            else
            {
                chxUseSetting.IsChecked = false;
            }

            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.rapid.active\"").ToString() == "1")
            {
                chxDist.IsChecked = true;
            }
            else
            {
                chxDist.IsChecked = false;
            }

            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.rapid.MaxAngleBetweenPoints.active\"").ToString() == "1")
            {
                chxAngle.IsChecked = true;
            }
            else
            {
                chxAngle.IsChecked = false;
            }
            tbxAngle.Text = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.rapid.MaxAngleBetweenPoints.Value\"").ToString();

            tbxFactor.Text = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.rapid.Filter.Factor\"").ToString();
            string type = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.rapid.Filter.type\"").ToString();
            for (int i = 0; i < cbxType.Items.Count; i++)
            {
                ComboBoxItem item = cbxType.Items[i] as ComboBoxItem;
                if (item.Tag.ToString().ToLower() == type.ToLower())
                {
                    cbxType.SelectedIndex = i;
                    break;
                }
            }

            tbxDistance.Text = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.rapid.MaxDistanceBetweenPoints.value\"").ToString();
            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.PointDistribution.rapid.MaxDistanceBetweenPoints.active\"").ToString() == "1")
            {
                chxDistance.IsChecked = true;
            }
            else
            {
                chxDistance.IsChecked = false;
            }

            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').Connections.gougecheck\"").ToString() == "1")
            {
                chxGouge.IsChecked = true;
            }
            else
            {
                chxGouge.IsChecked = false;
            }
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox.Tag.ToString() == "GOUGECHECK")
            {
                string v = checkBox.IsChecked ?? false ? "Y" : "N";
                powerMILL.Execute($"EDIT TOOLPATH LEADS GOUGECHECK {v}");
            }
            else
            {
                string v = checkBox.IsChecked ?? false ? "1" : "0";
                powerMILL.Execute($"EDIT PAR 'Connections.PointDistribution.{checkBox.Tag}' {v}");
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            if (powerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                string tag = (comboBox.SelectedItem as ComboBoxItem).Tag.ToString();

                powerMILL.Execute($"EDIT PAR 'Connections.PointDistribution.Rapid.Filter.Type' '{tag}'");

                switch (tag)
                {
                    case "strip":
                    case "strip_smash_arcs":
                        gpbDistance.IsEnabled = true;
                        gpbAngle.IsEnabled = false;
                        break;
                    case "redistribute":
                        gpbDistance.IsEnabled = true;
                        gpbAngle.IsEnabled = true;
                        break;
                    case "arcfit":
                        gpbDistance.IsEnabled = false;
                        gpbAngle.IsEnabled = false;
                        break;
                }
            }           
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (powerMILL != null)
            {
                TextBox textBox = sender as TextBox;
                powerMILL.Execute($"EDIT PAR 'Connections.PointDistribution.Rapid.{textBox.Tag}' \"{textBox.Text}\"");
            }
        }
    }
}
