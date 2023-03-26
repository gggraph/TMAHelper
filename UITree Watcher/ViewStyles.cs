using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;
using System.Windows.Automation;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Windows.Forms;
using System.Drawing;
namespace UITree_Watcher
{
    public class ViewStyle
    {
        public virtual void HighlightNode(Node n, bool _enabled) { }
        public virtual void ApplyNodeStyle(Node n) { }
        public virtual void ApplyEdgeStyle(Edge e) { }
        public virtual void ApplyViewerStyle(GViewer viewer) { }
        public virtual void ApplyGraphStyle(Graph graph) { }
    }

    public class SimpleDarkStyle : ViewStyle 
    {
        public override void HighlightNode(Node n, bool _enabled) 
        {
            if (_enabled)
                n.Attr.LineWidth = 6;
            else
                n.Attr.LineWidth = 3;
        }
        public override void ApplyNodeStyle(Node n)
        {
            n.Attr.Shape = Shape.Box;
            n.Attr.Color = Microsoft.Msagl.Drawing.Color.White;
            n.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Black;
            n.Label.FontName = "Courier New";
            n.Label.FontColor = Microsoft.Msagl.Drawing.Color.White;
            AutomationElement element = (AutomationElement)n.UserData;
            n.LabelText = element.Current.ClassName + "\n" + element.Current.Name;
        }
        public override void ApplyEdgeStyle(Edge e)
        {
            e.Attr.Color = Microsoft.Msagl.Drawing.Color.White;
        }
        public override void ApplyViewerStyle(GViewer viewer)
        {
            viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            viewer.OutsideAreaBrush = Brushes.Black;
        }
        public override void ApplyGraphStyle(Graph graph)
        {
            graph.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
            graph.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.Black;
        }
    }
    public class SimpleLightStyle : ViewStyle
    {
        public override void HighlightNode(Node n, bool _enabled)
        {
            if (_enabled)
                n.Attr.LineWidth = 6;
            else
                n.Attr.LineWidth = 3;
        }
        public override void ApplyNodeStyle(Node n)
        {
            n.Attr.Shape = Shape.Box;
            n.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
            n.Attr.FillColor = Microsoft.Msagl.Drawing.Color.White;
            n.Label.FontName = "Courier New";
            n.Label.FontColor = Microsoft.Msagl.Drawing.Color.Black;
            AutomationElement element = (AutomationElement)n.UserData;
            n.LabelText = element.Current.ClassName + "\n" + element.Current.Name;

        }
        public override void ApplyEdgeStyle(Edge e)
        {
            e.Attr.Color = Microsoft.Msagl.Drawing.Color.Black;
        }
        public override void ApplyViewerStyle(GViewer viewer)
        {
            viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            viewer.OutsideAreaBrush = Brushes.White;
        }
        public override void ApplyGraphStyle(Graph graph)
        {
            graph.Attr.Color = Microsoft.Msagl.Drawing.Color.White;
            graph.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.White;
        }
    }

    public class DummySPRendererStyle : ViewStyle 
    {
        private Font font;

        static float radiusRatio = 0.3f;

        static internal PointF PointF(Microsoft.Msagl.Core.Geometry.Point p) { return new PointF((float)p.X, (float)p.Y); }
        static System.Drawing.Drawing2D.GraphicsPath FillTheGraphicsPath(ICurve iCurve)
        {
            var curve = ((RoundedRect)iCurve).Curve;
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            foreach (ICurve seg in curve.Segments)
                AddSegmentToPath(seg, ref path);
            return path;
        }
        private static void AddSegmentToPath(ICurve seg, ref System.Drawing.Drawing2D.GraphicsPath p)
        {
            const float radiansToDegrees = (float)(180.0 / Math.PI);
            LineSegment line = seg as LineSegment;
            if (line != null)
                p.AddLine(PointF(line.Start), PointF(line.End));
            else
            {
                CubicBezierSegment cb = seg as CubicBezierSegment;
                if (cb != null)
                    p.AddBezier(PointF(cb.B(0)), PointF(cb.B(1)), PointF(cb.B(2)), PointF(cb.B(3)));
                else
                {
                    Ellipse ellipse = seg as Ellipse;
                    if (ellipse != null)
                        p.AddArc((float)(ellipse.Center.X - ellipse.AxisA.Length), (float)(ellipse.Center.Y - ellipse.AxisB.Length),
                            (float)(2 * ellipse.AxisA.Length), (float)(2 * ellipse.AxisB.Length), (float)(ellipse.ParStart * radiansToDegrees),
                            (float)((ellipse.ParEnd - ellipse.ParStart) * radiansToDegrees));
                }
            }
        }
        private ICurve GetNodeBoundary(Microsoft.Msagl.Drawing.Node node)
        {
            // Get image from node id
            //Image image = ImageOfNode(node);
            AutomationElement element = (AutomationElement)node.UserData;
            string cls = element.Current.ClassName;
            string name = element.Current.Name;
            System.Drawing.Size fSize = TextRenderer.MeasureText(cls, font);
            System.Drawing.Size fSize2 = TextRenderer.MeasureText(name, font);

            double width, height;
            height = Math.Max(10, fSize.Height + fSize2.Height);
            width = Math.Max(fSize.Width, fSize2.Width);
            if (width < 10)
                width = 10;

            // Create a curve rectangle with (w,h,rad,rad)
            return CurveFactory.CreateRectangleWithRoundedCorners(width, height, 
                width * radiusRatio, width * radiusRatio,
                new Microsoft.Msagl.Core.Geometry.Point());
        }
        private bool DrawNode(Node node, object graphics)
        {
            Graphics g = (Graphics)graphics;
            AutomationElement element = (AutomationElement)node.UserData;

            using (System.Drawing.Drawing2D.Matrix m = g.Transform)
            {
                using (System.Drawing.Drawing2D.Matrix saveM = m.Clone())
                {

                    g.SetClip(FillTheGraphicsPath(node.GeometryNode.BoundaryCurve));
                    using (var m2 = new System.Drawing.Drawing2D.Matrix(1, 0, 0, -1, 0, 2 * (float)node.GeometryNode.Center.Y))
                        m.Multiply(m2);

                    g.Transform = m;
                    System.Drawing.Point pStart = new System.Drawing.Point((int)(node.GeometryNode.Center.X - node.GeometryNode.Width / 2),
                        (int)(node.GeometryNode.Center.Y - node.GeometryNode.Height / 2));
                    System.Drawing.Point pEnd = new System.Drawing.Point((int)(node.GeometryNode.Center.X + node.GeometryNode.Width / 2),
                        (int)(node.GeometryNode.Center.Y + node.GeometryNode.Height / 2));

                    g.FillRectangle(Brushes.Black, new Rectangle(pStart.X, pStart.Y, (int)(node.GeometryNode.Width), (int)(node.GeometryNode.Height)));

                    string cls = element.Current.ClassName;
                    string name = element.Current.Name;
                    g.DrawString(cls, font, Brushes.White, new PointF((float)(node.GeometryNode.Center.X - node.GeometryNode.Width / 2),
                        (float)(node.GeometryNode.Center.Y - node.GeometryNode.Height / 2)));
                    g.DrawString(name, font, Brushes.White, new PointF((float)(node.GeometryNode.Center.X - node.GeometryNode.Width / 2),
                        (float)(node.GeometryNode.Center.Y - node.GeometryNode.Height / 2) + TextRenderer.MeasureText(cls, font).Height));

                    g.Transform = saveM;
                    g.ResetClip();
                }
            }

            return true;
        }
        public override void HighlightNode(Node n, bool _enabled)
        {
            if (_enabled)
                n.Attr.LineWidth = 6;
            else
                n.Attr.LineWidth = 3;
        }
        public override void ApplyNodeStyle(Node n)
        {
            n.Attr.Shape = Shape.DrawFromGeometry;
            n.DrawNodeDelegate = new DelegateToOverrideNodeRendering(DrawNode);
            n.NodeBoundaryDelegate = new DelegateToSetNodeBoundary(GetNodeBoundary);
        }
        public override void ApplyViewerStyle(GViewer viewer)
        {
            viewer.Dock = System.Windows.Forms.DockStyle.Fill;
        }
       

    }

    
}
