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
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            session.l
            //powerMILL.DialogsOff();
            powerMILL.Execute("MODE WORKPLANE_CREATE ; INTERACTIVE GEOMETRY");
            powerMILL.Execute("EXPLORER SELECT Workplane \"Workplane\\2\" NEW");
            powerMILL.Execute("ACTIVATE Workplane \"2\"");
            powerMILL.Execute("EXPLORER SELECT Workplane \"Workplane\\2\" NEW");
            powerMILL.Execute("CREATE TOOL ; PROBE FORM TOOL");
            powerMILL.Execute($"EDIT TOOL \"1\" DIAMETER \"{tbxDiameter.Text}\"");
            powerMILL.Execute("EDIT TOOL \"1\" SHANK_COMPONENT ADD");
            powerMILL.Execute("EDIT TOOL \"1\" SHANK_COMPONENT LOWERDIA \"4\"");
            powerMILL.Execute("EDIT TOOL \"1\" SHANK_COMPONENT UPPERDIA \"4\"");
            powerMILL.Execute("EDIT TOOL \"1\" SHANK_COMPONENT LENGTH \"100\"");
            powerMILL.Execute("EDIT TOOL \"1\" HOLDER_COMPONENT ADD");
            powerMILL.Execute("EDIT TOOL \"1\" HOLDER_COMPONENT LOWERDIA \"30\"");
            powerMILL.Execute("EDIT TOOL \"1\" HOLDER_COMPONENT LENGTH \"30\"");
            powerMILL.Execute("EDIT TOOL \"1\" OVERHANG \"100\"");
            powerMILL.Execute("TOOL ACCEPT");
            powerMILL.Execute("FORM STRATEGYSELECTOR");
            powerMILL.Execute("STRATEGYSELECTOR CATEGORY 'Probing' NEW");
            powerMILL.Execute("STRATEGYSELECTOR STRATEGY \"Probing / Surface - Inspection.ptf\" NEW");
            powerMILL.Execute("IMPORT TEMPLATE ENTITY TOOLPATH TMPLTSELECTORGUI \"Probing / Surface - Inspection.ptf\"");
            powerMILL.Execute("CREATE PATTERN ; EDIT PATTERN ; CURVEEDITOR START");
            powerMILL.Execute("EDIT PAR 'Pattern' \"1\"");
            powerMILL.Execute("CURVEEDITOR MODE LINE_MULTI");
            powerMILL.Execute("CURVEEDITOR FINISH ACCEPT");
            powerMILL.Execute("EDIT TOOLPATH \"1\" CALCULATE");
            powerMILL.Execute("FORM ACCEPT SFSurfaceInspect");
            powerMILL.Execute("EDIT TOOLPATH LEADS RAISEFORM");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA CALCULATE_DIMENSIONS");
            powerMILL.Execute("EDIT TOOLPATH SAFEAREA APPLY");
            powerMILL.Execute("PROCESS TPLEADS");
            powerMILL.Execute("LEADS ACCEPT");
        }
    }
}
