namespace Cv4s.Common.Exceptions
{
    public class MeasurementNotFoundException : Exception
    {
        public MeasurementNotFoundException() : base("The desired measurement/opearion chain does not exist!")
        {
        }
    }
}
