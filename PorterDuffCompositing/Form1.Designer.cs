namespace PorterDuffCompositing
{
	partial class Form1
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
			this.comboBoxSelectedMode = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// comboBoxSelectedMode
			// 
			this.comboBoxSelectedMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxSelectedMode.FormattingEnabled = true;
			this.comboBoxSelectedMode.Location = new System.Drawing.Point(12, 12);
			this.comboBoxSelectedMode.Name = "comboBoxSelectedMode";
			this.comboBoxSelectedMode.Size = new System.Drawing.Size(121, 21);
			this.comboBoxSelectedMode.TabIndex = 0;
			this.comboBoxSelectedMode.SelectedIndexChanged += new System.EventHandler(this.comboBoxSelectedMode_SelectedIndexChanged);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.comboBoxSelectedMode);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox comboBoxSelectedMode;
	}
}

