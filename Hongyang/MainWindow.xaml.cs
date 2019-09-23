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

            cbxToolpaths.ItemsSource = session.Toolpaths.Select(t => t.Name);
            cbxCopyToLevels.ItemsSource = session.LevelsAndSets.Select(l => l.Name);
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
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
                CollisionCheck(rcToolpath.Name, rcToolpath.Name);
            }
            else
            {
                ActivateWorldPlane();
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
                toolpath.Delete();
                CollisionCheck(clone.Name, cbxLevel.Text);
            }
          
            cbxToolpaths.ItemsSource = session.Toolpaths.Select(t => t.Name); 

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

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;

            ActivateWorldPlane();

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
            string workplaneName = powerMILL.ExecuteEx($"print par terse \"entity('toolpath', '{toolpath.Name}').Workplane.Name\"").ToString();
            powerMILL.Execute($"EDIT PATTERN \"{patternName}\" CLIPBOARD COPY");
            powerMILL.Execute("CREATE PATTERN CLIPBOARD");
            powerMILL.Execute($"COPY WORKPLANE \"{workplaneName}\"");
            session.Refresh();
            PMPattern pattern = session.Patterns.Last();
            powerMILL.Execute($"EDIT PATTERN \"{pattern.Name}\" CURVEEDITOR START");
            powerMILL.Execute("CURVEEDITOR MODE ROTATE");
            powerMILL.Execute($"MODE TRANSFORM ROTATE ANGLE \"{a1 - a0}\"");
            powerMILL.Execute("CURVEEDITOR FINISH ACCEPT\rYES");
            int index = session.Workplanes.IndexOf(session.Workplanes.First(w => w.))
            PMWorkplane workplane = session.Workplanes[workplaneName + "_1"];
            powerMILL.Execute($"MODE WORKPLANE_EDIT START \"{workplane.Name}\"");
            powerMILL.Execute("MODE WORKPLANE_EDIT TWIST Z");
            powerMILL.Execute($"MODE WORKPLANE_EDIT TWIST \"{a1 - a0}\"");
            powerMILL.Execute("WPETWIST ACCEPT");

            powerMILL.Execute($"ACTIVATE TOOLPATH \"{toolpath.Name}\" FORM TOOLPATH");
            powerMILL.Execute($"EDIT TOOLPATH \"{toolpath.Name}\" CLONE");
            powerMILL.Execute("FORM CANCEL SFSurfaceInspect");
            session.Refresh();
            PMToolpath cloned = session.Toolpaths.Last();
            powerMILL.Execute($"ACTIVATE TOOLPATH \"{cloned.Name}\" FORM TOOLPATH");
            powerMILL.Execute($"ACTIVATE WORKPLANE \"{workplane.Name}\"");
            powerMILL.Execute("EDIT TPPAGE SWSurfaceInspect");
            powerMILL.Execute($"EDIT PAR 'Pattern' \"{pattern.Name}\"");            
            powerMILL.Execute($"EDIT TOOLPATH \"{cloned.Name}\" CALCULATE");
            powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");

            CollisionCheck(cloned.Name, cbxCopyToLevels.Text);

            cbxToolpaths.ItemsSource = session.Toolpaths.Select(t => t.Name);

            WindowState = WindowState.Normal;
            MessageBox.Show("复制完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

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
            powerMILL.Execute("FORM COLLISION");
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
    }
}
