using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
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
    /// Config.xaml 的交互逻辑
    /// </summary>
    public partial class Config : Page
    {
        private ObservableCollection<Color> colors;
        private string jsonFile = AppContext.BaseDirectory + @"color.txt";

        public Config()
        {
            InitializeComponent();

            imgProject.Tag = ConfigurationManager.AppSettings["projectFolder"];
            imgNC.Tag = ConfigurationManager.AppSettings["ncFolder"];
            imgMachine.Tag = ConfigurationManager.AppSettings["msrFolder"];

            StreamReader reader = new StreamReader(jsonFile);
            string json = reader.ReadToEnd();
            reader.Close();
            colors = JsonConvert.DeserializeObject<ObservableCollection<Color>>(json);
            if (colors == null)
            {
                colors = new ObservableCollection<Color>();
            }
            dgColor.ItemsSource = colors;
        }

        private void ImgProject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                imgProject.Tag = dialog.SelectedPath;                
                Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                cfa.AppSettings.Settings["projectFolder"].Value = dialog.SelectedPath;
                cfa.Save();
            }
        }

        private void ImgNC_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                imgNC.Tag = dialog.SelectedPath;
                Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                cfa.AppSettings.Settings["ncFolder"].Value = dialog.SelectedPath;
                cfa.Save();
            }
        }

        private void ImgMachine_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                imgMachine.Tag = dialog.SelectedPath;
                Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                cfa.AppSettings.Settings["msrFolder"].Value = dialog.SelectedPath;
                cfa.Save();
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            colors.Add(new Color());
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {            
            List<Color> toDelete = new List<Color>();
            foreach (Color color in dgColor.SelectedItems)
            {
                toDelete.Add(color);
            }
            foreach (Color color in toDelete)
            {
                colors.Remove(color);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string json = JsonConvert.SerializeObject(colors.Distinct());
            StreamWriter writer = new StreamWriter(jsonFile, false);
            writer.Write(json);
            writer.Close();
            MessageBox.Show("颜色配置保存成功。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private void DgColor_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Color color = (Color)e.Row.Item;
            if (
                (color.R == 255 && color.G == 0 && color.B == 0) || 
                (color.R == 0 && color.G == 255 && color.B == 0) || 
                (color.R == 0 && color.G == 0 && color.B == 255) || 
                (color.R == 255 && color.G == 255 && color.B == 0)
                )
            {
                MessageBox.Show($"R{color.R}G{color.G}B{color.B}为保留色，不能配置此RGB值。", "Warming", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                e.Row.Item = colors[e.Row.GetIndex()];
            }            
            else
            {
                colors[e.Row.GetIndex()] = (Color)e.Row.Item;
            }
        }
    }
}
