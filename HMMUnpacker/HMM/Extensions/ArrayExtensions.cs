namespace HMMUnpacker.HMM.Extensions
{
  public static class ArrayExtensions
  {
    public static byte[] Slice(this byte[] source, int offset, int quantity)
    {
      return source.Skip(offset).Take(quantity).ToArray();
    }
  }
}
