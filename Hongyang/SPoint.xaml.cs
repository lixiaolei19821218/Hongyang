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
    /// Interaction logic for SPoint.xaml
    /// </summary>
    public partial class SPoint : Page
    {
        public PMAutomation PowerMILL;
        public PMProject Session;

        public SPoint()
        {
            InitializeComponent();

            PowerMILL = (Application.Current.MainWindow as MainWindow).PowerMILL;
            Session = (Application.Current.MainWindow as MainWindow).Session;

            //Refresh();
        }

        public void Apply()
        {
            if (imgLock.Source.ToString().Contains("unlock"))
            {
                PowerMILL.Execute("EDIT TOOLPATH START MODE AUTOMATIC");
                PowerMILL.Execute($"EDIT TOOLPATH START {cbxType.Tag} {(cbxType.SelectedItem as ComboBoxItem).Tag}");
                if ((cbxType.SelectedItem as ComboBoxItem).Tag.ToString() == "ABSOLUTE")
                {
                    if (chxMove.IsChecked ?? false)
                    {
                        PowerMILL.Execute("EDIT TOOLPATH START DIRECT_MOVE Y");
                    }
                    else
                    {
                        PowerMILL.Execute("EDIT TOOLPATH START DIRECT_MOVE N");
                    }
                    PowerMILL.Execute($"EDIT TOOLPATH START {tbxX.Tag} \"{tbxX.Text}\"");
                    PowerMILL.Execute($"EDIT TOOLPATH START {tbxY.Tag} \"{tbxY.Text}\"");
                    PowerMILL.Execute($"EDIT TOOLPATH START {tbxZ.Tag} \"{tbxZ.Text}\"");
                }
                if (chxSeparate.IsChecked ?? false)
                {
                    PowerMILL.Execute("EDIT TOOLPATH START SEPARATE Y");
                    PowerMILL.Execute($"EDIT TOOLPATH START {cbxDir.Tag} {(cbxDir.SelectedItem as ComboBoxItem).Tag}");
                    PowerMILL.Execute($"EDIT TOOLPATH START {tbxDir.Tag} \"{tbxDir.Text}\"");
                }
                else
                {
                    PowerMILL.Execute("EDIT TOOLPATH START SEPARATE N");
                }
            }
            else
            {
                PowerMILL.Execute("EDIT TOOLPATH START MODE FIXED");
            }
        }

        public void Refresh()
        {
            if (PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.mode\"").ToString() == "fixed")
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

            if (PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.directMove\"").ToString() == "1")
            {
                chxMove.IsChecked = true;
            }
            else
            {
                chxMove.IsChecked = false;
            }

            tbxX.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.position[0]\"").ToString();
            tbxY.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.position[1]\"").ToString();
            tbxZ.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.position[2]\"").ToString();

            string type = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.type\"").ToString();
            switch (type)
            {
                case "block_centre":
                    cbxType.SelectedIndex = 0;
                    break;
                case "first_safe":
                    cbxType.SelectedIndex = 1;
                    break;
                case "first_incr":
                    cbxType.SelectedIndex = 2;
                    break;
                case "absolute":
                    cbxType.SelectedIndex = 3;
                    break;
            }

            if (PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.SeparateFinalRetract\"").ToString() == "1")
            {
                chxSeparate.IsChecked = true;
            }
            else
            {
                chxSeparate.IsChecked = false;
            }

            string direction = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.movedirection\"").ToString();
            switch (direction)
            {
                case "tool_axis":
                    cbxDir.SelectedIndex = 0;
                    break;
                case "normal":
                    cbxDir.SelectedIndex = 1;
                    break;
            }

            tbxDir.Text = PowerMILL.ExecuteEx("print par terse \"entity('toolpath', '').startpoint.Distance\"").ToString();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                ComboBox comboBox = sender as ComboBox;
                ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;
                PowerMILL.Execute($"EDIT TOOLPATH START {comboBox.Tag} {comboBoxItem.Tag}");

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
            if (PowerMILL != null)
            {
                CheckBox checkBox = sender as CheckBox;
                string v = checkBox.IsChecked ?? false ? "Y" : "N";
                PowerMILL.Execute($"EDIT TOOLPATH START {checkBox.Tag} {v}");
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PowerMILL != null)
            {
                TextBox textBox = sender as TextBox;               
                PowerMILL.Execute($"EDIT TOOLPATH START {textBox.Tag} \"{textBox.Text}\"");
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
                PowerMILL.Execute($"EDIT TOOLPATH START MODE FIXED");
            }
            else
            {
                image.Source = new BitmapImage(new Uri(@"Icon\unlock.gif", UriKind.Relative));
                skpMain1.IsEnabled = true;
                skpMain2.IsEnabled = true;
                PowerMILL.Execute($"EDIT TOOLPATH START MODE AUTOMATIC");
            }
        }
    }
}
