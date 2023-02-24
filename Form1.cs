using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Enumeration;

namespace ListViewTest
{
    /*
     un scheduler de job.
    une liste d'item en fonction du job. 
    une liste de logs en fonction du job.


    En cliquant sur un job:
	    J'update les queueitems en fonction de l'heure de départ/l'heure de fin &ou l'heure de création de l'item dans la range de l'heure de départ/l'heure de fin du job.
	    J'update les logs en fonction du job-key du job.

    En cliquant sur un queueitem, je navigue jusqu'au log de traitement du queueitem/ou bien vers des logs correspondant au plus proche à l'heure de fin du queueitem.
    En cliquant sur un log, je navigue jusqu'au queueitem dont l'heure d'ouverture/l'heure du traitement se situe entre les timestamps de ce log.

    En cliquant sur la bulle détails sur les logs, j'ouvre les informations du log.
    En cliquant sur la bulle détails du queueitem, j'ouvre les détails du queueitem.

    En cliquant droit sur un message de log qui n'est pas labelisé 
     
     */
    public partial class Form1 : Form
    {
       
        public Form1()
        {
            InitializeComponent();
            ExampleTreeView();
            //MessageBox.Show(r.ToString());
        }

        public class Node
        {
            public System.DateTime Date;
            public string Message;
            public List<Node> Nodes;
            public int type = 1;
            public Node(System.DateTime Date, string Message)
            {
                this.Date = Date;
                this.Message = Message;
                Nodes = new List<Node>();
            }
        }

        // supporting * and ? ...
        public bool FindMatches(string str, string pattern) 
        {
            bool[] prev = new bool[str.Length + 1];
            bool[] curr = new bool[str.Length + 1];

            prev[0] = true;

            for (int i = 1; i <= pattern.Length; i++)
            {
                bool flag = true;
                for (int ii = 1; ii < i; ii++)
                {
                    if (pattern[ii - 1] != '*')
                    {
                        flag = false;
                        break;
                    }
                }
                curr[0] = flag;
                for (int j = 1; j <= str.Length; j++)
                {
                    if (pattern[i - 1] == '*')
                        curr[j] = curr[j - 1] || prev[j];

                    else if (pattern[i - 1] == '?'
                        || str[j - 1] == pattern[i - 1])
                        curr[j] = prev[j - 1];

                    else
                        curr[j] = false;
                }
                prev = (bool[])curr.Clone();
            }
            return prev[str.Length];
        }
     
        public void ShortExample() 
        {
            // [1] Crafting the tree models...
            treeListView1.CanExpandGetter = model => ((Node)model).Nodes.Count > 0;
            // Override children getting methods, telling we return Nodes of Node.
            treeListView1.ChildrenGetter = delegate (object model){return ((Node)model).Nodes;};

            // [2] Create columns
            BrightIdeasSoftware.OLVColumn dateColumn = new BrightIdeasSoftware.OLVColumn();
            dateColumn.Text = "Date";
            BrightIdeasSoftware.OLVColumn msgColumn = new BrightIdeasSoftware.OLVColumn();
            msgColumn.Text = "Message";

            // [3] Specific what we will show foreach column
            
            dateColumn.AspectGetter = delegate (object rowObject) { return ((Node)rowObject).Date.ToLongTimeString();};
            msgColumn.AspectGetter = delegate (object rowObject) { return ((Node)rowObject).Message;};

            // [4] Add the columns
            treeListView1.Columns.Add(dateColumn);
            treeListView1.Columns.Add(msgColumn);

            // [5] Set up our nodes
            List<Node> nodesList = new List<Node>();
            Node root = new Node(DateTime.Now, "somehting happened");
            root.Nodes.Add(new Node(DateTime.Now.AddDays(8), "somewhere"));
            nodesList.Add(root);
            
            // [6] Add our nodes to trreelistview object
            treeListView1.SetObjects(nodesList);
            treeListView1.GetItem(0).BackColor = Color.Blue;
            treeListView1.Refresh();
        }
        public void ExampleTreeView() 
        {
            // [1] Crafting the tree models...

            // model is the currently queried object, we return true or false according to the amount of children we have in our MyClasses List
            treeListView1.CanExpandGetter = model => ((Node)model).
                                                          Nodes.Count > 0;
            // We return the list of MyClasses that shall be considered Children.
            treeListView1.ChildrenGetter = delegate (object model)
            {
                return ((Node)model).
                        Nodes;
            };

            // [2] Create columns
            BrightIdeasSoftware.OLVColumn dateColumn = new BrightIdeasSoftware.OLVColumn();
            dateColumn.Text = "Date";
            BrightIdeasSoftware.OLVColumn msgColumn = new BrightIdeasSoftware.OLVColumn();
            msgColumn.Text = "Message";
            BrightIdeasSoftware.OLVColumn detailColumn = new BrightIdeasSoftware.OLVColumn();
            detailColumn.Text = "Details";
            BrightIdeasSoftware.OLVColumn typeColumn = new BrightIdeasSoftware.OLVColumn();
            typeColumn.Text = "Type";


            // [3] Set up column aspect (text printed) 

            // this lets you handle the model object directly
            dateColumn.AspectGetter = delegate (object rowObject) {
                return ((Node)rowObject).Date;
            };
            msgColumn.AspectGetter = delegate (object rowObject) {
                return ((Node)rowObject).Message;
            };

            // Set up icon stuff...
            // Construct the ImageList.
            ImageList icons = new ImageList();
            // Set the ImageSize property to a larger size 
            // (the default is 16 x 16).
            icons.ImageSize = new Size(32, 32);
            // Add two images to the list.
            icons.Images.Add( Image.FromFile("eyeicon.png"));
            icons.Images.Add(Image.FromFile("logicon.png"));
            icons.Images.Add(Image.FromFile("warningicon.png"));
            icons.Images.Add(Image.FromFile("erroricon.png"));
            treeListView1.SmallImageList = icons;

            detailColumn.ImageGetter = delegate (object rowObject) { return 0; }; // is index of imagelist...
            detailColumn.ImageAspectName = "Image";
            typeColumn.ImageGetter = delegate (object rowObject) {
                return ((Node)rowObject).type;
            }; // is index of imagelist...
            typeColumn.ImageAspectName = "Image";

            treeListView1.Columns.Add(dateColumn);
            treeListView1.Columns.Add(typeColumn);
            treeListView1.Columns.Add(msgColumn);
            treeListView1.Columns.Add(detailColumn);
            treeListView1.CellClick += new EventHandler<BrightIdeasSoftware.CellClickEventArgs>(OnCellTreeClick);
            //BrightIdeasSoftware.OLVListSubItem
            List<Node> nodesList = new List<Node>();
            Node root = new Node(DateTime.Now, "somehting happened");
            root.type = 2;
            root.Nodes.Add(new Node(DateTime.Now.AddDays(8), "somewhere"));
            nodesList.Add(root);
            // We also need to tell OLV what objects to display as root nodes

            treeListView1.SetObjects(nodesList);
        }

        private void OnCellTreeClick(object sender, BrightIdeasSoftware.CellClickEventArgs e ) 
        {
            string cIndex = e.ColumnIndex.ToString();
            string rIndex = e.RowIndex.ToString();
            if ( e.ColumnIndex == 3) 
            {
                string fpath = @"C:\Users\gaelg\source\repos\ListViewTest\ListViewTest\bin\Debug\net5.0-windows\eyeicon.png";
                string t = "whistle index" + Environment.NewLine + " something else";
                int[] result = DetailDialog.ShowDialog(t, "804d5d55d", fpath);
            }
          
        }

    }

    public static class DetailDialog
    {
        public static int[] ShowDialog(string text, string ID, string imagePath = null)
        {
            Form prompt = new Form();
            prompt.Width = 180;
            prompt.Height = 180;
            prompt.Text = "Détails of transaction " + ID;

            FlowLayoutPanel panel = new FlowLayoutPanel();
            Label label = new Label();
            label.Text = text;
            //label.Size = new Size(180, 100);

            Button ok = new Button() { Text = "OK" };
            ok.Click += (sender, e) => { prompt.Close(); };
            //ok.Location = new Point(0, 100);
            panel.Controls.Add(label);
            panel.Controls.Add(ok);

            if ( imagePath != null) 
            {
                Button show = new Button() { Text = "Screenshot" };
                show.Click += (sender, e) => {

                    // Open the file...
                    new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo(imagePath)
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                };
                panel.Controls.Add(show);
            }

            prompt.Controls.Add(panel);
            prompt.ShowDialog();

            return new int[2] { 0,0 };
        }
    }
}
