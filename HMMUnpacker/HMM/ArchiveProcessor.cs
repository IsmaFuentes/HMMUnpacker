using System.Text;
using HMMUnpacker.HMM.Interface;
using HMMUnpacker.HMM.Extensions;
using HMMUnpacker.HMM.Models;

namespace HMMUnpacker.HMM
{
  /// <summary>
  /// Source code used as a baseline (Python): https://github.com/ross-spencer/hmmunpack/blob/main/hmmunpack.py
  /// </summary>
  public class ArchiveProcessor : IArchiveProcessor
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

    /// <summary>
    /// Unpacks an HMM PAK File
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="outputPath"></param>
    /// <param name="writeToConsole"></param>
    /// <exception cref="Exception"></exception>
    public void Unpack(string filePath, string outputPath, Action<string, bool> writeToConsole)
    {
      byte[] bytes = File.ReadAllBytes(filePath);

      // Header (40 bytes of length)
      var sig = bytes.Take(16).ToArray();          // Signature 
      var unk = bytes.Skip(16).Take(4).ToArray();  // ???
      var nul = bytes.Skip(20).Take(12).ToArray(); // Null
      var num = bytes.Skip(32).Take(4).ToArray();  // Nº of files
      var len = bytes.Skip(36).Take(4).ToArray();  // Directory length

      var hmmSignature = Encoding.UTF8.GetBytes("HMMSYS PackFile\x0a");

      if (!sig.SequenceEqual(hmmSignature))
      {
        throw new Exception("Unsupported PAK format!");
      }

      int hmmFileCount = BitConverter.ToInt32(num);
      var hmmFileDirectory = bytes.Skip(40).Take(bytes.Length - 40).ToArray();

      // File processing
      int processed = 0;
      int dirLength = 0;

      var files = new List<HMMFileInfo>();
      while (processed < hmmFileCount)
      {
        int fnameLen = hmmFileDirectory.Slice(0, 1)[0];
        int reuseLen = hmmFileDirectory.Slice(1, 1)[0];
        int namePart = fnameLen - reuseLen;
        var filename = hmmFileDirectory.Slice(2, namePart);

        int offset = 2 + namePart;
        int length = offset + 4;

        int fileOffset = BitConverter.ToInt32(hmmFileDirectory.Slice(offset, offset + 4));
        int fileLength = BitConverter.ToInt32(hmmFileDirectory.Slice(length, length + 4));

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

      if (dirLength != BitConverter.ToInt32(len))
      {
        throw new Exception("Directory lengths do not match");
      }

      var additionalDataLenght = hmmFileCount * 4;
      var padding = hmmFileDirectory.Slice(0, additionalDataLenght); // probably not useful
      var firstFileOffset = (dirLength + additionalDataLenght + 40);

      if (files.First().Offset != firstFileOffset)
      {
        throw new Exception("File offsets do not match");
      }

      // Process the files...
      string rootDirectory = Path.Combine(outputPath, "Archive");
      string lastProcessedFileName = string.Empty;
      foreach (var hmmFile in files)
      {
        string fileName = hmmFile.FileName;

        if (hmmFile.FileNameReuseLength > 0)
        {
          fileName = $"{lastProcessedFileName.Substring(0, hmmFile.FileNameReuseLength)}{fileName}";
        }

        try
        {
          // Create all directories detected in given path
          string fileRootPath = Path.Combine(rootDirectory, fileName.Replace(Path.GetFileName(fileName), string.Empty));
          Directory.CreateDirectory(fileRootPath);
          lastProcessedFileName = fileName;

          writeToConsole($"Extracting {fileName}", false);
          var fileBytes = bytes.Skip(hmmFile.Offset).Take(hmmFile.Length).ToArray();
          File.WriteAllBytes(Path.Combine(rootDirectory, fileName), fileBytes);
        }
        catch (Exception ex)
        {
          writeToConsole($"Error while creating the file '{fileName}': {ex.Message}", true);
        }
      }

      writeToConsole("DONE!", false);
    }

    public void Repack(string directoryPath, string outputFilePath, Action<string, bool> writeToConsole)
    {
      // TODO
    }
  }
}
