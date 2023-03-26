using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Automation;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Windows.Input;
using System.Windows;
using System.Runtime.InteropServices;


namespace UITree_Watcher
{

    /*
     Things to add :
     Navigate with arrow                                                                            [DONE]
     on click [+][-]develop/reduce Graph tree
     Left click for drag moving on plane

     develop style on mouseover/selected [OK] ->  There is so much potential style it is so good    [++  ] 
     see more info on right pane of selected node
     freeze node / recompute 
     
    
     */

    /*
     * 
     Css: 

        

     Methods:
        - Branch expansion:
            Expand(node,maxDistance) 0 means expand everything, 1 means expend only first branch, 2 -> up to second etc. ...
            Reduce(node) reduce all branches from that nodes

            Keystroke from selNode:
            space : expand(1)/reduce
            shift+space : expand(0)/reduce

       - Watcher : 
            Create a new diagnostic : 
                it records a branch path (by saving element properties and attributes) from a starting node to an 
                ending node. 
            Running diagnostics are present in a ribbon bar...
            Start GlobalWatch : 
                create a bunch of watches from any ending node to root node

       - Path : 
            StrictPath and expected properties (broke)
            /0/2/6/8/9/5 (are successive child index to ui) 
            relativePath
                a series of path based on AND/OR equality on element properties
            
                
            
            
     */
    public partial class Form1 : Form
    {
        #region USER23DLL

        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("User32.dll")]
        public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);

        public static void ScreenHL(Rectangle rec) 
        {
            IntPtr desktopPtr = GetDC(IntPtr.Zero);
            Graphics g = Graphics.FromHdc(desktopPtr);

            g.DrawRectangle(new Pen(Brushes.Red,5), rec);

            g.Dispose();
            ReleaseDC(IntPtr.Zero, desktopPtr);

        }

        private const int WmPaint = 0x000F;
       

        [DllImport("User32.dll")]
        public static extern Int64 SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static void ForcePaint(IntPtr Hwnd)
        {
            SendMessage(Hwnd, WmPaint, IntPtr.Zero, IntPtr.Zero);
        }


        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow); //ShowWindow needs an IntPtr


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        // Usage: Call this method with the handle of the target window and the new position.
        public static void MoveWindow(IntPtr hWnd, int x, int y)
        {
            RECT rect;
            GetWindowRect(hWnd, out rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, 0);
           
        }
        #endregion


        private const int extHandle = 0x003B0094;
        public static Font font;
        public Form1()
        {
            base.KeyPreview = true;
            InitializeComponent();
            AutomationElement root = AutomationElement.FromHandle((IntPtr)extHandle);
           
            FontFamily fontFamily = new FontFamily("Courier New");
            font = new Font(
               fontFamily,
               16,
               System.Drawing.FontStyle.Regular,
               GraphicsUnit.Pixel);

            CreateDummy(root);
        }
        public static void CreateDummy(AutomationElement root) 
        {
            //create a form 
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            //create a viewer object 
            Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            //create a graph object 
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph");

            // Adding a stack of AutomationElement
            /*
            Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node("0");
            cNode.UserData = root;
            graph.AddNode(cNode);
            ExpandNodeFromRootNode(graph, cNode);
            */
            
            List<AutomationElement> stack = new List<AutomationElement>() { root };
            Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node("0");
            cNode.UserData = root;
            graph.AddNode(cNode);
            int i = 1;
            int n = 0;
            while (stack.Count > 0) 
            {

                AutomationElementCollection child = stack[0].FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.IsEnabledProperty, true));
                foreach (AutomationElement children in child)
                {
                    // @ Add new node to the layer
                    cNode = new Microsoft.Msagl.Drawing.Node(i.ToString());
                    cNode.UserData = children;
                    cNode.Attr.Shape = Shape.Box;
                    //ExpandNodeInformation(cNode, true);
                    
                    // Faster and efficient
                    cNode.Attr.Color = Microsoft.Msagl.Drawing.Color.White;
                    cNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Black;
                    cNode.Label.FontName = "Courier New";
                    cNode.Label.FontColor = Microsoft.Msagl.Drawing.Color.White;
                    cNode.LabelText = children.Current.ClassName+"\n"+ children.Current.Name;
                    
                    
                    graph.AddNode(cNode);
                    //@ Add children to the stack
                    stack.Add(children);
                    //@ Create connection
                    Edge e = graph.AddEdge(n.ToString(), cNode.Id);
                    e.Attr.Color = Microsoft.Msagl.Drawing.Color.White;

                    i++;
                }
                //Consume
                stack.RemoveAt(0);
                n++;
            }
            
            graph.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.Black;
            viewer.BackColor = System.Drawing.Color.Black;
            viewer.Click += Viewer_Click;
            viewer.KeyDown += Viewer_KeyDown;
            viewer.MouseMove += Viewer_MouseMove;
            viewer.MouseWheel += Viewer_MouseWheel;
            //bind the graph to the viewer 
            viewer.Graph = graph;

            //associate the viewer with the form ---->(°_°)<----
            form.SuspendLayout();
            viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            form.Controls.Add(viewer);
            form.ResumeLayout();
            form.BackColor = System.Drawing.Color.Black;
            //show the form 
            form.ShowDialog();
        }

        public static void ExpandNodeFromRootNode(Graph graph, Node rootNode, int iterationLimit = 0) 
        {
            // Adding a stack of AutomationElement
            List<AutomationElement> stack = new List<AutomationElement>() { (AutomationElement)rootNode.UserData };

            int sti = graph.NodeCount;
            int i = sti;
            int n = int.Parse(rootNode.Id);
            int iter = 0;
            while (stack.Count > 0)
            {

                AutomationElementCollection child = stack[0].FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.IsEnabledProperty, true));
                foreach (AutomationElement children in child)
                {
                    // @ Add new node to the layer
                    Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node(i.ToString());
                    cNode.UserData = children;
                    cNode.Attr.Shape = Shape.Box;
                    //ExpandNodeInformation(cNode, true);

                    // Faster and efficient
                    cNode.Attr.Color = Microsoft.Msagl.Drawing.Color.White;
                    cNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Black;
                    cNode.Label.FontName = "Courier New";
                    cNode.Label.FontColor = Microsoft.Msagl.Drawing.Color.White;
                    cNode.LabelText = children.Current.ClassName + "\n" + children.Current.Name;


                    graph.AddNode(cNode);
                    //@ Add children to the stack
                    stack.Add(children);
                    //@ Create connection
                    Edge e = graph.AddEdge(n.ToString(), cNode.Id);
                    e.Attr.Color = Microsoft.Msagl.Drawing.Color.White;

                    i++;
                }
                //Consume
                stack.RemoveAt(0);

                //Jump to initial top if iteration 0
                if (iter==0)
                    n = sti;
                else
                    n++;

                iter++;
                if (iter > iterationLimit)
                    break;
            }

        }
        

        private static void Viewer_MouseWheel(object sender, MouseEventArgs e)
        {
            GViewer viewer = sender as GViewer;
            Console.WriteLine(viewer.ZoomF +"->"+viewer.Bounds.Width + " " +viewer.Bounds.Height);
        }

        public static Node selNode;
        private static void Viewer_KeyDown(object sender, KeyEventArgs e)
        {
            GViewer viewer = sender as GViewer;
            if (selNode != null )
            {
                Node node = selNode as Node;
                Node next = null;
                if ( e.KeyCode == Keys.Z) 
                {
                    if (node.InEdges.Count() > 0) 
                        next = node.InEdges.LastOrDefault().SourceNode;
                }
                else if (e.KeyCode == Keys.S)
                {
                    if (node.OutEdges.Count() > 0)
                        next = node.OutEdges.ToList()[node.OutEdges.Count() / 2].TargetNode;
                }
                else if (e.KeyCode == Keys.D && node.InEdges.Count() > 0)
                {
                    Node parent = node.InEdges.FirstOrDefault().SourceNode;
                    List<Edge> edges = parent.OutEdges.ToList();
                    for (int i = 0; i < edges.Count; i++) 
                    {
                        if (edges[i].TargetNode==node && i > 0) 
                        {
                            next = edges[i - 1].TargetNode;
                            break;
                        }
                    }
                }
                else if (e.KeyCode == Keys.Q && node.InEdges.Count() > 0)
                {
                    Node parent = node.InEdges.FirstOrDefault().SourceNode;
                    List<Edge> edges = parent.OutEdges.ToList();
                    for (int i = 0; i < edges.Count; i++)
                    {
                        if (edges[i].TargetNode == node && i < edges.Count-1)
                        {
                            next = edges[i + 1].TargetNode;
                            break;
                        }
                    }
                }
                if ( next != null) 
                {
                    //Psudo select next...
                    node.Attr.LineWidth = 3;
                    next.Attr.LineWidth = 6;
                    selNode = next;
                    Console.WriteLine(((AutomationElement)next.UserData).Current.Name);
                    viewer.Refresh();
                    double z = viewer.ZoomF;
                    viewer.ShowBBox(new Microsoft.Msagl.Core.Geometry.Rectangle
                    {
                        Center = selNode.Pos,
                        Width = viewer.Bounds.Width,
                        Height = viewer.Bounds.Height,
                    });
                    viewer.ZoomF = z;

                    // Highlight One Element...
                    AutomationElement ae = (AutomationElement)selNode.UserData;

                    RECT winRect;
                    GetWindowRect((IntPtr)extHandle, out winRect);
                    MoveWindow((IntPtr)extHandle, winRect.Left, winRect.Top);
                    //;
                   
                    SetForegroundWindow((IntPtr)viewer.Handle);
                    ScreenHL(
                      new System.Drawing.Rectangle(
                          (int)ae.Current.BoundingRectangle.X,
                          (int)ae.Current.BoundingRectangle.Y,
                          (int)ae.Current.BoundingRectangle.Width,
                          (int)ae.Current.BoundingRectangle.Height));

                }

            }

        }
       

        #region Node Rendering
        static ICurve GetNodeBoundary(Microsoft.Msagl.Drawing.Node node)
        {
            // Get image from node id
            //Image image = ImageOfNode(node);
            AutomationElement element = (AutomationElement)node.UserData;
            string cls = element.Current.ClassName;
            string name= element.Current.Name;
            System.Drawing.Size fSize = TextRenderer.MeasureText(cls, font);
            System.Drawing.Size fSize2 =TextRenderer.MeasureText(name, font);

            double width, height;
            height = Math.Max(10, fSize.Height + fSize2.Height);
            width = Math.Max(fSize.Width, fSize2.Width);
            if (width < 10)
                width = 10;

            // Create a curve rectangle with (w,h,rad,rad)
            return CurveFactory.CreateRectangleWithRoundedCorners(width, height, 0, 0, new Microsoft.Msagl.Core.Geometry.Point());
        }

        static System.Drawing.Drawing2D.GraphicsPath FillTheGraphicsPath(ICurve iCurve)
        {
            var curve = ((RoundedRect)iCurve).Curve;
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            foreach (ICurve seg in curve.Segments)
                AddSegmentToPath(seg, ref path);
            return path;
        }
        static internal PointF PointF(Microsoft.Msagl.Core.Geometry.Point p) { return new PointF((float)p.X, (float)p.Y); }
        private static void AddSegmentToPath(ICurve seg, ref System.Drawing.Drawing2D.GraphicsPath p)
        {
            const float radiansToDegrees = (float)(180.0 / Math.PI);
            LineSegment line = seg as LineSegment;
            if (line != null)
                p.AddLine( PointF(line.Start),  PointF(line.End));
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
        static bool DrawNode(Node node, object graphics)
        {
            Graphics g = (Graphics)graphics;
            AutomationElement element = (AutomationElement)node.UserData;
            
            //Image image = ImageOfNode(node);
            //flip the image around its center
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
                        (float)(node.GeometryNode.Center.Y - node.GeometryNode.Height / 2)+ TextRenderer.MeasureText(cls, font).Height));

                    g.Transform = saveM;
                    g.ResetClip();
                }
            }

            return true;//returning false would enable the default rendering
        }

        public static Node lastVisited;

        public static void ExpandNodeInformation(Node node, bool _expand ) 
        {
            if (_expand) 
            {
                AutomationElement element = (AutomationElement)node.UserData;
                //node.Attr.Shape = Shape.Diamond;

                node.Attr.Shape = Shape.DrawFromGeometry;
                node.DrawNodeDelegate = new DelegateToOverrideNodeRendering(DrawNode);
                node.NodeBoundaryDelegate = new DelegateToSetNodeBoundary(GetNodeBoundary);
            }
            else 
            {
                node.Attr.Shape = Shape.Box;
                node.DrawNodeDelegate = null;
                node.NodeBoundaryDelegate = null;
            }
            

            
        }
        private static void DumpingNodePosition(GViewer viewer) 
        {
            foreach (Node n in viewer.Graph.Nodes)
            {
                Microsoft.Msagl.Core.Geometry.Point center = n.GeometryNode.Center;
                Console.WriteLine(n.Id + ":"+center.X + " " + center.Y);
            }
        }
        private static Node GetNearestNode(Microsoft.Msagl.Core.Geometry.Point graphPos, GViewer viewer, float max = 50) 
        {
            double b= max;
            Node r = null;
            foreach ( Node n in viewer.Graph.Nodes) 
            {
                Microsoft.Msagl.Core.Geometry.Point center = n.GeometryNode.Center;
                double d = (Math.Abs(center.X - graphPos.X) + Math.Abs(center.Y - graphPos.Y) / 2);
                if (d < b) 
                {
                    b = d;
                    r = n;
                }
            }
            return r;
        }
        #endregion
        private static void Viewer_MouseMove(object sender, MouseEventArgs e)
        {
            return;
            GViewer viewer = sender as GViewer;
            Microsoft.Msagl.Core.Geometry.Point graphPos = viewer.ScreenToSource(new Microsoft.Msagl.Core.Geometry.Point(e.Location.X, e.Location.Y));
            
            Node node = GetNearestNode(graphPos, viewer);
            if (node != null)
            {
                if ( lastVisited != node)
                {
                    if (lastVisited != null)
                        ExpandNodeInformation(lastVisited, false);
                    ExpandNodeInformation( node, true);
                    
                    PlaneTransformation transform = viewer.Transform;
                    double zoomLevel = viewer.ZoomF;
                    viewer.Graph = viewer.Graph;
                    viewer.ZoomF = zoomLevel;
                    viewer.Transform = transform;
                }
                lastVisited = node;
            }
        }


        private static void Viewer_Click(object sender, EventArgs e)
        {
            GViewer viewer = sender as GViewer;
            if (viewer.SelectedObject is Node)
            {
                selNode = viewer.SelectedObject as Node;
                return;
                Node node = viewer.SelectedObject as Node;
                ExpandNodeInformation( node, true);
                viewer.Graph = viewer.Graph;
            }
        }

        public static void DeepSearch(AutomationElement root)
        {
            Console.WriteLine(root.Current.ClassName + " " + root.Current.Name);
            AutomationElementCollection child = root.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.IsEnabledProperty, true));
            foreach (AutomationElement children in child)
                DeepSearch(children);
        }
        public static void TestGraph() 
        {
            //create a form 
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            //create a viewer object 
            Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            //create a graph object 
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph");
            //create the graph content 

            //Create links:
            graph.AddEdge("A", "B"); // Link A to B
            graph.AddEdge("B", "C");
            graph.AddEdge("A", "C").Attr.Color = Microsoft.Msagl.Drawing.Color.Green; // Set the wire color to green...

            graph.FindNode("A").Attr.FillColor = Microsoft.Msagl.Drawing.Color.Magenta; // Fill the node to magenta
            graph.FindNode("B").Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;

            Microsoft.Msagl.Drawing.Node c = graph.FindNode("C");
            c.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PaleGreen;
            c.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Diamond;
            
            //bind the graph to the viewer 
            viewer.Graph = graph;
            //associate the viewer with the form 
            form.SuspendLayout();
            viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            form.Controls.Add(viewer);
            form.ResumeLayout();
            //show the form 
            form.ShowDialog();

        }
    }
}
