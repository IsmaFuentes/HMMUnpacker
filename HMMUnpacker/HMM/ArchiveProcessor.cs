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
      var bytes = new List<byte>();

      string[] files = Directory
        .GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

      var hmmDirectoryBytesLength = files
        .Select(File.ReadAllBytes)
        .Aggregate(new List<byte>(), (accumulator, current) =>
      {
        accumulator.AddRange(current.ToArray());
        return accumulator;
      })
        .ToArray()
        .Count();

      // Header (40 bytes)
      bytes.AddRange(Encoding.UTF8.GetBytes("HMMSYS PackFile\x0a")); // Signature   (16)
      bytes.AddRange(Enumerable.Repeat((byte)0, 4));                 // Unknown     (4)
      bytes.AddRange(Enumerable.Repeat((byte)0, 12));                // Null        (12)
      bytes.AddRange(BitConverter.GetBytes(files.Count()));          // Nº of files (4)
      bytes.AddRange(BitConverter.GetBytes(hmmDirectoryBytesLength));// Dir length  (4)

      // Files
      string lastFile = string.Empty;
      foreach(string file in files)
      {
        // For each file:
        // -----------------------------
        // byte {1}     - Filename Length
        // byte {1}     - Previous Filename Reuse Length
        // char {X}     - Filename Part (length = filenameLength - previousFilenameReuseLength)
        // uint32 {4}   - File Offset
        // uint32 {4}   - File Length

        string fileName = file.Replace(directoryPath, string.Empty);
        writeToConsole($"Compressing {fileName}", false);
        if (string.IsNullOrEmpty(lastFile))
        {
          lastFile = fileName;
        }

        byte[] hmmFileBytes = File.ReadAllBytes(file);

        int reuseFileLen = 0;
        if(fileName != lastFile)
        {
          string prevPath = lastFile.Replace(Path.GetFileName(lastFile), string.Empty);
          string currPath = fileName.Replace(Path.GetFileName(fileName), string.Empty);

          if(prevPath == currPath && currPath != "\\")
          {
            reuseFileLen = prevPath.Length;
          }

          string actualFileName = fileName.Substring(reuseFileLen);
          bytes.Add((byte)actualFileName.Length);
          lastFile = fileName;
        }
        else
        {
          bytes.Add((byte)fileName.Length);
        }

        bytes.Add((byte)reuseFileLen);
        bytes.AddRange(BitConverter.GetBytes(0));

        byte[] content = File.ReadAllBytes(file);

        int offset = bytes.Count + content.Length + BitConverter.GetBytes(content.Length).Length;
        bytes.AddRange(BitConverter.GetBytes(offset));          // offset
        bytes.AddRange(BitConverter.GetBytes(content.Length));  // length
        bytes.AddRange(content);
      }

      File.WriteAllBytes(outputFilePath, bytes.ToArray());
    }
  }
}
