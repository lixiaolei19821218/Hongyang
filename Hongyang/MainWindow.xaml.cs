﻿using Autodesk.Geometry;
using Autodesk.ProductInterface.PowerMILL;
using Hongyang.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public PMAutomation PowerMILL;
        public PMProject Session;

        private List<TreeItem> treeItems = new List<TreeItem>();

        private Toolpath Toolpath;
        private ToolAxOVec ToolAxOVec;
        private ToolRapidMv ToolRapidMv;
        private ToolRapidMvClear ToolRapidMvClear;
        private Link Link;

        public ObservableCollection<string> Toolpaths = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();

            PowerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            Session = PowerMILL.ActiveProject;
            cbxToolpaths.ItemsSource = Toolpaths;

            InitPages();

            RefreshToolpaths();
            InitTreeView();
            frame.Navigate(Toolpath);
        }

        private void InitPages()
        {
            Toolpath = new Toolpath();
            ToolAxOVec = new ToolAxOVec();
            ToolRapidMv = new ToolRapidMv();
            ToolRapidMvClear = new ToolRapidMvClear();
            Link = new Link();
        }

        public void RefreshToolpaths()
        {
            Session.Refresh();
            Toolpaths.Clear();
            foreach (PMToolpath toolpath in Session.Toolpaths)
            {
                Toolpaths.Add(toolpath.Name);
            }
            if (Session.Toolpaths.ActiveItem != null)
            {
                cbxToolpaths.SelectedItem = Session.Toolpaths.ActiveItem.Name;
            }
        }
         
        private void InitTreeView()
        {
            TreeItem treeItem;

            treeItem = new TreeItem() { Icon = @"\Icon\Toolpath.ico", Name = "刀路" };
            treeItems.Add(treeItem);

            treeItem = new TreeItem() { Icon = @"\Icon\Bore-Finishing.ico", Name = "加工轴控制" };
            treeItems.Add(treeItem);

            treeItem = new TreeItem() { Icon = @"\Icon\Corner-Pencil-Finishing.ico", Name = "快进移动" };
            treeItem.TreeItems.Add(new TreeItem() { Icon = @"\Icon\Corner-Pencil-Finishing.ico", Name = "移动和间隙" });
            treeItems.Add(treeItem);

            treeItem = new TreeItem() { Icon = @"\Icon\2D-Curve-Profile.ico", Name = "切入切出和连接" };
            treeItem.TreeItems.Add(new TreeItem() { Icon = @"\Icon\2D-Curve-Profile.ico", Name = "连接" });
            treeItem.TreeItems.Add(new TreeItem() { Icon = @"\Icon\2D-Curve-Profile.ico", Name = "点分布" });
            treeItems.Add(treeItem);

            treeItem = new TreeItem() { Icon = @"\Icon\Cooling.ico", Name = "开始点" };           
            treeItems.Add(treeItem);

            treeItem = new TreeItem() { Icon = @"\Icon\Cooling.ico", Name = "结束点" };           
            treeItems.Add(treeItem);

            treeView.ItemsSource = treeItems;
        }       

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeItem treeItem = e.NewValue as TreeItem;
            switch (treeItem.Name)
            {
                case "刀路":
                    frame.Navigate(Toolpath);
                    break;
                case "加工轴控制":
                    frame.Navigate(ToolAxOVec);
                    break;
                case "快进移动":
                    frame.Navigate(ToolRapidMv);
                    break;
                case "移动和间隙":
                    frame.Navigate(ToolRapidMvClear);
                    break;
                case "连接":
                    frame.Navigate(Link);
                    break;
                default:
                    break;
            }
            lblBanner.Content = treeItem.Name;
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            PowerMILL.Execute($"EDIT TOOLPATH \"{cbxToolpaths.Text}\" CALCULATE");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
