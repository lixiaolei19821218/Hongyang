﻿#pragma checksum "..\..\EPoint.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "D44493D5E5E2C2A98E2E0F701C2AFCE70EEC86DA"
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

using Hongyang;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Hongyang {
    
    
    /// <summary>
    /// EPoint
    /// </summary>
    public partial class EPoint : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 12 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel skpMain1;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbxType;
        
        #line default
        #line hidden
        
        
        #line 23 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chxMove;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel skpMain2;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chxSeparate;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbxDir;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbxDir;
        
        #line default
        #line hidden
        
        
        #line 43 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.GroupBox gpbPosition;
        
        #line default
        #line hidden
        
        
        #line 45 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbxX;
        
        #line default
        #line hidden
        
        
        #line 46 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbxY;
        
        #line default
        #line hidden
        
        
        #line 47 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox tbxZ;
        
        #line default
        #line hidden
        
        
        #line 51 "..\..\EPoint.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image imgLock;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Hongyang;component/epoint.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\EPoint.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.skpMain1 = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 2:
            this.cbxType = ((System.Windows.Controls.ComboBox)(target));
            
            #line 15 "..\..\EPoint.xaml"
            this.cbxType.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.ComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 3:
            this.chxMove = ((System.Windows.Controls.CheckBox)(target));
            
            #line 23 "..\..\EPoint.xaml"
            this.chxMove.Click += new System.Windows.RoutedEventHandler(this.ChxSeparate_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.skpMain2 = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 5:
            this.chxSeparate = ((System.Windows.Controls.CheckBox)(target));
            
            #line 27 "..\..\EPoint.xaml"
            this.chxSeparate.Click += new System.Windows.RoutedEventHandler(this.ChxSeparate_Click);
            
            #line default
            #line hidden
            return;
            case 6:
            this.cbxDir = ((System.Windows.Controls.ComboBox)(target));
            
            #line 32 "..\..\EPoint.xaml"
            this.cbxDir.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.ComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 7:
            this.tbxDir = ((System.Windows.Controls.TextBox)(target));
            
            #line 39 "..\..\EPoint.xaml"
            this.tbxDir.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.TextBox_TextChanged);
            
            #line default
            #line hidden
            return;
            case 8:
            this.gpbPosition = ((System.Windows.Controls.GroupBox)(target));
            return;
            case 9:
            this.tbxX = ((System.Windows.Controls.TextBox)(target));
            
            #line 45 "..\..\EPoint.xaml"
            this.tbxX.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.TextBox_TextChanged);
            
            #line default
            #line hidden
            return;
            case 10:
            this.tbxY = ((System.Windows.Controls.TextBox)(target));
            
            #line 46 "..\..\EPoint.xaml"
            this.tbxY.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.TextBox_TextChanged);
            
            #line default
            #line hidden
            return;
            case 11:
            this.tbxZ = ((System.Windows.Controls.TextBox)(target));
            
            #line 47 "..\..\EPoint.xaml"
            this.tbxZ.TextChanged += new System.Windows.Controls.TextChangedEventHandler(this.TextBox_TextChanged);
            
            #line default
            #line hidden
            return;
            case 12:
            this.imgLock = ((System.Windows.Controls.Image)(target));
            
            #line 51 "..\..\EPoint.xaml"
            this.imgLock.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.Image_MouseLeftButtonUp);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

