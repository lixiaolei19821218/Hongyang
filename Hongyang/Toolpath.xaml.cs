using Autodesk.Geometry;
using Autodesk.ProductInterface.PowerMILL;
using System;
using System.Collections.Generic;
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
    /// Tool.xaml 的交互逻辑
    /// </summary>
    public partial class Toolpath : Page
    {
        private PMAutomation powerMILL;
        private PMProject session;

        public Toolpath()
        {
            InitializeComponent();

            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;

            //cbxLevel.ItemsSource = session.LevelsAndSets.Select(l => l.Name);           

            CreateTool();
            cbxTool.ItemsSource = session.Tools.Select(t => t.Name);

            RefreshToolpaths();
            RefreshLevels();
            LoadPmoptz();
            
            Style itemContainerStyle = new Style(typeof(ListBoxItem));
            itemContainerStyle.Setters.Add(new Setter(ListBoxItem.AllowDropProperty, true));
            itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(s_PreviewMouseLeftButtonDown)));            
            itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.DropEvent, new DragEventHandler(listbox_Drop)));
            lstSelected.ItemContainerStyle = itemContainerStyle;

            itemContainerStyle = new Style(typeof(ListBoxItem));            
            itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(s_PreviewMouseLeftButtonDown)));
            lstToolpath.ItemContainerStyle = itemContainerStyle;
            lstAllLevel.ItemContainerStyle = itemContainerStyle;
            lstSelectedLevel.ItemContainerStyle = itemContainerStyle;
        }

        private void LoadPmoptz()
        {
            foreach (string file in Directory.GetFiles(AppContext.BaseDirectory + "Pmoptz\\", "*.pmoptz"))
            {
                cbxOpt.Items.Add(new ComboBoxItem { Content = System.IO.Path.GetFileNameWithoutExtension(file), Tag = System.IO.Path.GetFileName(file) });
            }
        }

        private void RefreshLevels()
        {
            session.Refresh();
            lstAllLevel.ItemsSource = session.LevelsAndSets.OrderBy(l => l.Name);
        }

        private void s_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void listbox_Drop(object sender, DragEventArgs e)
        {            
            PMToolpath droppedData = e.Data.GetData(typeof(PMToolpath)) as PMToolpath;
            if (droppedData != null)
            {
                if (sender is ListBoxItem)
                {
                    PMToolpath target = ((ListBoxItem)(sender)).DataContext as PMToolpath;

                    int removedIdx = lstSelected.Items.IndexOf(droppedData);
                    if (removedIdx == -1)
                    {
                        return;
                    }
                    int targetIdx = lstSelected.Items.IndexOf(target);

                    if (removedIdx < targetIdx)
                    {
                        lstSelected.Items.Insert(targetIdx + 1, droppedData);
                        lstSelected.Items.RemoveAt(removedIdx);
                    }
                    else
                    {
                        int remIdx = removedIdx + 1;
                        if (lstSelected.Items.Count + 1 > remIdx)
                        {
                            lstSelected.Items.Insert(targetIdx, droppedData);
                            lstSelected.Items.RemoveAt(remIdx);
                        }
                    }
                }
                else
                {
                    ListBox listBox = sender as ListBox;
                    if (listBox.Name == "lstSelected")
                    {
                        foreach (PMToolpath toolpath in lstToolpath.SelectedItems)
                        {
                            if (!lstSelected.Items.Contains(toolpath))
                            {
                                lstSelected.Items.Add(toolpath);
                            }
                        }
                    }
                    else if (listBox.Name == "lstToolpath")
                    {
                        lstSelected.Items.Remove(droppedData);
                    }
                }
            }
        }

        private void listboxLevel_Drop(object sender, DragEventArgs e)
        {
            PMLevelOrSet droppedData = e.Data.GetData(typeof(PMLevelOrSet)) as PMLevelOrSet;
            if (droppedData != null)
            {
                ListBox listBox = sender as ListBox;
                if (listBox.Name == "lstSelectedLevel")
                {
                    foreach (PMLevelOrSet level in lstAllLevel.SelectedItems)
                    {
                        if (!lstSelectedLevel.Items.Contains(level))
                        {
                            lstSelectedLevel.Items.Add(level);
                        }
                    }
                }
                else
                {
                    lstSelectedLevel.Items.Remove(droppedData);
                }
            }
        }
        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (chxAdjust.IsChecked ?? false)
            {
                if (lstSelectedLevel.SelectedItem == null)
                {
                    MessageBox.Show("请选一个已计算的层", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }                
                session.Refresh();
                string level = (lstSelectedLevel.SelectedItem as PMLevelOrSet).Name;
                PMToolpath rcToolpath = session.Toolpaths.FirstOrDefault(t => t.Name == level);
                if (rcToolpath == null)
                {
                    MessageBox.Show($"层{level}还未计算", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
                string patternName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{rcToolpath.Name}').Pattern.Name\"").ToString();
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{rcToolpath.Name}\"");
                if (chxNewTP.IsEnabled && (chxNewTP.IsChecked ?? false))
                {
                    powerMILL.Execute($"EDIT TOOLPATH \"{rcToolpath.Name}\" CLONE");
                    powerMILL.Execute($"EDIT PATTERN \"{patternName}\" CLIPBOARD COPY");
                    powerMILL.Execute("CREATE PATTERN CLIPBOARD");
                    session.Refresh();
                    rcToolpath = session.Toolpaths.ActiveItem;
                    PMPattern pattern = session.Patterns.Last();
                    pattern.Name = rcToolpath.Name;
                    patternName = pattern.Name;
                }
                else
                {
                    powerMILL.Execute($"EDIT TOOLPATH \"{rcToolpath.Name}\" RECYCLE");
                }

                powerMILL.Execute("EDIT TPPAGE SWSurfaceInspect");
                powerMILL.Execute("EDIT PAR 'Pattern' \" \"");
                powerMILL.Execute($"EDIT TOOLPATH \"{rcToolpath.Name}\" REAPPLYFROMGUI\rYes");

                powerMILL.Execute($"EDIT PATTERN \"{patternName}\" CURVEEDITOR START");
                powerMILL.Execute("FORM RIBBON TAB \"CurveEditor.Edit\"");
                powerMILL.Execute("CURVEEDITOR MODE TRANSLATE");
                powerMILL.Execute($"MODE COORDINPUT COORDINATES {tbxX.Text} {tbxY.Text} {tbxZ.Text}");

                powerMILL.Execute("MODE TRANSFORM FINISH");
                powerMILL.Execute("FORM RIBBON TAB \"Pattern\"");
                powerMILL.Execute("CURVEEDITOR FINISH ACCEPT\rYes");
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{rcToolpath.Name}\"");
                powerMILL.Execute("EDIT TPPAGE SWSurfaceInspect");
                powerMILL.Execute($"EDIT PAR 'Pattern' \"{patternName}\"");

                powerMILL.Execute($"EDIT TOOLPATH \"{rcToolpath.Name}\" CALCULATE");
                powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
                CollisionCheck(rcToolpath.Name, rcToolpath.Name);                
                MessageBox.Show("调整完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
            else
            {
                if (lstSelectedLevel.Items.Count == 0)
                {
                    MessageBox.Show("请至少选择一个要计算的层", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
                foreach (PMLevelOrSet level in lstSelectedLevel.Items)
                {
                    Calculate(level.Name);
                }
                //(Application.Current.MainWindow as MainWindow).RefreshToolpaths();
                MessageBox.Show("计算完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        private void Calculate(string level)
        {
            powerMILL.DialogsOff();
            session.Refresh();
            ClearToolpath(level);
            CreateWorkplane(level);

            powerMILL.Execute("STRATEGYSELECTOR CATEGORY 'Probing' NEW");
            powerMILL.Execute("STRATEGYSELECTOR STRATEGY \"Probing/Surface-Inspection.ptf\" NEW");
            powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
            session.Refresh();
            PMToolpath toolpath = session.Toolpaths.Last();

            powerMILL.Execute("CREATE PATTERN ; EDIT PATTERN ; CURVEEDITOR START");
            session.Refresh();
            PMPattern pattern1 = session.Patterns.Last();
            powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern1.Name}\"");
            powerMILL.Execute("CURVEEDITOR MODE LINE_MULTI");
            powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");
            powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
            powerMILL.Execute("EDIT TOOLPATH LEADS RAISEFORM");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA APPLY");
            powerMILL.Execute("PROCESS TPLEADS");
            powerMILL.Execute("LEADS ACCEPT");

            powerMILL.Execute("edit model all deselect all");
            powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
            powerMILL.Execute("CREATE PATTERN ;");
            session.Refresh();
            PMPattern pattern = session.Patterns.FirstOrDefault(p => p.Name == level);
            if (pattern != null)
            {
                pattern.Delete();
            }
            PMPattern pattern2 = session.Patterns.Last();
            pattern2.Name = level;
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" INSERT MODEL");
            powerMILL.Execute("edit model all deselect all");
            powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
            powerMILL.Execute("VIEW MODEL ; SHADE OFF");
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" DESELECT ALL");
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" SELECT 0");
            string output = powerMILL.ExecuteEx($"size pattern '{pattern2.Name}' selected").ToString();
            double z0Min = double.Parse(output.Split('\r')[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
            double z0Max = double.Parse(output.Split('\r')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" SELECT ALL");
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" DESELECT 0");
            output = powerMILL.ExecuteEx($"size pattern '{pattern2.Name}' selected").ToString();
            double height;
            if (output.Split('\r').Length == 1)
            {
                height = z0Max - z0Min;
            }
            else
            {
                double z1Min = double.Parse(output.Split('\r')[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
                double z1Max = double.Parse(output.Split('\r')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
                if (z0Min < z1Min)
                {
                    powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" DESELECT ALL");
                    powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" SELECT 0");
                    height = z1Max - z1Min;
                }
                else
                {
                    height = z0Max - z0Min;
                }
                powerMILL.Execute("DELETE SELECTION");
            }
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" SELECT ALL");
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" CURVEEDITOR START");
            powerMILL.Execute("FORM RIBBON TAB \"CurveEditor.Edit\"");
            powerMILL.Execute("CURVEEDITOR MODE TRANSLATE");
            if (ckbManul.IsChecked ?? false)
            {
                powerMILL.Execute($"MODE COORDINPUT COORDINATES {tbxX.Text} {tbxY.Text} {tbxZ.Text}");
            }
            else
            {
                powerMILL.Execute($"MODE COORDINPUT COORDINATES 0 0 -{height / 10}");
            }
            powerMILL.Execute("MODE TRANSFORM FINISH");
            powerMILL.Execute("FORM RIBBON TAB \"CurveTools.EditCurve\"");
            powerMILL.Execute("CURVEEDITOR REPOINT RAISE");
            powerMILL.Execute($"CURVEEDITOR REPOINT POINTS \"{tbxPoints.Text}\"");
            powerMILL.Execute("FORM APPLY CEREPOINTCURVE");
            powerMILL.Execute("FORM CANCEL CEREPOINTCURVE");
            powerMILL.Execute("CURVEEDITOR POINT NUMBER ON");
            powerMILL.Execute("VIEW MODEL ; SHADE OFF");
            powerMILL.Execute("VIEW MODEL ; SHADE NORMAL");
            powerMILL.Execute("VIEW MODEL ; SHADE OFF");
            powerMILL.Execute("FORM RIBBON TAB \"CurveEditor.Edit\"");
            powerMILL.Execute("FORM RIBBON TAB \"CurveTools.EditCurve\"");
            powerMILL.Execute("CURVEEDITOR FILLET INSERT RAISE");
            powerMILL.Execute("FORM ACCEPT CEINSERTFILLET");
            powerMILL.Execute("FORM RIBBON TAB \"CurveEditor.Edit\"");
            powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");

            powerMILL.Execute($"ACTIVATE TOOLPATH \"{toolpath.Name}\"");
            powerMILL.Execute($"EDIT TOOLPATH \"{toolpath.Name}\" CLONE");
            powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern2.Name}\"");
            session.Refresh();
            PMToolpath clone = session.Toolpaths.Last();

            MainWindow window = Application.Current.MainWindow as MainWindow;
            window.EPoint.Apply();
            window.LeadLink.Apply();
            window.Link.Apply();
            window.LinkFilter.Apply();
            window.SPoint.Apply();
            window.ToolAxOVec.Apply();
            window.ToolRapidMv.Apply();
            window.ToolRapidMvClear.Apply();

            powerMILL.Execute($"EDIT TOOLPATH \"{clone.Name}\" CALCULATE");
            powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
            powerMILL.Execute("VIEW MODEL ; SHADE NORMAL");
            toolpath.Delete();
            pattern1.Delete();
            CollisionCheck(clone.Name, level);

            //cbxToolpaths.ItemsSource = session.Toolpaths.Select(t => t.Name);
            (Application.Current.MainWindow as MainWindow).RefreshToolpaths();            
            RefreshToolpaths();
        }       

        /*
        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            //WindowState = WindowState.Minimized;

            //ClearToolpath(cbxCopyToLevels.Text);
            ActivateWorldPlane();
            CreateTool();

            PMToolpath toolpath = session.Toolpaths.FirstOrDefault(t => t.Name == cbxToolpaths.Text);
            BoundingBox boundingBox = toolpath.BoundingBox;
            double x0 = boundingBox.VolumetricCentre.X;
            double y0 = boundingBox.VolumetricCentre.Y;
            double a0 = Math.Atan2(y0, x0) * 180 / Math.PI;

            powerMILL.Execute("edit model all deselect all");
            powerMILL.Execute($"EDIT LEVEL \"{cbxCopyToLevels.Text}\" SELECT ALL");
            string output = powerMILL.ExecuteEx("SIZE MODEL").ToString();
            string[] min = output.Split('\r')[8].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] max = output.Split('\r')[9].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double x1 = (double.Parse(max[1]) + double.Parse(min[1])) / 2;
            double y1 = (double.Parse(max[2]) + double.Parse(min[2])) / 2;
            double a1 = Math.Atan2(y1, x1) * 180 / Math.PI;

            string patternName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Pattern.Name\"").ToString();
            powerMILL.Execute($"EDIT PATTERN \"{patternName}\" CLIPBOARD COPY");
            powerMILL.Execute("CREATE PATTERN CLIPBOARD");
            session.Refresh();
            PMPattern pattern = session.Patterns.Last();
            pattern.Name = cbxCopyToLevels.Text;
            powerMILL.Execute($"EDIT PATTERN \"{pattern.Name}\" CURVEEDITOR START");
            powerMILL.Execute("CURVEEDITOR MODE ROTATE");
            powerMILL.Execute($"MODE TRANSFORM ROTATE ANGLE \"{a1 - a0}\"");
            powerMILL.Execute("CURVEEDITOR FINISH ACCEPT\rYES");

            PMWorkplane workplane = CreateWorkplane(cbxCopyToLevels.Text);

            powerMILL.Execute($"ACTIVATE TOOLPATH \"{toolpath.Name}\"");
            powerMILL.Execute($"EDIT TOOLPATH \"{toolpath.Name}\" CLONE");
            powerMILL.Execute("FORM CANCEL SFSurfaceInspect");
            session.Refresh();
            PMToolpath cloned = session.Toolpaths.Last();
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{cloned.Name}\"");
            workplane.IsActive = true;
            powerMILL.Execute("EDIT TPPAGE SWSurfaceInspect");
            powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern.Name}\"");
            powerMILL.Execute($"EDIT TOOLPATH \"{cloned.Name}\" CALCULATE");
            powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");

            CollisionCheck(cloned.Name, cbxCopyToLevels.Text);

            cbxToolpaths.ItemsSource = session.Toolpaths.Select(t => t.Name);

            //WindowState = WindowState.Normal;
            MessageBox.Show("复制完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        */

        private void ActivateWorldPlane()
        {
            for (int i = 0; i < session.Workplanes.Count; i++)
            {
                PMWorkplane w = session.Workplanes[0];
                powerMILL.Execute($"EXPLORER SELECT Workplane \"Workplane\\{w.Name}\" NEW");
                powerMILL.Execute("DEACTIVATE WORKPLANE");
            }
        }

        private void CollisionCheck(string toolpathName, string newName)
        {
            session.Refresh();
            int count = session.Toolpaths.Count;
            PMToolpath toolpath = session.Toolpaths.FirstOrDefault(t => t.Name == toolpathName);
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{toolpath.Name}\"");
            //powerMILL.Execute("FORM COLLISION");
            powerMILL.Execute("EDIT COLLISION SPLIT_TOOLPATH Y");
            powerMILL.Execute("EDIT COLLISION TYPE GOUGE");
            powerMILL.Execute("EDIT COLLISION HIT_OUTPUT N");
            powerMILL.Execute("EDIT COLLISION APPLY");
            session.Refresh();
            if (session.Toolpaths.Count > count)
            {
                toolpath.Delete();
                toolpath = session.Toolpaths.ActiveItem;
                session.Refresh();
                count = session.Toolpaths.Count;
            }
            powerMILL.Execute("EDIT COLLISION TYPE COLLISION");
            powerMILL.Execute("EDIT COLLISION APPLY");
            powerMILL.Execute("COLLISION ACCEPT");
            session.Refresh();
            if (session.Toolpaths.Count > count)
            {
                toolpath.Delete();
                toolpath = session.Toolpaths.ActiveItem;
            }
            toolpath.Name = newName;
        }

        private PMWorkplane CreateWorkplane(string level)
        {
            ActivateWorldPlane();
            powerMILL.Execute("edit model all deselect all");
            powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
            string output = powerMILL.ExecuteEx("SIZE MODEL").ToString();
            string[] min = output.Split('\r')[8].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] max = output.Split('\r')[9].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] length = output.Split('\r')[10].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            double x = (double.Parse(min[1]) + double.Parse(max[1])) / 2;
            double y = (double.Parse(min[2]) + double.Parse(max[2])) / 2;
            double z = (double.Parse(min[3]) + double.Parse(max[3])) / 2;
            double a = Math.Atan2(y, x) * 180 / Math.PI;
            double b;
            if (level.Length >= 2 && level.Substring(level.Length - 2, 2).ToLower() == "_h")
            {
                b = 90;
            }
            else
            {
                b = x > 0 ? Math.Atan2(x, z) * 180 / Math.PI : 90 + Math.Atan2(x, z) * 180 / Math.PI;
            }

            PMWorkplane workplane = session.Workplanes.FirstOrDefault(w => w.Name == level);
            if (workplane != null)
            {
                workplane.Delete();
            }
            powerMILL.Execute("MODE WORKPLANE_CREATE ; SELECTION CENTRE");
            session.Refresh();
            workplane = session.Workplanes.FirstOrDefault(w => w.Name == level);          
            workplane = session.Workplanes.Last();
            workplane.Name = level;
            if (double.Parse(length[3]) == 0.0)
            {

            }
            else
            {
                powerMILL.Execute($"MODE WORKPLANE_EDIT START \"{workplane.Name}\"");
                powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Z");
                powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"{a}\"");
                powerMILL.Execute("WPETWIST ACCEPT");
                powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Y");
                powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"{b}\"");
                powerMILL.Execute("WPETWIST ACCEPT");
                powerMILL.Execute("MODE WORKPLANE_EDIT FINISH ACCEPT");
                powerMILL.Execute($"EXPLORER SELECT Workplane \"Workplane\\{workplane.Name}\" NEW");                
            }
            powerMILL.Execute($"ACTIVATE Workplane \"{workplane.Name}\"");
            return workplane;
        }

        private double CalculateHeight(string patternName)
        {
            foreach (PMPattern pattern in session.Patterns)
            {
                powerMILL.Execute($"EDIT PATTERN \"{pattern.Name}\" DESELECT ALL");
            }
            powerMILL.Execute($"EDIT PATTERN \"{patternName}\" SELECT ALL");
            string output = powerMILL.ExecuteEx($"size pattern '{patternName}' selected").ToString();
            double zMin = double.Parse(output.Split('\r')[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
            double zMax = double.Parse(output.Split('\r')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
            double height = zMax - zMin;
            return height;
        }

        private void ChxAdjust_Checked(object sender, RoutedEventArgs e)
        {
            ckbManul.IsChecked = true;
        }

        private void ClearToolpath(string toolpathName)
        {
            PMToolpath toolpath = session.Toolpaths.FirstOrDefault(t => t.Name == toolpathName);
            if (toolpath != null)
            {
                string patternName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Pattern.Name\"").ToString();
                toolpath.Delete();
                PMPattern pattern = session.Patterns.FirstOrDefault(p => p.Name == patternName);
                if (pattern != null)
                {
                    pattern.Delete();
                }
            }
        }

        private void CreateTool()
        {
            session.Refresh();
            PMTool tool = session.Tools.FirstOrDefault(t => t.Name == "D6-R3-L50");
            if (tool != null)
            {
                tool.Delete();
            }
            session.Refresh();
            tool = session.Tools.FirstOrDefault(t => t.Name == "D4-R2-L50");
            if (tool != null)
            {
                tool.Delete();
            }
            session.Refresh();
            tool = session.Tools.FirstOrDefault(t => t.Name == "D3-R1.5-L50");
            if (tool != null)
            {
                tool.Delete();
            }
            string ptf = AppContext.BaseDirectory + @"Ptf\Tools2019.ptf";
            powerMILL.Execute($"FORM RIBBON BACKSTAGE CLOSE IMPORT TEMPLATE PROJECT FILEOPEN\r '{ptf}'");
            /*
            string toolName = "D6-R3-L50";
            PMTool tool = session.Tools.FirstOrDefault(t => t.Name == toolName);
            string pmlth = AppContext.BaseDirectory + @"Macro\HKS-A-63-toolholder.pmlth";
            if (tool == null)
            {
                powerMILL.Execute("CREATE TOOL ; PROBE FORM TOOL");
                session.Refresh();
                tool = session.Tools.Last();
                tool.Name = toolName;
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" DIAMETER \"6\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" NUMBER COMMANDFROMUI 20");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT ADD");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT UPPERDIA \"4\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT LENGTH \"100\"");               
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" IMPORT_HOLDER FILEOPEN '{pmlth}'");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" OVERHANG \"50\"");
                powerMILL.Execute("TOOL ACCEPT");
            }
            toolName = "D4-R2-L50";
            tool = session.Tools.FirstOrDefault(t => t.Name == toolName);
            if (tool == null)
            {
                powerMILL.Execute("CREATE TOOL ; PROBE FORM TOOL");
                session.Refresh();
                tool = session.Tools.Last();
                tool.Name = toolName;
                tool.Diameter = 4;
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" NUMBER COMMANDFROMUI 21");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT ADD");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT UPPERDIA \"3\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT LENGTH \"100\"");                
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" IMPORT_HOLDER FILEOPEN '{pmlth}'");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" OVERHANG \"50\"");
                powerMILL.Execute("TOOL ACCEPT");
            }
            toolName = "D3-R1.5-L50";
            tool = session.Tools.FirstOrDefault(t => t.Name == toolName);
            if (tool == null)
            {
                powerMILL.Execute("CREATE TOOL ; PROBE FORM TOOL");
                session.Refresh();
                tool = session.Tools.Last();
                tool.Name = toolName;
                tool.Diameter = 3;
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" NUMBER COMMANDFROMUI 22");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT ADD");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT UPPERDIA \"2\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT LOWERDIA \"2\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT LENGTH \"100\"");               
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" IMPORT_HOLDER FILEOPEN '{pmlth}'");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" OVERHANG \"50\"");
                powerMILL.Execute("TOOL ACCEPT");
            }
            */
            session.Refresh();
            session.Tools.First().IsActive = true;
        }

        private void RefreshToolpaths()
        {
            session.Refresh();
            lstToolpath.ItemsSource = session.Toolpaths.Where(t => t.IsCalculated);
        }

        private void CbxTool_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            powerMILL.Execute($"ACTIVATE TOOL \"{comboBox.SelectedValue}\"");
        }

        private void BtnTo_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button.Name == "btnToRight")
            {
                foreach (PMToolpath toolpath in lstToolpath.SelectedItems)
                {
                    if (!lstSelected.Items.Contains(toolpath))
                    {
                        lstSelected.Items.Add(toolpath);
                    }
                }
            }
            else if (button.Name == "btnToLeft")
            {
                List<PMToolpath> selected = new List<PMToolpath>();
                foreach (PMToolpath toolpath in lstSelected.SelectedItems)
                {
                    selected.Add(toolpath);
                }
                foreach (PMToolpath toolpath in selected)
                {
                    lstSelected.Items.Remove(toolpath);
                }
            }
            else if (button.Name == "btnL2R")
            {
                foreach (PMLevelOrSet level in lstAllLevel.SelectedItems)
                {
                    if (!lstSelectedLevel.Items.Contains(level))
                    {
                        lstSelectedLevel.Items.Add(level);
                    }
                }
            }
            else if (button.Name == "btnL2L")
            {
                List<PMLevelOrSet> selected = new List<PMLevelOrSet>();
                foreach (PMLevelOrSet level in lstSelectedLevel.SelectedItems)
                {
                    selected.Add(level);
                }
                foreach (PMLevelOrSet level in selected)
                {
                    lstSelectedLevel.Items.Remove(level);
                }
            }
            else if (button.Name == "btnL2RAll")
            {
                lstSelectedLevel.Items.Clear();
                foreach (var l in lstAllLevel.Items)
                {
                    lstSelectedLevel.Items.Add(l);
                }

            }
            else if (button.Name == "btnL2LAll")
            {
                MessageBoxResult result = MessageBox.Show("确实要移除所有已选层吗？这样将导致已计算刀路不能调整偏移。", "Warming", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly);
                if (result == MessageBoxResult.Yes)
                {
                    lstSelectedLevel.Items.Clear();
                }
            }
            else if (button.Name == "btnToRightAll")
            {
                lstSelected.Items.Clear();
                foreach (var tp in lstToolpath.Items)
                {
                    lstSelected.Items.Add(tp);
                }
            }
            else if (button.Name == "btnToLeftAll")
            {
                lstSelected.Items.Clear();                
            }
        }

        private void BtnGenerateNC_Click(object sender, RoutedEventArgs e)
        {
            if (session.Directory == null)
            {
                MessageBox.Show("请先保存PowerMILL项目。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            string nc = DateTime.Now.ToString("yyyyMMdd_HHmmss");           
            powerMILL.DialogsOff();            
            powerMILL.Execute($"CREATE NCPROGRAM '{nc}'");
            foreach (PMToolpath toolpath in lstSelected.Items)
            {
                powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpath.Name}\"");
            }
            powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" QUIT FORM NCTOOLPATH");
            string path = powerMILL.ExecuteEx($"print par terse \"entity('ncprogram', '{nc}').filename\"").ToString();
            path = path.Insert(path.IndexOf("{ncprogram}"), cbxOpt.Text + nc + "/");
            powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" FILENAME FILESAVE\r'{path}'");
            powerMILL.Execute($"EDIT NCPROGRAM '{nc}' SET WORKPLANE \" \"");           
            powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" TAPEOPTIONS \"{AppContext.BaseDirectory + "Pmoptz\\" + (cbxOpt.SelectedItem as ComboBoxItem).Tag}\" FORM ACCEPT SelectOptionFile");
            if (cbxOpt.Text.ToLower().Contains("fidia"))
            {
                powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" TOOLCOORDS CENTRE");
            }
            else
            {
                powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" TOOLCOORDS TIP");
            }
            powerMILL.Execute($"ACTIVATE NCPROGRAM \"{nc}\" KEEP NCPROGRAM ;\rYes\rYes");
            
            powerMILL.Execute($"NCTOOLPATH ACCEPT FORM ACCEPT NCTOOLPATHLIST FORM ACCEPT NCTOOLLIST FORM ACCEPT PROBINGNCOPTS");
            powerMILL.Execute("TEXTINFO ACCEPT");
            MessageBox.Show("NC程序生成完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private void BtnImportModel_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                powerMILL.Execute($"IMPORT MODEL FILEOPEN '{fileDialog.FileName}'");
                RefreshLevels();
            }
        }
    }
}
