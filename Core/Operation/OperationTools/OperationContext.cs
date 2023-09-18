using Core.Interfaces.Image;
using Core.Interfaces.Operation;
using Core.Services;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace Core.Operation
{
    public class InternalOutput : IInternalOutput
    {

    }
    public class OperationContext : BaseEntity
    {
        public int Id { get; set; }

        public string OperationName { get; set; }

        public int ActiveLayer { get; set; }

        public int MeasurementId { get; set; }

        public OperationProperties OperationProperties { get; set; }


        public IEnumerable<IArtifact> Artifacts { get; set; }

        public Dictionary<string, InternalOutput> InternalOutputs { get; set; }

        public OperationRunEventArgs OperationRunEventArgs { get; set; }

        public IDictionary<int, IBlobImage> BlobImages { get; set; }

        public IBlobImage ActiveBlobImage => BlobImages[ActiveLayer];


        public IDictionary<int, Bitmap> RawImages { get; set; }

        public RawImageMetadata RawImageMetadata { get; set; }


        public OperationContext()
        {

        }

        internal void AddInternalOutput(string key, InternalOutput newInternalOutput)
        {
            if (InternalOutputs == null)
                InternalOutputs = new Dictionary<string, InternalOutput>();
            InternalOutputs[key] = newInternalOutput;
        }

        // BlomImageProxyFactory @measurement?
        // currentFrame
        // minden layerhez -> RawImage, BlobImage
        // további képek: Artifacts

        // AddArtifact(string key, IArtifact artifact)
        // AddInternalOutput
    }
}
