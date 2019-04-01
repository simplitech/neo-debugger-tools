﻿using System.Globalization;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using AvaloniaEdit.Utils;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace NeoDebuggerUI
{
    class BreakpointMargin : LineNumberMargin
    {
        private HashSet<int> BreakpointLines { get; set; } = new HashSet<int>();
        private int CurrentLine { get; set; } = 0;
        private int SelectionStartOffset { get; set; } = 0;
        private int SelectionEndOffset { get; set; } = 0;

        private IBrush BreakpointLineColor = new SolidColorBrush(Colors.Tomato);
        private IBrush StepLineColor = new SolidColorBrush(Colors.Gold);

        public override void Render(DrawingContext drawingContext)
        {
            drawingContext.FillRectangle(new SolidColorBrush(Colors.Snow), Bounds);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.LightGray), 0.5), Bounds.TopRight, Bounds.BottomRight);

            var textView = TextView;
            var renderSize = Bounds.Size;
            if (textView != null && textView.VisualLinesValid)
            {
                foreach (var line in textView.VisualLines)
                {
                    if (BreakpointLines.Contains(line.FirstDocumentLine.LineNumber))
                    {
                        var y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);

                        var breakpointForm = new EllipseGeometry(new Rect((Bounds.Size.Width / 4) - 1,
                                                                           y + (Bounds.Size.Width / 4) - TextView.VerticalOffset,
                                                                           line.Height / 1.5, line.Height / 1.5));

                        drawingContext.DrawGeometry(new SolidColorBrush(Colors.Red),
                                                    new Pen(new SolidColorBrush(Colors.DarkSlateGray), 0.5), breakpointForm);
                    }
                }

                var currentLine = textView.GetVisualLine(CurrentLine);
                if (currentLine != null)
                {
                    IBrush highlightColor;

                    if (BreakpointLines.Contains(CurrentLine))
                    {
                        // highlight only the text with BreakpointLineColor
                        highlightColor = BreakpointLineColor;
                    }
                    else
                    {
                        // highlight full line with StepLineColor
                        highlightColor = StepLineColor;
                    }

                    foreach (var element in currentLine.Elements)
                    {
                        if (currentLine.Elements.Count == 1)
                        {
                            element.BackgroundBrush = highlightColor;
                            break;
                        }
                        
                        if (element.RelativeTextOffset >= SelectionStartOffset && element.RelativeTextOffset < SelectionEndOffset)
                        {
                            element.BackgroundBrush = highlightColor;
                        }
                    }
                }
            }
        }

        public void UpdateBreakpointView(HashSet<int> breakpoints, int currentLine, int offset = 0, int length = 0)
        {
            BreakpointLines = breakpoints;
            if (currentLine >= 0 && currentLine != CurrentLine)
            {
                if (CurrentLine != 0)
                {
                    var line = TextView.GetVisualLine(CurrentLine);
                    if (line != null)
                    {
                        foreach (var element in line.Elements)
                        {
                            element.BackgroundBrush = null;
                        }
                    }
                }
                CurrentLine = currentLine;
                SelectionStartOffset = offset;
                SelectionEndOffset = offset + length;
            }
        }

        public void UpdateBreakpointMargin(HashSet<int> breakpoints, int newBreakpointLine)
        {
            BreakpointLines = breakpoints;

            if (CurrentLine != 0 && CurrentLine == newBreakpointLine)
            {
                var line = TextView.GetVisualLine(CurrentLine);
                if (line != null)
                {
                    foreach (var element in line.Elements)
                    {
                        if (element.BackgroundBrush == BreakpointLineColor)
                        {
                            element.BackgroundBrush = StepLineColor;
                        }
                        else if (element.BackgroundBrush == StepLineColor)
                        {
                            element.BackgroundBrush = BreakpointLineColor;
                        }
                    }
                }
            }
        }
    }
}
