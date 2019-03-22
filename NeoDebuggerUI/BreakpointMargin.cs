using System.Globalization;
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

        public delegate void BreakpointListChanged();
        public event BreakpointListChanged EvtBreakpointListChanged;

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

        public void AddBreakpoint(int lineNumber)
        {
            BreakpointLines.Add(lineNumber);
            EvtBreakpointListChanged?.Invoke();
        }

        public void RemoveBreakpoint(int lineNumber)
        {
            BreakpointLines.Remove(lineNumber);
            EvtBreakpointListChanged?.Invoke();
        }
    }
}
