namespace HMMUnpacker
{
  partial class MainForm
  {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      bntUnpack = new Button();
      SuspendLayout();
      // 
      // bntUnpack
      // 
      bntUnpack.Location = new Point(12, 12);
      bntUnpack.Name = "bntUnpack";
      bntUnpack.Size = new Size(75, 23);
      bntUnpack.TabIndex = 0;
      bntUnpack.Text = "Unpack";
      bntUnpack.UseVisualStyleBackColor = true;
      bntUnpack.Click += bntUnpack_Click;
      // 
      // MainForm
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(800, 450);
      Controls.Add(bntUnpack);
      Name = "MainForm";
      Text = "Form1";
      ResumeLayout(false);
    }

    #endregion

    private Button bntUnpack;
  }
}
