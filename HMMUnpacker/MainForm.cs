using System.Text;

namespace HMMUnpacker
{
  public partial class MainForm : Form
  {
    public MainForm()
    {
      InitializeComponent();
    }

    /// <summary>
    ///  Source code (Python): https://github.com/ross-spencer/hmmunpack/blob/main/hmmunpack.py
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void bntUnpack_Click(object sender, EventArgs e)
    {
      /*
       
          Format Specifications
          char {16}    - Header ("HMMSYS PackFile" + (byte)10)
          uint32 {4}   - Unknown (26)
          byte {12}    - null
          uint32 {4}   - Number Of Files
          uint32 {4}   - Directory Length [+40 archive header]

          // for each file

          byte {1}     - Filename Length
          byte {1}     - Previous Filename Reuse Length
          char {X}     - Filename Part (length = filenameLength - previousFilenameReuseLength)
          uint32 {4}   - File Offset
          uint32 {4}   - File Length

          // Presumably after the directory...

          byte {X}     - Padding (repeating 153,121,150,50) until first file offset
          byte {X}     - File Data

          +=============================================+
          |               HMM Packfile                  |
          +=============================================+
          | Format header (40 bytes)                    |
          +---------------------------------------------+
          | File directory (n-bytes * no_files)         |
          | (1, 1, max(255), max(255), 4, 4) * no_files |
          +---------------------------------------------+
          | Padding (no_files * 4-bytes)                |
          +---------------------------------------------+
          | Files (n-bytes * no_files * file_length)    |
          +---------------------------------------------+

       */
      byte[] bytes = File.ReadAllBytes(@"C:\Users\ifuen\Desktop\Test\data.pak");

      // Header (40 bytes of length)
      var sig = bytes.Take(16).ToArray();          // Signature 
      var unk = bytes.Skip(16).Take(4).ToArray();  // ???
      var nul = bytes.Skip(20).Take(12).ToArray(); // Null
      var num = bytes.Skip(32).Take(4).ToArray();  // Nº of files
      var len = bytes.Skip(36).Take(4).ToArray();  // Directory length

      var hmmSignature = Encoding.UTF8.GetBytes("HMMSYS PackFile\x0a");

      if (!sig.SequenceEqual(hmmSignature))
      {
        MessageBox.Show(this, "Unsupported PAK format!", "Error");
      }

      int hmmFileCount = BitConverter.ToInt32(num);
      //int maxDirLength = 522; // ??
      //int approxDirLen = hmmFileCount * maxDirLength + (hmmFileCount * 4);
      //var hmmFileDirectory = bytes.Skip(40).Take(approxDirLen).ToArray();
      var hmmFileDirectory = bytes.Skip(40).Take(bytes.Length - 40).ToArray();

      // File processing
      int processed = 0;
      int dirLength = 0;

      var files = new List<HMMFileInfo>();

      while(processed < hmmFileCount)
      {
        int fnameLen = Slice(hmmFileDirectory, 0, 1)[0];
        int reuseLen = Slice(hmmFileDirectory, 1, 1)[0];
        int namePart = fnameLen - reuseLen;
        var filename = Slice(hmmFileDirectory, 2, namePart);

        int offset = 2 + namePart;
        int length = offset + 4;

        int fileOffset = BitConverter.ToInt32(Slice(hmmFileDirectory, offset, offset + 4)); // offset + 4
        int fileLength = BitConverter.ToInt32(Slice(hmmFileDirectory, length, length + 4)); // length + 4

        files.Add(new HMMFileInfo
        {
          Offset = fileOffset,
          Length = fileLength,
          FileName = Encoding.UTF8.GetString(filename),
          FileNameLength = fnameLen,
          FileNameReuseLength = reuseLen,
        });

        int eof = length + 4;
        dirLength += eof;
        hmmFileDirectory = hmmFileDirectory.Skip(eof).ToArray();

        processed++;
      }

      // process the files...
      string root = @"C:\Users\ifuen\Desktop\Test";
      string rootDirectory = Path.Combine(root, "Archive");

      int totalLength = 0;
      string lastProcessedFileName = string.Empty;
      foreach (var hmmFile in files)
      {
        totalLength += hmmFile.Length;

        string fileName = hmmFile.FileName;
        if(hmmFile.FileNameReuseLength > 0)
        {
          fileName = $"{lastProcessedFileName.Substring(0, hmmFile.FileNameReuseLength)}{fileName}";
        }

        // Create all directories detected in given path
        string fileRootPath = Path.Combine(rootDirectory, fileName.Replace(Path.GetFileName(fileName), string.Empty));
        Directory.CreateDirectory(fileRootPath);
        lastProcessedFileName = fileName;
        try
        {
          var fileBytes = bytes.Skip(hmmFile.Offset).Take(hmmFile.Length).ToArray();
          File.WriteAllBytes(Path.Combine(rootDirectory, fileName), fileBytes);
        }
        catch(Exception ex)
        {
          // ??
        }
      }

      // DONE...
    }

    private static byte[] Slice(byte[] source, int offset, int quantity)
    {
      return source.Skip(offset).Take(quantity).ToArray();
    }

    public class HMMFileInfo
    {
      public int Offset { get; set; } = 0;
      public int Length { get; set; } = 0;
      public string FileName { get; set; } = string.Empty;
      public int FileNameLength { get; set; } = 0;
      public int FileNameReuseLength { get; set; } = 0;
    }
  }
}
