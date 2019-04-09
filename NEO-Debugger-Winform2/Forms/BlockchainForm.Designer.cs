namespace Neo.Debugger.Forms
{
    partial class BlockchainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.treeView2 = new System.Windows.Forms.TreeView();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newAddressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.markAsFromToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendAssetsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nEOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gASToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(946, 422);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.treeView2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(938, 396);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Transactions";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // treeView2
            // 
            this.treeView2.Location = new System.Drawing.Point(6, 6);
            this.treeView2.Name = "treeView2";
            this.treeView2.Size = new System.Drawing.Size(926, 384);
            this.treeView2.TabIndex = 0;
            this.treeView2.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView2_NodeMouseClick);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.treeView1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(938, 396);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Addresses";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // treeView1
            // 
            this.treeView1.Location = new System.Drawing.Point(6, 6);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(926, 384);
            this.treeView1.TabIndex = 0;
            this.treeView1.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newAddressToolStripMenuItem,
            this.markAsFromToolStripMenuItem,
            this.sendAssetsToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(157, 92);
            // 
            // newAddressToolStripMenuItem
            // 
            this.newAddressToolStripMenuItem.Name = "newAddressToolStripMenuItem";
            this.newAddressToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.newAddressToolStripMenuItem.Text = "New Address";
            this.newAddressToolStripMenuItem.Click += new System.EventHandler(this.newAddressToolStripMenuItem_Click);
            // 
            // markAsFromToolStripMenuItem
            // 
            this.markAsFromToolStripMenuItem.Name = "markAsFromToolStripMenuItem";
            this.markAsFromToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.markAsFromToolStripMenuItem.Text = "Mark as \"From\"";
            this.markAsFromToolStripMenuItem.Click += new System.EventHandler(this.markAsFromToolStripMenuItem_Click);
            // 
            // sendAssetsToolStripMenuItem
            // 
            this.sendAssetsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nEOToolStripMenuItem,
            this.gASToolStripMenuItem});
            this.sendAssetsToolStripMenuItem.Name = "sendAssetsToolStripMenuItem";
            this.sendAssetsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.sendAssetsToolStripMenuItem.Text = "Send Assets";
            // 
            // nEOToolStripMenuItem
            // 
            this.nEOToolStripMenuItem.Name = "nEOToolStripMenuItem";
            this.nEOToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.nEOToolStripMenuItem.Text = "NEO";
            this.nEOToolStripMenuItem.Click += new System.EventHandler(this.nEOToolStripMenuItem_Click);
            // 
            // gASToolStripMenuItem
            // 
            this.gASToolStripMenuItem.Name = "gASToolStripMenuItem";
            this.gASToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.gASToolStripMenuItem.Text = "GAS";
            this.gASToolStripMenuItem.Click += new System.EventHandler(this.gASToolStripMenuItem_Click);
            // 
            // BlockchainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(970, 440);
            this.Controls.Add(this.tabControl1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BlockchainForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Virtual Blockchain";
            this.Shown += new System.EventHandler(this.BlockchainForm_Shown);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.TreeView treeView2;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem newAddressToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem markAsFromToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sendAssetsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nEOToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gASToolStripMenuItem;
    }
}