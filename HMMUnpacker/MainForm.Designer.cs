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
      MainPanel = new Panel();
      consoleBox = new RichTextBox();
      btnOpenFile = new Button();
      txtSelectedFile = new TextBox();
      MainPanel.SuspendLayout();
      SuspendLayout();
      // 
      // bntUnpack
      // 
      bntUnpack.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
      bntUnpack.Location = new Point(713, 415);
      bntUnpack.Name = "bntUnpack";
      bntUnpack.Size = new Size(75, 23);
      bntUnpack.TabIndex = 0;
      bntUnpack.Text = "Unpack";
      bntUnpack.UseVisualStyleBackColor = true;
      bntUnpack.Click += bntUnpack_Click;
      // 
      // MainPanel
      // 
      MainPanel.Controls.Add(consoleBox);
      MainPanel.Controls.Add(btnOpenFile);
      MainPanel.Controls.Add(txtSelectedFile);
      MainPanel.Controls.Add(bntUnpack);
      MainPanel.Dock = DockStyle.Fill;
      MainPanel.Location = new Point(0, 0);
      MainPanel.Name = "MainPanel";
      MainPanel.Size = new Size(800, 450);
      MainPanel.TabIndex = 1;
      // 
      // consoleBox
      // 
      consoleBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
      consoleBox.Location = new Point(12, 41);
      consoleBox.Name = "consoleBox";
      consoleBox.ReadOnly = true;
      consoleBox.Size = new Size(776, 368);
      consoleBox.TabIndex = 2;
      consoleBox.Text = "";
      // 
      // btnOpenFile
      // 
      btnOpenFile.Location = new Point(12, 12);
      btnOpenFile.Name = "btnOpenFile";
      btnOpenFile.Size = new Size(75, 23);
      btnOpenFile.TabIndex = 1;
      btnOpenFile.Text = "Open";
      btnOpenFile.UseVisualStyleBackColor = true;
      btnOpenFile.Click += btnOpenFile_Click;
      // 
      // txtSelectedFile
      // 
      txtSelectedFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
      txtSelectedFile.Location = new Point(93, 12);
      txtSelectedFile.Name = "txtSelectedFile";
      txtSelectedFile.ReadOnly = true;
      txtSelectedFile.Size = new Size(695, 23);
      txtSelectedFile.TabIndex = 0;
      // 
      // MainForm
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(800, 450);
      Controls.Add(MainPanel);
      Name = "MainForm";
      Text = "HMM Unpacker";
      MainPanel.ResumeLayout(false);
      MainPanel.PerformLayout();
      ResumeLayout(false);
    }

    #endregion

    private Button bntUnpack;
    private Panel MainPanel;
    private TextBox txtSelectedFile;
    private Button btnOpenFile;
    private RichTextBox consoleBox;
  }
}
