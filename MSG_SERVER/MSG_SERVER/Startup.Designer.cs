namespace MSG_SERVER
{
    partial class Startup
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
            this.buttonConnect = new System.Windows.Forms.Button();
            this.groupData = new System.Windows.Forms.GroupBox();
            this.textPort = new System.Windows.Forms.TextBox();
            this.passWord = new Micahz.Controls.PassBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkPass = new System.Windows.Forms.CheckBox();
            this.groupData.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonConnect
            // 
            this.buttonConnect.Font = new System.Drawing.Font("Microsoft Sans Serif", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonConnect.Location = new System.Drawing.Point(176, 18);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(152, 98);
            this.buttonConnect.TabIndex = 0;
            this.buttonConnect.Text = "Start Server";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // groupData
            // 
            this.groupData.Controls.Add(this.passWord);
            this.groupData.Controls.Add(this.checkPass);
            this.groupData.Controls.Add(this.label1);
            this.groupData.Controls.Add(this.textPort);
            this.groupData.Location = new System.Drawing.Point(13, 13);
            this.groupData.Name = "groupData";
            this.groupData.Size = new System.Drawing.Size(142, 103);
            this.groupData.TabIndex = 1;
            this.groupData.TabStop = false;
            this.groupData.Text = "Server Details";
            // 
            // textPort
            // 
            this.textPort.Location = new System.Drawing.Point(41, 17);
            this.textPort.Name = "textPort";
            this.textPort.Size = new System.Drawing.Size(91, 20);
            this.textPort.TabIndex = 0;
            // 
            // passWord
            // 
            this.passWord.Enabled = false;
            this.passWord.Location = new System.Drawing.Point(9, 66);
            this.passWord.Name = "passWord";
            this.passWord.PasswordChar = '*';
            this.passWord.Size = new System.Drawing.Size(123, 20);
            this.passWord.TabIndex = 2;
            this.passWord.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Port:";
            // 
            // checkPass
            // 
            this.checkPass.AutoSize = true;
            this.checkPass.Location = new System.Drawing.Point(9, 43);
            this.checkPass.Name = "checkPass";
            this.checkPass.Size = new System.Drawing.Size(123, 17);
            this.checkPass.TabIndex = 5;
            this.checkPass.Text = "Password Encrypted";
            this.checkPass.UseVisualStyleBackColor = true;
            this.checkPass.CheckedChanged += new System.EventHandler(this.checkPass_CheckedChanged);
            // 
            // Startup
            // 
            this.AcceptButton = this.buttonConnect;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(337, 126);
            this.Controls.Add(this.groupData);
            this.Controls.Add(this.buttonConnect);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "Startup";
            this.Text = "Message Server Setup";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Startup_FormClosed);
            this.Load += new System.EventHandler(this.Startup_Load);
            this.groupData.ResumeLayout(false);
            this.groupData.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.GroupBox groupData;
        private Micahz.Controls.PassBox passWord;
        private System.Windows.Forms.CheckBox checkPass;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textPort;
    }
}