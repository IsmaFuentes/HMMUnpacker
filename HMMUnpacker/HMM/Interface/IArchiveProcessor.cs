namespace HMMUnpacker.HMM.Interface
{
  internal interface IArchiveProcessor
  {
    public void Unpack(string filePath, string outputPath, Action<string> writeToConsole);
    public void Repack(string directoryPath, string outputFilePath);
  }
}
