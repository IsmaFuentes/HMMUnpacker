using System.Text;
using HMMUnpacker.HMM.Interface;
using HMMUnpacker.HMM.Extensions;
using HMMUnpacker.HMM.Models;

namespace HMMUnpacker.HMM
{
  /// <summary>
  /// Source code used as a baseline (Python): https://github.com/ross-spencer/hmmunpack/blob/main/hmmunpack.py
  /// http://fileformats.archiveteam.org/wiki/HMM_Packfile
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
        | (1, 1, max(255), 4, 4) * no_files |
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
      var sig = bytes.Slice(0, 16);  // Signature 
      var unk = bytes.Slice(16, 4);  // ???
      var nul = bytes.Slice(20, 12); // Null
      var num = bytes.Slice(32, 4);  // Nº of files
      var len = bytes.Slice(36, 4);  // Directory length

      var hmmSignature = Encoding.UTF8.GetBytes("HMMSYS PackFile\x0a");

      if (!sig.SequenceEqual(hmmSignature))
      {
        throw new Exception("Unsupported PAK format!");
      }

      int hmmFileCount = BitConverter.ToInt32(num);
      var hmmFileDirectory = bytes.Slice(40, bytes.Length - 40);

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

      int directoryLength = BitConverter.ToInt32(len);
      if (dirLength != directoryLength)
      {
        throw new Exception("Directory lengths do not match");
      }

      var additionalDataLength = hmmFileCount * 4;
      var padding = hmmFileDirectory.Slice(0, additionalDataLength); // probably not useful
      var firstFileOffset = (dirLength + additionalDataLength + 40);

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
      string[] files = Directory
        .GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

      var hmmFileDirectoryPairs = new List<KeyValuePair<int, List<byte>>>();
      var hmmFileDirectoryBytes = new List<byte>();
      var hmmFileData = new List<byte>();

      string lastFile = string.Empty;

      foreach(var path in files)
      {
        var fileBytes = File.ReadAllBytes(path);
        hmmFileData.AddRange(fileBytes);

        string fileName = Path.GetRelativePath(directoryPath, path);
        writeToConsole($"Compressing {fileName}", false);

        if (lastFile == string.Empty)
        {
          lastFile = fileName;
        }

        int fnameResueLen = 0;
        string prevPath = lastFile.Replace(Path.GetFileName(lastFile), string.Empty);
        string currPath = fileName.Replace(Path.GetFileName(fileName), string.Empty);

        if (prevPath == currPath)
        {
          fnameResueLen = prevPath.Length;
        }

        lastFile = fileName;

        string fnamePart = fileName.Substring(fnameResueLen);

        var hmmFileBytes = new List<byte>();
        hmmFileBytes.Add((byte)fileName.Length);                        // fileName (1)
        hmmFileBytes.Add((byte)fnameResueLen);                          // Reuse file length (1)
        hmmFileBytes.AddRange(Encoding.UTF8.GetBytes(fnamePart));       // Filename part (length = fileName.Length - reuseFileLen, max 255)

        // Because we still don't know the full length of the file directory which is needed to calculate the offset (each file has a different filename length), 
        // we will be using a KeyValuePair to store the initial parameters.  
        hmmFileDirectoryPairs.Add(new KeyValuePair<int, List<byte>>(fileBytes.Length, hmmFileBytes));
      }

      var padding = new byte[files.Length * 4];
      // Accumulated directoryFile length in bytes + pending byte count needed for offsets and lengths + padding
      int offset = 40 + hmmFileDirectoryPairs.Sum(e => e.Value.Count) + (files.Count() * 8) + padding.Length;

      foreach(var pair in hmmFileDirectoryPairs)
      { // Now that we have access to the real offset, we can finish encoding the file directory using the stored key value pairs
        int len = pair.Key; 
        pair.Value.AddRange(BitConverter.GetBytes(offset));   // file offset (4)
        pair.Value.AddRange(BitConverter.GetBytes(len));      // file length (4)
        hmmFileDirectoryBytes.AddRange(pair.Value);
        offset += len;
      }

      // Header (40 bytes)
      var hmmHeader = new List<byte>();
      hmmHeader.AddRange(Encoding.UTF8.GetBytes("HMMSYS PackFile\x0a"));      // Signature        (16)
      hmmHeader.AddRange(Enumerable.Repeat((byte)0, 4));                      // Unknown          (4)
      hmmHeader.AddRange(Enumerable.Repeat((byte)0, 12));                     // Null             (12)
      hmmHeader.AddRange(BitConverter.GetBytes(files.Count()));               // Nº of files      (4)
      hmmHeader.AddRange(BitConverter.GetBytes(hmmFileDirectoryBytes.Count)); // Directory length (4)

      var hmmBytes = new List<byte>();
      hmmBytes.AddRange(hmmHeader);                                           // Header 
      hmmBytes.AddRange(hmmFileDirectoryBytes);                               // DirectoryInfo
      hmmBytes.AddRange(padding);                                             // Padding
      hmmBytes.AddRange(hmmFileData);                                         // FileData

      File.WriteAllBytes(outputFilePath, hmmBytes.ToArray());
      writeToConsole("DONE!", false);
    }
  }
}
