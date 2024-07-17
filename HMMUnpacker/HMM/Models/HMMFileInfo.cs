namespace HMMUnpacker.HMM.Models
{
  public class HMMFileInfo
  {
    public int Offset { get; set; } = 0;
    public int Length { get; set; } = 0;
    public string FileName { get; set; } = string.Empty;
    public int FileNameLength { get; set; } = 0;
    public int FileNameReuseLength { get; set; } = 0;
  }
}
