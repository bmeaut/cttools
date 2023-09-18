namespace Core.Interfaces.Image
{
    public interface ITag<T>
    {
        public string Name { get; set; }

        public T Value { get; set; }
    }
}
