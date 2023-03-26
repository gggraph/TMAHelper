using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Automation;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Core.Geometry.Curves;
using System.Windows.Input;
using System.Windows;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection;
using System.Diagnostics;


namespace UITree_Watcher
{
   
    public class UiTreeWatcher
    {
        #region variables and constructor
        private class UiTree 
        {
            public Graph  graph;
            public IntPtr handle;
            public string windowName;
            public string processName;
            public int    nodectr;

            public UiTree(Graph graph, IntPtr handle, string win, string proc) 
            {
                this.graph = graph;
                this.handle = handle;
                windowName = win;
                processName = proc;
            }
        }

        private Control mContainer;

        private ViewStyle mStyle;

        private Node hlNode;

        private List<UiTree> trees = new List<UiTree>();

        private UiTree currentTree;

        private GViewer mViewer;

        private bool uiTracerEnabled = false;

        public UiTreeWatcher(Control ctrl, ViewStyle style) 
        {
            mContainer = ctrl;
            mStyle = style;
            InitViewer(new Microsoft.Msagl.GraphViewerGdi.GViewer());
            InitViewerToolBar();
            mContainer.Controls.Add(mViewer);
            InstallThreads();
        }

        #endregion

        #region Main functions
       

        public bool TryWatchNewWindow(IntPtr handle) 
        {
            foreach ( UiTree tree in trees) 
            {
                if (tree.handle == handle)
                {
                    mViewer.Graph = tree.graph;
                    return false;
                }
            }
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph_"+handle.ToString("X"));
            AutomationElement root = AutomationElement.FromHandle((IntPtr)handle);

            UiTree nUiTree = new UiTree(graph, handle, null, null);

            Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node("0");
            cNode.UserData = root;
            mStyle.ApplyNodeStyle(cNode);
            graph.AddNode(cNode);
            ExpandNodeFromRootNode(nUiTree, cNode);

            trees.Add(nUiTree);
            currentTree = nUiTree;

            mViewer.Graph = nUiTree.graph;
            mStyle.ApplyGraphStyle(graph);

            return true;
        }
        public void DummySetGraph(IntPtr handle) 
        {
            //create a graph object 
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("graph_test");
            AutomationElement root = AutomationElement.FromHandle((IntPtr)handle);

            UiTree nUiTree = new UiTree(graph, handle,null,null);

            Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node("0");
            cNode.UserData = root;
            graph.AddNode(cNode);
            ExpandNodeFromRootNode(nUiTree, cNode);

            trees.Add(nUiTree);
            currentTree = nUiTree;

            mViewer.Graph = nUiTree.graph;
            mStyle.ApplyGraphStyle(graph);
        }
        #endregion

        #region Graph

        // We add asynchronous task for smoother experience. 
        // Updating the viewer graph has a cost (of like near 100ms for complex graph),
        // then we could use RecomputeMSAGLViewAsync() and call InitViewer ( without changing the conctrol 
        // but by overriding it) ...

        // Refresh() add a specific worker that will recompute graph in other thread pool 

        private Task<Graph> worker;

        
        private void Refresh() 
        {
            if ( worker == null) 
            {
                worker = Task<Graph>.Run(() => { return RecomputeMSAGLGraphAsync(currentTree, mStyle); });
            }
            else if (worker.IsCompleted) 
            {
                // Destroy our current viewer from the container
                UpdateViewerGraph(worker.Result);
                // start recompute
                worker = Task<Graph>.Run(() => { return RecomputeMSAGLGraphAsync(currentTree, mStyle); });
            } 
        }
        private Graph RecomputeMSAGLGraphAsync(UiTree baseTree, ViewStyle style)
        {

            Graph graph = new Microsoft.Msagl.Drawing.Graph("graph_" + baseTree.handle.ToString("X"));
            UiTree tree = new UiTree(graph, baseTree.handle, baseTree.windowName, baseTree.processName);
            AutomationElement root = AutomationElement.FromHandle((IntPtr)tree.handle);
            Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node("0");
            cNode.UserData = root;
            style.ApplyNodeStyle(cNode);
            graph.AddNode(cNode);
            ExpandNodeFromRootNode(tree, cNode);
            style.ApplyGraphStyle(graph);
            return graph;
        }
        private GViewer RecomputeMSAGLViewAsync (UiTree baseTree, ViewStyle style) 
        {
           
            Graph graph = new Microsoft.Msagl.Drawing.Graph("graph_" + baseTree.handle.ToString("X"));
            UiTree tree = new UiTree(graph, baseTree.handle,baseTree.windowName, baseTree.processName);
            AutomationElement root = AutomationElement.FromHandle((IntPtr)tree.handle);
            Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node("0");
            cNode.UserData = root;
            style.ApplyNodeStyle(cNode);
            graph.AddNode(cNode);
            ExpandNodeFromRootNode(tree, cNode);
            style.ApplyGraphStyle(graph);
            Microsoft.Msagl.GraphViewerGdi.GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();
            viewer.Graph = graph;
            return viewer;
        }

        private void UpdateViewerGraph(Graph graph) 
        {
            double z = mViewer.ZoomF;
            PlaneTransformation transform = mViewer.Transform;
            mViewer.Graph = graph;
            mViewer.Transform = transform;
            mViewer.ZoomF = z;
        }
        private void ExpandNodeFromRootNode(UiTree tree, Node rootNode, int iterationLimit = 0)
        {

            Graph graph = tree.graph;
            // Adding a stack of AutomationElement
            List<AutomationElement> stack = new List<AutomationElement>() { (AutomationElement)rootNode.UserData };

            int sti = tree.nodectr;
            int n = int.Parse(rootNode.Id);
            int iter = 0;
            while (stack.Count > 0)
            {

                AutomationElementCollection child = stack[0].FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.IsEnabledProperty, true));
                foreach (AutomationElement children in child)
                {
                    // @ Add new node to the layer
                    Microsoft.Msagl.Drawing.Node cNode = new Microsoft.Msagl.Drawing.Node(tree.nodectr.ToString());
                    cNode.UserData = children;
                   
                    mStyle.ApplyNodeStyle(cNode);

                    graph.AddNode(cNode);

                    //@ Add children to the stack
                    stack.Add(children);

                    //@ Create connection
                    Edge e = graph.AddEdge(n.ToString(), cNode.Id);
                    
                    mStyle.ApplyEdgeStyle(e);

                    tree.nodectr++;
                }
                //Consume
                stack.RemoveAt(0);

                //Jump to initial top if iteration 0
                if (iter == 0)
                    n = sti;
                else
                    n++;

                iter++;
                if (iter == iterationLimit)
                    break;
            }

        }
        private Node FindFirstCadet(List<Node> branch)
        {
            foreach (Node n in branch)
            {

                if (n.OutEdges.Count() == 0)
                    return n;

                bool _isCadet = true;
                foreach (Edge e in n.OutEdges)
                {
                    if (branch.Contains(e.TargetNode))
                    {
                        _isCadet = false;
                        break;
                    }
                }
                if (_isCadet)
                    return n;
            }
            return null;
        }
        // Give better result than cadet
        private int MeasureBranchHeightFromNode(Node curr, List<Node> branch, int h) 
        {
            h++;
            if (curr.InEdges.Count() > 0)
                h = MeasureBranchHeightFromNode(
                    curr.InEdges.FirstOrDefault().SourceNode, branch, h
                    );

            return h;
        }
        private Node FindLowestNodeInBranch(List<Node> branch) 
        {
            List<Tuple<int, int>> r = new List<Tuple<int, int>>();

            List <Node> branchList = branch.ToList();

            for (int i = 0; i < branch.Count; i++) 
                r.Add(new Tuple<int,int>(i,MeasureBranchHeightFromNode(branch[i], branch, 0)));

            r.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            return branchList[r[0].Item1] ;
        }

        #endregion

        #region Threading

        private void InstallThreads() 
        {
            new Thread(ElementDetectionThread) { IsBackground = true }.Start(mViewer.FindForm());
            new Thread(GraphWorkerThread) { IsBackground = true }.Start(mViewer.FindForm());
        }
        private void GraphWorkerThread(object arg)
        {
            Form win = (Form)arg;
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(2000);
                win.Invoke(new MethodInvoker(delegate
                {
                    if (mViewer.Graph != null)
                    {
                        Refresh();
                    }

                }));
            }
        }
        private void ElementDetectionThread(object arg) 
        {
            Form win = (Form)arg;
            Thread.Sleep(1000);
            while (true) 
            {
                Thread.Sleep(200);
                win.Invoke(new MethodInvoker(delegate
                {
                    if (mViewer.Graph != null && uiTracerEnabled)
                    {
                        HighlightElementAboveMouse();
                    }

                }));
            }
        }

        #endregion

        #region Interactions
        private void HighLightNode(Node newNode) 
        {
            //Psudo select next..
            if (hlNode!=null)
                mStyle.HighlightNode(hlNode, false);
            mStyle.HighlightNode(newNode, true);
            mViewer.Refresh();
            hlNode = newNode;

            // Focus on the node
            FocusViewOnNode(mViewer, hlNode);

            AutomationElement element = (AutomationElement)hlNode.UserData;
            // Highlight the node automation element
            HighLightUIElement(element, mViewer);

            //Update Node Details ... if we wanna
            OutputAutomationElement(element);   
        }
        private void OutputAutomationElement(AutomationElement element) 
        {
           
            
            string info = "";
            
            info += "Name : "+ element.Current.Name + Environment.NewLine;
            info += "Help Text :" + element.Current.HelpText + Environment.NewLine;

            info += "ClassName : " + element.Current.ClassName + Environment.NewLine;
            info += "Control Type : " + element.Current.ControlType.ProgrammaticName + Environment.NewLine;
            info += "Localized Control Type : " + element.Current.LocalizedControlType + Environment.NewLine;
            info += "Type d'item : " + element.Current.ItemType + Environment.NewLine;
            info += "Statut de l'item : " + element.Current.ItemStatus + Environment.NewLine;


            info += "Automation ID : " + element.Current.AutomationId + Environment.NewLine;
            info += "Framework  ID : " + element.Current.FrameworkId + Environment.NewLine;
            info += "Raccourci Clavier : " + element.Current.AccessKey + Environment.NewLine;

            
            info += "Window Handle : " + element.Current.NativeWindowHandle.ToString("X") + Environment.NewLine;
            info += "Proc ID : " + element.Current.ProcessId + Environment.NewLine;
            Console.WriteLine(info);
        }

        private List<Node> GetAllUIElementsAboveScreenPosition(int x, int y) 
        {
            
            List<Node> r = new List<Node>();
            foreach ( Node n in mViewer.Graph.Nodes ) 
            {
                AutomationElement e = (AutomationElement)n.UserData;
               
                if (e.Current.BoundingRectangle.Contains(new System.Windows.Point(x, y)))
                {
                    r.Add(n);
                }
            }
            return r;
        }

     

        
        private void HighlightElementAboveMouse() 
        {
            try 
            {
                System.Drawing.Point p = System.Windows.Forms.Cursor.Position;
                if (mViewer.FindForm().Bounds.Contains(p))
                    return;
                List<Node> nodes = GetAllUIElementsAboveScreenPosition(p.X, p.Y);
                // find the children'est' node.
                Node cadet = FindLowestNodeInBranch(nodes);
                if (cadet == null)
                    return;

                HighLightNode(cadet);
            }
            catch (System.Exception e) 
            {
                // elementnotavailable exception.
                // This one will be a ass to avoid. this will be our next challenge.
            }
            
        }

        private void FocusViewOnNode(GViewer viewer, Node n) 
        {
            
            double z = viewer.ZoomF;
            viewer.ShowBBox(new Microsoft.Msagl.Core.Geometry.Rectangle
            {
                Center = n.Pos,
                Width =  viewer.Bounds.Width,
                Height = viewer.Bounds.Height,
            });
            viewer.ZoomF = z;
        }

        private void HighLightUIElement(AutomationElement element, GViewer viewer) 
        {
            WinAPI.RECT winRect;
            WinAPI.GetWindowRect((IntPtr)currentTree.handle, out winRect);
            WinAPI.MoveWindow((IntPtr)currentTree.handle, winRect.Left, winRect.Top);
            //;

            WinAPI.SetForegroundWindow((IntPtr)viewer.Handle);
            WinAPI.ScreenHL(
              new System.Drawing.Rectangle(
                  (int)element.Current.BoundingRectangle.X,
                  (int)element.Current.BoundingRectangle.Y,
                  (int)element.Current.BoundingRectangle.Width,
                  (int)element.Current.BoundingRectangle.Height));
        }

        #endregion

        #region Events
        private void Viewer_MouseWheel(object sender, MouseEventArgs e)
        {
            GViewer viewer = sender as GViewer;
            Console.WriteLine(viewer.ZoomF + "->" + viewer.Bounds.Width + " " + viewer.Bounds.Height);
        }
        private void Viewer_KeyDown(object sender, KeyEventArgs e)
        {
            GViewer viewer = sender as GViewer;
            if (hlNode == null)
                return;

            Node node = hlNode;
            Node next = null;

            if (e.KeyCode == Keys.Z)
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
                    if (edges[i].TargetNode == node && i > 0)
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
                    if (edges[i].TargetNode == node && i < edges.Count - 1)
                    {
                        next = edges[i + 1].TargetNode;
                        break;
                    }
                }
            }
            if (next == null)
                return;

            HighLightNode(next);

        }
        private void Viewer_MouseDown(object sender, MouseEventArgs e)
        {
            if (((MouseEventArgs)e).Button == System.Windows.Forms.MouseButtons.Right)
            {
                /*Here we could handle auto panning*/
            }

        }
        private  void Viewer_Click(object sender, EventArgs e)
        {
          

            GViewer viewer = sender as GViewer;
            if (viewer.SelectedObject is Node)
            {
              
                Node node = viewer.SelectedObject as Node;
                HighLightNode(node);

            }
        }
        private  void Viewer_MouseMove(object sender, MouseEventArgs e)
        {
            
        }
        #endregion

        #region Controls
        private void InitViewer(GViewer viewer)
        {
            Form mForm = mContainer.FindForm();
            mForm.SuspendLayout();
            mStyle.ApplyViewerStyle(viewer);

            mForm.ResumeLayout();

            // Setup handlers
            viewer.Click += Viewer_Click;
            viewer.KeyDown += Viewer_KeyDown;
            viewer.MouseMove += Viewer_MouseMove;
            viewer.MouseWheel += Viewer_MouseWheel;
            viewer.MouseDown += Viewer_MouseDown;

            viewer.PanButtonPressed = true;
            mViewer = viewer;
        }
        private ToolStrip GetViewerToolStrip()
        {
            foreach (Control c in mViewer.Controls)
            {
                if (c.GetType() == typeof(ToolStrip))
                    return (ToolStrip)c;
            }
            return null;
        }
        private void InitViewerToolBar()
        {
            // Remove undesired default tools...

            List<string> itemsToKeep = new List<string>()
            {
                "homeZoomButton",
                "zoomin",
                "zoomout",
                "undoButton",
                "redoButton",
                "layoutSettingsButton"
            };

            ToolStrip tools = GetViewerToolStrip();

            for (int i = tools.Items.Count - 1; i >= 0; i--)
            {
                if (!itemsToKeep.Contains(tools.Items[i].Name))
                {
                    tools.Items.RemoveAt(i);
                }
            }
            // Add some toolstrip button : 
            // Trace UI
            // Install Branch Diagnostic
            ToolStripButton reference = (ToolStripButton)tools.Items[0];

            ToolStripButton traceButton = new ToolStripButton()
            {
                Size = reference.Size,
                Image = Image.FromFile("trace_enab.png"),
                Name = "UITracer",
                ToolTipText = "Trace UI",
                

            };
            traceButton.Click += TraceButton_Click;
            
            ToolStripButton diagButton = new ToolStripButton()
            {
                Size = reference.Size,
                Image = Image.FromFile("diagnose.png"),
                Name = "watch",
                ToolTipText = "Diagnose"

            };
            diagButton.Click += DiagButton_Click;
            tools.Items.Add(traceButton);
            tools.Items.Add(diagButton);
        }

        private void DiagButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Diagnostic and watchers are not fully done... Please wait advanced release...");
        }

        private void TraceButton_Click(object sender, EventArgs e)
        {
            ToolStripButton btn = (ToolStripButton)sender;
            if (!uiTracerEnabled)
                btn.Image = Image.FromFile("trace_disab.png");
            else
                btn.Image = Image.FromFile("trace_enab.png");
            uiTracerEnabled = !uiTracerEnabled;

        }
        #endregion
    }

}
