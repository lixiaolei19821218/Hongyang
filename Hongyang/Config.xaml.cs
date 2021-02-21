using Hongyang.Model;
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
        private ObservableCollection<LevelConfig> levelConfigs;
        private string colorFile = AppContext.BaseDirectory + ConfigurationManager.AppSettings["SavedData"] + @"\color.txt";        

        public static ObservableCollection<string> MethodList = new ObservableCollection<string>(ConfigurationManager.AppSettings["methods"].Split(','));

        public Config()
        {
            InitializeComponent();

            imgProject.Tag = ConfigurationManager.AppSettings["projectFolder"];
            imgNC.Tag = ConfigurationManager.AppSettings["ncFolder"];
            imgMachine.Tag = ConfigurationManager.AppSettings["msrFolder"];            

            StreamReader reader = new StreamReader(colorFile);
            string json = reader.ReadToEnd();
            reader.Close();
            levelConfigs = JsonConvert.DeserializeObject<ObservableCollection<LevelConfig>>(json);
            if (levelConfigs == null)
            {
                levelConfigs = new ObservableCollection<LevelConfig>();
            }
            dgColor.ItemsSource = levelConfigs;
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
            levelConfigs.Add(new LevelConfig());
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {            
            List<LevelConfig> toDelete = new List<LevelConfig>();
            foreach (LevelConfig config in dgColor.SelectedItems)
            {
                toDelete.Add(config);
            }
            foreach (LevelConfig config in toDelete)
            {
                levelConfigs.Remove(config);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string json = JsonConvert.SerializeObject(levelConfigs.Distinct());
            StreamWriter writer = new StreamWriter(colorFile, false);
            writer.Write(json);
            writer.Close();
            MessageBox.Show("颜色配置保存成功。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private void DgColor_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {/*
            LevelConfig config = (LevelConfig)e.Row.Item;
            if (
                (config.R == 255 && config.G == 0 && config.B == 0) || 
                (config.R == 0 && config.G == 255 && config.B == 0) || 
                (config.R == 0 && config.G == 0 && config.B == 255) || 
                (config.R == 255 && config.G == 255 && config.B == 0)
                )
            {
                MessageBox.Show($"R{config.R}G{config.G}B{config.B}为保留色，不能配置此RGB值。", "Warming", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                e.Row.Item = levelConfigs[e.Row.GetIndex()];
            }            
            else
            {
                levelConfigs[e.Row.GetIndex()] = (LevelConfig)e.Row.Item;
            }*/
        }
    }
}
