using Autodesk.Geometry;
using Autodesk.ProductInterface.PowerMILL;
using Hongyang.Model;
using Newtonsoft.Json;
using PowerINSPECTAutomation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private List<NCOutput> NCOutputs = new List<NCOutput>();
        private IEnumerable<LevelConfig> colors;

        public Toolpath()
        {
            InitializeComponent();

            powerMILL = (Application.Current.MainWindow as MainWindow).PowerMILL;
            session = (Application.Current.MainWindow as MainWindow).Session;

            cbxMethod.ItemsSource = ConfigurationManager.AppSettings["methods"].Split(',');

            /*
            cbxTool.ItemsSource = session.Tools.Select(t => t.Name);
            RefreshToolpaths();
            RefreshLevels();            
            */

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

            InitComboBox();

            int uAngle = int.Parse(ConfigurationManager.AppSettings["uAngle"]);
            int count = 360 / uAngle;
            for (int i = 0; i < count; i++)
            {
                NCOutputs.Add(new NCOutput { Angle = uAngle * i });
            }
        }

        private void ConnectToPM()
        {
            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;
            session.Refresh();
            cbxTool.ItemsSource = session.Tools.Select(t => t.Name);
            lstToolpath.ItemsSource = session.Toolpaths.Where(t => t.IsCalculated && t.Name.EndsWith("Probing"));
            lstAllLevel.ItemsSource = null;
            lstAllLevel.ItemsSource = session.LevelsAndSets;
        }

        private void LoadColorConfig()
        {
            StreamReader reader = new StreamReader(AppContext.BaseDirectory + ConfigurationManager.AppSettings["SavedData"] + @"\color.txt");
            string json = reader.ReadToEnd();
            reader.Close();
            colors = JsonConvert.DeserializeObject<IEnumerable<LevelConfig>>(json).Where(c => c.Method != "不计算");
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
            lstAllLevel.ItemsSource = null;
            lstAllLevel.ItemsSource = session.LevelsAndSets;
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
            ConnectToPM();

            if (powerMILL == null)
            {
                MessageBox.Show("未连接PowerMILl，请导入模型开始。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            if (session.Tools.All(t => t.Name != ConfigurationManager.AppSettings["ENDMILL"]))
            {
                powerMILL.Execute($"CREATE TOOL '{ConfigurationManager.AppSettings["ENDMILL"]}' ENDMILL");
                powerMILL.Execute($"EDIT TOOL \"{ConfigurationManager.AppSettings["ENDMILL"]}\" DIAMETER \"0.5\"");
                powerMILL.Execute($"EDIT TOOL \"{ConfigurationManager.AppSettings["ENDMILL"]}\" LENGTH \"60\"");
            }
            if (session.Tools.All(t => t.Name != ConfigurationManager.AppSettings["BALLNOSED"]))
            {
                powerMILL.Execute($"CREATE TOOL '{ConfigurationManager.AppSettings["BALLNOSED"]}' BALLNOSED");
                powerMILL.Execute($"EDIT TOOL \"{ConfigurationManager.AppSettings["BALLNOSED"]}\" DIAMETER \"0.5\"");
                powerMILL.Execute($"EDIT TOOL \"{ConfigurationManager.AppSettings["BALLNOSED"]}\" LENGTH \"60\"");
            }
            if (session.Tools.All(t => t.Name != ConfigurationManager.AppSettings["ENDMILL_D10"]))
            {
                powerMILL.Execute($"CREATE TOOL '{ConfigurationManager.AppSettings["ENDMILL_D10"]}' ENDMILL");
                powerMILL.Execute($"EDIT TOOL \"{ConfigurationManager.AppSettings["ENDMILL_D10"]}\" DIAMETER \"10\"");
                powerMILL.Execute($"EDIT TOOL \"{ConfigurationManager.AppSettings["ENDMILL_D10"]}\" LENGTH \"60\"");
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
                        MessageBox.Show("请至少选择一个要计算的层。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        return;
                    }
                   
                    uint v;                   
                    if (!uint.TryParse(tbxGreenPoints.Text, out v) || v == 0)
                    {
                        MessageBox.Show("曲面点数必须是正整数。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        return;
                    }                   
                    double d;
                    if (double.TryParse(tbxStepdown.Text, out d))
                    {
                        if (d <= 0.0)
                        {
                            MessageBox.Show("行距必须是正数。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("行距必须是正数。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        return;
                    }

                    Application.Current.MainWindow.WindowState = WindowState.Minimized;

                    foreach (PMLevelOrSet level in lstSelectedLevel.Items)
                    {
                        try
                        {
                            Calculate(level.Name);
                        }
                        catch
                        {
                            MessageBox.Show("刀具计算异常，请检测策略设置。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        }
                    }
                }
                else
                {
                    Application.Current.MainWindow.WindowState = WindowState.Minimized;
                    LoadColorConfig();
                    foreach (PMLevelOrSet level in lstAllLevel.Items)
                    {
                        try
                        {
                            Calculate(level.Name);
                        }
                        catch
                        {
                            MessageBox.Show("刀具计算异常，请检测策略设置。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
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
            powerMILL.Execute($"CURVEEDITOR REPOINT POINTS \"{cbxPoints.Text}\"");
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
            powerMILL.Execute($"CURVEEDITOR REPOINT POINTS \"{cbxPoints.Text}\"");
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
            string output = powerMILL.ExecuteEx($"SIZE LEVEL \"{level}\"").ToString();
            if (output.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length == 3)
            {
                //空层
                return;
            }

            session.Refresh();

            string method;
            if (chxSelectMethod.IsChecked == true)
            {
                method = cbxMethod.SelectedItem.ToString();
            }
            else
            {
                LevelConfig config = colors.FirstOrDefault(c => c.Level == level);
                if (config == null)
                {
                    return;
                }
                method = config.Method;
                /*
                if (level == "Red")
                {
                    method = "P";
                }
                else if (level == "Blue")
                {
                    method = "S";
                }
                else if (level == "Green" || level == "Yellow")
                {
                    method = "Y";
                }   
                else if (level == "U型槽")
                {
                    method = "U";
                }
                else
                {
                    Regex r = new Regex(@"^R\d+G\d+B\d+");
                    if (r.IsMatch(level))
                    {
                        method = "Y";                        
                    }    
                    else
                    {
                        //MessageBox.Show($"自动计算的层必须以Red，Blue，Green，Yellow命名或是RGB命名的预留层。当前层：{level}。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        return;
                    }
                }*/
            }

            if (method != "U型槽底面模型比对")
            {
                powerMILL.Execute("FORM RIBBON BACKSTAGE CLOSE RESET ALL");
            }
            string tpName = level + "_" + method;

            powerMILL.Execute("edit model all deselect all");
            powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
            powerMILL.Execute("ACTIVATE WORKPLANE \" \"");

            if (method == "横面（距离）")
            {
                string swarfTpName = tpName + "_Swarf";
                string probingTpName = tpName + "_Probing";
                //ClearToolpath(probingTpName);
                //ClearToolpath(swarfTpName);
                ClearToolpath_V2(probingTpName);

                CreateWorkplane(level, tpName);

                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                //Swarf刀路            
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Swarf-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = swarfTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{swarfTpName}\" FORM TOOLPATH");
                powerMILL.Execute("EDIT BLOCK COORDINATE WORLD");
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute($"ACTIVATE Tool \"{ConfigurationManager.AppSettings["ENDMILL"]}\"");
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"60\"");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'offset_down'");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Active' 1");
                int cut = int.Parse(chxSelectMethod.IsChecked == true ? cbxStepdownLimit.Text : ConfigurationManager.AppSettings["StepdownLimit"]) + 1;
                powerMILL.Execute($"EDIT PAR 'StepdownLimit.Value' \"{cut}\"");//因为会删除一条，所以这里要加上一条
                string interval = chxSelectMethod.IsChecked == true ? tbxStepdown.Text : ConfigurationManager.AppSettings["Stepdown"];
                powerMILL.Execute($"EDIT PAR 'AxialDepthOfCut.UserDefined' '1' EDIT PAR 'Stepdown' \"{interval}\"");
                powerMILL.Execute("EDIT PAR 'Filter.Type' 'redistribute'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Active' '1'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Value' \"5\"");
                powerMILL.Execute("EDIT TOOLPATH START TYPE POINT_SAFE");
                //powerMILL.Execute("EDIT TOOLAXIS TYPE AUTOMATIC");
                //powerMILL.Execute("EDIT PAR 'AxisAlignment' 'from'");
                powerMILL.Execute($"EDIT TOOLPATH \"{swarfTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
                //删除两侧第一条线
                powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {0} TOGGLE");
                powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {cut} TOGGLE");
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");

                //参考线
                powerMILL.Execute($"CREATE PATTERN {tpName}");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" INSERT TOOLPATH ;");
                powerMILL.Execute("SET TOOLPATHPOINTS ;");

                //曲面检测刀路               
                CalculateProbingPath(probingTpName, tpName);

                //底面
                PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;                
                if (page.rbMode2.IsChecked == true)
                {
                    string tpPrefix = level + "_" + method;
                    double diameter = double.Parse(powerMILL.ExecuteEx($"print par terse \"entity('tool', '{cbxTool.Text}').Diameter\"").ToString());//测头直径
                    powerMILL.Execute("edit model all deselect all");
                    powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");

                    CalculateBottom(tpPrefix, tpName, diameter, ConfigurationManager.AppSettings["ENDMILL"], level);

                    /*先注释掉，怕竖面和横面不是一个槽的，毛坯就会算错
                    //从配置找角度的层名
                    LevelConfig config = colors.FirstOrDefault(c => c.Method == "角度");
                    if (config != null)
                    {
                        CalculateBottom(tpPrefix, tpName, diameter, ConfigurationManager.AppSettings["ENDMILL"], level, config.Level);
                    }
                    else
                    {
                        CalculateBottom(tpPrefix, tpName, diameter, ConfigurationManager.AppSettings["ENDMILL"], level);
                    } */                   
                }
            }
            else if (method == "竖面（角度）" || method == "竖面（距离）")
            {
                string swarfTpName = tpName + "_Swarf";
                string patternTpName = tpName + "_Pattern";
                string probingTpName = tpName + "_Probing";
                //ClearToolpath(probingTpName);
                //ClearToolpath(patternTpName);
                //ClearToolpath(swarfTpName);
                ClearToolpath_V2(probingTpName);               

                //Swarf刀路     
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Swarf-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = swarfTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{swarfTpName}\" FORM TOOLPATH");
                powerMILL.Execute("EDIT BLOCK COORDINATE WORLD");
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute($"ACTIVATE Tool \"{ConfigurationManager.AppSettings["ENDMILL"]}\"");
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"0\"");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'offset_down'");
                powerMILL.Execute("EDIT PAR 'Tolerance' \"0.01\"");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Active' 1");
                int cut = int.Parse(chxSelectMethod.IsChecked == true ? cbxStepdownLimit.Text : ConfigurationManager.AppSettings["StepdownLimit"]) + 1;
                powerMILL.Execute($"EDIT PAR 'StepdownLimit.Value' \"{cut}\"");//因为会删除一条，所以这里要加上一条
                string interval = chxSelectMethod.IsChecked == true ? tbxStepdown.Text : ConfigurationManager.AppSettings["Stepdown"];
                powerMILL.Execute($"EDIT PAR 'AxialDepthOfCut.UserDefined' '1' EDIT PAR 'Stepdown' \"{interval}\"");
                powerMILL.Execute("EDIT TOOLPATH START TYPE POINT_SAFE");
                powerMILL.Execute("EDIT TOOLAXIS TYPE AUTOMATIC");
                powerMILL.Execute("EDIT PAR 'AxisAlignment' 'from'");
                powerMILL.Execute($"EDIT TOOLPATH \"{swarfTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
                //删除两侧第一条线
                powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {0} TOGGLE");
                powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r {cut} TOGGLE");
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");

                CreateWorkplanebySwarf(swarfTpName, tpName);

                //参考线精加工
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Pattern-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = patternTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{patternTpName}\" FORM TOOLPATH");
                powerMILL.Execute($"ACTIVATE Tool \"{ConfigurationManager.AppSettings["BALLNOSED"]}\"");
                powerMILL.Execute("EDIT PAR 'UseToolpathAsPattern' 1");
                powerMILL.Execute($"EDIT PAR 'ReferenceToolpath' \"{swarfTpName}\"");
                powerMILL.Execute("EDIT PAR 'PatternBasePosition' 'drive_curve'");
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'off'");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"80\"");
                powerMILL.Execute("EDIT PAR 'ToolAxis.LeadLeanMode' 'PM2012R2'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Active' '1'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Value' \"5\"");
                powerMILL.Execute($"EDIT TOOLPATH \"{patternTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                //参考线
                powerMILL.Execute($"CREATE PATTERN {tpName}");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" INSERT TOOLPATH ;");
                powerMILL.Execute("SET TOOLPATHPOINTS ;");

                //曲面检测刀路               
                CalculateProbingPath(probingTpName, tpName);
            }
            else if (method == "模型比对" || method == "顶端内侧圆弧")
            {
                string swarfTpName = tpName + "_Swarf";
                string patternTpName = tpName + "_Pattern";
                string probingTpName = tpName + "_Probing";
               
                ClearToolpath_V2(probingTpName);
                
                powerMILL.Execute($"ACTIVATE Tool \"{ConfigurationManager.AppSettings["ENDMILL"]}\"");

                //Swarf刀路    
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Swarf-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = swarfTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{swarfTpName}\" FORM TOOLPATH");
                powerMILL.Execute("EDIT BLOCK COORDINATE WORLD");                
                powerMILL.Execute("EDIT BLOCK RESET");                
                powerMILL.Execute("EDIT PAR 'Tolerance' \"0.01\"");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'offset_down'");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Active' 1");               
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Value' \"2\"");    
                if (method == "模型比对")
                {
                    powerMILL.Execute("EDIT PAR 'Degouge.Active' 0");
                }
                else if (method == "顶端内侧圆弧")
                {
                    powerMILL.Execute("EDIT PAR 'Degouge.Active' 1");
                }
                string value = chxSelectMethod.IsChecked == true ? tbxStepdown.Text : ConfigurationManager.AppSettings["GreenStepdown"];
                powerMILL.Execute($"EDIT PAR 'AxialDepthOfCut.UserDefined' '1' EDIT PAR 'Stepdown' \"{value}\"");
                powerMILL.Execute("EDIT TOOLPATH START TYPE POINT_SAFE");
                powerMILL.Execute("EDIT TOOLAXIS TYPE AUTOMATIC");
                powerMILL.Execute("EDIT PAR 'AxisAlignment' 'from'");
                powerMILL.Execute($"EDIT TOOLPATH \"{swarfTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                string pattern = tpName + "_S";
                if (method == "模型比对")
                {
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\\r 0 NEW");
                    powerMILL.Execute("DELETE TOOLPATH ; SELECTED");

                    powerMILL.Execute($"DELETE PATTERN \"{pattern}\"");
                    powerMILL.Execute($"CREATE PATTERN {pattern}");
                    powerMILL.Execute($"EDIT PATTERN \"{pattern}\" INSERT TOOLPATH ;");
                    powerMILL.Execute($"EDIT PATTERN \"{pattern}\" CURVEEDITOR START");
                    powerMILL.Execute($"EDIT PATTERN \"{pattern}\" SELECT 0");
                    powerMILL.Execute("CURVEEDITOR REPOINT RAISE");
                    powerMILL.Execute("CURVEEDITOR REPOINT POINTS \"20\"");
                    powerMILL.Execute("FORM APPLY CEREPOINTCURVE");
                    powerMILL.Execute("FORM CANCEL CEREPOINTCURVE");
                    powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");
                    
                    CreateWorkplanebySwarf(swarfTpName, tpName);
                }
                else if (method == "顶端内侧圆弧")
                {
                    int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{swarfTpName}').Statistics.PlungesIntoStock\"").ToString());
                    for (int i = 0; i < count; i++)
                    {
                        if (i % 2 == 0)
                        {
                            powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {i} TOGGLE");                           
                        }
                    }
                    powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
                    powerMILL.Execute("ACTIVATE WORKPLANE \" \"");
                }

                //参考线精加工
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Pattern-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = patternTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{patternTpName}\" FORM TOOLPATH");
                powerMILL.Execute($"ACTIVATE Tool \"{ConfigurationManager.AppSettings["BALLNOSED"]}\"");
                if (method == "模型比对")
                {
                    powerMILL.Execute("EDIT PAR 'UseToolpathAsPattern' 0");
                    powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern}\"");
                }
                else if (method == "顶端内侧圆弧")
                {
                    powerMILL.Execute("EDIT PAR 'UseToolpathAsPattern' 1");
                    powerMILL.Execute($"EDIT PAR 'ReferenceToolpath' \"{swarfTpName}\"");
                }
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"80.0\"");
                powerMILL.Execute("EDIT PAR 'ToolAxis.LeadLeanMode' 'PM2012R2'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Active' '1'");
                powerMILL.Execute("EDIT PAR 'MaxDistanceBetweenPoints.Value' \"5\"");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'off'");
                powerMILL.Execute($"EDIT TOOLPATH \"{patternTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
                powerMILL.Execute("PROCESS TPLEADS");

                //参考线
                powerMILL.Execute($"CREATE PATTERN {tpName}");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" INSERT TOOLPATH ;");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" CURVEEDITOR START");
                powerMILL.Execute("FORM RIBBON TAB \"CurveTools.EditCurve\"");
                powerMILL.Execute("CURVEEDITOR REPOINT RAISE");
                value = chxSelectMethod.IsChecked == true ? tbxGreenPoints.Text : ConfigurationManager.AppSettings["greenPoints"];
                powerMILL.Execute($"CURVEEDITOR REPOINT POINTS \"{value}\"");
                powerMILL.Execute("FORM APPLY CEREPOINTCURVE");
                powerMILL.Execute("FORM CANCEL CEREPOINTCURVE");
                powerMILL.Execute("FORM RIBBON TAB \"CurveEditor.Edit\"");
                powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");

                CalculateProbingPath(probingTpName, tpName);

                powerMILL.Execute($"ACTIVATE Toolpath \"{probingTpName}\"");
                powerMILL.Execute("EDIT PAR 'Connections.Link[0].ProbingType' 'probing_straight'");
                powerMILL.Execute("PROCESS TPLEADS");
            }
            else if (method == "C" || method == "Z")
            {
                string probingTpName = tpName + "_Probing";
                ClearToolpath(probingTpName);
                CreateWorkplane(level, tpName, chxLean.IsChecked ?? false);

                powerMILL.Execute("STRATEGYSELECTOR CATEGORY 'Probing' NEW");
                powerMILL.Execute("STRATEGYSELECTOR STRATEGY \"Probing/Surface-Inspection.ptf\" NEW");
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
                session.Refresh();
                PMToolpath toolpath = session.Toolpaths.Last();

                PMPattern pattern2 = method == "C" ? CreatePatternOld(level, tpName) : CreatePattern(level, tpName);

                powerMILL.Execute($"ACTIVATE TOOLPATH \"{toolpath.Name}\"");
                powerMILL.Execute($"EDIT TOOLPATH \"{toolpath.Name}\" CLONE");
                powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern2.Name}\"");
                session.Refresh();
                PMToolpath clone = session.Toolpaths.Last();

                MainWindow window = Application.Current.MainWindow as MainWindow;
                window.Link.Apply(probingTpName);

                powerMILL.Execute($"EDIT TOOLPATH \"{clone.Name}\" CALCULATE");
                powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
                powerMILL.Execute("VIEW MODEL ; SHADE NORMAL");
                toolpath.Delete();

                CollisionCheck(clone.Name, probingTpName);
            }
            else if (method == "U型槽模型比对")
            {
                string swarfTpName = tpName + "_Swarf";
                string patternTpName = tpName + "_Pattern";
                string probingTpName = tpName + "_Probing";

                ClearToolpath_V2(probingTpName);
             
                powerMILL.Execute("edit model all deselect all");

                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Swarf-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = swarfTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{swarfTpName}\" FORM TOOLPATH");

                powerMILL.Execute("FORM THICKNESS EDIT THICKNESS TAB COMPONENTS TOOLPATH");
                powerMILL.Execute("EDIT TOOLPATH THICKNESS LIST UPDATE\r 0 NEW");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS COMPONENTS MACHINE");
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS ACQUIRE");
                powerMILL.Execute("EDIT TOOLPATH THICKNESS LIST UPDATE\r 1 NEW");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS COMPONENTS IGNORE");
                powerMILL.Execute("EDIT MODEL ALL SELECT INVERT ALL");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS ACQUIRE");
                powerMILL.Execute("THICKNESS APPLY");
                powerMILL.Execute("THICKNESS ACCEPT");
                powerMILL.Execute("edit model all deselect all");

                powerMILL.Execute("ACTIVATE WORKPLANE \" \"");
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute($"ACTIVATE Tool \"{ConfigurationManager.AppSettings["ENDMILL"]}\"");
                powerMILL.Execute($"EDIT PAR 'Tolerance' \"0.01\"");
                powerMILL.Execute("EDIT PAR 'ReverseAxis' '1'");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'offset_down'");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Active' 1");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Value' \"2\"");
                powerMILL.Execute("EDIT PAR 'AxialDepthOfCut.UserDefined' '1' EDIT PAR 'Stepdown' \"5\"");
                powerMILL.Execute("EDIT TOOLPATH START TYPE POINT_SAFE");
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA MEASURE_FROM BLOCK");
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
                powerMILL.Execute("EDIT TOOLAXIS TYPE AUTOMATIC");
                powerMILL.Execute($"EDIT TOOLPATH \"{swarfTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();
                powerMILL.Execute($"SIMULATE TOOLPATH \"{swarfTpName}\" FORM RIBBON TAB SIMULATION");
                powerMILL.Execute("FORM TPLIST");
                powerMILL.Execute("EDIT TPSELECT ; TPLIST UPDATE\r 0 NEW");
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
                powerMILL.Execute("TPLIST ACCEPT");

                //建立坐标系
                CreateWorkplanebySwarf(swarfTpName, tpName);

                //存坐标系和层，底面要用
                App.Current.Resources["workplane"] = tpName;
                App.Current.Resources["level"] = level;

                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Pattern-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = patternTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{patternTpName}\" FORM TOOLPATH");

                powerMILL.Execute("edit model all deselect all");
                powerMILL.Execute("FORM THICKNESS EDIT THICKNESS TAB COMPONENTS TOOLPATH");
                powerMILL.Execute("EDIT TOOLPATH THICKNESS LIST UPDATE\r 0 NEW");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS COMPONENTS MACHINE");
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS ACQUIRE");
                powerMILL.Execute("EDIT TOOLPATH THICKNESS LIST UPDATE\r 1 NEW");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS COMPONENTS IGNORE");
                powerMILL.Execute("EDIT MODEL ALL SELECT INVERT ALL");
                powerMILL.Execute("EDIT TOOLPATH ; THICKNESS ACQUIRE");
                powerMILL.Execute("THICKNESS APPLY");
                powerMILL.Execute("THICKNESS ACCEPT");
                powerMILL.Execute("edit model all deselect all");

                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                powerMILL.Execute("EDIT PAR 'UseToolpathAsPattern' 1");
                powerMILL.Execute($"EDIT PAR 'ReferenceToolpath' \"{swarfTpName}\"");
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT PAR 'ToolAxis.LeadLeanMode' 'PM2012R2'");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"70.0\"");
                powerMILL.Execute($"ACTIVATE WORKPLANE \"{tpName}\"");
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
                powerMILL.Execute($"EDIT TOOLPATH \"{patternTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                powerMILL.Execute($"CREATE PATTERN {tpName}");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" INSERT TOOLPATH ;");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" CURVEEDITOR START");
                powerMILL.Execute("FORM RIBBON TAB \"CurveTools.EditCurve\"");
                powerMILL.Execute("CURVEEDITOR REPOINT RAISE");
                powerMILL.Execute("CURVEEDITOR REPOINT POINTS \"10\"");
                powerMILL.Execute("FORM APPLY CEREPOINTCURVE");
                powerMILL.Execute("FORM CANCEL CEREPOINTCURVE");
                powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");

                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = probingTpName;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{probingTpName}\" FORM TOOLPATH");
                powerMILL.Execute($"EDIT PAR 'Pattern' \"{tpName}\"");
                powerMILL.Execute($"ACTIVATE Tool \"{cbxTool.Text}\"");               
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");                
                powerMILL.Execute($"EDIT TOOLPATH \"{probingTpName}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                CollisionCheck(probingTpName, probingTpName);

                //删除第一和最后一点
                int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probingTpName}').Statistics.PlungesIntoStock\"").ToString());
                powerMILL.Execute("FORM TPLIST");
                powerMILL.Execute("EDIT TPSELECT ; TPLIST UPDATE\r 0 NEW");
                powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {count - 1} TOGGLE");
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
                powerMILL.Execute("TPLIST ACCEPT");               

                if (App.Current.Resources["workplane"] == null)
                {
                    MessageBox.Show($"必须先计算U型槽模型比对。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                //底面是用槽的曲面和坐标系计算                
                double radius = double.Parse(ConfigurationManager.AppSettings["radius"]);
                CalculateBottom(level + "_底面" + "_" + method, tpName, radius, ConfigurationManager.AppSettings["ENDMILL_D10"], level);
            }
            else if (method == "U型槽底面模型比对")//放到U型槽结尾自动做
            {/*
                if (App.Current.Resources["workplane"] == null)
                {
                    MessageBox.Show($"{method}必须先计算U型槽模型比对。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }

                //底面是用槽的曲面和坐标系计算
                string workplane = App.Current.Resources["workplane"].ToString();
                level = App.Current.Resources["level"].ToString();
                double radius = double.Parse(ConfigurationManager.AppSettings["radius"]);
                CalculateBottom(tpName, workplane, radius, level);*/
            }
            else if (method == "顶端平面")
            {
                string boundary1 = tpName + "_1";
                string boundary2 = tpName + "_2";
                string surface = tpName + "_Surface";
                string pattern = tpName + "_Pattern";
                string probing = tpName + "_Probing";
                string patternU0 = pattern + "_U0";
                string patternU180 = pattern + "_U180";
                string probingU0 = probing + "_U0";
                string probingU180 = probing + "_U180";

                powerMILL.Execute($"DELETE TOOLPATH \"{probing}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{probingU0}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{probingU180}\"");
                powerMILL.Execute($"DELETE PATTERN \"{tpName}\"");
                powerMILL.Execute($"DELETE PATTERN \"{patternU0}\"");
                powerMILL.Execute($"DELETE PATTERN \"{patternU180}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{pattern}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{patternU0}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{patternU180}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{surface}\"");
                powerMILL.Execute($"DELETE BOUNDARY \"{boundary1}\"");
                powerMILL.Execute($"DELETE BOUNDARY \"{boundary2}\"");

                //计算放缩值
                PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;
                double diameter = double.Parse(page.tbxDiameter.Text);
                double minWidth = double.Parse(page.tbxMinWidth.Text);
                double factor = minWidth / diameter / 2;           

                //创建顶端曲面边界并缩小                
                powerMILL.Execute($"CREATE BOUNDARY {boundary1} CONTACTPOINT FORM BOUNDARY");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary1}\" PRIVATE NO");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary1}\" INSERT MODEL");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary1}\" ACCEPT BOUNDARY ACCEPT");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary1}\" CURVEEDITOR START");
                powerMILL.Execute("CURVEEDITOR MODE SCALE");                
                powerMILL.Execute($"MODE TRANSFORM SCALE FACTOR \"{1 - factor}\"");
                powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");

                //创建顶端曲面边界并放大                
                powerMILL.Execute($"CREATE BOUNDARY {boundary2} CONTACTPOINT FORM BOUNDARY");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary2}\" PRIVATE NO");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary2}\" INSERT MODEL");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary2}\" ACCEPT BOUNDARY ACCEPT");
                powerMILL.Execute($"EDIT BOUNDARY \"{boundary2}\" CURVEEDITOR START");
                powerMILL.Execute("CURVEEDITOR MODE SCALE");
                powerMILL.Execute($"MODE TRANSFORM SCALE FACTOR \"{1 + factor}\"");
                powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");                

                //曲面精加工
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Surface-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = surface;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{surface}\" FORM TOOLPATH");
                double toolDiameter = diameter / 2;
                string toolName = $"D{toolDiameter}-ENDMILL";
                if (session.Tools.All(t => t.Name != toolName))
                {
                    powerMILL.Execute($"CREATE TOOL '{toolName}' ENDMILL");
                    powerMILL.Execute($"EDIT TOOL \"{toolName}\" DIAMETER \"{toolDiameter}\"");
                    powerMILL.Execute($"EDIT TOOL \"{toolName}\" LENGTH \"100\"");
                }
                else
                {
                    powerMILL.Execute($"ACTIVATE TOOL \"{toolName}\"");
                }
                powerMILL.Execute($"ACTIVATE BOUNDARY \"{boundary1}\"");
                powerMILL.Execute($"EDIT PAR 'RadialDepthOfCut.UserDefined' '1' Edit Par 'Stepover' \"{diameter / 4}\"");
                powerMILL.Execute("edit model all select all");
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute("edit model all deselect all");
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
                powerMILL.Execute($"EDIT TOOLPATH \"{surface}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                //参考线精加工
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Pattern-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = pattern;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{pattern}\" FORM TOOLPATH");
                powerMILL.Execute("EDIT PAR 'UseToolpathAsPattern' 1");
                powerMILL.Execute($"EDIT PAR 'ReferenceToolpath' \"{surface}\"");
                powerMILL.Execute($"ACTIVATE BOUNDARY \"{boundary2}\"");
                powerMILL.Execute($"EDIT TOOLPATH \"{pattern}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                if (page.cbxOPT.Text == ConfigurationManager.AppSettings["uPmoptz"])//Fidia
                {
                    string pattern1 = pattern + "_U0";
                    string pattern2 = pattern + "_U180";                    

                    //复制一条用来在U180坐标系下面裁剪
                    powerMILL.Execute($"ACTIVATE Toolpath \"{pattern}\"");
                    powerMILL.Execute($"EDIT TOOLPATH \"{pattern}\" CLONE");
                    session.Refresh();
                    PMToolpath cloned = session.Toolpaths.ActiveItem;
                    cloned.Calculate();
                    
                    powerMILL.Execute($"ACTIVATE Toolpath \"{pattern}\"");
                    powerMILL.Execute("ACTIVATE WORKPLANE \" \"");
                    powerMILL.Execute($"QUIT EDITTOOLAXIS CANCEL FORM TPLIMIT");
                    powerMILL.Execute("EDIT TOOLPATH LIMIT PLANEOPTIONS SELECT Y");
                    powerMILL.Execute("EDIT TOOLPATH LIMIT KEEP OUTER");
                    powerMILL.Execute("EDIT TOOLPATH LIMIT DELETE Y");
                    powerMILL.Execute("PROCESS TPLIMIT");
                    powerMILL.Execute("FORM CANCEL TPLIMIT");
                    session.Refresh();
                    session.Toolpaths.ActiveItem.Name = pattern1;
                    
                    powerMILL.Execute($"ACTIVATE Toolpath \"{cloned.Name}\"");
                    powerMILL.Execute("ACTIVATE WORKPLANE \"U180\"");
                    powerMILL.Execute($"QUIT EDITTOOLAXIS CANCEL FORM TPLIMIT");
                    powerMILL.Execute("EDIT TOOLPATH LIMIT PLANEOPTIONS SELECT Y");
                    powerMILL.Execute("EDIT TOOLPATH LIMIT KEEP OUTER");
                    powerMILL.Execute("EDIT TOOLPATH LIMIT DELETE Y");
                    powerMILL.Execute("PROCESS TPLIMIT");
                    powerMILL.Execute("FORM CANCEL TPLIMIT");
                    session.Refresh();
                    session.Toolpaths.ActiveItem.Name = pattern2;

                    CalculateTop(pattern1, patternU0, "U0", probingU0);
                    CalculateTop(pattern2, patternU180, "U180", probingU180);
                }
                else
                {
                    CalculateTop(pattern, tpName, " ", probing);//" "是世界坐标系
                }           
            }     
            else if (method == "顶孔" || method == "侧孔")
            {
                powerMILL.Execute($"DELETE FEATURESET \"{tpName}\"");
                powerMILL.Execute("EDIT FEATURECREATE TYPE HOLE EDIT FEATURECREATE CIRCULAR ON EDIT FEATURECREATE FILTER HOLES EDIT FEATURECREATE TOPDEFINE ABSOLUTE EDIT FEATURECREATE BOTTOMDEFINE ABSOLUTE FORM CANCEL FEATURE FORM CREATEHOLE");
                powerMILL.Execute("EDIT FEATURECREATE CREATEHOLES");
                powerMILL.Execute("FORM CANCEL CREATEHOLE");
                session.Refresh();
                if (session.FeatureSets.ActiveItem == null)
                {
                    MessageBox.Show("无法产生特征集，请确认曲面是否是孔。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                session.FeatureSets.ActiveItem.Name = tpName;    
                
                double holeDiameter = double.Parse(powerMILL.ExecuteEx("print par terse $widget(\"EditHole.Shell.Geom.UpperDia\").Value").ToString());
                double toolDiameter = double.Parse(powerMILL.ExecuteEx($"print par terse \"entity('tool', '{cbxTool.Text}').Diameter\"").ToString());
                if (holeDiameter < toolDiameter * 2)
                {
                    MessageBox.Show("孔直径小于两倍探针直径，无法检测。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                //获取孔数量
                string msg = powerMILL.ExecuteEx($"SIZE FEATURESET \"{tpName}\"").ToString();
                int holdCount = int.Parse(msg.Split('\r')[4].Split(':')[1]);

                string swarf = tpName + "_Swarf";
                string pattern = tpName + "_Pattern";
                string probing = tpName + "_Probing";

                ClearToolpath_V2(probing);

                if (method == "侧孔")
                {
                    powerMILL.Execute($"ACTIVATE WORKPLANE FROMENTITY FEATURESET \"{tpName}\"");
                }
                else if (method == "顶孔")
                {
                    powerMILL.Execute("ACTIVATE WORKPLANE \" \"");
                }

                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Swarf-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = swarf;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{swarf}\" FORM TOOLPATH");                  
                powerMILL.Execute("EDIT BLOCK RESET");
                powerMILL.Execute($"ACTIVATE Tool \"{ConfigurationManager.AppSettings["ENDMILL"]}\"");
                powerMILL.Execute("EDIT PAR 'Tolerance' \"0.01\"");
                powerMILL.Execute("EDIT PAR 'MultipleCuts' 'offset_down'");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Active' 1");
                powerMILL.Execute("EDIT PAR 'StepdownLimit.Value' \"1\"");
                powerMILL.Execute($"EDIT TOOLPATH \"{swarf}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                powerMILL.Execute("EDIT TOOLPATH LEADS RAISEFORM");
                powerMILL.Execute("EDIT TOOLPATH START TYPE POINT_SAFE");
                powerMILL.Execute("RESET TOOLPATH START_END");
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
                powerMILL.Execute("PROCESS TPLEADS");
                powerMILL.Execute("LEADS ACCEPT");                       

                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Pattern-Finishing.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = pattern;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{pattern}\" FORM TOOLPATH");                
                powerMILL.Execute("EDIT PAR 'UseToolpathAsPattern' 1");
                powerMILL.Execute($"EDIT PAR 'ReferenceToolpath' \"{swarf}\"");
                powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");
                powerMILL.Execute("EDIT PAR 'ToolAxis.LeadLeanMode' 'PM2012R2'");
                powerMILL.Execute("EDIT TOOLAXIS LEAN \"80.0\"");
                powerMILL.Execute($"EDIT TOOLPATH \"{pattern}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                powerMILL.Execute($"CREATE PATTERN {tpName}");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" INSERT TOOLPATH ;");
                powerMILL.Execute($"EDIT PATTERN \"{tpName}\" CURVEEDITOR START");
                powerMILL.Execute("CURVEEDITOR MODE TRANSLATE");
                PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;
                powerMILL.Execute($"MODE COORDINPUT COORDINATES 0 0 -{page.tbxHoleDistance.Text}");
                if (method == "侧孔")
                {
                    powerMILL.Execute("MODE TRANSFORM COPY YES");
                    powerMILL.Execute($"MODE COORDINPUT COORDINATES 0 0 -{page.tbxHoleDepth.Text}");
                }
                powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");

                if (method == "顶孔")
                {
                    powerMILL.Execute($"EDIT PATTERN \"{tpName}\" CURVEEDITOR START");
                    //int segment = holdCount * 2;//参考线段数等于洞数 * （切削次数 + 复制次数）
                    int segment = holdCount;//不复制了，保留上面4个点即可
                    for (int i = 0; i < segment; i++)
                    {
                        powerMILL.Execute($"EDIT PATTERN \"{tpName}\" DESELECT ALL");
                        powerMILL.Execute($"EDIT PATTERN \"{tpName}\" SELECT {i}");
                        powerMILL.Execute("CURVEEDITOR REPOINT RAISE");
                        powerMILL.Execute("CURVEEDITOR REPOINT POINTS \"4\"");
                        powerMILL.Execute("FORM APPLY CEREPOINTCURVE");
                        powerMILL.Execute("FORM CANCEL CEREPOINTCURVE");
                    }
                    powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");
                }

                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
                session.Refresh();
                session.Toolpaths.ActiveItem.Name = probing;
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{probing}\" FORM TOOLPATH");
                powerMILL.Execute($"EDIT PAR 'Pattern' \"{tpName}\"");
                powerMILL.Execute($"ACTIVATE Tool \"{cbxTool.Text}\"");
                powerMILL.Execute($"EDIT PAR 'Probing.Approach' \"{toolDiameter}\"");
                powerMILL.Execute($"EDIT PAR 'Probing.Retract' \"{toolDiameter}\"");
                powerMILL.Execute($"EDIT TOOLPATH \"{probing}\" REAPPLYFROMGUI\rYes");
                session.Toolpaths.ActiveItem.Calculate();

                CollisionCheck(probing, probing);
                if (method == "侧孔")
                {
                    KeepPointsByPatternAndPoint(probing, 2, 4);
                }

                powerMILL.Execute("EDIT TOOLPATH LEADS RAISEFORM");
                powerMILL.Execute("EDIT TOOLPATH LEADS PAGE LINK");
                powerMILL.Execute("EDIT PAR 'Connections.Link[0].ProbingType' 'probing_skim'");
                powerMILL.Execute("PROCESS TPLEADS");
                powerMILL.Execute("EDIT TOOLPATH END TYPE POINT_SAFE");
                powerMILL.Execute("RESET TOOLPATH START_END");
                powerMILL.Execute("LEADS ACCEPT");                
            }
            else
            {

            }

            RefreshToolpaths();
        }

        /// <summary>
        /// 参考线精加工之后的顶端计算
        /// </summary>
        /// <param name="toolpath">插入参考线的刀路</param>
        /// <param name="pattern">参考线名称</param>
        /// <param name="workplane">坐标系</param>
        /// <param name="probing">检测路径名称</param>
        private void CalculateTop(string toolpath, string pattern, string workplane, string probing)
        {
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{toolpath}\"");
            powerMILL.Execute($"CREATE PATTERN {pattern}");
            powerMILL.Execute($"EDIT PATTERN \"{pattern}\" INSERT TOOLPATH ;");

            powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
            session.Refresh();
            session.Toolpaths.ActiveItem.Name = probing;
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{probing}\" FORM TOOLPATH");
            powerMILL.Execute($"ACTIVATE WORKPLANE \"{workplane}\"");
            powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern}\"");
            powerMILL.Execute($"ACTIVATE Tool \"{cbxTool.Text}\"");
            powerMILL.Execute($"EDIT TOOLPATH \"{probing}\" REAPPLYFROMGUI\rYes");
            session.Toolpaths.ActiveItem.Calculate();

            CollisionCheck(probing, probing);

            //删除奇数点
            int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probing}').Statistics.PlungesIntoStock\"").ToString());
            powerMILL.Execute("FORM TPLIST");
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 1)
                {
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {i} TOGGLE");
                }
            }
            powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
            powerMILL.Execute("TPLIST ACCEPT");
            powerMILL.Execute("EDIT TOOLPATH LEADS RAISEFORM");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
            powerMILL.Execute("EDIT TOOLPATH START TYPE POINT_SAFE");
            powerMILL.Execute("RESET TOOLPATH START_END");
            powerMILL.Execute("PROCESS TPLEADS");
            powerMILL.Execute("LEADS ACCEPT");
            powerMILL.Execute("FORM TPLIST");
            powerMILL.Execute("EDIT TOOLPATH REORDER N");
            powerMILL.Execute("TPLIST ACCEPT");            
        }

        /// <summary>
        /// 计算底面
        /// </summary>
        /// <param name="tpPrefix">刀路前缀</param>
        /// <param name="workplane">坐标系</param>
        /// <param name="shrink">毛坯缩小值</param>
        /// <param name="tool">刀具</param>
        /// <param name="levels">层</param>
        public void CalculateBottom(string tpPrefix, string workplane, double shrink, string tool, params string[] levels)
        {
            powerMILL.Execute("edit model all deselect all");
            foreach (string level in levels)
            {
                powerMILL.Execute($"EDIT LEVEL \"{level}\" SELECT ALL");
            }

            string finishing = tpPrefix + "_Finishing";
            string probing = tpPrefix + "_Probing";
            powerMILL.Execute($"DELETE TOOLPATH \"{probing}\"");
            powerMILL.Execute($"DELETE PATTERN \"{tpPrefix}\"");
            powerMILL.Execute($"DELETE TOOLPATH \"{finishing}\"");
            powerMILL.Execute($"ACTIVATE WORKPLANE \"{workplane}\"");
            powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Finishing/Raster-Finishing.002.ptf\"");
            session.Refresh();
            session.Toolpaths.ActiveItem.Name = finishing;
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{finishing}\" FORM TOOLPATH");
            powerMILL.Execute("FORM THICKNESS EDIT THICKNESS TAB COMPONENTS TOOLPATH");
            powerMILL.Execute("EDIT TOOLPATH THICKNESS LIST UPDATE\r 1 NEW");
            powerMILL.Execute("EDIT TOOLPATH ; THICKNESS COMPONENTS MACHINE");
            powerMILL.Execute("THICKNESS ACCEPT");
            powerMILL.Execute("EDIT PAR 'Tolerance' \"0.1\"");
            powerMILL.Execute("EDIT TPPAGE SWBlock");
            powerMILL.Execute("EDIT BLOCK COORDINATE WORKPLANE");
            powerMILL.Execute("EDIT BLOCK RESET");            
            double minX = double.Parse(powerMILL.ExecuteEx("print par terse $widget(\"SFRasterFin.Shell.SWBlock.LimitFrame.MinX\").Value").ToString());
            powerMILL.Execute($"EDIT BLOCK XMIN \"{minX + shrink}\"");
            double minY = double.Parse(powerMILL.ExecuteEx("print par terse $widget(\"SFRasterFin.Shell.SWBlock.LimitFrame.MinY\").Value").ToString());
            powerMILL.Execute($"EDIT BLOCK YMIN \"{minY + shrink}\"");
            double maxX = double.Parse(powerMILL.ExecuteEx("print par terse $widget(\"SFRasterFin.Shell.SWBlock.LimitFrame.MaxX\").Value").ToString());
            powerMILL.Execute($"EDIT BLOCK XMAX \"{maxX - shrink}\"");
            double maxY = double.Parse(powerMILL.ExecuteEx("print par terse $widget(\"SFRasterFin.Shell.SWBlock.LimitFrame.MaxY\").Value").ToString());
            powerMILL.Execute($"EDIT BLOCK YMAX \"{maxY - shrink}\"");
            double minZ = double.Parse(powerMILL.ExecuteEx("print par terse $widget(\"SFRasterFin.Shell.SWBlock.LimitFrame.MinZ\").Value").ToString());
            powerMILL.Execute($"EDIT BLOCK ZMIN \"{minZ - 10}\"");//Z最小固定减少10
            
            powerMILL.Execute($"ACTIVATE TOOL \"{tool}\"");
            //powerMILL.Execute($"ACTIVATE WORKPLANE \"{workplane}\"");
            powerMILL.Execute("EDIT TOOLAXIS TYPE LEADLEAN");            
            powerMILL.Execute("EDIT TOOLAXIS LEAN 0.0");
            //powerMILL.Execute("EDIT TOOLAXIS TYPE VERTICAL");
            powerMILL.Execute("EDIT PAR 'ToolAxis.LeadLeanMode' 'contact_normal'");
            powerMILL.Execute($"EDIT TOOLPATH \"{finishing}\" REAPPLYFROMGUI\rYes");
            session.Toolpaths.ActiveItem.Calculate();

            powerMILL.Execute($"CREATE PATTERN {tpPrefix}");
            powerMILL.Execute($"EDIT PATTERN \"{tpPrefix}\" INSERT TOOLPATH ;");

            powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
            session.Refresh();
            session.Toolpaths.ActiveItem.Name = probing;
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{probing}\" FORM TOOLPATH");
            powerMILL.Execute($"EDIT PAR 'Pattern' \"{tpPrefix}\"");
            powerMILL.Execute($"ACTIVATE Tool \"{cbxTool.Text}\"");
            powerMILL.Execute($"EDIT TOOLPATH \"{probing}\" REAPPLYFROMGUI\rYes");
            session.Toolpaths.ActiveItem.Calculate();
            /*
            powerMILL.Execute("FORM COLLISION");
            powerMILL.Execute("EDIT COLLISION APPLY");
            powerMILL.Execute("EDIT COLLISION TYPE GOUGE");
            powerMILL.Execute("EDIT COLLISION APPLY");
            powerMILL.Execute("COLLISION ACCEPT");
            */
            CollisionCheck(probing, probing);

            //取两个三等分点
            int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probing}').Statistics.PlungesIntoStock\"").ToString());
            powerMILL.Execute("FORM TPLIST");
            for (int i = 0; i < count; i++)
            {
                if (i == count / 3 || i == count - count / 3)
                {
                    continue;
                }
                powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {i} TOGGLE");
            }
            powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
            powerMILL.Execute("TPLIST ACCEPT");
            //CollisionCheck(probing, probing);
        }

        /// <summary>
        /// 根据Swarf刀路仿真建立坐标系
        /// </summary>
        /// <param name="swarf">Swarf刀路名</param>
        /// <param name="workplane">坐标系名</param>
        public void CreateWorkplanebySwarf(string swarf, string workplane)
        {            
            powerMILL.Execute("ACTIVATE WORKPLANE \" \"");
            powerMILL.Execute($"ACTIVATE Toolpath \"{swarf}\"");
            powerMILL.Execute("QUIT EDITTOOLAXIS CANCEL FORM TPLIMIT");
            powerMILL.Execute("PROCESS TPLIMIT");
            powerMILL.Execute("FORM CANCEL TPLIMIT");
            session.Refresh();
            string toolpath = swarf + "_1";//用来建立坐标系的刀路，有些情况会新建
            if (session.Toolpaths.Any(t => t.Name == toolpath))
            {
                toolpath = swarf + "_1";
            }     
            else
            {
                toolpath = swarf;
            }
            powerMILL.Execute($"SIMULATE TOOLPATH \"{toolpath}\" FORM RIBBON TAB SIMULATION");
            string elevation = powerMILL.ExecuteEx("print par terse $widget(\"ToolPosition.AxisDirection.AxisElevation\").Value").ToString();//仰角
            string azimuth = powerMILL.ExecuteEx("print par terse $widget(\"ToolPosition.AxisDirection.AxisAzimuth\").Value").ToString();//方位角
            if (elevation == "" || azimuth == "")
            {
                MessageBox.Show("不能计算方位角，请检测刀路。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            powerMILL.Execute($"CREATE WORKPLANE {workplane} EDITOR");
            powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Z");
            powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"{azimuth}\"");
            powerMILL.Execute("WPETWIST ACCEPT");
            powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Y");
            powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"{90 - double.Parse(elevation)}\"");
            powerMILL.Execute("WPETWIST ACCEPT");
            powerMILL.Execute("MODE WORKPLANE_EDIT FINISH ACCEPT");
            powerMILL.Execute($"ACTIVATE Workplane \"{workplane}\"");
            if (toolpath != swarf)
            {
                powerMILL.Execute($"DELETE TOOLPATH \"{toolpath}\"");
            }
        }

        public void CalculateProbingPath(string probingTpName, string patternName)
        {
            powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTOR \"Probing/Surface-Inspection.ptf\"");
            session.Refresh();
            session.Toolpaths.ActiveItem.Name = probingTpName;
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{probingTpName}\" FORM TOOLPATH");
            powerMILL.Execute($"EDIT PAR 'Pattern' \"{patternName}\"");
            powerMILL.Execute($"ACTIVATE Tool \"{cbxTool.Text}\"");
            (Application.Current.MainWindow as MainWindow).Link.Apply(probingTpName);            
            powerMILL.Execute($"EDIT TOOLPATH \"{probingTpName}\" REAPPLYFROMGUI\rYes");
            session.Toolpaths.ActiveItem.Calculate();

            //刀路优化
            powerMILL.Execute($"ACTIVATE Toolpath \"{probingTpName}\"");
            powerMILL.Execute($"EDIT TOOLPATH LEADS RAISEFORM");
            powerMILL.Execute("EDIT TOOLPATH LEADS PAGE LINK");
            powerMILL.Execute("EDIT PAR 'Connections.Link[0].ProbingType' 'probing_straight'");
            powerMILL.Execute("EDIT PAR 'Connections.Link[0].ApplyConstraints' '1'");
            powerMILL.Execute("EDIT PAR 'Connections.Link[1].ProbingType' 'probing_skim'");
            powerMILL.Execute("EDIT PAR 'Connections.Link[1].ApplyConstraints' '0'");
            powerMILL.Execute("PROCESS TPLINKS");
            powerMILL.Execute("EDIT TOOLPATH LEADS PAGE RAPIDMOVES");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA APPLY");
            powerMILL.Execute("EDIT TOOLPATH LEADS PAGE STARTENDPT");
            powerMILL.Execute("EDIT TOOLPATH START TYPE POINT_SAFE");
            powerMILL.Execute("EDIT TOOLPATH END TYPE POINT_SAFE");
            powerMILL.Execute("RESET TOOLPATH START_END");
            powerMILL.Execute("PROCESS TPLEADS");
            powerMILL.Execute("LEADS ACCEPT");

            /*
            powerMILL.Execute("EDIT COLLISION TYPE GOUGE");
            string message = powerMILL.ExecuteEx("EDIT COLLISION APPLY").ToString();
            if (message == "信息： 整个刀具路径过切" || message == "信息： 找不到过切")
            {

            }
            else
            {
                powerMILL.Execute($"DELETE TOOLPATH \"{probingTpName}\"");
                powerMILL.Execute($"DELETE TOOLPATH \"{probingTpName + "_2"}\"");
                powerMILL.Execute($"RENAME Toolpath \"{probingTpName + "_1"}\" \"{probingTpName}\"");
            }
            */
            CollisionCheck(probingTpName, probingTpName);

            string method = probingTpName.Split('_')[1];
            if (method == "竖面（角度）" || method == "竖面（距离）" || method == "横面（距离）")
            {
                KeepPointsByPattern(probingTpName);
            }
            else if (method == "顶端内侧圆弧")
            {
                PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;
                int curve = int.Parse(page.tbxCurve.Text);
                int curvePoint = int.Parse(page.tbxCurvePoint.Text);

                KeepPointsByPatternAndPoint(probingTpName, curve, curvePoint);
            }
            else if (method == "侧孔" || method == "顶孔")
            {
                //上下两条线各4点
                KeepPointsByPatternAndPoint(probingTpName, 2, 4);
            }
            else if (method == "底面")
            {
                //取两个三等分点
                int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probingTpName}').Statistics.PlungesIntoStock\"").ToString());
                powerMILL.Execute("FORM TPLIST");
                for (int i = 0; i < count; i++)
                {
                    if (i == count / 3 || i == count - count / 3)
                    {
                        continue;
                    }
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {i} TOGGLE");
                }
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
                powerMILL.Execute("TPLIST ACCEPT");
            }
            else if (method == "U型槽模型比对")
            {
                //删除第一和最后一点
                int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probingTpName}').Statistics.PlungesIntoStock\"").ToString());
                powerMILL.Execute("FORM TPLIST");
                powerMILL.Execute("EDIT TPSELECT ; TPLIST UPDATE\r 0 NEW");
                powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {count - 1} TOGGLE");
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
                powerMILL.Execute("TPLIST ACCEPT");
            }
            else if (method == "顶端平面")
            {
                //删除奇数点
                int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probingTpName}').Statistics.PlungesIntoStock\"").ToString());
                powerMILL.Execute("FORM TPLIST");
                for (int i = 0; i < count; i++)
                {
                    if (i % 2 == 1)
                    {
                        powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {i} TOGGLE");
                    }
                }
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
                powerMILL.Execute("TPLIST ACCEPT");
            }
        }

        /// <summary>
        /// 根据参考线段数，和每段的点数保留点
        /// </summary>
        /// <param name="probing">检测路径</param>
        /// <param name="pattern">参考线段数</param>
        /// <param name="point">每段要保留的点数</param>
        public void KeepPointsByPatternAndPoint(string probing, int pattern, int point)
        {
            powerMILL.Execute($"ACTIVATE Toolpath \"{probing}\"");
            int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probing}').Statistics.PlungesIntoStock\"").ToString());
            int length = count / pattern;//每段圆弧的点数
            int step = length / (point + 1);//每段圆弧内一段的长度
            List<int> points = new List<int>();//保存要保留的点
            for (int i = 0; i < pattern; i++)
            {
                int start = length * i;
                for (int j = 1; j <= point; j++)
                {
                    points.Add(start + j * step);
                }
            }
            for (int i = 0; i < count; i++)
            {
                if (!points.Contains(i))
                {
                    powerMILL.Execute($"EDIT TPSELECT ; TPLIST UPDATE\r {i} TOGGLE");
                }
            }
            powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
        }

        public void KeepPointsByPattern(string tpName/*, string tag*/)
        {
            powerMILL.Execute($"ACTIVATE Toolpath \"{tpName}\"");
            //powerMILL.Execute("EDIT TOOLPATH REORDER N");
            int count = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{tpName}').Statistics.PlungesIntoStock\"").ToString());//检测路径点数
            int max = chxSelectMethod.IsChecked == true ? int.Parse(cbxPoints.Text) : int.Parse(ConfigurationManager.AppSettings["points"]);//要保留的最大点数
            if (count > max)
            {
                //string swarfTpName = tpName.Substring(0, tpName.IndexOf("Probing") + "Probing".Length).Replace("Probing", "Swarf");
                //int stepdown = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{swarfTpName}').StepdownLimit.Value\"").ToString());//参考线条数
                /*
                int section;//两个面，所以参选线段数量要乘以2。参考线减一是因为红面之前删除了一条
                if (tag == "S")
                {
                    section = stepdown * 2;
                }
                else if (tag == "P")
                {
                    section = (stepdown - 1) * 2;
                }
                else
                {
                    return;
                }                
                */

                int pattern = chxSelectMethod.IsChecked == true ? int.Parse(cbxStepdownLimit.Text) : int.Parse(ConfigurationManager.AppSettings["StepdownLimit"]);//参考线条数
                int section = pattern * 2;//因为有两个面，参考线段数要*2
                int step = count / section;//每一段的点数量
                int start = 0;
                List<int> points = new List<int>();//要保留的点

                for (int i = 0; i < section; i++)
                {
                    int center = start + step / 2;
                    int a, b;//前后两点
                    if (step > 8)//每段点数大于8
                    {
                        a = center - 2;
                        b = center + 2;
                    }
                    else
                    {
                        a = center - 1;
                        b = center + 1;
                    }
                    points.Add(a);
                    points.Add(b);//这里已经在一段参考线上加了2点了，如果section == 6，即3条参考线，一定就是12个点

                    if (section == 4)//2条参考线
                    {
                        //如果section == 4，即2条参考线，不加就是8个点
                        if (max == 10 && i % 2 == 0)//保留10个点，奇数段加上中点
                        {
                            points.Add(center);
                        }
                        else if (max == 12)//保留12个点，每段都加上中点
                        {
                            points.Add(center);
                        }
                    }

                    if (section == 4 && i % 2 == 0)//自动计算保留5点的
                    {
                        //points.Add(center);//又要保留4点
                    }
                    start += step;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!points.Contains(i))
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
                }
                powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
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

        /// <summary>
        /// 通过检测刀路前缀删除所有刀路，参考线，工作平面，刀具
        /// </summary>
        /// <param name="tpName"></param>
        private void ClearToolpath_V2(string probingTpName, bool isCopied = false)
        {
            session.Refresh();

            string orgProbingPath = probingTpName.Substring(0, probingTpName.IndexOf("Probing") + "Probing".Length);//去掉旋转过后的角度字符
            string swarfTpName = orgProbingPath.Replace("Probing", "Swarf");
            string patternTpName = orgProbingPath.Replace("Probing", "Pattern");

            PMToolpath toolpath = session.Toolpaths.FirstOrDefault(t => t.Name == probingTpName);
            if (toolpath != null)
            {
                string patternName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probingTpName}').Pattern.Name\"").ToString();
                PMPattern pattern = session.Patterns.FirstOrDefault(p => p.Name == patternName);
                PMWorkplane workplane = toolpath.WorkplaneInformation;

                session.Toolpaths.Remove(toolpath, true);
                session.Workplanes.Remove(workplane, true);
                if (pattern != null)
                {
                    session.Patterns.Remove(pattern, true);
                }
            }

            if (isCopied == false)
            {
                toolpath = session.Toolpaths.FirstOrDefault(t => t.Name == patternTpName);
                if (toolpath != null)
                {
                    session.Toolpaths.Remove(toolpath, true);
                }

                toolpath = session.Toolpaths.FirstOrDefault(t => t.Name == swarfTpName);
                if (toolpath != null)
                {
                    //powerMILL.Execute($"DELETE TOOL '{toolpath.ToolName}' NOQUIBBLE");
                    session.Toolpaths.Remove(toolpath, true);
                }
            }
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

        /// <summary>
        /// 产出Fidia设备的后处理文件
        /// </summary>
        /// <param name="uPmoptz">分角度的后处理</param>
        /// <param name="totalPmoptz">Total后处理</param>
        /// <param name="totalNCFile">输出的NC文件名</param>
        /// <param name="ncProgram">PM中的NC程序名</param>
        private void GenerateFidiaNC(string uPmoptz, string totalPmoptz, string totalNCFile, string ncProgram)
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

            foreach (NCOutput n in NCOutputs)
            {
                powerMILL.Execute($"CREATE NCPROGRAM '{n.NC}'");
            }

            session.Refresh();
            foreach (PMToolpath toolpath in session.Toolpaths.Where(tp => tp.IsCalculated))
            {
                string strategy = powerMILL.ExecuteEx($"PRINT PAR terse \"entity('toolpath', '{toolpath.Name}').Strategy\"").ToString();
                if (strategy == "surface_inspection")
                {
                    double azimuth;
                    if (toolpath.WorkplaneName == "#错误: 无效名称")//用的世界坐标系
                    {
                        azimuth = 0;
                    }
                    else
                    {
                        azimuth = double.Parse(powerMILL.ExecuteEx($"PRINT PAR terse \"entity('workplane', '{toolpath.WorkplaneName}').ZAngle\"").ToString());
                        if (azimuth < 0.0)
                        {
                            azimuth = 360 + azimuth;
                        }
                        //对于U型槽出NC时，还需要将其坐标系方位角加上180度，然后再判断是否在0-360内，超出的减去360.使用这个值来分配到合适的分度角。
                        if (toolpath.Name.Contains("U型槽模型比对"))
                        {
                            if (azimuth + 180 > 360)
                            {
                                azimuth = azimuth + 180 - 360;
                            }
                        }
                    }
                    NCOutput output = NCOutputs.First(n => azimuth >= n.Angle && azimuth < n.Angle + int.Parse(ConfigurationManager.AppSettings["uAngle"]));
                    powerMILL.Execute($"ACTIVATE NCProgram \"{output.NC}\"");
                    powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpath.Name}\"");
                }
            }

            List<string> ncFiles = new List<string>();
            string opt = uPmoptz;
            foreach (NCOutput n in NCOutputs)
            {
                if (ConfigurationManager.AppSettings["mock"] == "true")
                {
                    //产生测试用的tap
                    opt = totalPmoptz;
                }
                ncFiles.Add(ExportNC(n.NC, opt, n.Workplane, n.NC + ".tap"));                
            }            

            if (ConfigurationManager.AppSettings["mock"] == "true")
            {
                //生成测试用的msr
                string msr = System.IO.Path.GetFileNameWithoutExtension(totalNCFile);
                StreamWriter writer = new StreamWriter(ConfigurationManager.AppSettings["msrFolder"] + $"\\{msr}-001.msr", false);
                int i = 0;
                foreach (string tap in ncFiles.Where(n => !string.IsNullOrWhiteSpace(n)))
                {
                    StreamReader reader = new StreamReader(tap);
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        if (line.Contains("R3.0"))
                        {
                            string x = line.Substring(line.IndexOf("X") + 1, line.IndexOf("Y") - (line.IndexOf("X") + 1)).Trim();
                            string y = line.Substring(line.IndexOf("Y") + 1, line.IndexOf("Z") - (line.IndexOf("Y") + 1)).Trim();
                            string z = line.Substring(line.IndexOf("Z") + 1, line.IndexOf("R") - (line.IndexOf("Z") + 1)).Trim();
                            writer.WriteLine($"N{(i++).ToString().PadLeft(7)}X{x.PadLeft(11)}Y{y.PadLeft(11)}Z{z.PadLeft(11)}");
                        }
                        line = reader.ReadLine();
                    }
                    reader.Close();
                }
                writer.Close();
            }
            else
            {
                string projectName = powerMILL.ExecuteEx("print $project_pathname(1)").ToString().Trim();
                string totalFolder = ConfigurationManager.AppSettings["ncFolder"] + "\\" + projectName + $"\\{ncProgram}";
                if (!Directory.Exists(totalFolder))
                {
                    Directory.CreateDirectory(totalFolder);
                }

                StreamReader reader = new StreamReader(AppContext.BaseDirectory + @"\NC\NC_Header.tap");
                StreamWriter writer = new StreamWriter($"{totalFolder}\\{totalNCFile}", false);
                writer.Write(reader.ReadToEnd().Replace("OMV", totalNCFile.Replace(".nc", "-00X")));//DGT => IPC C:\MSR\OMV.msr这个行替换成OMV_零件名.msr
                reader.Close();
                writer.WriteLine();

                foreach (string ncFile in ncFiles.Where(n => !string.IsNullOrEmpty(n)))
                {
                    reader = new StreamReader(ncFile);
                    writer.Write(reader.ReadToEnd());
                    reader.Close();
                }

                writer.WriteLine();
                reader = new StreamReader(AppContext.BaseDirectory + @"\NC\NC_Booter.tap");
                writer.Write(reader.ReadToEnd());
                reader.Close();
                writer.Close();
            }
            
            powerMILL.Execute($"CREATE NCPROGRAM '{ncProgram}'");
            session.Refresh();
            foreach (PMNCProgram program in session.NCPrograms.Where(n => n.Name != ncProgram))
            {
                /*
                string output = powerMILL.ExecuteEx($"EDIT NCPROGRAM '{program.Name}' LIST").ToString();
                string[] toolpaths = output.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 2; i < toolpaths.Length - 1; i++)
                {
                    powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpaths[i]}\"");
                }*/
                ///此方法有bug， program.Toolpaths包含的刀路是乱的
                foreach (PMToolpath toolpath in program.Toolpaths)
                {
                    powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpath.Name}\"");
                }
            }
            
            ExportNC(ncProgram, totalPmoptz, "NC", totalNCFile.Replace(".nc", ".tap"));
            powerMILL.Execute("PROJECT SAVE");

            MessageBox.Show("NC程序生成完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        /// <summary>
        /// 产生其他设备的后置
        /// </summary>
        /// <param name="totalPmoptz">后处理文件</param>
        /// <param name="ncFile">输出的NC文件</param>
        /// <param name="ncProgram">PM中的NC程序名称</param>
        private void GenerateOtherNC(string totalPmoptz, string ncFile, string ncProgram)
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
            powerMILL.Execute($"CREATE NCPROGRAM '{ncProgram}'");            
            session.Refresh();

            foreach (PMToolpath toolpath in session.Toolpaths.Where(tp => tp.IsCalculated))
            {
                string strategy = powerMILL.ExecuteEx($"PRINT PAR terse \"entity('toolpath', '{toolpath.Name}').Strategy\"").ToString();
                if (strategy == "surface_inspection")
                {                    
                    powerMILL.Execute($"EDIT NCPROGRAM ; APPEND TOOLPATH \"{toolpath.Name}\"");
                }
            }

            ExportNC(ncProgram, totalPmoptz, "NC", ncFile);//"NC"在PM输出中表示世界坐标系
            powerMILL.Execute("PROJECT SAVE");

            MessageBox.Show("NC程序生成完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private void BtnGenerateNC_Click(object sender, RoutedEventArgs e)
        {
            ConnectToPM();

            PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;
            string part = page.tbxPart.Text.Trim();
            if (string.IsNullOrEmpty(part))
            {
                MessageBox.Show("请在参数设置页输入零件名称。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string partNumber = page.tbxPartNumber.Text.Trim();
            if (string.IsNullOrEmpty(partNumber))
            {
                MessageBox.Show("请在参数设置页输入零件代码。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string equipment = page.tbxEquipment.Text.Trim();
            if (string.IsNullOrEmpty(equipment))
            {
                MessageBox.Show("请在参数设置页输入测量设备。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string process = page.tbxProcess.Text.Trim();
            if (string.IsNullOrEmpty(process))
            {
                MessageBox.Show("请在参数设置页输入工序号。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string stage = page.tbxStage.Text.Trim();
            if (string.IsNullOrEmpty(stage))
            {
                MessageBox.Show("请在参数设置页输入阶段标记。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            string ncProgram = $"{partNumber}.{stage}.{process}.ZXJC.{equipment}.001";
            string ncFile = $"OMV.{partNumber}.{stage}.{process}.nc";            

            if (page.cbxOPT.Text == ConfigurationManager.AppSettings["uPmoptz"])//Fidia
            {
                string uPmoptz = AppContext.BaseDirectory + @"\Pmoptz\" + ConfigurationManager.AppSettings["uPmoptz"] + ".pmoptz";
                string totalPmoptz = AppContext.BaseDirectory + @"\Pmoptz\" + ConfigurationManager.AppSettings["totalPmoptz"] + ".pmoptz";
                GenerateFidiaNC(uPmoptz, totalPmoptz, ncFile, ncProgram);
            }
            else
            {
                string totalPmoptz = AppContext.BaseDirectory + @"\Pmoptz\" + page.cbxOPT.Text + ".pmoptz";
                GenerateOtherNC(totalPmoptz, ncFile, ncProgram);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ncProgram">PM中的NC程序名</param>
        /// <param name="opt">后置文件完整路径</param>
        /// <param name="workplane">PM中的坐标系名</param>
        /// <param name="ncFile">输出的NC文件名</param>
        /// <returns>生成的后置文件完整路径</returns>
        public string ExportNC(string ncProgram, string opt, string workplane, string ncFile)
        {
            session.Refresh();
            PMNCProgram program = session.NCPrograms.FirstOrDefault(n => n.Name == ncProgram);
            string ncPath = string.Empty;
            
            if (program != null)
            {
                string output = powerMILL.ExecuteEx($"EDIT NCPROGRAM '{program.Name}' LIST").ToString();
                string[] toolpaths = output.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (toolpaths.Length > 3)
                {
                    powerMILL.Execute($"EDIT NCPROGRAM \"{ncProgram}\" QUIT FORM NCTOOLPATH");
                    //string path = powerMILL.ExecuteEx($"print par terse \"entity('ncprogram', '{nc}').filename\"").ToString();
                    //path = path.Insert(path.IndexOf("{ncprogram}"), nc + "/");
                    string projectName = powerMILL.ExecuteEx("print $project_pathname(1)").ToString().Trim();
                    string path = ConfigurationManager.AppSettings["ncFolder"] + "\\" + projectName + "\\" + ncProgram;
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    ncPath = path + "\\" + ncFile;
                    powerMILL.Execute($"EDIT NCPROGRAM \"{ncProgram}\" FILENAME FILESAVE\r'{ncPath}'");
                    powerMILL.Execute($"EDIT NCPROGRAM '{ncProgram}' SET WORKPLANE \"{workplane}\"");
                    powerMILL.Execute($"EDIT NCPROGRAM \"{ncProgram}\" TAPEOPTIONS \"{opt}\" FORM ACCEPT SelectOptionFile");

                    powerMILL.Execute($"EDIT NCPROGRAM \"{ncProgram}\" TOOLCOORDS CENTRE");
                    powerMILL.Execute($"ACTIVATE NCPROGRAM \"{ncProgram}\" KEEP NCPROGRAM ;\rYes\rYes");

                    powerMILL.Execute($"NCTOOLPATH ACCEPT FORM ACCEPT NCTOOLPATHLIST FORM ACCEPT NCTOOLLIST FORM ACCEPT PROBINGNCOPTS");
                    powerMILL.Execute("TEXTINFO ACCEPT");
                    
                    if (ncProgram.Contains("ZXJC")) //保存刀路信息，生成PI报告的时候要用
                    {                        
                        string pmFolder = powerMILL.ExecuteEx("print $project_pathname(0)").ToString().Trim();                        

                        List<Model.Toolpath> probed = new List<Model.Toolpath>();
                        foreach (PMToolpath p in program.Toolpaths)
                        {
                            Model.Toolpath probe = new Model.Toolpath() { Name = p.Name };
                            probe.Point = int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{probe.Name}').Statistics.PlungesIntoStock\"").ToString());//点数
                            if (probe.Name.Contains("顶孔"))
                            {
                                string feature = probe.Name.Replace("_Probing", "");
                                string msg = powerMILL.ExecuteEx($"SIZE FEATURESET \"{feature}\"").ToString();
                                probe.Hole = int.Parse(msg.Split('\r')[4].Split(':')[1]);
                            }
                            probed.Add(probe);
                        }

                        //存Fidia分角度
                        List<NCOutput> ncOutputs = new List<NCOutput>();
                        foreach (PMNCProgram p in session.NCPrograms.Where(nc => !nc.Name.Contains("ZXJC")))
                        {
                            string status = powerMILL.ExecuteEx($"print par terse \"entity('ncprogram', '{p.Name}').Status\"").ToString();
                            if (status == "written")
                            {
                                double angle = double.Parse(powerMILL.ExecuteEx($"print par terse \"entity('ncprogram', '{p.Name}').OutputWorkplane.ZAngle\"").ToString()) / 180 * Math.PI;
                                //nc程序中的点数                
                                int count = p.Toolpaths.Sum(t => int.Parse(powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{t.Name}').Statistics.PlungesIntoStock\"").ToString()));
                                ncOutputs.Add(new NCOutput { Name = p.Name, ZAngle = angle, Point = count });
                            }
                        }

                        var saved = new { PMFolder = pmFolder, Probed = probed, NCPrograms = ncOutputs };
                        string json = JsonConvert.SerializeObject(saved);
                        string savedFile = AppContext.BaseDirectory + ConfigurationManager.AppSettings["SavedData"] + "\\" + ncFile.Replace(".nc", ".txt").Replace(".tap", ".txt");
                        StreamWriter writer = new StreamWriter(savedFile, false);
                        writer.Write(json);
                        writer.Close();
                    }                    
                }
            }
            return ncPath;
        }

        private void BtnImportModel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;

            ConnectToPM();
            cbxTool.SelectedIndex = 0;

            powerMILL.DialogsOff();
            string reset = powerMILL.ExecuteEx("project reset").ToString();

            if (reset.Contains("Cancel"))
            {
                MessageBox.Show("未保存或放弃保存当前PowerMILL项目，将不会导入模型。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
            else
            {
                System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    powerMILL.Execute($"IMPORT MODEL FILEOPEN '{fileDialog.FileName}'");
                    /*
                    powerMILL.Execute("CREATE LEVEL Red");
                    powerMILL.Execute("CREATE LEVEL Blue");
                    powerMILL.Execute("CREATE LEVEL Green");
                    powerMILL.Execute("CREATE LEVEL Yellow");
                    */
                    //创建预留层
                    LoadColorConfig();
                    if (colors != null)
                    {
                        foreach (LevelConfig color in colors)
                        {
                            powerMILL.Execute($"CREATE LEVEL {color.Level}");
                        }
                    }

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
                            int r = int.Parse(items[3]);
                            int g = int.Parse(items[4]);
                            int b = int.Parse(items[5]);

                            LevelConfig config = colors.FirstOrDefault(c => c.R == r && c.G == g && c.B == b);
                            if (config != null)
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"{config.Level}\" ACQUIRE SELECTED");
                            }
                            /*
                            if (r == 255 && g == 0 && b == 0)
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"Red\" ACQUIRE SELECTED");
                            }
                            else if (r == 0 && g == 255 && b == 0)
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"Green\" ACQUIRE SELECTED");
                            }
                            else if (r == 0 && g == 0 && b == 255)
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"Blue\" ACQUIRE SELECTED");
                            }
                            else if (r == 255 && g == 255 && b == 0)
                            {
                                powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                powerMILL.Execute($"EDIT LEVEL \"Yellow\" ACQUIRE SELECTED");
                            }
                            else
                            {                               
                                if (colors.Any(c => c.R == r && c.G == g && c.B == b))
                                {
                                    string level = $"R{r}G{g}B{b}";
                                    powerMILL.Execute($"edit model '{model.Name}' select '{items[0]}'");
                                    powerMILL.Execute($"EDIT LEVEL \"{level}\" ACQUIRE SELECTED");
                                }
                            }  */                          
                        }                       
                    }

                    CreateTool();
                    cbxTool.ItemsSource = session.Tools.Select(t => t.Name);

                    //创建NC坐标系
                    powerMILL.Execute("DELETE WORKPLANE ALL");

                    PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;

                    foreach (NCOutput output in NCOutputs)
                    {
                        CreateNCWorkplane(output.Workplane, output.Angle);
                    }

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

            string method = cbxMethod.SelectedItem.ToString();
            if (method == "模型比对")
            {
                tbxStepdown.Text = ConfigurationManager.AppSettings["GreenStepdown"];
            }
            else if (method == "角度" || method == "距离" || method == "U型槽模型比对")
            {                
                tbxStepdown.Text = ConfigurationManager.AppSettings["Stepdown"];
            }            
        }

        private void BtnTransform_Click(object sender, RoutedEventArgs e)
        {
            ConnectToPM();

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

            double d;
            if (!double.TryParse(tbxAngle.Text, out d))
            {
                MessageBox.Show($"角度必须是数值。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            uint v;
            if (!uint.TryParse(tbxCopies.Text, out v) || v <= 0)
            {
                MessageBox.Show($"复制个数必须是正整数。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
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
                    for (int i = 1; i <= copies; i++)
                    {
                        ClearToolpath_V2(toolpath.Name + "_" + i * angle, true);
                    }

                    //采用参考线旋转方式
                    string patternName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Pattern.Name\"").ToString();

                    powerMILL.Execute($"ACTIVATE Pattern \"{patternName}\"");
                    powerMILL.Execute("MODE GEOMETRY_TRANSFORM START PATTERN ;");
                    powerMILL.Execute("MODE TRANSFORM TYPE ROTATE");
                    powerMILL.Execute($"MODE TRANSFORM COPIES {copies}");
                    powerMILL.Execute($"MODE TRANSFORM ROTATE ANGLE \"{angle}\"");
                    powerMILL.Execute("MODE GEOMETRY_TRANSFORM FINISH ACCEPT");

                    //复制一个坐标系来旋转，原来刀路的不能直接变换，否则要重制刀路                    
                    string workplane = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Workplane.Name\"").ToString();
                    powerMILL.Execute($"COPY WORKPLANE \"{workplane}\" ");
                    string w1 = workplane + "_1";
                    powerMILL.Execute($"ACTIVATE Workplane \"{w1}\"");
                    powerMILL.Execute("MODE WORKPLANE_TRANSFORM START ;");
                    ActivateWorldPlane();
                    powerMILL.Execute("STATUS EDITING_PLANE XY");
                    powerMILL.Execute("MODE TRANSFORM TYPE ROTATE");
                    powerMILL.Execute("MODE TRANSFORM COPY YES");
                    powerMILL.Execute($"MODE TRANSFORM COPIES {copies}");
                    powerMILL.Execute($"MODE TRANSFORM ROTATE ANGLE \"{angle}\"");
                    powerMILL.Execute("MODE WORKPLANE_TRANSFORM FINISH ACCEPT");
                    powerMILL.Execute($"DELETE WORKPLANE \"{w1}\"");

                    for (int i = 1; i <= copies; i++)
                    {
                        string p0 = patternName + "_" + i;
                        string p1 = patternName + "_" + i * angle;
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
            PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;

            //零件信息
            string part = page.tbxPart.Text.Trim();
            string partNumber = page.tbxPartNumber.Text.Trim();
            string equipment = page.tbxEquipment.Text.Trim();
            string process = page.tbxProcess.Text.Trim();
            string stage = page.tbxStage.Text.Trim();
            string product = $"OMV.{partNumber}.{stage}.{process}";

            //读取保存的PM检测路径信息
            string partFile = AppContext.BaseDirectory + $"{ConfigurationManager.AppSettings["SavedData"]}\\{product}.txt";
            if (!File.Exists(partFile))
            {
                MessageBox.Show($"未找到{partFile}，请检查零件信息是否输入正确。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            StreamReader reader = new StreamReader(partFile);
            var saved = JsonConvert.DeserializeAnonymousType(reader.ReadToEnd(), new { PMFolder = string.Empty, Probed = new List<Model.Toolpath>(), NCPrograms = new List<NCOutput>()});
            reader.Close();

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
            ISurfaceGroup inspect1 = doc.SequenceItems[4] as ISurfaceGroup;//导入Total NC的初始检测组，设置点不输出到报告
            if (inspect1.SequenceItems.Count != saved.Probed.Sum(p => p.Point))
            {
                MessageBox.Show("请导入PI的检测点数和保存的PM检测不一致，请确认是否导入了正确的TAP/MSR。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            IBagOfPoints points = inspect1.BagOfPoints[measure];
            for (int i = 1; i <= inspect1.SequenceItems.Count; i++)
            {
                inspect1.SequenceItems[i].OutputToReport = false;
            }

            //取设置的名义值            
            double nAngle, nDistance, nMinDistance, nMaxDistance;
            double.TryParse(page.tbxAngle.Text, out nAngle);
            double.TryParse(page.tbxDistance.Text, out nDistance);
            double.TryParse(page.tbxMinDistance.Text, out nMinDistance);
            double.TryParse(page.tbxMaxDistance.Text, out nMaxDistance);

            bool model1 = page.rbMode1.IsChecked == true;//角度或距离

            ISequenceGroup geometricGroup;//角度或距离的几何组
            ISurfaceGroup inspect2;//角度和距离选择为模型比对之后，角度，距离，底面这些点建立在一个检测组
            ISurfaceGroup inspect3;//U型槽的模型比对，包括底面，也是建立一个检测组

            int index = 1;
            foreach (Model.Toolpath toolpath in saved.Probed)
            {
                int n = toolpath.Point;//点数
                int a = n / 2;//前面一半
                int b = n - a;//后面一半
                int[] indices;

                if (toolpath.Name.Contains("竖面（角度）") && model1)//角度
                {
                    if (model1)
                    {
                        if (geometricGroup == null)
                        {
                            geometricGroup = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_GeometricGroup);
                        }
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
                        angle.PropertyAngle.Nominal = nAngle;

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
                    else//模型比对
                    {
                        if (inspect2 == null)
                        {
                            inspect2 = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_SurfPointsCNC) as ISurfaceGroup;
                        }

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
                else if (toolpath.Name.Contains("距离"))//横面距离或竖面距离
                {
                    if (model1)
                    {
                        if (geometricGroup == null)
                        {
                            geometricGroup = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_GeometricGroup);
                        }

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
                        distance.PropertyDistance.Nominal = nDistance;
                        distance.PropertyMaxDistance.Nominal = nMaxDistance;
                        distance.PropertyMinDistance.Nominal = nMinDistance;

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
                    else//模型比对
                    {
                        if (inspect2 == null)
                        {
                            inspect2 = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_SurfPointsCNC) as ISurfaceGroup;
                        }

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
                else if (toolpath.Name.Contains("顶孔"))
                {                    
                    int holeCount = toolpath.Hole;
                    int pointCount = n / holeCount;//每个孔的点数

                    IGeometricGroup geometric;
                    IPlane_ProbedItem plane;
                    if (Application.Current.Resources["顶端组"] == null)
                    {
                        geometric = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_GeometricGroup) as IGeometricGroup;
                        plane = geometric.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Plane_Probed_) as IPlane_ProbedItem;
                        Application.Current.Resources["顶端组"] = geometric;//顶端平面要用
                        Application.Current.Resources["顶端平面"] = plane;//顶端平面要用
                    }
                    else
                    {
                        geometric = Application.Current.Resources["顶端组"] as IGeometricGroup;
                        plane = Application.Current.Resources["顶端平面"] as IPlane_ProbedItem;
                    }                

                    for (int i = 0; i < holeCount; i++)
                    {                        
                        IGeometricCircleItem circle = geometric.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Feat_ProbedCircle_) as IGeometricCircleItem;
                        foreach(IFeature f in circle.ReferencePlane.PossibleFeatures)
                        {
                            if (f.Name == plane.Name)
                            {
                                circle.ReferencePlane.Feature = f;
                                break;
                            }
                        }
                        indices = new int[points.Count];
                        for (int j = 0; j < points.Count; j++)
                        {
                            if (j < pointCount)
                            {
                                indices[j] = index + j;
                            }
                            else
                            {
                                indices[j] = index;
                            }
                        }
                        index += pointCount;
                        points.CopyToClipboard(indices);
                        circle.BagOfPoints[measure].PasteFromClipboard();
                    }
                }
                else if (toolpath.Name.Contains("侧孔"))
                {
                    IGeometricGroup geometric = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_GeometricGroup) as IGeometricGroup;
                    IFeat_CylinderItem cylinder = geometric.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Feat_Cylinder_) as IFeat_CylinderItem;
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
                    cylinder.BagOfPoints[measure].PasteFromClipboard();
                }
                else if (toolpath.Name.Contains("顶端内侧圆弧"))
                {
                    IGeometricGroup geometric = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_GeometricGroup) as IGeometricGroup;
                    IPlane_ProbedItem plane = geometric.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Plane_Probed_) as IPlane_ProbedItem;
                    IGeometricCircleItem circle = geometric.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Feat_ProbedCircle_) as IGeometricCircleItem;
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
                    circle.BagOfPoints[measure].PasteFromClipboard();
                }
                else if (toolpath.Name.Contains("顶端平面"))
                {                   
                    IPlane_ProbedItem plane;
                    if (Application.Current.Resources["顶端组"] == null)
                    {
                        IGeometricGroup geometric = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_GeometricGroup) as IGeometricGroup;
                        plane = geometric.SequenceItems.AddItem(PWI_EntityItemType.pwi_ent_Plane_Probed_) as IPlane_ProbedItem;
                        Application.Current.Resources["顶端组"] = geometric;
                        Application.Current.Resources["顶端平面"] = plane;
                    }
                    else
                    {
                        plane = Application.Current.Resources["顶端平面"] as IPlane_ProbedItem;
                    }
                                       
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
                    plane.BagOfPoints[measure].PasteFromClipboard();                   
                }                
                else if (toolpath.Name.Contains("U型槽模型比对"))
                {
                    if (inspect3 == null)
                    {
                        ISurfaceGroup inspect3 = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_SurfPointsCNC) as ISurfaceGroup;
                    }

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
                    inspect3.BagOfPoints[measure].PasteFromClipboard();
                }
                else
                {
                    //其他模型比对
                    ISurfaceGroup inspect4 = doc.SequenceItems.AddGroup(PWI_GroupType.pwi_grp_SurfPointsCNC) as ISurfaceGroup;

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

            string piFolder = $"{Directory.GetParent(saved.PMFolder)}\\PI";
            Directory.CreateDirectory(piFolder);
            string file = $"{piFolder}\\{product}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.pwi";
            doc.SaveAs(file, false);

            //改报告模板，把零件名写到产品名称栏位
            string layout = AppContext.BaseDirectory + @"Report\Layout.html";
            if (File.Exists(layout))
            {
                reader = new StreamReader(layout, System.Text.Encoding.GetEncoding("GB2312"));
                string html = reader.ReadToEnd();                
                reader.Close();
                StreamWriter writer = new StreamWriter(layout, false, System.Text.Encoding.GetEncoding("GB2312"));
                //找出"<span id="idReportVariable" m_name="Customer" m_is_value="true">产品名称</span>这样的字符串，并替换其中的产品名称                
                html = FillHtml(html, "Customer", part);
                html = FillHtml(html, "Inspector", partNumber);
                html = FillHtml(html, "Part No.", equipment);
                html = FillHtml(html, "Customer phone No.", DateTime.Now.ToString("yyyy-MM-dd"));
                html = FillHtml(html, "Customer fax No.", process);
                writer.Write(html);
                writer.Close();
            }

            MessageBox.Show("检测完成。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        private string FillHtml(string html, string name, string value)
        {
            string prefix = $"<span id=\"idReportVariable\" m_name=\"{name}\" m_is_value=\"true\">";
            string suffix = "</span>";
            int start = html.IndexOf(prefix);
            int end = html.IndexOf(suffix, start);
            string oldHtml = html.Substring(start, end - start);
            string newHtml = prefix + value;

            return html.Replace(oldHtml, newHtml);
        }

        private void BtnMergeTotal_Click(object sender, RoutedEventArgs e)
        {            
            PINominal page = (Application.Current.MainWindow as MainWindow).PINominal;
            string part = page.tbxPart.Text.Trim();
            if (string.IsNullOrEmpty(part))
            {
                MessageBox.Show("请在参数设置页输入零件名称。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string partNumber = page.tbxPartNumber.Text.Trim();
            if (string.IsNullOrEmpty(partNumber))
            {
                MessageBox.Show("请在参数设置页输入零件代码。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string equipment = page.tbxEquipment.Text.Trim();
            if (string.IsNullOrEmpty(equipment))
            {
                MessageBox.Show("请在参数设置页输入测量设备。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string process = page.tbxProcess.Text.Trim();
            if (string.IsNullOrEmpty(process))
            {
                MessageBox.Show("请在参数设置页输入工序号。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string stage = page.tbxStage.Text.Trim();
            if (string.IsNullOrEmpty(stage))
            {
                MessageBox.Show("请在参数设置页输入工序号。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            string product = $"OMV.{partNumber}.{stage}.{process}";
            //读取保存的PM检测路径信息
            string partFile = $"{ConfigurationManager.AppSettings["SavedData"]}\\{product}.txt";
            if (!File.Exists(partFile))
            {
                MessageBox.Show($"未找到{partFile}，请检查零件信息是否输入正确。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            StreamReader reader = new StreamReader(partFile);
            var saved = JsonConvert.DeserializeAnonymousType(reader.ReadToEnd(), new { PMFolder = string.Empty, NCPrograms = new List<NCOutput>() });
            reader.Close();

            string[] msrFiles = Directory.GetFiles(ConfigurationManager.AppSettings["msrFolder"], $"{product}.*.msr");
            if (msrFiles.Length == 0)
            {
                MessageBox.Show($"未在{ConfigurationManager.AppSettings["msrFolder"]}找到格式为{product}.XXX的MSR文件。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }
            string msr = msrFiles.OrderBy(m => m).Last();
            if (!File.Exists(msr))
            {
                MessageBox.Show($"没有找到mrs文件：{msr}，请检测设定并在该路径放置了msr文件。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            Queue<Autodesk.Geometry.Point> points = new Queue<Autodesk.Geometry.Point>();
            reader = new StreamReader(msr);
            string line = reader.ReadLine();
            while (line != null)
            {
                double x = double.Parse(line.Substring(9, 11));
                double y = double.Parse(line.Substring(21, 11));
                double z = double.Parse(line.Substring(33));
                points.Enqueue(new Autodesk.Geometry.Point() { X = x, Y = y, Z = z });
                line = reader.ReadLine();
            }
            reader.Close(); 
            
            string project = new DirectoryInfo(saved.PMFolder).Name;
            string ncProgram = $"{partNumber}.{stage}.{process}.ZXJC.{equipment}.001";

            string total = $"{ConfigurationManager.AppSettings["ncFolder"]}\\{project}\\{ncProgram}\\{product}.tap";
            if (!File.Exists(total))
            {
                MessageBox.Show($"没有找到{total}。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            //读取total.tap
            reader = new StreamReader(total);
            List<string> totalLines = new List<string>();
            line = reader.ReadLine();
            while (line != null)
            {
                totalLines.Add(line);
                line = reader.ReadLine();
            }
            reader.Close();

            if (totalLines.Count - 2 != points.Count * 2) //total.tap去掉头尾的Start和End，Total.tap一个点有名义值和实测值，所以是点数的两倍
            {
                MessageBox.Show($"msr文件中的总点数和Total.tap中的不一至。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            //保存并转换msr中的坐标值
            List<string> transformed = new List<string>();                              
            int n = 0;//整合文件的行Index
            int totalCount = 0;
            foreach (NCOutput program in saved.NCPrograms)
            {
                double angle = program.ZAngle;
                //nc程序中的点数                
                int count = program.Point;
                totalCount += count;//continue;
                if (points.Count < count)
                {
                    MessageBox.Show($"msr文件中的总点数少于PowerMILL中NC程序（U0到 - U315）的总点数。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    return;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        Autodesk.Geometry.Point point = points.Dequeue();
                        transformed.Add($"G801 N{n++} X{Math.Round(point.X * Math.Cos(angle) - point.Y * Math.Sin(angle), 3)} Y{Math.Round(point.X * Math.Sin(angle) + point.Y * Math.Cos(angle),3)} Z{point.Z} R3.0");
                    }
                }
            }
            if (points.Count > 0)
            {
                MessageBox.Show($"msr文件中的总点数大于PowerMILL中NC程序（U0到 - U315）的总点数。", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            string integrated = $"{ConfigurationManager.AppSettings["ncFolder"]}\\{project}\\{ncProgram}\\{System.IO.Path.GetFileNameWithoutExtension(msr) + ".tap"}";
            
            StreamWriter writer = new StreamWriter(integrated, false);
            n = 0;
            foreach (string l in totalLines)
            {
                if (l.Contains("R3.0"))//实际测量值的列写来自msr的
                {
                    writer.WriteLine(transformed[n++]);
                }
                else
                {
                    writer.WriteLine(l);//Start，End和名义值列
                }
            }
            writer.Close();

            if (MessageBox.Show("生成整合文件Total_Integrated.tap完成，是否打开所在文件夹？", "Info", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start("explorer.exe", $"{ConfigurationManager.AppSettings["ncFolder"]}\\{project}\\{ncProgram}");
            }
        }

        private int[] points2 = new int[] { 8, 10, 12 };
        private int[] points3 = new int[] { 12 };

        private void CbxStepdownLimit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxPoints != null)
            {
                ComboBoxItem item = cbxStepdownLimit.SelectedItem as ComboBoxItem;
                int[] points = null;
                if (item.Content.ToString() == "2")
                {
                    points = points2;
                }
                else if (item.Content.ToString() == "3")
                {
                    points = points3;
                }
                cbxPoints.ItemsSource = points;
                cbxPoints.SelectedItem = points.First();
            }
        }

        /// <summary>
        /// 2条参考线，有8，10，12点可选；3条线，只能选12
        /// </summary>
        private void InitComboBox()
        {
            cbxPoints.ItemsSource = points2;
            cbxPoints.SelectedItem = points2.First();
        }        
    }
}
