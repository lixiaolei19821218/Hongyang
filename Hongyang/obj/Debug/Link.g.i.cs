﻿#pragma checksum "..\..\Link.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "EF3725AAA203084A1AAE1CD73490A83DF8975C1E"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Hongyang.Converter;
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
    /// Link
    /// </summary>
    public partial class Link : System.Windows.Controls.Page, System.Windows.Markup.IComponentConnector {
        
        
        #line 16 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbxType0;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chx1stConstraint;
        
        #line default
        #line hidden
        
        
        #line 37 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbx1stLink1stConstraint;
        
        #line default
        #line hidden
        
        
        #line 50 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbx1stLink2ndConstraint;
        
        #line default
        #line hidden
        
        
        #line 70 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbxType1;
        
        #line default
        #line hidden
        
        
        #line 78 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chx2ndConstraint;
        
        #line default
        #line hidden
        
        
        #line 91 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbx2ndLink1stConstraint;
        
        #line default
        #line hidden
        
        
        #line 104 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbx2ndLink2ndConstraint;
        
        #line default
        #line hidden
        
        
        #line 122 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox cbxTypeDef;
        
        #line default
        #line hidden
        
        
        #line 127 "..\..\Link.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox chxGouge;
        
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
            System.Uri resourceLocater = new System.Uri("/Hongyang;component/link.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\Link.xaml"
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
            this.cbxType0 = ((System.Windows.Controls.ComboBox)(target));
            
            #line 16 "..\..\Link.xaml"
            this.cbxType0.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CbxProbing_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.chx1stConstraint = ((System.Windows.Controls.CheckBox)(target));
            
            #line 24 "..\..\Link.xaml"
            this.chx1stConstraint.Click += new System.Windows.RoutedEventHandler(this.CheckBox_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.cbx1stLink1stConstraint = ((System.Windows.Controls.ComboBox)(target));
            
            #line 37 "..\..\Link.xaml"
            this.cbx1stLink1stConstraint.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CbxConstraint_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 45 "..\..\Link.xaml"
            ((System.Windows.Controls.ComboBox)(target)).SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.ComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 5:
            
            #line 49 "..\..\Link.xaml"
            ((System.Windows.Controls.TextBox)(target)).SelectionChanged += new System.Windows.RoutedEventHandler(this.TextBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 6:
            this.cbx1stLink2ndConstraint = ((System.Windows.Controls.ComboBox)(target));
            
            #line 50 "..\..\Link.xaml"
            this.cbx1stLink2ndConstraint.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CbxConstraint_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 7:
            
            #line 58 "..\..\Link.xaml"
            ((System.Windows.Controls.ComboBox)(target)).SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.ComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 8:
            
            #line 62 "..\..\Link.xaml"
            ((System.Windows.Controls.TextBox)(target)).SelectionChanged += new System.Windows.RoutedEventHandler(this.TextBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 9:
            this.cbxType1 = ((System.Windows.Controls.ComboBox)(target));
            
            #line 70 "..\..\Link.xaml"
            this.cbxType1.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CbxProbing_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 10:
            this.chx2ndConstraint = ((System.Windows.Controls.CheckBox)(target));
            
            #line 78 "..\..\Link.xaml"
            this.chx2ndConstraint.Click += new System.Windows.RoutedEventHandler(this.CheckBox_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            this.cbx2ndLink1stConstraint = ((System.Windows.Controls.ComboBox)(target));
            
            #line 91 "..\..\Link.xaml"
            this.cbx2ndLink1stConstraint.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CbxConstraint_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 12:
            
            #line 99 "..\..\Link.xaml"
            ((System.Windows.Controls.ComboBox)(target)).SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.ComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 13:
            
            #line 103 "..\..\Link.xaml"
            ((System.Windows.Controls.TextBox)(target)).SelectionChanged += new System.Windows.RoutedEventHandler(this.TextBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 14:
            this.cbx2ndLink2ndConstraint = ((System.Windows.Controls.ComboBox)(target));
            
            #line 104 "..\..\Link.xaml"
            this.cbx2ndLink2ndConstraint.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CbxConstraint_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 15:
            
            #line 112 "..\..\Link.xaml"
            ((System.Windows.Controls.ComboBox)(target)).SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.ComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 16:
            
            #line 116 "..\..\Link.xaml"
            ((System.Windows.Controls.TextBox)(target)).SelectionChanged += new System.Windows.RoutedEventHandler(this.TextBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 17:
            this.cbxTypeDef = ((System.Windows.Controls.ComboBox)(target));
            
            #line 122 "..\..\Link.xaml"
            this.cbxTypeDef.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.CbxConstraint_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 18:
            this.chxGouge = ((System.Windows.Controls.CheckBox)(target));
            
            #line 127 "..\..\Link.xaml"
            this.chxGouge.Click += new System.Windows.RoutedEventHandler(this.CheckBox_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

