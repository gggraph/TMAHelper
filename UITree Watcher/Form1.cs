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

        - Graph.cs
            UI Tree implementation with MSAGL
        - Diagnostics.cs
            Tree branch diagnostics from attempting to get to path
        - NodeStyles.cs
            Different dummies to render tree nodes.

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

        public static UiTreeWatcher  uiWatcher;
        public static HandlesWatcher procWatcher;
        public Form1()
        {
            InitializeComponent();
            this.HandleCreated += Form1_HandleCreated;
            
        }

        private void Form1_HandleCreated(object sender, EventArgs e)
        {
            //Set up the layout
            tabPage1.Controls.Add(treeView1);
            treeView1.Location = new System.Drawing.Point(0, 0);
            tabPage1.Text = "Handles";
            tabPage2.Text = "Node Details";
            uiWatcher = new UiTreeWatcher(layoutPanel, new SimpleLightStyle());
            procWatcher = new HandlesWatcher(uiWatcher, treeView1);
            this.SizeChanged += Form1_SizeChanged;
            Form1_SizeChanged(null, new EventArgs());
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            // Calculate the new column and row sizes based on the new form size

            layoutPanel.Size = this.ClientSize;
            // Update the column and row styles to scale the cells
            layoutPanel.ColumnStyles[0].SizeType = SizeType.Absolute;
            layoutPanel.ColumnStyles[0].Width = this.ClientSize.Width*0.75f;
            layoutPanel.ColumnStyles[1].SizeType = SizeType.Absolute;
            layoutPanel.ColumnStyles[1].Width = this.ClientSize.Width * 0.25f;

            layoutPanel.RowStyles[0].SizeType = SizeType.Absolute;
            layoutPanel.RowStyles[0].Height = this.ClientSize.Height;

            tabControl1.Size = new System.Drawing.Size((int)(this.ClientSize.Width * 0.25f), (int)this.ClientSize.Height);
            tabPage1.Width = (int) (this.ClientSize.Width * 0.25f);
            tabPage1.Height = (int)this.ClientSize.Height;
            treeView1.Size = tabPage1.Size;
        }
    }
}
