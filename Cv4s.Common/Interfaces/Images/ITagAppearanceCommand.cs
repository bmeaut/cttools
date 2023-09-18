namespace Cv4s.Common.Interfaces.Images
{
    public interface ITagAppearanceCommand
    {
        public int Priority { get; set; }
        public string TagName { get; }
        public IColorProviderForTag ColorProviderForTag { get; }
    }
}
