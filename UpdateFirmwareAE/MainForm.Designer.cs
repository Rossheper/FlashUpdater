﻿namespace UpdateFirmwareAE
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.textBox_nameFRMfile = new System.Windows.Forms.TextBox();
            this.button_openFRMfile = new System.Windows.Forms.Button();
            this.statusStripMenu = new System.Windows.Forms.StatusStrip();
            this.toolStripProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.openFileDialogFRM = new System.Windows.Forms.OpenFileDialog();
            this.toolTip_FRM = new System.Windows.Forms.ToolTip(this.components);
            this.button_progFRMfile = new System.Windows.Forms.CheckBox();
            this.label_DSP = new System.Windows.Forms.Label();
            this.label_PLIS = new System.Windows.Forms.Label();
            this.button_openPLISfile = new System.Windows.Forms.Button();
            this.checkBox_version = new System.Windows.Forms.CheckBox();
            this.textBox_namePLISfile = new System.Windows.Forms.TextBox();
            this.openFileDialog_PLIS = new System.Windows.Forms.OpenFileDialog();
            this.button_progPLISfile = new System.Windows.Forms.CheckBox();
            this.toolTip_PLIS = new System.Windows.Forms.ToolTip(this.components);
            this.toolStripStatusState = new System.Windows.Forms.Label();
            this.statusStripMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_nameFRMfile
            // 
            this.textBox_nameFRMfile.CausesValidation = false;
            this.textBox_nameFRMfile.Location = new System.Drawing.Point(16, 27);
            this.textBox_nameFRMfile.Name = "textBox_nameFRMfile";
            this.textBox_nameFRMfile.ReadOnly = true;
            this.textBox_nameFRMfile.Size = new System.Drawing.Size(169, 20);
            this.textBox_nameFRMfile.TabIndex = 1;
            // 
            // button_openFRMfile
            // 
            this.button_openFRMfile.Location = new System.Drawing.Point(207, 24);
            this.button_openFRMfile.Name = "button_openFRMfile";
            this.button_openFRMfile.Size = new System.Drawing.Size(56, 23);
            this.button_openFRMfile.TabIndex = 2;
            this.button_openFRMfile.Text = "Обзор";
            this.button_openFRMfile.UseVisualStyleBackColor = true;
            this.button_openFRMfile.Click += new System.EventHandler(this.button_openFRMfile_Click);
            // 
            // statusStripMenu
            // 
            this.statusStripMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBar,
            this.toolStripStatus});
            this.statusStripMenu.Location = new System.Drawing.Point(0, 232);
            this.statusStripMenu.Name = "statusStripMenu";
            this.statusStripMenu.Size = new System.Drawing.Size(384, 22);
            this.statusStripMenu.TabIndex = 3;
            this.statusStripMenu.Text = "statusStrip1";
            // 
            // toolStripProgressBar
            // 
            this.toolStripProgressBar.Name = "toolStripProgressBar";
            this.toolStripProgressBar.Size = new System.Drawing.Size(100, 16);
            // 
            // toolStripStatus
            // 
            this.toolStripStatus.Name = "toolStripStatus";
            this.toolStripStatus.Size = new System.Drawing.Size(46, 17);
            this.toolStripStatus.Text = "Статус:";
            // 
            // richTextBoxLog
            // 
            this.richTextBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxLog.Location = new System.Drawing.Point(12, 107);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.Size = new System.Drawing.Size(360, 122);
            this.richTextBoxLog.TabIndex = 4;
            this.richTextBoxLog.Text = "";
            // 
            // openFileDialogFRM
            // 
            this.openFileDialogFRM.Filter = "FRM| *.mcs";
            // 
            // toolTip_FRM
            // 
            this.toolTip_FRM.AutoPopDelay = 5000;
            this.toolTip_FRM.InitialDelay = 100;
            this.toolTip_FRM.ReshowDelay = 100;
            // 
            // button_progFRMfile
            // 
            this.button_progFRMfile.Appearance = System.Windows.Forms.Appearance.Button;
            this.button_progFRMfile.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.button_progFRMfile.Enabled = false;
            this.button_progFRMfile.Location = new System.Drawing.Point(280, 24);
            this.button_progFRMfile.Name = "button_progFRMfile";
            this.button_progFRMfile.Size = new System.Drawing.Size(92, 23);
            this.button_progFRMfile.TabIndex = 7;
            this.button_progFRMfile.Text = "Прошить DSP";
            this.button_progFRMfile.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.button_progFRMfile.UseVisualStyleBackColor = true;
            this.button_progFRMfile.CheckedChanged += new System.EventHandler(this.button_progFRMfile_click);
            // 
            // label_DSP
            // 
            this.label_DSP.AutoSize = true;
            this.label_DSP.Location = new System.Drawing.Point(13, 9);
            this.label_DSP.Name = "label_DSP";
            this.label_DSP.Size = new System.Drawing.Size(87, 13);
            this.label_DSP.TabIndex = 8;
            this.label_DSP.Text = "Прошивка DSP:";
            // 
            // label_PLIS
            // 
            this.label_PLIS.AutoSize = true;
            this.label_PLIS.Location = new System.Drawing.Point(13, 59);
            this.label_PLIS.Name = "label_PLIS";
            this.label_PLIS.Size = new System.Drawing.Size(88, 13);
            this.label_PLIS.TabIndex = 9;
            this.label_PLIS.Text = "Прошивка PLIS:";
            // 
            // button_openPLISfile
            // 
            this.button_openPLISfile.Location = new System.Drawing.Point(207, 70);
            this.button_openPLISfile.Name = "button_openPLISfile";
            this.button_openPLISfile.Size = new System.Drawing.Size(56, 23);
            this.button_openPLISfile.TabIndex = 10;
            this.button_openPLISfile.Text = "Обзор";
            this.button_openPLISfile.UseVisualStyleBackColor = true;
            this.button_openPLISfile.Click += new System.EventHandler(this.button_openPLISfile_Click);
            // 
            // checkBox_version
            // 
            this.checkBox_version.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBox_version.AutoSize = true;
            this.checkBox_version.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.checkBox_version.Location = new System.Drawing.Point(291, 236);
            this.checkBox_version.Name = "checkBox_version";
            this.checkBox_version.Size = new System.Drawing.Size(71, 17);
            this.checkBox_version.TabIndex = 11;
            this.checkBox_version.Text = "ТКБ995";
            this.checkBox_version.UseVisualStyleBackColor = true;
            this.checkBox_version.CheckedChanged += new System.EventHandler(this.checkBox_version_CheckedChanged);
            // 
            // textBox_namePLISfile
            // 
            this.textBox_namePLISfile.CausesValidation = false;
            this.textBox_namePLISfile.Location = new System.Drawing.Point(16, 73);
            this.textBox_namePLISfile.Name = "textBox_namePLISfile";
            this.textBox_namePLISfile.ReadOnly = true;
            this.textBox_namePLISfile.Size = new System.Drawing.Size(169, 20);
            this.textBox_namePLISfile.TabIndex = 12;
            this.textBox_namePLISfile.CausesValidationChanged += new System.EventHandler(this.FRM_CausesValidationChanged);
            // 
            // openFileDialog_PLIS
            // 
            this.openFileDialog_PLIS.Filter = "\"PLIS| *.mcs";
            // 
            // button_progPLISfile
            // 
            this.button_progPLISfile.Appearance = System.Windows.Forms.Appearance.Button;
            this.button_progPLISfile.Enabled = false;
            this.button_progPLISfile.Location = new System.Drawing.Point(280, 70);
            this.button_progPLISfile.Name = "button_progPLISfile";
            this.button_progPLISfile.Size = new System.Drawing.Size(92, 23);
            this.button_progPLISfile.TabIndex = 13;
            this.button_progPLISfile.Text = "Прошить PLIS";
            this.button_progPLISfile.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.button_progPLISfile.UseVisualStyleBackColor = true;
            this.button_progPLISfile.CheckedChanged += new System.EventHandler(this.button_progPLISfile_CheckedChanged);
            // 
            // toolTip_PLIS
            // 
            this.toolTip_PLIS.AutoPopDelay = 5000;
            this.toolTip_PLIS.InitialDelay = 100;
            this.toolTip_PLIS.ReshowDelay = 100;
            // 
            // toolStripStatusState
            // 
            this.toolStripStatusState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.toolStripStatusState.AutoEllipsis = true;
            this.toolStripStatusState.Location = new System.Drawing.Point(150, 237);
            this.toolStripStatusState.Name = "toolStripStatusState";
            this.toolStripStatusState.Size = new System.Drawing.Size(141, 15);
            this.toolStripStatusState.TabIndex = 14;
            this.toolStripStatusState.Text = "Ожидание...";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 254);
            this.Controls.Add(this.toolStripStatusState);
            this.Controls.Add(this.button_progPLISfile);
            this.Controls.Add(this.textBox_namePLISfile);
            this.Controls.Add(this.checkBox_version);
            this.Controls.Add(this.button_openPLISfile);
            this.Controls.Add(this.label_PLIS);
            this.Controls.Add(this.label_DSP);
            this.Controls.Add(this.button_progFRMfile);
            this.Controls.Add(this.richTextBoxLog);
            this.Controls.Add(this.statusStripMenu);
            this.Controls.Add(this.button_openFRMfile);
            this.Controls.Add(this.textBox_nameFRMfile);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(400, 241);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Обновление ПО МПС АЭ";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.statusStripMenu.ResumeLayout(false);
            this.statusStripMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_nameFRMfile;
        private System.Windows.Forms.Button button_openFRMfile;
        private System.Windows.Forms.StatusStrip statusStripMenu;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar;
        private System.Windows.Forms.RichTextBox richTextBoxLog;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatus;
        private System.Windows.Forms.OpenFileDialog openFileDialogFRM;
        private System.Windows.Forms.ToolTip toolTip_FRM;
        private System.Windows.Forms.CheckBox button_progFRMfile;
        private System.Windows.Forms.Label label_DSP;
        private System.Windows.Forms.Label label_PLIS;
        private System.Windows.Forms.Button button_openPLISfile;
        private System.Windows.Forms.CheckBox checkBox_version;
        private System.Windows.Forms.TextBox textBox_namePLISfile;
        private System.Windows.Forms.OpenFileDialog openFileDialog_PLIS;
        private System.Windows.Forms.CheckBox button_progPLISfile;
        private System.Windows.Forms.ToolTip toolTip_PLIS;
        private System.Windows.Forms.Label toolStripStatusState;
    }
}

