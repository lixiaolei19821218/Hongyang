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
