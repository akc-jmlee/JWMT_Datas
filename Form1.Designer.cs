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
        private TextBox txtFolder;
        private Button btnBrowse;
        private Button btnLoad;
        private Button btnSave;
        private Label lblPanelSize;
        private TextBox txtPanelWidth;
        private Label lblX;
        private TextBox txtPanelHeight;
        private CheckBox chkAutoFit;
        private PictureBox picMap;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progress;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.topPanel = new Panel();
            this.txtFolder = new TextBox();
            this.btnBrowse = new Button();
            this.btnLoad = new Button();
            this.btnSave = new Button();
            this.lblPanelSize = new Label();
            this.txtPanelWidth = new TextBox();
            this.lblX = new Label();
            this.txtPanelHeight = new TextBox();
            this.chkAutoFit = new CheckBox();
            this.picMap = new PictureBox();
            this.statusStrip = new StatusStrip();
            this.lblStatus = new ToolStripStatusLabel();
            this.progress = new ToolStripProgressBar();
            this.topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMap)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            //
            // txtFolder
            //
            this.txtFolder.Location = new Point(12, 12);
            this.txtFolder.Name = "txtFolder";
            this.txtFolder.Size = new Size(520, 23);
            this.txtFolder.TabIndex = 0;
            //
            // btnBrowse
            //
            this.btnBrowse.Location = new Point(538, 11);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new Size(70, 25);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "폴더...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new EventHandler(this.btnBrowse_Click);
            //
            // btnLoad
            //
            this.btnLoad.Location = new Point(614, 11);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new Size(90, 25);
            this.btnLoad.TabIndex = 2;
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
            this.btnSave.TabIndex = 3;
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
            this.lblPanelSize.TabIndex = 4;
            this.lblPanelSize.Text = "패널 크기 (um):";
            //
            // txtPanelWidth
            //
            this.txtPanelWidth.Location = new Point(112, 44);
            this.txtPanelWidth.Name = "txtPanelWidth";
            this.txtPanelWidth.Size = new Size(80, 23);
            this.txtPanelWidth.TabIndex = 5;
            this.txtPanelWidth.Text = "510000";
            //
            // lblX
            //
            this.lblX.AutoSize = true;
            this.lblX.Location = new Point(198, 47);
            this.lblX.Name = "lblX";
            this.lblX.Size = new Size(14, 15);
            this.lblX.TabIndex = 6;
            this.lblX.Text = "x";
            //
            // txtPanelHeight
            //
            this.txtPanelHeight.Location = new Point(218, 44);
            this.txtPanelHeight.Name = "txtPanelHeight";
            this.txtPanelHeight.Size = new Size(80, 23);
            this.txtPanelHeight.TabIndex = 7;
            this.txtPanelHeight.Text = "515000";
            //
            // chkAutoFit
            //
            this.chkAutoFit.AutoSize = true;
            this.chkAutoFit.Location = new Point(312, 46);
            this.chkAutoFit.Name = "chkAutoFit";
            this.chkAutoFit.Size = new Size(150, 19);
            this.chkAutoFit.TabIndex = 8;
            this.chkAutoFit.Text = "데이터 범위에 맞추기";
            this.chkAutoFit.UseVisualStyleBackColor = true;
            this.chkAutoFit.CheckedChanged += new EventHandler(this.Redraw_Changed);
            //
            // topPanel
            //
            this.topPanel.Controls.Add(this.txtFolder);
            this.topPanel.Controls.Add(this.btnBrowse);
            this.topPanel.Controls.Add(this.btnLoad);
            this.topPanel.Controls.Add(this.btnSave);
            this.topPanel.Controls.Add(this.lblPanelSize);
            this.topPanel.Controls.Add(this.txtPanelWidth);
            this.topPanel.Controls.Add(this.lblX);
            this.topPanel.Controls.Add(this.txtPanelHeight);
            this.topPanel.Controls.Add(this.chkAutoFit);
            this.topPanel.Dock = DockStyle.Top;
            this.topPanel.Location = new Point(0, 0);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new Size(1000, 78);
            this.topPanel.TabIndex = 0;
            //
            // picMap
            //
            this.picMap.BackColor = Color.White;
            this.picMap.Dock = DockStyle.Fill;
            this.picMap.Location = new Point(0, 78);
            this.picMap.Name = "picMap";
            this.picMap.Size = new Size(1000, 644);
            this.picMap.SizeMode = PictureBoxSizeMode.Normal;
            this.picMap.TabIndex = 1;
            this.picMap.TabStop = false;
            //
            // statusStrip
            //
            this.statusStrip.Items.AddRange(new ToolStripItem[] { this.lblStatus, this.progress });
            this.statusStrip.Location = new Point(0, 722);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new Size(1000, 22);
            this.statusStrip.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(700, 17);
            this.lblStatus.Spring = true;
            this.lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.lblStatus.Text = "리포트 CSV 가 있는 폴더를 고르고 [불러오기] 를 누르세요.";
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
            this.Controls.Add(this.picMap);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.topPanel);
            this.MinimumSize = new Size(700, 500);
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
