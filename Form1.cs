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
    public partial class Form1 : Form
    {
       
        public Form1()
        {
            InitializeComponent();
            ShortExample();
            bool r = NaiveMatches("something else", "something *es");
            MessageBox.Show(r.ToString());
        }

        public class Node
        {
            public System.DateTime Date;
            public string Message;
            public List<Node> Nodes;
            public Node(System.DateTime Date, string Message)
            {
                this.Date = Date;
                this.Message = Message;
                Nodes = new List<Node>();
            }
        }
        public bool NaiveMatches (string str, string pattern) 
        {
            bool skMode = false;
            char skChar = ' ';
            for (int i = 0; i < str.Length; i++) 
            {
                if (pattern.Length <= i)
                    break;
                if (pattern[i] == '*')
                {
                    // find next char in pattern which is not * or 
                    bool f = false;
                    for (int n = i+1; n < pattern.Length; n++) 
                    {
                        if ( pattern[n] != '*')
                        {
                            f = true;
                            skChar = pattern[n];
                            break;
                        }
                    }
                    if (!f)
                        break;
                    skMode = true;
                    continue;
                }
                if (skMode) 
                {
                    if (str[i] == skChar)
                        skMode = false;
                    continue;
                }
                if (str[i] != pattern[i])
                    return false;
            }
            return true;
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
        public void TestMatchString() 
        {
            // can be used like this *
            string text = "X is a string with ZY in the middle and at the end is P";
            bool isMatch = FileSystemName.MatchesSimpleExpression("X*ZY*P", text);
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


            // [3] Set up column aspect (text printed) 

            // this lets you handle the model object directly
            dateColumn.AspectGetter = delegate (object rowObject) {
                // check if that is the expected model type
                if (rowObject is Node)
                {
                    return ((Node)rowObject).Date;
                }
                else
                {
                    return "";
                }
            };
            msgColumn.AspectGetter = delegate (object rowObject) {
                // check if that is the expected model type
                if (rowObject is Node)
                {
                    return ((Node)rowObject).Message;
                }
                else
                {
                    return "";
                }
            };
            treeListView1.Columns.Add(dateColumn);
            treeListView1.Columns.Add(msgColumn);
            //BrightIdeasSoftware.OLVListSubItem
            List<Node> nodesList = new List<Node>();
            Node root = new Node(DateTime.Now, "somehting happened");
            root.Nodes.Add(new Node(DateTime.Now.AddDays(8), "somewhere"));
            nodesList.Add(root);
            // We also need to tell OLV what objects to display as root nodes

            treeListView1.SetObjects(nodesList);
        }

    }
}
