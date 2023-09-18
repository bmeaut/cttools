namespace Core.Workspaces
{
    public class UserGeneratedFile : BaseEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; } // TODO: handle filesystem, onedrive, etc.


        public int MaterialSampleId { get; set; }

        public MaterialSample MaterialSample { get; set; }
    }
}