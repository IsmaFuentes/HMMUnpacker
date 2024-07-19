namespace HMMUnpacker.HMM.Interface
{
  internal interface IArchiveProcessor
  {
    public void Unpack(string filePath, string outputPath, Action<string, bool> writeToConsole);
    public void Repack(string directoryPath, string outputFilePath, Action<string, bool> writeToConsole);
  }
}
