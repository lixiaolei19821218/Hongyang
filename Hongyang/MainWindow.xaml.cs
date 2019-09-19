using Autodesk.Geometry;
using Autodesk.ProductInterface.PowerMILL;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PMAutomation powerMILL;
        private PMProject session;

        public MainWindow()
        {
            InitializeComponent();

            powerMILL = new PMAutomation(Autodesk.ProductInterface.InstanceReuse.UseExistingInstance);
            session = powerMILL.ActiveProject;

            cbxLevel.ItemsSource = session.LevelsAndSets.Select(l => l.Name);
            cbxLevel.SelectedIndex = 1; 
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            powerMILL.DialogsOff();
            //powerMILL.Execute("MODE WORKPLANE_CREATE ; INTERACTIVE GEOMETRY");           
            //powerMILL.Execute("PICK -266.521 -149.743 266.521 149.743 -875.776 -386.645 488.954 -0.994872 -0.0357478 0.0946113 0.0962291 -0.0466247 0.994267 0 -21.046 -21.2323 -21.046 -21.2323");
            for (int i = 0; i < session.Workplanes.Count; i++)
            {
                PMWorkplane w = session.Workplanes[0];
                w.IsActive = false;
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
            //powerMILL.Execute("VIEW MODEL ; SHADE OFF");
            //powerMILL.Execute("PICK -108.57 -60.9996 108.57 60.9996 -746.448 -671.323 259.199 -0.77469 -0.551684 -0.30903 0.00239525 -0.491265 0.871007 0 14.3395 -21.6989 17.526 -29.2859");
            //powerMILL.Execute($"EDIT LEVEL \"{cbxLevel.Text}\" SELECT ALL");
            //powerMILL.Execute("DELETE SELECTION");
            powerMILL.Execute("edit model all deselect all");
            powerMILL.Execute($"EDIT LEVEL \"{cbxLevel.Text}\" SELECT ALL");
            powerMILL.Execute("VIEW MODEL ; SHADE OFF");
            //powerMILL.Execute("PICK -108.57 -60.9996 108.57 60.9996 -746.448 -671.323 259.199 -0.77469 -0.551684 -0.30903 0.00239525 -0.491265 0.871007 0 -61.8342 -13.0497 -61.8342 -13.0497");
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" DESELECT 1");
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" SELECT 0");            
            powerMILL.Execute($"EDIT PATTERN \"{pattern2.Name}\" CURVEEDITOR START");
            powerMILL.Execute("FORM RIBBON TAB \"CurveTools.EditCurve\"");
            powerMILL.Execute("CURVEEDITOR REPOINT RAISE");
            powerMILL.Execute("CURVEEDITOR REPOINT POINTS \"15\"");
            powerMILL.Execute("FORM APPLY CEREPOINTCURVE");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA APPLY");
            powerMILL.Execute("FORM CANCEL CEREPOINTCURVE");
            powerMILL.Execute("CURVEEDITOR UNDO");
            powerMILL.Execute("FORM RIBBON TAB \"CurveEditor.Edit\"");
            powerMILL.Execute("CURVEEDITOR MODE TRANSLATE");
            powerMILL.Execute($"MODE COORDINPUT COORDINATES {tbxX.Text} {tbxY.Text} {tbxZ.Text}");
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
            //powerMILL.Execute("PICK -153.567 -86.2806 153.567 86.2806 -694.01 -774.64 376.336 -0.716069 -0.691892 -0.0923641 -0.0390162 -0.0924418 0.994953 0 -119.441 1.5024 -117.724 -38.2039");
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
            powerMILL.Execute("PICK -86.3812 -48.5329 86.3812 48.5329 -743.703 -699.275 320.499 -0.748899 -0.632926 -0.196355 -0.140636 -0.137758 0.980431 0 -73.8255 9.53755 -62.477 -18.23");
            powerMILL.Execute("FORM RIBBON TAB \"ToolpathEdit\"");
            powerMILL.Execute("DELETE TOOLPATH ; SELECTED");
            powerMILL.Execute("PICK -86.3812 -48.5329 86.3812 48.5329 -743.703 -699.275 320.499 -0.748899 -0.632926 -0.196355 -0.140636 -0.137758 0.980431 0 34.7094 8.08881 45.2128 -23.9042");
            powerMILL.Execute("DELETE TOOLPATH ; SELECTED");

            WindowState = WindowState.Normal;
            MessageBox.Show("计算完成", "Info", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
    }
}
