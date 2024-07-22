using HMMUnpacker.HMM;

namespace HMMUnpacker
{
  public partial class MainForm : Form
  {
    public MainForm()
    {
      InitializeComponent();
    }

    private string? SelectedFile { get; set; }
    private void btnOpenFile_Click(object sender, EventArgs e)
    {
      using (var dialog = new OpenFileDialog())
      {
        dialog.Filter = "pak files (*.pak)|*.pak";
        dialog.RestoreDirectory = true;

        if (dialog.ShowDialog() == DialogResult.OK)
        {
          SelectedFile = dialog.FileName;
          txtSelectedFile.Text = SelectedFile;
        }
      }
    }

    private void bntUnpack_Click(object sender, EventArgs e)
    {
      if (!string.IsNullOrEmpty(SelectedFile))
      {
        using (var dialog = new FolderBrowserDialog()) // File output directory
        {
          if (dialog.ShowDialog() == DialogResult.OK)
          {
            Task.Run(() =>
            {
              var processor = new ArchiveProcessor();

              try
              {
                processor.Unpack(SelectedFile, dialog.SelectedPath, (message, isError) =>
                {
                  consoleBox.Invoke(() =>
                  {
                    consoleBox.ForeColor = isError ? Color.Red : Color.LimeGreen;
                    consoleBox.Text += $"{message}\n";
                    consoleBox.SelectionStart = consoleBox.Text.Length;
                    consoleBox.ScrollToCaret();
                  });
                });
              }
              catch(Exception ex)
              {
                Invoke(() => MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error));
              }
            });
          }
        }
      }
    }

    private void bntRepack_Click(object sender, EventArgs e)
    {
      using (var dialog = new FolderBrowserDialog()) // Archive output directory
      {
        if (dialog.ShowDialog() == DialogResult.OK)
        {
          int x = DesktopLocation.X + (Width  / 4);
          int y = DesktopLocation.Y + (Height / 4);
          string pakName = Microsoft.VisualBasic.Interaction.InputBox("Please, specify a PAK name", "HMMUnpacker", "data", x, y);

          Task.Run(() =>
          {
            var processor = new ArchiveProcessor();

            try
            {
              processor.Repack(dialog.SelectedPath, Path.Combine(Directory.GetParent(dialog.SelectedPath).FullName, $"{pakName}.pak"), (message, isError) =>
              {
                consoleBox.Invoke(() =>
                {
                  consoleBox.ForeColor = isError ? Color.Red : Color.LimeGreen;
                  consoleBox.Text += $"{message}\n";
                  consoleBox.SelectionStart = consoleBox.Text.Length;
                  consoleBox.ScrollToCaret();
                });
              });
            }
            catch(Exception ex)
            {
              Invoke(() => MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
          });
        }
      }
    }
  }
}
