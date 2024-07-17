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

    private void bntUnpack_Click(object sender, EventArgs e)
    {
      if(!string.IsNullOrEmpty(SelectedFile))
      {
        // Ask for output directory
        using (var dialog = new FolderBrowserDialog())
        {
          if(dialog.ShowDialog() == DialogResult.OK)
          {
            Task.Run(() =>
            {
              var processor = new ArchiveProcessor();

              processor.Unpack(SelectedFile, dialog.SelectedPath, (e) =>
              {
                consoleBox.Invoke(() =>
                {
                  consoleBox.Text += $"{e}\n";
                  consoleBox.SelectionStart = consoleBox.Text.Length;
                  consoleBox.ScrollToCaret();
                });
              });
            });
          }
        }
      }
    }

    private void btnOpenFile_Click(object sender, EventArgs e)
    {
      using(var dialog = new OpenFileDialog())
      {
        dialog.Filter = "pak files (*.pak)|*.pak";
        dialog.RestoreDirectory = true;

        if(dialog.ShowDialog() == DialogResult.OK)
        {
          SelectedFile = dialog.FileName;
          txtSelectedFile.Text = SelectedFile;
        }
      }
    }
  }
}
