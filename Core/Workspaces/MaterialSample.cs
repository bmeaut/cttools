using Core.Interfaces.Image;
using System.Collections.Generic;

namespace Core.Workspaces
{
    public class MaterialSample : BaseEntity
    {
        public int Id { get; set; }

        public string Label { get; set; }


        public int WorkspaceId { get; set; }

        public Workspace Workspace { get; set; }

        public MaterialScan MaterialScan { get; set; }

        public IEnumerable<UserGeneratedFile> UserGeneratedFiles { get; set; }

        public IEnumerable<Measurement> Measurements { get; set; }

        public int? CurrentStatusId { get; set; }

        public Status CurrentStatus { get; set; }

        public ICollection<Status> Statuses { get; set; }


        public IRawImageSource RawImages { get; set; }

        private int _dicomLevel = 0;
        private int _dicomRange = 1000;
        public int DicomLevel
        {
            get
            {
                if (RawImages != null)
                    return RawImages.DicomLevel;
                else return _dicomLevel;
            }
            set
            {
                if (RawImages != null)
                    RawImages.DicomLevel = value;
                _dicomLevel = value;
            }
        }
        public int DicomRange
        {
            get
            {
                if (RawImages != null)
                    return RawImages.DicomRange;
                else return _dicomRange;
            }
            set
            {
                if (RawImages != null)
                    RawImages.DicomRange = value;
                _dicomRange = value;
            }
        }

    }
}