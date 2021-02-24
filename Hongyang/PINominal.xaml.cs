using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
    /// PINominal.xaml 的交互逻辑
    /// </summary>
    public partial class PINominal : Page
    {
        private string partFile = AppContext.BaseDirectory + ConfigurationManager.AppSettings["SavedData"] + @"\part.txt";

        public PINominal()
        {
            InitializeComponent();
            
            //读取后处理文件
            foreach(string file in System.IO.Directory.GetFiles(@"Pmoptz"))
            {
                string opt = System.IO.Path.GetFileNameWithoutExtension(file);
                if (opt != ConfigurationManager.AppSettings["totalPmoptz"])
                {
                    cbxOPT.Items.Add(opt);
                }                
            }
            cbxOPT.SelectedIndex = 0;

            //读取保存的零件信息
            StreamReader reader = new StreamReader(partFile);
            string json = reader.ReadToEnd();
            reader.Close();
            Dictionary<string, string> part = JsonConvert.DeserializeObject<Dictionary<string, string>> (json);
            tbxPart.Text = part["Part"];
            tbxPartNumber.Text = part["PartNumber"];
            tbxEquipment.Text = part["Equipment"];
            tbxProcess.Text = part["Process"];
            tbxStage.Text = part["Stage"];
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> part = new Dictionary<string, string>();//保存零件名称，代号，设备，工序，阶段标记
            part.Add("Part", tbxPart.Text.Trim());
            part.Add("PartNumber", tbxPartNumber.Text.Trim());
            part.Add("Equipment", tbxEquipment.Text.Trim());
            part.Add("Process", tbxProcess.Text.Trim());
            part.Add("Stage", tbxStage.Text.Trim());

            string json = JsonConvert.SerializeObject(part);
            StreamWriter writer = new StreamWriter(partFile, false);
            writer.Write(json);
            writer.Close();
        }
    }
}
