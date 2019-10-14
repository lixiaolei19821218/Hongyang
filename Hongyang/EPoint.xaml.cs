﻿using Autodesk.ProductInterface.PowerMILL;
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
    /// Interaction logic for EPoint.xaml
    /// </summary>
    public partial class EPoint : Page
    {
        private PMAutomation powerMILL;
        private PMProject session;

        public EPoint()
        {
            InitializeComponent();

            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;

            Refresh();
        }

        public void Refresh()
        {            
            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.mode\"").ToString() == "fixed")
            {
                imgLock.Source = new BitmapImage(new Uri(@"Icon\lock.gif", UriKind.Relative));
                skpMain1.IsEnabled = false;
                skpMain2.IsEnabled = false;
            }
            else
            {
                imgLock.Source = new BitmapImage(new Uri(@"Icon\unlock.gif", UriKind.Relative));
                skpMain1.IsEnabled = true;
                skpMain2.IsEnabled = true;
            }

            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.directMove\"").ToString() == "1")
            {
                chxMove.IsChecked = true;
            }
            else
            {
                chxMove.IsChecked = false;
            }

            tbxX.Text = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.position[0]\"").ToString();
            tbxY.Text = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.position[1]\"").ToString();
            tbxZ.Text = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.position[2]\"").ToString();

            string type = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.type\"").ToString();
            switch (type)
            {
                case "block_centre":
                    cbxType.SelectedIndex = 0;
                    break;
                case "last_safe":
                    cbxType.SelectedIndex = 1;
                    break;
                case "last_incr":
                    cbxType.SelectedIndex = 2;
                    break;
                case "absolute":
                    cbxType.SelectedIndex = 3;
                    break;
            }

            if (powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.SeparateFinalRetract\"").ToString() == "1")
            {
                chxSeparate.IsChecked = true;
            }
            else
            {
                chxSeparate.IsChecked = false;
            }

            string direction = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.movedirection\"").ToString();
            switch (direction)
            {
                case "tool_axis":
                    cbxDir.SelectedIndex = 0;
                    break;
                case "normal":
                    cbxDir.SelectedIndex = 1;
                    break;               
            }            

            tbxDir.Text = powerMILL.ExecuteEx("print par terse \"entity('toolpath', '').endpoint.Distance\"").ToString();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (powerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                powerMILL.Execute($"EDIT TOOLPATH END {comboBox.Tag} {comboBoxItem.Tag}");

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
                powerMILL.Execute($"EDIT TOOLPATH END {checkBox.Tag} {v}");
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (powerMILL != null)
            {
                TextBox textBox = sender as TextBox;
                powerMILL.Execute($"EDIT TOOLPATH END {textBox.Tag} \"{textBox.Text}\"");
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            if (image.Source.ToString().Contains("unlock"))
            {
                image.Source = new BitmapImage(new Uri(@"Icon\lock.gif", UriKind.Relative));
                skpMain1.IsEnabled = false;
                skpMain2.IsEnabled = false;
                powerMILL.Execute($"EDIT TOOLPATH END MODE FIXED");
            }
            else
            {
                image.Source = new BitmapImage(new Uri(@"Icon\unlock.gif", UriKind.Relative));
                skpMain1.IsEnabled = true;
                skpMain2.IsEnabled = true;
                powerMILL.Execute($"EDIT TOOLPATH END MODE AUTOMATIC");
            }
        }
    }
}