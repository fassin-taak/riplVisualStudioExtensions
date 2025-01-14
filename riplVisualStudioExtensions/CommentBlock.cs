﻿/*
ISC License

Copyright(c) 2021, Raghavendra Chandrashekara

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted, provided that the above
copyright notice and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

// Code is derived from: https://docs.microsoft.com/en-gb/previous-versions/visualstudio/visual-studio-2015/extensibility/walkthrough-using-a-shell-command-with-an-editor-extension?view=vs-2015&redirectedfrom=MSDN

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace riplVisualStudioExtensions {
  class CommentBlock : Canvas {
    private Geometry textGeometry;
    private Grid commentGrid;
    private static Brush brush;
    private static Pen solidPen;
    private static Pen dashPen;

    public CommentBlock(double textRightEdge, double viewRightEdge, Geometry newTextGeometry,
      string author, string body) {

      if (brush == null) {
        brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0xff, 0x00));
        brush.Freeze();

        var penBrush = new SolidColorBrush(Colors.Green);
        penBrush.Freeze();

        var solidPen = new Pen(penBrush, 0.5);
        solidPen.Freeze();

        var dashPen = new Pen(penBrush, 0.5);
        dashPen.DashStyle = DashStyles.Dash;
        dashPen.Freeze();
      }

      this.textGeometry = newTextGeometry;

      TextBlock tb1 = new TextBlock();
      tb1.Text = author;
      TextBlock tb2 = new TextBlock();
      tb2.Text = body;

      const int MarginWidth = 8;
      this.commentGrid = new Grid();
      this.commentGrid.RowDefinitions.Add(new RowDefinition());
      this.commentGrid.RowDefinitions.Add(new RowDefinition());
      ColumnDefinition cEdge = new ColumnDefinition();
      cEdge.Width = new GridLength(MarginWidth);
      ColumnDefinition cEdge2 = new ColumnDefinition();
      cEdge2.Width = new GridLength(MarginWidth);
      this.commentGrid.ColumnDefinitions.Add(cEdge);
      this.commentGrid.ColumnDefinitions.Add(new ColumnDefinition());
      this.commentGrid.ColumnDefinitions.Add(cEdge2);

      System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle();
      rect.RadiusX = 6;
      rect.RadiusY = 3;
      rect.Fill = brush;
      rect.Stroke = Brushes.Green;

      Size inf = new Size(double.PositiveInfinity, double.PositiveInfinity);
      tb1.Measure(inf);
      tb2.Measure(inf);
      double middleWidth = Math.Max(tb1.DesiredSize.Width, tb2.DesiredSize.Width);
      this.commentGrid.Width = middleWidth + 2 * MarginWidth;

      Grid.SetColumn(rect, 0);
      Grid.SetRow(rect, 0);
      Grid.SetRowSpan(rect, 2);
      Grid.SetColumnSpan(rect, 3);
      Grid.SetRow(tb1, 0);
      Grid.SetColumn(tb1, 1);
      Grid.SetRow(tb2, 1);
      Grid.SetColumn(tb2, 1);
      this.commentGrid.Children.Add(rect);
      this.commentGrid.Children.Add(tb1);
      this.commentGrid.Children.Add(tb2);

      Canvas.SetLeft(this.commentGrid, Math.Max(viewRightEdge - this.commentGrid.Width - 20.0, textRightEdge + 20.0));
      Canvas.SetTop(this.commentGrid, textGeometry.GetRenderBounds(solidPen).Top);

      this.Children.Add(this.commentGrid);
    }


    protected override void OnRender(DrawingContext dc) {
      base.OnRender(dc);
      if (this.textGeometry != null) {
        dc.DrawGeometry(brush, solidPen, this.textGeometry);
        Rect textBounds = this.textGeometry.GetRenderBounds(solidPen);
        Point p1 = new Point(textBounds.Right, textBounds.Bottom);
        Point p2 = new Point(Math.Max(Canvas.GetLeft(this.commentGrid) - 20.0, p1.X), p1.Y);
        Point p3 = new Point(Math.Max(Canvas.GetLeft(this.commentGrid), p1.X), (Canvas.GetTop(this.commentGrid) + p1.Y) * 0.5);
        dc.DrawLine(dashPen, p1, p2);
        dc.DrawLine(dashPen, p2, p3);
      }
    }

  }
}
