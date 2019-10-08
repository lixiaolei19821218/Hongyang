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
    /// Interaction logic for SPoint.xaml
    /// </summary>
    public partial class SPoint : Page
    {
        private PMAutomation powerMILL;
        private PMProject session;

        public SPoint()
        {
            InitializeComponent();

            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (powerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                powerMILL.Execute($"EDIT TOOLPATH START {comboBox.Tag} {comboBoxItem.Tag}");

                if (comboBox.Tag.ToString() == "TYPE")
                {
                    if (comboBoxItem.Tag.ToString() == "ABSOLUTE")
                    {
                        chxMove.IsEnabled = true;
                        gpbPosition.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        chxMove.IsEnabled = false;
                        gpbPosition.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        private void ChxSeparate_Click(object sender, RoutedEventArgs e)
        {
            if (powerMILL != null)
            {
                CheckBox checkBox = sender as CheckBox;
                string v = checkBox.IsChecked ?? false ? "Y" : "N";
                powerMILL.Execute($"EDIT TOOLPATH START {checkBox.Tag} {v}");
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (powerMILL != null)
            {
                TextBox textBox = sender as TextBox;               
                powerMILL.Execute($"EDIT TOOLPATH START {textBox.Tag} \"{textBox.Text}\"");
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            if (image.Source.ToString().Contains("unlock"))
            {
                image.Source = new BitmapImage(new Uri(@"Icon\lock.gif", UriKind.Relative));
                skpMain.IsEnabled = false;
                powerMILL.Execute($"EDIT TOOLPATH START MODE FIXED");
            }
            else
            {
                image.Source = new BitmapImage(new Uri(@"Icon\unlock.gif", UriKind.Relative));
                skpMain.IsEnabled = true;
                powerMILL.Execute($"EDIT TOOLPATH START MODE AUTOMATIC");
            }
        }
    }
}
