using Autodesk.Geometry;
using Autodesk.ProductInterface.PowerMILL;
using System;
using System.Collections.Generic;
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
        private PMAutomation powerMILL;
        private PMProject session;
        private double? height;

        public MainWindow()
        {
            InitializeComponent();           
            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;

            cbxLevel.ItemsSource = session.LevelsAndSets.Select(l => l.Name);
            cbxLevel.SelectedItem = "CAO1"; 
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            powerMILL.DialogsOff();           

            session.Refresh();
            PMToolpath rcToolpath = session.Toolpaths.FirstOrDefault(t => t.Name == cbxLevel.SelectedItem.ToString());
            if (rcToolpath != null && (ckbManul.IsChecked ?? false))
            {
                string patternName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{rcToolpath.Name}').Pattern.Name\"").ToString();
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{rcToolpath.Name}\" FORM TOOLPATH");
                powerMILL.Execute($"EDIT TOOLPATH \"{rcToolpath.Name}\" RECYCLE");

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
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{rcToolpath.Name}\" FORM TOOLPATH");
                powerMILL.Execute("EDIT TPPAGE SWSurfaceInspect");
                powerMILL.Execute($"EDIT PAR 'Pattern' \"{patternName}\"");

                powerMILL.Execute($"EDIT TOOLPATH \"{rcToolpath.Name}\" CALCULATE");
                powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
            }
            else
            {
                for (int i = 0; i < session.Workplanes.Count; i++)
                {
                    PMWorkplane w = session.Workplanes[0];
                    powerMILL.Execute($"EXPLORER SELECT Workplane \"Workplane\\{w.Name}\" NEW");
                    powerMILL.Execute("DEACTIVATE WORKPLANE");
                }
                powerMILL.Execute("edit model all deselect all");
                powerMILL.Execute($"EDIT LEVEL \"{cbxLevel.Text}\" SELECT ALL");
                string output = powerMILL.ExecuteEx("SIZE MODEL").ToString();
                string[] lengths = output.Split('\r')[10].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                double x = double.Parse(lengths[1]);
                double y = double.Parse(lengths[2]);
                double a = Math.Atan2(x, y) * 180 / Math.PI;
                powerMILL.Execute("MODE WORKPLANE_CREATE ; SELECTION CENTRE");
                session.Refresh();
                PMWorkplane workplane = session.Workplanes.Last();
                powerMILL.Execute($"MODE WORKPLANE_EDIT START \"{workplane.Name}\"");
                powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Z");
                powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"{a}\"");
                powerMILL.Execute("WPETWIST ACCEPT");
                powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Y");
                powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"-90\"");
                powerMILL.Execute("WPETWIST ACCEPT");
                powerMILL.Execute("MODE WORKPLANE_EDIT FINISH ACCEPT");
                powerMILL.Execute($"EXPLORER SELECT Workplane \"Workplane\\{workplane.Name}\" NEW");
                powerMILL.Execute($"ACTIVATE Workplane \"{workplane.Name}\"");
                powerMILL.Execute("CREATE TOOL ; PROBE FORM TOOL");
                //powerMILL.Execute("CREATE TOOL");
                session.Refresh();
                PMTool tool = session.Tools.Last();
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" DIAMETER \"{tbxDiameter.Text}\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT ADD");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT LOWERDIA \"4\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT UPPERDIA \"4\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" SHANK_COMPONENT LENGTH \"{tbxShankLength.Text}\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" HOLDER_COMPONENT ADD");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" HOLDER_COMPONENT LOWERDIA \"30\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" HOLDER_COMPONENT LENGTH \"30\"");
                powerMILL.Execute($"EDIT TOOL \"{tool.Name}\" OVERHANG \"{tbxOverhang.Text}\"");
                powerMILL.Execute("TOOL ACCEPT");
                //powerMILL.Execute("FORM STRATEGYSELECTOR");
                powerMILL.Execute("STRATEGYSELECTOR CATEGORY 'Probing' NEW");
                powerMILL.Execute("STRATEGYSELECTOR STRATEGY \"Probing/Surface-Inspection.ptf\" NEW");
                powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTORGUI \"Probing/Surface-Inspection.ptf\"");
                session.Refresh();
                PMToolpath toolpath = session.Toolpaths.Last();
                powerMILL.Execute("CREATE PATTERN ; EDIT PATTERN ; CURVEEDITOR START");
                session.Refresh();
                PMPattern pattern1 = session.Patterns.Last();
                powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern1.Name}\"");
                powerMILL.Execute("CURVEEDITOR MODE LINE_MULTI");
                powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");
                powerMILL.Execute($"EDIT TOOLPATH \"{toolpath.Name}\" CALCULATE");
                powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
                powerMILL.Execute("EDIT TOOLPATH LEADS RAISEFORM");
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
                powerMILL.Execute("EDIT TOOLPATH SAFEAREA APPLY");
                powerMILL.Execute("PROCESS TPLEADS");
                powerMILL.Execute("LEADS ACCEPT");

                powerMILL.Execute("edit model all deselect all");
                powerMILL.Execute($"EDIT LEVEL \"{cbxLevel.Text}\" SELECT ALL");
                powerMILL.Execute("CREATE PATTERN ;");
                session.Refresh();
                PMPattern pattern2 = session.Patterns.Last();
                powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" INSERT MODEL");
                powerMILL.Execute("edit model all deselect all");
                powerMILL.Execute($"EDIT LEVEL \"{cbxLevel.Text}\" SELECT ALL");
                powerMILL.Execute("VIEW MODEL ; SHADE OFF");

                powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" DESELECT ALL");
                powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" SELECT 0");
                output = powerMILL.ExecuteEx($"size pattern '{pattern2.Name}' selected").ToString();
                double z0Min = double.Parse(output.Split('\r')[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
                double z0Max = double.Parse(output.Split('\r')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
                powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" SELECT ALL");
                powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" DESELECT 0");
                output = powerMILL.ExecuteEx($"size pattern '{pattern2.Name}' selected").ToString();
                double z1Min = double.Parse(output.Split('\r')[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
                double z1Max = double.Parse(output.Split('\r')[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3]);
                double height;
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

                powerMILL.Execute($"EXPLORER SELECT Toolpath \"Toolpath\\{toolpath.Name}\" NEW");
                powerMILL.Execute($"ACTIVATE TOOLPATH \"{toolpath.Name}\" FORM TOOLPATH");
                powerMILL.Execute($"EDIT TOOLPATH \"{toolpath.Name}\" CLONE");
                powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern2.Name}\"");
                session.Refresh();
                PMToolpath clone = session.Toolpaths.Last();
                powerMILL.Execute($"EDIT TOOLPATH \"{clone.Name}\" CALCULATE");
                powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
                powerMILL.Execute("VIEW MODEL ; SHADE NORMAL");                
            }

            powerMILL.Execute("FORM COLLISION");
            powerMILL.Execute("EDIT COLLISION TYPE GOUGE");
            powerMILL.Execute("EDIT COLLISION HIT_OUTPUT N");
            powerMILL.Execute("EDIT COLLISION APPLY");
            powerMILL.Execute("EDIT COLLISION TYPE COLLISION");
            powerMILL.Execute("EDIT COLLISION APPLY");
            powerMILL.Execute("COLLISION ACCEPT");

            session.Refresh();
            var oldToolpaths = session.Toolpaths.Where(t => t.Name.Contains(cbxLevel.SelectedItem.ToString()) && t.IsActive == false);
            foreach (PMToolpath tp in oldToolpaths)
            {
                powerMILL.Execute($"DELETE TOOLPATH \"{tp.Name}\"");
            }
            session.Refresh();
            PMToolpath final = session.Toolpaths.Last();
            final.Name = cbxLevel.SelectedItem.ToString();

            WindowState = WindowState.Normal;
            MessageBox.Show("计算完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private void CbxLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbxLevel.SelectedItem.ToString() == "CAO1")
            {
                tbxPoints.Text = "33";
                tbxZ.Text = "0.5";
            }
            else if (cbxLevel.SelectedItem.ToString() == "CAO2")
            {
                tbxPoints.Text = "33";
                tbxZ.Text = "-2";
            }
            else
            {
                tbxPoints.Text = "16";
                tbxZ.Text = "-10";
            }
        }  
    }
}
