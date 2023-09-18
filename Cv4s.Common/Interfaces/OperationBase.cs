using Cv4s.Common.Enums;
using Cv4s.Common.Interfaces.Images;
using Cv4s.Common.Models;
using System.Drawing;

namespace Cv4s.Common.Interfaces
{
    public abstract class OperationBase
    {
        public virtual OperationProperties? Properties { get; set; }

        //This is providing iformation about the called opration and can the ui can provide information about the drawing procedure
        public OperationRunEventArgs RunEventArgs { get; protected set; } = new OperationRunEventArgs();

        //TODO parameters for the drawing binding
        //parameters for the dicom settings, and others.
        //two way binding with loading data.
    }
}