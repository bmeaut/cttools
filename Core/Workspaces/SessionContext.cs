namespace Core.Workspaces
{
    public class SessionContext : BaseEntity
    {
        public int Id { get; set; }


        public int CurrentLayerIndex { get; set; }

        public int CurrentMeasurementId { get; set; }

        public Measurement CurrentMeasurement { get; set; }
    }
}