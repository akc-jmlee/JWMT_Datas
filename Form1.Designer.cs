namespace JWMT_Datas
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private Panel topPanel;
        private Label lblFile;
        private TextBox txtFile;
        private Button btnBrowse;
        private Button btnLoad;
        private Button btnSave;
        private Label lblPanelSize;
        private TextBox txtPanelWidth;
        private Label lblX;
        private TextBox txtPanelHeight;
        private CheckBox chkAutoFit;
        private Button btnResetView;
        private TabControl tabs;
        private ContextMenuStrip tabMenu;
        private ToolStripMenuItem menuCloseTab;
        private ToolStripMenuItem menuCloseAll;
        private PictureBox picMap;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblCursor;
        private ToolStripProgressBar progress;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.topPanel = new Panel();
            this.lblFile = new Label();
            this.txtFile = new TextBox();
            this.btnBrowse = new Button();
            this.btnLoad = new Button();
            this.btnSave = new Button();
            this.lblPanelSize = new Label();
            this.txtPanelWidth = new TextBox();
            this.lblX = new Label();
            this.txtPanelHeight = new TextBox();
            this.chkAutoFit = new CheckBox();
            this.btnResetView = new Button();
            this.tabs = new TabControl();
            this.tabMenu = new ContextMenuStrip(this.components);
            this.menuCloseTab = new ToolStripMenuItem();
            this.menuCloseAll = new ToolStripMenuItem();
            this.picMap = new PictureBox();
            this.statusStrip = new StatusStrip();
            this.lblStatus = new ToolStripStatusLabel();
            this.lblCursor = new ToolStripStatusLabel();
            this.progress = new ToolStripProgressBar();
            this.topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMap)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // lblFile
            //
            this.lblFile.AutoSize = true;
            this.lblFile.Location = new Point(12, 15);
            this.lblFile.Name = "lblFile";
            this.lblFile.Size = new Size(62, 15);
            this.lblFile.TabIndex = 0;
            this.lblFile.Text = "리포트 파일:";
            //
            // txtFile
            //
            this.txtFile.Location = new Point(92, 12);
            this.txtFile.Name = "txtFile";
            this.txtFile.PlaceholderText = "<날짜>_JWMT_Datas_001.csv  (창에 끌어다 놓아도 됩니다)";
            this.txtFile.Size = new Size(440, 23);
            this.txtFile.TabIndex = 1;
            //
            // btnBrowse
            //
            this.btnBrowse.Location = new Point(538, 11);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new Size(70, 25);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "파일...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new EventHandler(this.btnBrowse_Click);
            //
            // btnLoad
            //
            this.btnLoad.Location = new Point(614, 11);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new Size(90, 25);
            this.btnLoad.TabIndex = 3;
            this.btnLoad.Text = "불러오기";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new EventHandler(this.btnLoad_Click);
            //
            // btnSave
            //
            this.btnSave.Enabled = false;
            this.btnSave.Location = new Point(710, 11);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new Size(90, 25);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "PNG 저장";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new EventHandler(this.btnSave_Click);
            //
            // lblPanelSize
            //
            this.lblPanelSize.AutoSize = true;
            this.lblPanelSize.Location = new Point(12, 47);
            this.lblPanelSize.Name = "lblPanelSize";
            this.lblPanelSize.Size = new Size(94, 15);
            this.lblPanelSize.TabIndex = 5;
            this.lblPanelSize.Text = "패널 크기 (um):";
            //
            // txtPanelWidth
            //
            this.txtPanelWidth.Location = new Point(112, 44);
            this.txtPanelWidth.Name = "txtPanelWidth";
            this.txtPanelWidth.Size = new Size(80, 23);
            this.txtPanelWidth.TabIndex = 6;
            this.txtPanelWidth.Text = "510000";
            this.txtPanelWidth.TextChanged += new EventHandler(this.Redraw_Changed);
            //
            // lblX
            //
            this.lblX.AutoSize = true;
            this.lblX.Location = new Point(198, 47);
            this.lblX.Name = "lblX";
            this.lblX.Size = new Size(14, 15);
            this.lblX.TabIndex = 7;
            this.lblX.Text = "x";
            //
            // txtPanelHeight
            //
            this.txtPanelHeight.Location = new Point(218, 44);
            this.txtPanelHeight.Name = "txtPanelHeight";
            this.txtPanelHeight.Size = new Size(80, 23);
            this.txtPanelHeight.TabIndex = 8;
            this.txtPanelHeight.Text = "515000";
            this.txtPanelHeight.TextChanged += new EventHandler(this.Redraw_Changed);
            //
            // chkAutoFit
            //
            this.chkAutoFit.AutoSize = true;
            this.chkAutoFit.Location = new Point(312, 46);
            this.chkAutoFit.Name = "chkAutoFit";
            this.chkAutoFit.Size = new Size(150, 19);
            this.chkAutoFit.TabIndex = 9;
            this.chkAutoFit.Text = "데이터 범위에 맞추기";
            this.chkAutoFit.UseVisualStyleBackColor = true;
            this.chkAutoFit.CheckedChanged += new EventHandler(this.Redraw_Changed);
            //
            // btnResetView
            //
            this.btnResetView.Enabled = false;
            this.btnResetView.Location = new Point(478, 43);
            this.btnResetView.Name = "btnResetView";
            this.btnResetView.Size = new Size(110, 25);
            this.btnResetView.TabIndex = 10;
            this.btnResetView.Text = "전체 보기";
            this.btnResetView.UseVisualStyleBackColor = true;
            this.btnResetView.Click += new EventHandler(this.btnResetView_Click);
            //
            // topPanel
            //
            this.topPanel.Controls.Add(this.lblFile);
            this.topPanel.Controls.Add(this.txtFile);
            this.topPanel.Controls.Add(this.btnBrowse);
            this.topPanel.Controls.Add(this.btnLoad);
            this.topPanel.Controls.Add(this.btnSave);
            this.topPanel.Controls.Add(this.lblPanelSize);
            this.topPanel.Controls.Add(this.txtPanelWidth);
            this.topPanel.Controls.Add(this.lblX);
            this.topPanel.Controls.Add(this.txtPanelHeight);
            this.topPanel.Controls.Add(this.chkAutoFit);
            this.topPanel.Controls.Add(this.btnResetView);
            this.topPanel.Dock = DockStyle.Top;
            this.topPanel.Location = new Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new Size(1000, 78);
            this.topPanel.TabIndex = 0;
            //
            // tabMenu
            //
            this.tabMenu.Items.AddRange(new ToolStripItem[] { this.menuCloseTab, this.menuCloseAll });
            this.tabMenu.Name = "tabMenu";
            //
            // menuCloseTab
            //
            this.menuCloseTab.Name = "menuCloseTab";
            this.menuCloseTab.Size = new Size(150, 22);
            this.menuCloseTab.Text = "닫기";
            this.menuCloseTab.Click += new EventHandler(this.menuCloseTab_Click);
            //
            // menuCloseAll
            //
            this.menuCloseAll.Name = "menuCloseAll";
            this.menuCloseAll.Size = new Size(150, 22);
            this.menuCloseAll.Text = "모두 닫기";
            this.menuCloseAll.Click += new EventHandler(this.menuCloseAll_Click);
            //
            // tabs
            //
            this.tabs.ContextMenuStrip = this.tabMenu;
            this.tabs.Dock = DockStyle.Fill;
            this.tabs.Location = new Point(0, 78);
            this.tabs.Name = "tabs";
            this.tabs.SelectedIndex = 0;
            this.tabs.Size = new Size(1000, 644);
            this.tabs.TabIndex = 1;
            //
            // picMap
            //
            this.picMap.BackColor = Color.White;
            this.picMap.Dock = DockStyle.Fill;
            this.picMap.Location = new Point(0, 0);
            this.picMap.Name = "picMap";
            this.picMap.Size = new Size(992, 616);
            this.picMap.SizeMode = PictureBoxSizeMode.Normal;
            this.picMap.TabIndex = 0;
            this.picMap.TabStop = false;
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new ToolStripItem[] {
                this.lblStatus, this.lblCursor, this.progress });
            this.statusStrip.Location = new Point(0, 722);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new Size(1000, 22);
            this.statusStrip.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(500, 17);
            this.lblStatus.Spring = true;
            this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.lblStatus.Text = "리포트 파일을 고르거나 창에 끌어다 놓으세요.  휠=확대/축소, 드래그=이동, 더블클릭=전체 보기";
            //
            // lblCursor
            //
            this.lblCursor.AutoSize = false;
            this.lblCursor.BorderSides = ToolStripStatusLabelBorderSides.Left;
            this.lblCursor.Name = "lblCursor";
            this.lblCursor.Size = new Size(260, 17);
            this.lblCursor.TextAlign = ContentAlignment.MiddleLeft;
            //
            // progress
            //
            this.progress.Name = "progress";
            this.progress.Size = new Size(160, 16);
            this.progress.Visible = false;
            //
            // Form1
            //
            this.AllowDrop = true;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1000, 744);
            // picMap 은 선택된 탭 안으로 옮겨 붙이므로 폼에 직접 추가하지 않는다.
            this.Controls.Add(this.tabs);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.topPanel);
            this.MinimumSize = new Size(760, 520);
            this.Name = "Form1";
            this.Text = "JWMT Unit Info Map";
            this.topPanel.ResumeLayout(false);
            this.topPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMap)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
