namespace ReactiveTracker
{
    partial class ctrlPlayer
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayout = new DraggableTableLayoutPanel();
            //this.lblSingleCountCaption = new System.Windows.Forms.Label();
            this.lblSingleCount = new System.Windows.Forms.Label();
            this.lblSingleTimer = new System.Windows.Forms.Label();
            //this.lblGroupCountCaption = new System.Windows.Forms.Label();
            this.lblGroupCount = new System.Windows.Forms.Label();
            this.lblGroupTimer = new System.Windows.Forms.Label();
            this.tableLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayout
            // 
            this.tableLayout.ColumnCount = 4;
            this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            //this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            //this.tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayout.RowCount = 1;
            this.tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //this.tableLayout.Controls.Add(this.lblSingleCountCaption, 0, 0);
            this.tableLayout.Controls.Add(this.lblSingleCount,        0, 0);
            this.tableLayout.Controls.Add(this.lblSingleTimer,        1, 0);
            //this.tableLayout.Controls.Add(this.lblGroupCountCaption,  3, 0);
            this.tableLayout.Controls.Add(this.lblGroupCount,         2, 0);
            this.tableLayout.Controls.Add(this.lblGroupTimer,         3, 0);
            this.tableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayout.Name = "tableLayout";
            this.tableLayout.TabIndex = 0;
            // 
            // lblSingleCountCaption
            // 
            //this.lblSingleCountCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.lblSingleCountCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //this.lblSingleCountCaption.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            //this.lblSingleCountCaption.Text = "S";
            //this.lblSingleCountCaption.Name = "lblSingleCountCaption";
            //this.lblSingleCountCaption.TabIndex = 0;
            // 
            // lblSingleCount
            // 
            this.lblSingleCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSingleCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSingleCount.Text = "0";
            this.lblSingleCount.Name = "lblSingleCount";
            this.lblSingleCount.TabIndex = 1;
            // 
            // lblSingleTimer
            // 
            this.lblSingleTimer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSingleTimer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblSingleTimer.Text = "";
            this.lblSingleTimer.Name = "lblSingleTimer";
            this.lblSingleTimer.TabIndex = 2;
            // 
            // lblGroupCountCaption
            // 
            //this.lblGroupCountCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.lblGroupCountCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //this.lblGroupCountCaption.Text = "G";
            //this.lblGroupCountCaption.Name = "lblGroupCountCaption";
            //this.lblGroupCountCaption.TabIndex = 3;
            // 
            // lblGroupCount
            // 
            this.lblGroupCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGroupCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblGroupCount.Text = "0";
            this.lblGroupCount.Name = "lblGroupCount";
            this.lblGroupCount.TabIndex = 4;
            // 
            // lblGroupTimer
            // 
            this.lblGroupTimer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGroupTimer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblGroupTimer.Text = "";
            this.lblGroupTimer.Name = "lblGroupTimer";
            this.lblGroupTimer.TabIndex = 5;
            // 
            // ctrlReactive
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.tableLayout);
            this.Name = "ctrlPlayer";
            this.Size = new System.Drawing.Size(125, 68);
            this.tableLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private DraggableTableLayoutPanel tableLayout;
        //private System.Windows.Forms.Label lblSingleCountCaption;
        private System.Windows.Forms.Label lblSingleCount;
        private System.Windows.Forms.Label lblSingleTimer;
        //private System.Windows.Forms.Label lblGroupCountCaption;
        private System.Windows.Forms.Label lblGroupCount;
        private System.Windows.Forms.Label lblGroupTimer;

        #endregion
    }
}
