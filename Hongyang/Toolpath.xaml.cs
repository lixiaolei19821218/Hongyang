using Autodesk.Geometry;
using Autodesk.ProductInterface.PowerMILL;
using PowerINSPECTAutomation;
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
    /// Tool.xaml 的交互逻辑
    /// </summary>
    public partial class Toolpath : Page
    {
        private PMAutomation powerMILL;
        private PMProject session;

        public Toolpath()
        {
            InitializeComponent();

            powerMILL = (Application.Current.MainWindow as MainWindow).PowerMILL;
            session = (Application.Current.MainWindow as MainWindow).Session;

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
                //cbxOpt.Items.Add(new ComboBoxItem { Content = System.IO.Path.GetFileNameWithoutExtension(file), Tag = System.IO.Path.GetFileName(file) });                
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
            if (powerMILL == null)
            {
                MessageBox.Show("未连接PowerMILl，请导入模型开始。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            if (chxAdjust.IsChecked ?? false)
            {
                if (lstSelectedLevel.SelectedItem == null)
                {
                    MessageBox.Show("请选一个已计算的层", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                session.Refresh();
                string level = (lstSelectedLevel.SelectedItem as PMLevelOrSet).Name;
                string tpName = level + "_" + (cbxMethod.SelectedItem as ComboBoxItem).Tag;
                if (cbxMethod.Text == "Swarf" || cbxMethod.Text == "Pattern")
                {
                    tpName = tpName + "_Probing";
                }
                PMToolpath rcToolpath = session.Toolpaths.FirstOrDefault(t => t.Name == tpName);
                if (rcToolpath == null)
                {
                    string msg = $"没有找到名为{tpName}的刀路，{level}还未用{(cbxMethod.SelectedItem as ComboBoxItem).Content}计算";
                    MessageBox.Show(msg, "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
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
                RefreshToolpaths();

                MessageBox.Show("调整完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
            else
            {
                if (chxSelectMethod.IsChecked == true)
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
                }
                else
                {
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
                    foreach (PMLevelOrSet level in lstAllLevel.Items)
                    {
                        if (level.Name == "Red" || level.Name == "Blue" || level.Name == "Green")
                        {
                            Calculate(level.Name);
                        }
                    }
                }
                //(Application.Current.MainWindow as MainWindow).RefreshToolpaths();
                MessageBox.Show("计算完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        private PMPattern CreatePattern(string level, string name)
        {
            PMPattern pattern = session.Patterns.FirstOrDefault(p => p.Name == level);
            if (pattern != null)
            {
                pattern.Delete();
            }
            powerMILL.Execute("CREATE PATTERN ;");
            session.Refresh();
            pattern = session.Patterns.ActiveItem;
            pattern.Name = name;
            powerMILL.Execute($"EDIT PATTERN \"{pattern.Name}\" CURVEEDITOR START");
            powerMILL.Execute("STATUS EDITING_PLANE XY");
            powerMILL.Execute("CURVEEDITOR MODE OBLIQUE_CURVE");
            powerMILL.Execute("CURVEEDITOR OBLIQUE SELECTION_MODE SURFACE");
            powerMILL.Execute("edit model all deselect all");
            powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
            if (ckbManul.IsChecked ?? false)
            {
                powerMILL.Execute($"EDIT PAR 'powermill.CurveEditor.ObliqueCurve.Origin.X' \"{tbxX.Text}\"");
                powerMILL.Execute($"EDIT PAR 'powermill.CurveEditor.ObliqueCurve.Origin.Y' \"{tbxY.Text}\"");
                powerMILL.Execute($"EDIT PAR 'powermill.CurveEditor.ObliqueCurve.Origin.Z' \"{tbxZ.Text}\"");
            }
            else
            {
                powerMILL.Execute($"EDIT PAR 'powermill.CurveEditor.ObliqueCurve.Origin.X' \"0\"");
                powerMILL.Execute($"EDIT PAR 'powermill.CurveEditor.ObliqueCurve.Origin.Y' \"0\"");
                powerMILL.Execute($"EDIT PAR 'powermill.CurveEditor.ObliqueCurve.Origin.Z' \"2\"");
            }

            powerMILL.Execute("CURVEEDITOR OBLIQUE CREATE");
            powerMILL.Execute("CURVEEDITOR OBLIQUE CLOSE");
            powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");

            powerMILL.Execute($"EDIT PATTERN \"{pattern.Name}\" CURVEEDITOR START");
            powerMILL.Execute($"EDIT PATTERN \"{pattern.Name}\" SELECT ALL");
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

            return pattern;
        }

        private PMPattern CreatePatternOld(string level, string name)
        {
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
            pattern2.Name = name;
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
            pattern1.Delete();

            return pattern2;
        }

        private void Calculate(string level)
        {
            powerMILL.DialogsOff();
            session.Refresh();

            string tag;
            if (chxSelectMethod.IsChecked == true)
            {
                tag = (cbxMethod.SelectedItem as ComboBoxItem).Tag.ToString();
            }
            else
            {
                if (level == "Red")
                {
                    tag = "P";
                }
                else if (level == "Blue")
                {
                    tag = "S";
                }
                else if (level == "Green")
                {
                    tag = "C";
                }
                else
                {
                    MessageBox.Show($"自动计算的层必须以Red，Blue或Green命名。当前层：{level}。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
            }
            string tpName = level + "_" + tag;

            if (tag == "S")
            {
                string swarfTpName = tpName + "_Swarf";
                string probingTpName = tpName + "_Probing";
                ClearToolpath(probingTpName);
                ClearToolpath(swarfTpName);

                CreateWorkplane(level, tpName, chxLean.IsChecked ?? false);

                //Swarf刀路            
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Swarf-Finishing.ptf\"");
                powerMILL.Execute("EDIT BLOCK COORDINATE WORLD");
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute("CREATE TOOL ; BALLNOSED");
                session.Refresh();
                session.Tools.ActiveItem.Name = tpName + "_BALLNOSED";
                session.Tools.ActiveItem.Diameter = double.Parse(powerMILL.ExecuteEx($"print par terse \"entity('tool', '{cbxTool.Text}').Diameter\"").ToString());
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"60\"");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'offset_down'");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Active' 1");
                powerMILL.Execute($"EDIT PAR 'StepdownLimit.Value' \"{tbxStepdown.Text}\"");
                powerMILL.Execute($"EDIT PAR 'AxialDepthOfCut.UserDefined' '1' EDIT PAR 'Stepdown' \"{tbxDepth.Text}\"");
                powerMILL.Execute("EDIT PAR 'Filter.Type' 'redistribute'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Active' '1'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Value' \"5\"");
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = swarfTpName;
                session.Toolpaths.ActiveItem.Calculate();

                //参考线
                powerMILL.Execute("CREATE PATTERN ;");
                session.Refresh();
                session.Patterns.ActiveItem.Name = tpName;
                powerMILL.Execute($"EDIT PATTERN \"{session.Patterns.ActiveItem.Name}\" INSERT TOOLPATH ;");
                powerMILL.Execute("SET TOOLPATHPOINTS ;");

                //曲面检测刀路               
                CalculateProbingPath(probingTpName, session.Patterns.ActiveItem.Name);
            }
            else if (tag == "P")
            {
                string swarfTpName = tpName + "_Swarf";
                string patternTpName = tpName + "_Pattern";
                string probingTpName = tpName + "_Probing";
                ClearToolpath(probingTpName);
                ClearToolpath(patternTpName);
                ClearToolpath(swarfTpName);

                CreateWorkplane(level, tpName, chxLean.IsChecked ?? false);

                //Swarf刀路            
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Swarf-Finishing.ptf\"");
                powerMILL.Execute("EDIT BLOCK COORDINATE WORLD");
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute("CREATE TOOL ; ENDMILL");
                session.Refresh();
                session.Tools.ActiveItem.Name = tpName + "_ENDMILL";
                session.Tools.ActiveItem.Diameter = double.Parse(powerMILL.ExecuteEx($"print par terse \"entity('tool', '{cbxTool.Text}').Diameter\"").ToString());
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"0\"");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'offset_up'");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Active' 1");
                powerMILL.Execute($"EDIT PAR 'StepdownLimit.Value' \"{tbxStepdown.Text}\"");
                powerMILL.Execute($"EDIT PAR 'AxialDepthOfCut.UserDefined' '1' EDIT PAR 'Stepdown' \"{tbxDepth.Text}\"");
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = swarfTpName;
                session.Toolpaths.ActiveItem.Calculate();
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");

                //参考线精加工
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Pattern-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = patternTpName;
                powerMILL.Execute("EDIT PAR 'UseToolpathAsPattern' 1");
                powerMILL.Execute($"EDIT PAR 'ReferenceToolpath' \"{swarfTpName}\"");
                powerMILL.Execute("EDIT PAR 'PatternBasePosition' 'drive_curve'");
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                session.Toolpaths.ActiveItem.Calculate();

                //参考线
                powerMILL.Execute("CREATE PATTERN ;");
                session.Refresh();
                session.Patterns.ActiveItem.Name = tpName;
                powerMILL.Execute($"EDIT PATTERN \"{session.Patterns.ActiveItem.Name}\" INSERT TOOLPATH ;");
                powerMILL.Execute("SET TOOLPATHPOINTS ;");

                //曲面检测刀路               
                CalculateProbingPath(probingTpName, session.Patterns.ActiveItem.Name);
            }
            else
            {
                string probingTpName = tpName + "_Probing";
                ClearToolpath(probingTpName);
                CreateWorkplane(level, tpName, chxLean.IsChecked ?? false);

                powerMILL.Execute("STRATEGYSELECTOR CATEGORY 'Probing' NEW");
                powerMILL.Execute("STRATEGYSELECTOR STRATEGY \"Probing/Surface-Inspection.ptf\" NEW");
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
                session.Refresh();
                PMToolpath toolpath = session.Toolpaths.Last();

                PMPattern pattern2 = tag == "C" ? CreatePatternOld(level, tpName) : CreatePattern(level, tpName);

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

                CollisionCheck(clone.Name, probingTpName);
            }
            (Application.Current.MainWindow as MainWindow).RefreshToolpaths();
            RefreshToolpaths();
        }

        public void CalculateProbingPath(string probingTpName, string patternName)
        {
            powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");

            powerMILL.Execute($"EDIT PAR 'Pattern' \"{patternName}\"");
            powerMILL.Execute($"ACTIVATE Tool \"{cbxTool.Text}\"");
            session.Refresh();
            session.Toolpaths.ActiveItem.Name = probingTpName;
            session.Toolpaths.ActiveItem.Calculate();
            powerMILL.Execute("EDIT COLLISION TYPE GOUGE");
            string message = powerMILL.ExecuteEx("EDIT COLLISION APPLY").ToString();
            if (message != "信息： 找不到过切")
            {
                powerMILL.Execute($"DELETE TOOLPATH \"{probingTpName}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{probingTpName + "_2"}\"");
                powerMILL.Execute($"RENAME Toolpath \"{probingTpName + "_1"}\" \"{probingTpName}\"");
            }
            string tag = probingTpName.Split('_')[1];
            if (tag == "P" || tag == "S")
            {
                Keep10Points(probingTpName);
            }
        }

        /// <summary>
        /// 10点平均分布
        /// </summary>
        /// <param name="tpName"></param>
        public void Keep10Points(string tpName)
        {
            powerMILL.Execute($"ACTIVATE Toolpath \"{tpName}\"");
            powerMILL.Execute("EDIT TOOLPATH REORDER N");
            int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Statistics.PlungesIntoStock\"").ToString());//点数
            int points = int.Parse(System.Configuration.ConfigurationManager.AppSettings["points"]) * 2;
            if (count > points)
            {
                int l = (count - points) / 2;//中间10点前面
                for (int i = 0; i < l; i++)
                {
                    if (i == 0)
                    {
                        powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} NEW");
                    }
                    else
                    {
                        powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} TOGGLE");
                    }
                }
                int m = l + points;//中间10点后面
                for (int i = m; i < count; i++)
                {
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} TOGGLE");
                }
            }
            powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
        }

        private void Keep5Points(string tpName)
        {
            powerMILL.Execute($"ACTIVATE Toolpath \"{tpName}\"");
            //powerMILL.Execute("EDIT TOOLPATH REORDER N");
            int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Statistics.PlungesIntoStock\"").ToString());//点数
            int a = count / 2;//前段数量，这里默认两个面，所以分成段
            int b = count - a;//后段数量
            int points = int.Parse(System.Configuration.ConfigurationManager.AppSettings["points"]);

            if (a > points)
            {
                int l = (a - points) / 2;//中间5点前面
                for (int i = 0; i < l; i++)
                {
                    if (i == 0)
                    {
                        powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} NEW");
                    }
                    else
                    {
                        powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} TOGGLE");
                    }
                }
                int m = l + points;//中间5点后面
                for (int i = m; i < a; i++)
                {
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} TOGGLE");
                }
            }
            if (b > points)
            {
                int l = a + (b - points) / 2;
                for (int i = a; i < l; i++)
                {
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} TOGGLE");
                }
                int m = l + points;
                for (int i = m; i < count; i++)
                {
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {i} TOGGLE");
                }
            }
            powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
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

        private PMWorkplane CreateWorkplane(string level, string workplaneName, bool isLean = false)
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
            if (isLean)
            {
                b = x > 0 ? Math.Atan2(x, z) * 180 / Math.PI : 90 + Math.Atan2(x, z) * 180 / Math.PI;
            }
            else
            {
                b = 90;
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
            workplane.Name = workplaneName;
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
                string strategy = powerMILL.ExecuteEx($"PRINT PAR terse \"entity('toolpath', '{toolpath.Name}').Strategy\"").ToString();
                string workplaneName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Workplane.Name\"").ToString();
                string patternName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Pattern.Name\"").ToString();
                string toolName = powerMILL.ExecuteEx($"PRINT PAR terse \"entity('toolpath', '{toolpath.Name}').Tool.Name\"").ToString();
                toolpath.Delete();
                if (strategy == "swarf")
                {
                    PMTool tool = session.Tools.FirstOrDefault(t => t.Name == toolName);
                    if (tool != null)
                    {
                        session.Tools.Remove(tool, true);
                    }
                }
                else//检测路径删除参考线
                {
                    PMPattern pattern = session.Patterns.FirstOrDefault(p => p.Name == patternName);
                    if (pattern != null)
                    {
                        session.Patterns.Remove(pattern, true);
                    }
                }
                PMWorkplane workplane = session.Workplanes.FirstOrDefault(w => w.Name == workplaneName);
                if (workplane != null)
                {
                    session.Workplanes.Remove(workplane, true);
                }
            }
        }

        private void CreateTool()
        {
            session.Refresh();
            PMTool tool = session.Tools.FirstOrDefault(t => t.Name == "D6-R3-L50");
            if (tool != null)
            {
                return;
            }
            tool = session.Tools.FirstOrDefault(t => t.Name == "D4-R2-L50");
            if (tool != null)
            {
                return;
            }
            tool = session.Tools.FirstOrDefault(t => t.Name == "D3-R1.5-L50");
            if (tool != null)
            {
                return;
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
            lstToolpath.ItemsSource = session.Toolpaths.Where(t => t.IsCalculated && t.Name.EndsWith("Probing"));
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
            if (powerMILL == null)
            {
                MessageBox.Show("未连接PowerMILl，请导入模型开始。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }            

            Application.Current.MainWindow.WindowState = WindowState.Minimized;

            powerMILL.Execute("PROJECT SAVE");
            powerMILL.DialogsOff();
            ActivateWorldPlane();
            powerMILL.Execute($"DELETE NCPROGRAM ALL");
            powerMILL.Execute($"CREATE NCPROGRAM 'U0'");
            powerMILL.Execute($"CREATE NCPROGRAM 'U90'");
            powerMILL.Execute($"CREATE NCPROGRAM 'U180'");
            powerMILL.Execute($"CREATE NCPROGRAM 'U270'");
            session.Refresh();
            foreach (PMToolpath toolpath in session.Toolpaths.Where(tp => tp.IsCalculated))
            {
                string strategy = powerMILL.ExecuteEx($"PRINT PAR terse \"entity('toolpath', '{toolpath.Name}').Strategy\"").ToString();
                if (strategy == "surface_inspection")
                {
                    double azimuth = double.Parse(powerMILL.ExecuteEx($"PRINT PAR terse \"entity('workplane', '{toolpath.WorkplaneName}').Azimuth\"").ToString());
                    if (azimuth <= 90)
                    {
                        powerMILL.Execute("ACTIVATE NCProgram \"U0\"");
                    }
                    else if (azimuth <= 180)
                    {
                        powerMILL.Execute("ACTIVATE NCProgram \"U90\"");
                    }
                    else if (azimuth <= 270)
                    {
                        powerMILL.Execute("ACTIVATE NCProgram \"U180\"");
                    }
                    else
                    {
                        powerMILL.Execute("ACTIVATE NCProgram \"U270\"");
                    }
                    powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpath.Name}\"");
                }
            }

            string opt = AppContext.BaseDirectory + "Pmoptz\\Fidia_KR199_OMV_V4.pmoptz";
            ExportNC("U0", opt, "NC");
            ExportNC("U90", opt, "NC90");
            ExportNC("U180", opt, "NC180");
            ExportNC("U270", opt, "NC270");

            powerMILL.Execute($"CREATE NCPROGRAM 'Total'");
            session.Refresh();
            foreach (PMNCProgram program in session.NCPrograms.Where(n => n.Name != "Total"))
            {
                string output = powerMILL.ExecuteEx($"EDIT NCPROGRAM '{program.Name}' LIST").ToString();
                string[] toolpaths = output.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 2; i < toolpaths.Length - 1; i++)
                {
                    powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpaths[i]}\"");
                }
                /* 此方法有bug， program.Toolpaths包含的刀路是乱的
                foreach (PMToolpath toolpath in program.Toolpaths)
                {
                    powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpath.Name}\"");
                }
                */
            }
            opt = AppContext.BaseDirectory + "Pmoptz\\Results_Output_Generator_OMV2015.pmoptz";
            ExportNC("Total", opt, "NC");
            powerMILL.Execute("PROJECT SAVE");

            MessageBox.Show("NC程序生成完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        public void ExportNC(string nc, string opt, string workplane)
        {
            session.Refresh();
            PMNCProgram program = session.NCPrograms.FirstOrDefault(n => n.Name == nc);
            if (program != null)
            {
                string output = powerMILL.ExecuteEx($"EDIT NCPROGRAM '{program.Name}' LIST").ToString();
                string[] toolpaths = output.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (toolpaths.Length > 3)
                {
                    powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" QUIT FORM NCTOOLPATH");
                    //string path = powerMILL.ExecuteEx($"print par terse \"entity('ncprogram', '{nc}').filename\"").ToString();
                    //path = path.Insert(path.IndexOf("{ncprogram}"), nc + "/");
                    string projectName = powerMILL.ExecuteEx("print $project_pathname(1)").ToString().Trim();
                    string path = ConfigurationManager.AppSettings["ncFolder"] + "\\" + projectName + "\\" + nc;
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" FILENAME FILESAVE\r'{path}\\{nc}'");
                    powerMILL.Execute($"EDIT NCPROGRAM '{nc}' SET WORKPLANE \"{workplane}\"");
                    powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" TAPEOPTIONS \"{opt}\" FORM ACCEPT SelectOptionFile");

                    powerMILL.Execute($"EDIT NCPROGRAM \"{nc}\" TOOLCOORDS CENTRE");
                    powerMILL.Execute($"ACTIVATE NCPROGRAM \"{nc}\" KEEP NCPROGRAM ;\rYes\rYes");

                    powerMILL.Execute($"NCTOOLPATH ACCEPT FORM ACCEPT NCTOOLPATHLIST FORM ACCEPT NCTOOLLIST FORM ACCEPT PROBINGNCOPTS");
                    powerMILL.Execute("TEXTINFO ACCEPT");
                }
            }
        }

        private void BtnImportModel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;

            //powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);            
            //session = powerMILL.ActiveProject;
            powerMILL.DialogsOff();
            string r = powerMILL.ExecuteEx("project reset").ToString();

            if (r.Contains("Cancel"))
            {
                MessageBox.Show("未保存或放弃保存当前PowerMILL项目，将不会导入模型。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);                
            }
            else
            {
                System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    powerMILL.Execute($"IMPORT MODEL FILEOPEN '{fileDialog.FileName}'");
                    powerMILL.Execute("CREATE LEVEL Red");
                    powerMILL.Execute("CREATE LEVEL Blue");
                    powerMILL.Execute("CREATE LEVEL Green");

                    session.Refresh();
                    //识别层
                    foreach (PMModel model in session.Models)
                    {
                        string output = powerMILL.ExecuteEx($"size model '{model.Name}'").ToString();
                        string[] lines = output.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 8; i < lines.Length; i++)
                        {
                            powerMILL.Execute($"edit model '{model.Name}' deselect all");
                            string[] items = lines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (items[3] == "255" && items[4] == "0" && items[5] == "0")
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"Red\" ACQUIRE SELECTED");
                            }
                            if (items[3] == "0" && items[4] == "255" && items[5] == "0")
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"Green\" ACQUIRE SELECTED");
                            }
                            if (items[3] == "0" && items[4] == "0" && items[5] == "255")
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"Blue\" ACQUIRE SELECTED");
                            }
                        }
                    }                   

                    CreateTool();
                    cbxTool.ItemsSource = session.Tools.Select(t => t.Name);

                    //创建NC坐标系
                    powerMILL.Execute("DELETE WORKPLANE ALL");
                    CreateNCWorkplane("NC", 0);
                    CreateNCWorkplane("NC90", 90);
                    CreateNCWorkplane("NC180", 180);
                    CreateNCWorkplane("NC270", 270);

                    string tag = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    powerMILL.Execute($"PROJECT SAVE AS FILESAVE '{ConfigurationManager.AppSettings["projectFolder"]}\\{tag}\\PM_{tag}'");
                    
                    RefreshToolpaths();
                    RefreshLevels();

                    MessageBox.Show("初始化完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        public void CreateNCWorkplane(string workplane, double angle)
        {
            powerMILL.Execute("ACTIVATE WORKPLANE \" \"");
            powerMILL.Execute($"CREATE WORKPLANE '{workplane}'");
            powerMILL.Execute($"MODE WORKPLANE_EDIT START \"{workplane}\"");
            powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Z");
            powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"{angle}\"");
            powerMILL.Execute("WPETWIST ACCEPT");
            powerMILL.Execute("MODE WORKPLANE_EDIT FINISH ACCEPT");
        }

        private void CbxMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //自动计算，下面逻辑不再适用。因为这些数据可能要同时设置
            /*
            string tag = (cbxMethod.SelectedItem as ComboBoxItem).Tag.ToString();
            if (tag == "S" || tag == "P")
            {
                tbxStepdown.IsEnabled = true;
                tbxDepth.IsEnabled = true;
                tbxPoints.IsEnabled = false;
            }
            else
            {
                tbxStepdown.IsEnabled = false;
                tbxDepth.IsEnabled = false;
                tbxPoints.IsEnabled = true;
            }*/
        }

        private void BtnTransform_Click(object sender, RoutedEventArgs e)
        {
            if (powerMILL == null)
            {
                MessageBox.Show("未连接PowerMILl，请导入模型开始。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            if (lstSelected.Items.Count == 0)
            {
                MessageBox.Show("请选要复制的刀路到已选刀路列表。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            int copies = int.Parse(tbxCopies.Text);
            double angle = double.Parse(tbxAngle.Text);

            foreach (PMToolpath toolpath in lstSelected.Items)
            {
                ActivateWorldPlane();
                powerMILL.Execute("STATUS EDITING_PLANE XY");
                if (toolpath.Name.Contains("Probing"))
                {
                    //采用参考线旋转方式
                    string pattern = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Pattern.Name\"").ToString();
                    powerMILL.Execute($"ACTIVATE Pattern \"{pattern}\"");
                    powerMILL.Execute("MODE GEOMETRY_TRANSFORM START PATTERN ;");
                    powerMILL.Execute("MODE TRANSFORM TYPE ROTATE");
                    powerMILL.Execute($"MODE TRANSFORM COPIES {copies}");
                    powerMILL.Execute($"MODE TRANSFORM ROTATE ANGLE \"{angle}\"");
                    powerMILL.Execute("MODE GEOMETRY_TRANSFORM FINISH ACCEPT");

                    //复制一个坐标系来旋转，原来刀路的不能直接变换，否则要重制刀路
                    string workplane = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Workplane.Name\"").ToString();
                    powerMILL.Execute($"COPY WORKPLANE \"{workplane}\" ");
                    string w0 = workplane + "_1";
                    string w1 = workplane + "_0";
                    powerMILL.Execute($"RENAME Workplane \"{w0}\" \"{w1}\"");
                    powerMILL.Execute($"ACTIVATE Workplane \"{w1}\"");
                    powerMILL.Execute("MODE WORKPLANE_TRANSFORM START ;");
                    ActivateWorldPlane();
                    powerMILL.Execute("STATUS EDITING_PLANE XY");
                    powerMILL.Execute("MODE TRANSFORM TYPE ROTATE");
                    powerMILL.Execute("MODE TRANSFORM COPY YES");
                    powerMILL.Execute($"MODE TRANSFORM COPIES {copies}");
                    powerMILL.Execute($"MODE TRANSFORM ROTATE ANGLE \"{angle}\"");
                    powerMILL.Execute("MODE WORKPLANE_TRANSFORM FINISH ACCEPT");

                    for (int i = 1; i <= copies; i++)
                    {
                        string p0 = pattern + "_" + i;
                        string p1 = pattern + "_" + i * angle;
                        powerMILL.Execute($"RENAME Pattern \"{p0}\" \"{p1}\"");
                        powerMILL.Execute($"ACTIVATE Pattern \"{p1}\"");
                        string a = w1 + "_" + i;
                        string b = workplane + "_" + i * angle;
                        powerMILL.Execute($"RENAME Workplane \"{a}\" \"{b}\"");
                        powerMILL.Execute($"ACTIVATE Workplane \"{b}\"");

                        CalculateProbingPath(toolpath.Name + "_" + i * angle, p1);
                    }
                }
                else
                {
                    MessageBox.Show($"{toolpath.Name}不是检测刀路。检测刀路带有P_Probing或S_Probing字符。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    continue;
                }
            }
            MessageBox.Show("检测路径复制完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;
            if (powerMILL == null)
            {
                MessageBox.Show("未连接PowerMILl，请导入模型开始。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            session.Refresh();
            PMNCProgram program = session.NCPrograms.FirstOrDefault(n => n.Name == "Total");
            if (program == null)
            {
                MessageBox.Show("没有在PowerMILL中找到名为Total的NC程序。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }            

            IApplication application = new PIApplication() as IApplication;
            IPIDocument doc = application.ActiveDocument;
            if (doc == null)
            {
                MessageBox.Show("请启动PowerINSPECT并导入检测项目。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            if (doc.SequenceItems.Count < 4)
            {
                MessageBox.Show("请导入检测项目。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            Application.Current.MainWindow.WindowState = WindowState.Minimized;

            IMeasure measure = doc.get_ActiveMeasure();
            ISurfaceGroup inspect1 = doc.SequenceItems[4] as ISurfaceGroup;//导入Total NC的初始检测组
            IBagOfPoints points = inspect1.BagOfPoints[measure];           

            ISequenceGroup geometricGroup = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_GeometricGroup);
            int index = 1;
            foreach (PMToolpath toolpath in program.Toolpaths)
            {                 
                int n = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Statistics.PlungesIntoStock\"").ToString());//点数
                int a = n / 2;//前面一半
                int b = n - a;//后面一半
                int[] indices;

                if (toolpath.Name.StartsWith("Red"))//角度
                {
                    //存检测角度的两个平面，红面
                    IPlane_ProbedItem plane1 = geometricGroup.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Plane_Probed_) as IPlane_ProbedItem;
                    IPlane_ProbedItem plane2 = geometricGroup.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Plane_Probed_) as IPlane_ProbedItem;
                    IMeas_Angle2LinesItem angle = geometricGroup.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Meas_Angle2Lines_) as IMeas_Angle2LinesItem;
                    foreach (IFeature feature in angle.ReferencePlane.PossibleFeatures)
                    {
                        if (feature.Name == plane1.Name)
                        {
                            angle.ReferencePlane.Feature = feature;
                            break;
                        }
                    }
                    foreach (IFeature feature in angle.ReferenceLine1.PossibleFeatures)
                    {
                        if (feature.Name == plane2.Name)
                        {
                            angle.ReferenceLine1.Feature = feature;
                            break;
                        }
                    }
                    foreach (IFeature feature in angle.ReferenceLine2.PossibleFeatures)
                    {
                        if (feature.Name == "测量设备原点::平面 X(YOZ)")
                        {
                            angle.ReferenceLine2.Feature = feature;
                            break;
                        }
                    }

                    indices = new int[points.Count];
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i < a)
                        {
                            indices[i] = index + i;
                        }
                        else
                        {
                            indices[i] = index;
                        }
                    }
                    index += a;
                    points.CopyToClipboard(indices);
                    plane1.BagOfPoints[measure].PasteFromClipboard();

                    indices = new int[points.Count];
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i < b)
                        {
                            indices[i] = index + i;
                        }
                        else
                        {
                            indices[i] = index;
                        }
                    }
                    index += b;
                    points.CopyToClipboard(indices);
                    plane2.BagOfPoints[measure].PasteFromClipboard();
                }
                else if (toolpath.Name.StartsWith("Blue"))//距离
                {
                    //存检测距离的两个平面，蓝面
                    IPlane_ProbedItem plane3 = geometricGroup.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Plane_Probed_) as IPlane_ProbedItem;
                    IPlane_ProbedItem plane4 = geometricGroup.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Plane_Probed_) as IPlane_ProbedItem;
                    IMeas_Distance2PlanesItem distance = geometricGroup.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_SimplMeas_Distance2Planes_) as IMeas_Distance2PlanesItem;
                    foreach (IFeature feature in distance.ReferencePlane1.PossibleFeatures)
                    {
                        if (feature.Name == plane3.Name)
                        {
                            distance.ReferencePlane1.Feature = feature;
                            break;
                        }
                    }
                    foreach (IFeature feature in distance.ReferencePlane1.PossibleFeatures)
                    {
                        if (feature.Name == plane4.Name)
                        {
                            distance.ReferencePlane2.Feature = feature;
                            break;
                        }
                    }

                    indices = new int[points.Count];
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i < a)
                        {
                            indices[i] = index + i;
                        }
                        else
                        {
                            indices[i] = index;
                        }
                    }
                    index += a;
                    points.CopyToClipboard(indices);
                    plane3.BagOfPoints[measure].PasteFromClipboard();

                    indices = new int[points.Count];
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i < b)
                        {
                            indices[i] = index + i;
                        }
                        else
                        {
                            indices[i] = index;
                        }
                    }
                    index += b;
                    points.CopyToClipboard(indices);
                    plane4.BagOfPoints[measure].PasteFromClipboard();
                }
                else if (toolpath.Name.StartsWith("Green"))
                {
                    //存绿面
                    ISurfaceGroup inspect2 = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_SurfPointsCNC) as ISurfaceGroup;

                    indices = new int[points.Count];
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (i < n)
                        {
                            indices[i] = index + i;
                        }
                        else
                        {
                            indices[i] = index;
                        }
                    }
                    index += n;
                    points.CopyToClipboard(indices);
                    inspect2.BagOfPoints[measure].PasteFromClipboard();
                }
            }

            string pmFolder = powerMILL.ExecuteEx("print $project_pathname(0)").ToString().Trim();            
            string file = $"{Directory.GetParent(pmFolder)}\\PI_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.pwi";
            doc.SaveAs(file, false);

            MessageBox.Show("检测完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private void BtnMergeTotal_Click(object sender, RoutedEventArgs e)
        {
            string folder = ConfigurationManager.AppSettings["msrFolder"];
            if (!Directory.Exists(folder))
            {
                MessageBox.Show($"没有找到mrs文件的存放目录{folder}，请检测设定。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
            else
            {
                Dictionary<string, List<Dictionary<string, double>>> msrFiles = new Dictionary<string, List<Dictionary<string, double>>>();//保存msr文件
                for (int i = 0; i < 4; i++)
                {
                    string file = "OMVu" + 90 * i + ".msr";
                    string path = folder + "\\" + file;
                    if (File.Exists(path))
                    {
                        List<Dictionary<string, double>> lines = new List<Dictionary<string, double>>();
                        msrFiles.Add(file, lines);                 
                        StreamReader reader = new StreamReader(path);
                        string line = reader.ReadLine();
                        while (line != null)
                        {
                            Dictionary<string, double> values = new Dictionary<string, double>();//保存mrs文件一行的XYZ值
                            lines.Add(values);
                            double x = double.Parse(line.Substring(9, 11));
                            values.Add("X", x);
                            double y = double.Parse(line.Substring(21, 11));
                            values.Add("Y", y);
                            double z = double.Parse(line.Substring(33));
                            values.Add("Z", z);
                            line = reader.ReadLine();
                        }
                        reader.Close();
                    }
                }
                if (msrFiles.Count == 0)
                {
                    MessageBox.Show($"没有找到mrs文件。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
                else
                {
                    //int count = msrFiles.Sum(f => f.Value.Count);
                    powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
                    string project = powerMILL.ExecuteEx("print $project_pathname(1)").ToString().Trim();
                    string file = $"{ConfigurationManager.AppSettings["ncFolder"]}\\{project}\\Total\\Total.tap";
                    if (!File.Exists(file))
                    {
                        MessageBox.Show($"没有找到{file}。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }
                    else
                    {
                        //保存并转换msr中的坐标值
                        List<string> transformed = new List<string>();
                        int n = 0;
                        foreach (var f in msrFiles)
                        {
                            switch (f.Key)
                            {
                                case "OMVu0.msr":
                                    foreach (var l in f.Value)
                                    {
                                        transformed.Add($"G801 N{n} X{l["X"]} Y{l["Y"]} Z{l["Z"]} R3.0");
                                        n++;
                                    }
                                    break;
                                case "OMVu90.msr":
                                    foreach (var l in f.Value)
                                    {
                                        transformed.Add($"G801 N{n} X{-l["Y"]} Y{l["X"]} Z{l["Z"]} R3.0");
                                        n++;
                                    }
                                    break;
                                case "OMVu180.msr":
                                    foreach (var l in f.Value)
                                    {
                                        transformed.Add($"G801 N{n} X{-l["X"]} Y{-l["Y"]} Z{l["Z"]} R3.0");
                                        n++;
                                    }
                                    break;
                                case "OMVu270.msr":
                                    foreach (var l in f.Value)
                                    {
                                        transformed.Add($"G801 N{n} X{l["Y"]} Y{-l["X"]} Z{l["Z"]} R3.0");
                                        n++;
                                    }
                                    break;
                            }
                        }

                        //读取total.tap
                        StreamReader reader = new StreamReader(file);
                        List<string> totalLines = new List<string>();
                        string line = reader.ReadLine();
                        while (line != null)
                        {
                            totalLines.Add(line);
                            line = reader.ReadLine();
                        }
                        reader.Close();
                        if (totalLines.Count - 2 != transformed.Count * 2) //total.tap去掉头尾的Start和End，和名义值
                        {
                            MessageBox.Show($"msr文件中的总点数和Total.tap中的不一至。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        }
                        else
                        {
                            string integrated = $"{ConfigurationManager.AppSettings["ncFolder"]}\\{project}\\Total\\Total_Integrated.tap";
                            StreamWriter writer = new StreamWriter(integrated, false);

                            n = 0;
                            foreach (string l in totalLines)
                            {
                                if (l.Contains("R3.0"))
                                {
                                    writer.WriteLine(transformed[n++]);
                                }
                                else
                                {
                                    writer.WriteLine(l);
                                }
                            }
                            writer.Close();

                            if (MessageBox.Show("生成整合文件Total_Integrated.tap完成，是否打开所在文件夹？", "Info", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                            {
                                System.Diagnostics.Process.Start("explorer.exe", $"{ConfigurationManager.AppSettings["ncFolder"]}\\{project}\\Total");
                            }
                        }
                    }
                }
            }
        }
    }
}
