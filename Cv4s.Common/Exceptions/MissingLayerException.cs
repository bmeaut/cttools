namespace Cv4s.Common.Exceptions
{
    public class MissingLayerException : Exception
    {
        public int Index1 { get; }
        public int Index2 { get; }

        public override string Message => $"Distance between layer with index {Index1} and layer with index {Index2} is bigger then expected";

        public MissingLayerException(int lower_index, int higher_index)
        {
            Index1 = lower_index;
            Index2 = higher_index;
        }
    }
}
