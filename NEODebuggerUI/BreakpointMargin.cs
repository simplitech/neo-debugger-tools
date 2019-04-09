using System.Globalization;
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using AvaloniaEdit.Utils;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace NEODebuggerUI
{
    class BreakpointMargin : LineNumberMargin
    {
        private HashSet<int> BreakpointLines { get; set; } = new HashSet<int>();
        private int CurrentLine { get; set; } = 0;
        private int SelectionStartOffset { get; set; } = 0;
        private int SelectionEndOffset { get; set; } = 0;

        private IBrush BreakpointLineColor = new SolidColorBrush(Colors.PaleVioletRed);

        public override void Render(DrawingContext drawingContext)
        {
            drawingContext.FillRectangle(new SolidColorBrush(Colors.WhiteSmoke), Bounds);
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Colors.Gray), 0.5), Bounds.TopRight, Bounds.BottomRight);

            var textView = TextView;
            var renderSize = Bounds.Size;
            if (textView != null && textView.VisualLinesValid)
            {
                foreach (var line in textView.VisualLines)
                {
                    if (BreakpointLines.Contains(line.FirstDocumentLine.LineNumber))
                    {
                        if (line.FirstDocumentLine.LineNumber == CurrentLine)
                        {
                            foreach (var element in line.Elements)
                            {
                                if (line.Elements.Count == 1)
                                {
                                    element.BackgroundBrush = BreakpointLineColor;
                                    break;
                                }

                                var elementEndOffset = element.RelativeTextOffset + element.DocumentLength;
                                if (element.RelativeTextOffset >= SelectionStartOffset && elementEndOffset <= SelectionEndOffset)
                                {
                                    element.BackgroundBrush = BreakpointLineColor;
                                }
                            }
                        }

                        var y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);

                        var breakpointForm = new EllipseGeometry(new Rect((Bounds.Size.Width / 4) - 1,
                                                    y + (Bounds.Size.Width / 4) - TextView.VerticalOffset,
                                                    line.Height / 1.5, line.Height / 1.5));

                        drawingContext.DrawGeometry(new SolidColorBrush(Colors.Red),
                                                    new Pen(new SolidColorBrush(Colors.DarkSlateGray), 0.5), breakpointForm);
                    }
                }
            }
        }

        public void UpdateBreakpointMargin(HashSet<int> breakpoints, int currentLine = 0, int offset = 0, int length = 0)
        {
            BreakpointLines = breakpoints;
            if (currentLine >= 0 && currentLine != CurrentLine)
            {
                if (CurrentLine != 0)
                {
                    var line = TextView.GetVisualLine(CurrentLine);
                    foreach (var element in line.Elements)
                    {
                        element.BackgroundBrush = null;
                    }
                }
                CurrentLine = currentLine;
                SelectionStartOffset = offset;
                SelectionEndOffset = offset + length;
            }
        }
    }
}
