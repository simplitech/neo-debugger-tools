﻿using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using NeoDebuggerUI.ViewModels;
using ReactiveUI;
using System.IO;
using System.Linq;
using Avalonia.Layout;
using LunarLabs.Parser;
using Neo.Debugger.Core.Utils;
using Neo.Emulation;
using NeoDebuggerUI.Models;

namespace NeoDebuggerUI.Views
{
    public class InvokeWindow : ReactiveWindow<InvokeWindowViewModel>
    {
        public InvokeWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            RenderTestCaseParams(ViewModel.SelectedTestCaseParams);
            RegisterInteraction();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }  

        private void RenderTestCaseParams(DataNode tparams)
        {
            var grid = this.FindControl<Grid>("InputParametersGrid");
            
            var rowHeader = new RowDefinition {Height = new GridLength(20)};
            grid.RowDefinitions.Add(rowHeader);

            var paramHeader = new TextBlock {Text = "Parameter", FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center};
            Grid.SetRow(paramHeader, 0);
            Grid.SetColumn(paramHeader, 0);
            grid.Children.Add(paramHeader);

            var valueHeader = new TextBlock {Text = "Value", FontWeight = FontWeight.Bold, TextAlignment = TextAlignment.Center};
            Grid.SetRow(valueHeader, 0);
            Grid.SetColumn(valueHeader, 1);
            grid.Children.Add(valueHeader);

            var p1 = DebuggerUtils.ParseNode(tparams[0], 0);
            var p2 = DebuggerUtils.ParseNode(tparams[1], 1);

            RenderLine(grid, 1, "operation", p1);
            RenderLine(grid, 2, "args", p2);
        }

        private void RenderLine(Grid grid, int rowCount, string v1, string v2)
        {
            var rowView = new RowDefinition {Height = new GridLength(30)};
            grid.RowDefinitions.Add(rowView);
                
            var v1View = new TextBlock
            {
                Text = v1,
                TextAlignment = TextAlignment.Right, 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = Thickness.Parse("5")
            };
            Grid.SetRow(v1View, rowCount);
            Grid.SetColumn(v1View, 0);
            grid.Children.Add(v1View);

            var v2View = new TextBox {Text = v2};
            Grid.SetRow(v2View, rowCount);
            Grid.SetColumn(v2View, 1);
            grid.Children.Add(v2View);
        }

        private string ExtractValueFromGrid(int rowIndex, int colIndex)
        {
            var g = this.FindControl<Grid>("InputParametersGrid");

            foreach (var e in g.Children)
            {
                if (Grid.GetRow((AvaloniaObject) e) != rowIndex ||
                    Grid.GetColumn((AvaloniaObject) e) != colIndex) continue;
                
                if (e is TextBox box)
                {
                    return box.Text;
                }

                return "";
            }

            return "";
        }

        private void RegisterInteraction()
        {
            this.FindControl<Button>("DebugBtn").Click += (_,__) =>
            {
                var op = ExtractValueFromGrid(1, 1);
                var args = ExtractValueFromGrid(2, 1);
                ViewModel.DebugParams.ArgList = DebuggerUtils.GetArgsListAsNode(string.Concat(op, ",", args));
                ViewModel.Run();
                Close();
            };
        }
    }
}