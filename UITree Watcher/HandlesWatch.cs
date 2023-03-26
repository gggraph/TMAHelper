using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace UITree_Watcher
{
    public class HandlesWatcher
    {
        private TreeView mTreeView;
        private UiTreeWatcher mUIWatcher;
        public HandlesWatcher(UiTreeWatcher uiWatcher, TreeView treeView) 
        {
            mTreeView = treeView;
            mUIWatcher = uiWatcher;
            Refresh();
            mTreeView.AfterSelect += MTreeView_AfterSelect;
            //InstallThreads();
        }

        private void MTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // @ Dummy way to return if node not a subitem
            if (mTreeView.SelectedNode.Parent == null)
                return;

            IntPtr handle = (IntPtr)int.Parse(mTreeView.SelectedNode.Text.Split(':')[0], System.Globalization.NumberStyles.HexNumber);
            mUIWatcher.TryWatchNewWindow(handle);
        }

        private void Refresh() 
        {
            mTreeView.Nodes.Clear();

            TreeNode activeNode = new TreeNode("Active Processes");
            
            List<IntPtr> activeWindows = WinAPI.GetAllActiveWindows();
            foreach ( IntPtr handle in activeWindows) 
            {
                string wTitle = WinAPI.GetWindowText(handle);
                if ( wTitle.Length>0)
                    activeNode.Nodes.Add(handle.ToString("X") + ":" + wTitle);
            }
            TreeNode mainNode = new TreeNode("All Processes main window");
            foreach ( Process p in Process.GetProcesses()) 
            {
                if ( p.MainWindowHandle.ToInt64() != 0 )
                     mainNode.Nodes.Add(p.MainWindowHandle.ToString("X") + ":" + p.ProcessName);
            }
            mTreeView.Nodes.Add(activeNode);
            mTreeView.Nodes.Add(mainNode);
            
        }

        #region Threading
        private void InstallThreads()
        {
            new Thread(ProcWatcherThread) { IsBackground = true }.Start(mTreeView.FindForm());
        }
        private void ProcWatcherThread(object arg)
        {
            Form win = (Form)arg;
            Thread.Sleep(1000);
            while (true)
            {
                Thread.Sleep(2000);
                win.Invoke(new MethodInvoker(delegate
                {
                    Refresh();
                }));
            }
        }
        #endregion
    }
}
