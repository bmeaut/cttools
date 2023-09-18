using Cv4s.Common.Extensions;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models.Images;
using OpenCvSharp;
using System.Drawing;

namespace Cv4s.Common.Services
{
    public delegate Vec4b GetNextRandomColorDelegate();

    public class BlobAppearanceService : IBlobAppearanceService
    {
        private readonly Dictionary<int, SingleBlobAppearanceEngineService> appearanceServices = new();
        private readonly Vec4b noBlobColor = new Vec4b(0, 0, 0, 0);
        SingleBlobAppearanceEngineService currentAppearanceService;

        public Vec4b DefaultBlobColor { get => currentAppearanceService.DefaultBlobColor; set => currentAppearanceService.DefaultBlobColor = value; }


        public bool AssignRandomDefaultColors
        {
            get { return currentAppearanceService.AssignRandomDefaultColors; }
            set => currentAppearanceService.AssignRandomDefaultColors = value;
        }

        public BlobAppearanceService()
        {
            InitAppearanceEngine(0);
        }

        public Vec4b this[int blobId]
        {
            get
            {
                if (!currentAppearanceService.BlobColors.ContainsKey(blobId))
                {
                    return currentAppearanceService.NoBlobColor;
                }
                return currentAppearanceService[blobId];
            }
        }

        void InitAppearanceEngine(int id)
        {
            var newAppearance = new SingleBlobAppearanceEngineService();
            currentAppearanceService = newAppearance;
            appearanceServices.Add(id, currentAppearanceService);
        }

        public void SelectAppearance(int id)
        {
            if (!appearanceServices.ContainsKey(id))
                InitAppearanceEngine(id);
            currentAppearanceService = appearanceServices[id];
        }

        public void AddTagAppearanceCommand(string tagName, Vec4b color, int priority)
        {
            currentAppearanceService.AddTagAppearanceCommand(tagName, color, priority);
        }

        public void AddTagAppearanceCommand(ITagAppearanceCommand tagAppearanceCommand)
        {
            currentAppearanceService.AddTagAppearanceCommand(tagAppearanceCommand);
        }

        public void PrepareBlobs(IBlobImage blobImage)
        {
            currentAppearanceService.PrepareBlobs(blobImage);
        }

        public static Vec4b GetNextRandomColor()
        {
            return SingleBlobAppearanceEngineService.GetNextRandomColor();
        }

        internal static Vec4b GetBGRAVec4bFromColor(Color color)
        {
            return new Vec4b(color.B, color.G, color.R, color.A);
        }
    }

    /// <summary>
    /// Mapping blobs to colors w.r.t the tags on the blobs and
    /// the visual settings related to the individual tags.
    /// </summary>
    public class SingleBlobAppearanceEngineService : IBlobAppearanceService
    {
        private readonly Vec4b noBlobColor = new Vec4b(0, 0, 0, 0);
        private Vec4b defaultBlobColor = new Vec4b(0, 0, 0, 0);
        private readonly Dictionary<int, Vec4b> blobColors = new Dictionary<int, Vec4b>();
        private static readonly int randomColorCountPerHueCircle = 10;
        private static readonly int maxSupportedSaturationRounds = 3;

        private static int currentRandomIndex = 0;

        public bool AssignRandomDefaultColors { get; set; } = true;

        public Dictionary<int, Vec4b> BlobColors => blobColors;

        public Vec4b NoBlobColor => noBlobColor;

        public Vec4b DefaultBlobColor { get => defaultBlobColor; set => defaultBlobColor = value; }

        public void PrepareBlobs(IBlobImage blobImage)
        {
            BlobColors.Clear();
            var blobs = blobImage.CollectAllRealBlobIds();
            foreach (int b in blobs)
                BlobColors.Add(b, GetColorForBlob(blobImage.GetTagsForBlob(b)));
        }

        public Vec4b this[int blobId]
        {
            get
            {
                if (!BlobColors.ContainsKey(blobId))
                {
                    return NoBlobColor;
                }
                return BlobColors[blobId];
            }
        }

        // Indexing by tag name.
        private readonly Dictionary<string, ITagAppearanceCommand> appearanceCommandsByTagName
            = new Dictionary<string, ITagAppearanceCommand>();

        private readonly Dictionary<(string, int), Vec4b> colorForTags
            = new Dictionary<(string, int), Vec4b>();

        public void AddTagAppearanceCommand(string tagName, Vec4b color, int priority)
        {
            appearanceCommandsByTagName.Add(tagName,
                new SimpleTagAppearanceCommand(tagName, color, priority));
        }

        public void AddTagAppearanceCommand(ITagAppearanceCommand tagAppearanceCommand)
        {
            appearanceCommandsByTagName.Add(tagAppearanceCommand.TagName, tagAppearanceCommand);
        }

        private Vec4b GetColorForBlob(IEnumerable<ITag<int>> tags)
        {
            var commandWithHighestPriority = tags
                .Where(t => appearanceCommandsByTagName.ContainsKey(t.Name))
                .Select(t => appearanceCommandsByTagName[t.Name])
                .OrderByDescending(cmd => cmd.Priority)
                .FirstOrDefault();
            if (commandWithHighestPriority != null)
            {
                var currentTag = tags.FirstOrDefault(t => t.Name == commandWithHighestPriority?.TagName);
                return GetColorForTag(currentTag, commandWithHighestPriority);
            }
            if (!AssignRandomDefaultColors)
                return DefaultBlobColor;
            return GetNextRandomColorSimple();
        }

        private static readonly Random random = new Random();

        public static Vec4b GetNextRandomColor()
        {
            if (currentRandomIndex >= randomColorCountPerHueCircle * maxSupportedSaturationRounds)
                return GetNextRandomColorSimple();

            double step = 360 / randomColorCountPerHueCircle;
            double saturation = 1 - ((currentRandomIndex / randomColorCountPerHueCircle) / (double)maxSupportedSaturationRounds);
            double hue = step * (currentRandomIndex % randomColorCountPerHueCircle);
            var color = BlobAppearanceService.GetBGRAVec4bFromColor(ColorFromHSV(hue, saturation, 1));
            currentRandomIndex++;
            return color;
        }

        private static Vec4b GetNextRandomColorSimple()
        {
            return new Vec4b((byte)random.Next(255), (byte)random.Next(255),
                (byte)random.Next(255), 255);
        }


        private Vec4b GetColorForTag(ITag<int> currentTag, ITagAppearanceCommand commandWithHighestPriority)
        {
            Vec4b color = default;
            bool valueExistsInDictionary = colorForTags.TryGetValue((currentTag.Name, currentTag.Value), out color);
            if (valueExistsInDictionary)
            {
                return color;
            }
            else
            {
                color = commandWithHighestPriority.ColorProviderForTag.GetColor(currentTag);
                colorForTags.Add((currentTag.Name, currentTag.Value), color);
                return color;
            }
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        public void SelectAppearance(int id) => throw new NotImplementedException();
    }


    /// <summary>
    /// Simple command for the BlobAppearanceEngine: assigns a color to a given tag.
    /// If multiple tags assign color to a blob, the highest priority wins.
    /// Higher Priority value means higher priority.
    /// </summary>
    public class SimpleTagAppearanceCommand : TagAppearanceCommand
    {
        public SimpleTagAppearanceCommand(string tagName, Vec4b color, int priority) :
            base(tagName, new SimpleColorForTag(color), priority)
        { }
    }

    public class SimpleColorForTag : IColorProviderForTag
    {
        private Vec4b color;
        public SimpleColorForTag(Vec4b color) => this.color = color;
        public Vec4b GetColor(ITag<int> tag) => color;
    }

    public class TagComponentRandomColorProvider : IColorProviderForTag
    {
        private readonly Dictionary<(string, int), Vec4b> colorForTags
            = new Dictionary<(string, int), Vec4b>();
        private GetNextRandomColorDelegate GetNextRandomColor;

        public TagComponentRandomColorProvider(GetNextRandomColorDelegate function)
        {
            GetNextRandomColor = function;
        }

        public Vec4b GetColor(ITag<int> tag) => GetNextRandomColor();
    }

    public class TagRandomColorProviderSameForEachComponent : IColorProviderForTag
    {
        private readonly Dictionary<string, Vec4b> colorForTags
            = new Dictionary<string, Vec4b>();
        private GetNextRandomColorDelegate GetNextRandomColor;

        public TagRandomColorProviderSameForEachComponent(GetNextRandomColorDelegate function)
        {
            GetNextRandomColor = function;
        }

        public Vec4b GetColor(ITag<int> tag)
        {
            Vec4b color = default;
            bool valueExistsInDictionary = colorForTags.TryGetValue((tag.Name), out color);
            if (!valueExistsInDictionary)
            {
                color = GetNextRandomColor();
                colorForTags.Add(tag.Name, color);
            }
            return color;
        }
    }

    public class HeatMapColorForTag : IColorProviderForTag
    {
        private readonly Dictionary<int, Vec4b> colorForTags
            = new Dictionary<int, Vec4b>();

        public Vec4b GetColor(ITag<int> tag)
        {
            Vec4b color = default;
            bool valueExistsInDictionary = colorForTags.TryGetValue(tag.Value, out color);
            if (!valueExistsInDictionary)
            {
                color = BlobAppearanceService.GetBGRAVec4bFromColor(HeatMap(tag.Value, 0, 1000));
                colorForTags.Add(tag.Value, color);
            }
            return color;
        }

        public Color HeatMap(decimal value, decimal min, decimal max)
        {
            decimal val = (value - min) / (max - min);
            if (val > 255) val = 255;
            if (val < 0) val = 0;
            int A, B, R, G;
            A = 255;
            R = Convert.ToByte(255 * val);
            B = Convert.ToByte(255 * (1 - val));
            G = 0;
            return Color.FromArgb(A, R, G, B);
        }
    }
}
