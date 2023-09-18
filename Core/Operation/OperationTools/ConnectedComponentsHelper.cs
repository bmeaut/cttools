using Core.Image;
using Core.Interfaces.Image;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core.Operation
{
    public class ConnectedComponentsHelper
    {

        /// <summary>
        /// TODO - This id deprecated because it was VERY SLOW in Multichanneltresholding.
        /// Consider using OpenCV directly or optimize this helper method to avoid performance issues
        /// </summary>
        public static IEnumerable<Mat> GetConnectedComponentsMasks(Mat image)
        {
            if (image == null)
                throw new ArgumentException($"GetConnectedComponentsMasks needs input image.");
            if (image.Type() != MatType.CV_8UC1)
                throw new ArgumentException($"GetConnectedComponentsMasks needs input image of type CV_8UC1. This is {image.GetType()}");
            var labelImage = new Mat(image.Size(), MatType.CV_32SC1);
            var labelCount = Cv2.ConnectedComponents(image, labelImage, PixelConnectivity.Connectivity4);

            using (var cmp = new Mat(labelImage.Size(), MatType.CV_32SC1))
            {
                for (int i = 0; i < labelCount; i++)
                {
                    cmp.SetTo(i);
                    var result = new Mat();
                    Cv2.Compare(labelImage, cmp, result, CmpType.EQ);
                    if (result.CountNonZero() == 0)
                        continue;
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Return masks of connected componens after a classic thresholding.
        /// The locations "below the threshold" are not returned in any masks.
        /// </summary>
        /// <param name="image">Original (grayscale) image</param>
        /// <param name="threshold">See the threshold OpenCV function</param>
        /// <param name="maxVal">See the threshold OpenCV function</param>
        /// <param name="type">See the threshold OpenCV function</param>
        /// <returns>Binary masks (zero - nonzero) of the indicidual connected components.</returns>
        public static IEnumerable<Mat> GetThresholdResultMasks(Mat image, int threshold, int maxVal, ThresholdTypes type)
        {
            Mat thresholdResultMask = new Mat(image.Size(), MatType.CV_8UC1);
            Cv2.Threshold(image, thresholdResultMask, threshold, maxVal, type);
            Mat backgroundMask = new Mat(thresholdResultMask.Size(), MatType.CV_8UC1, new Scalar(255));
            backgroundMask.SetTo(0, thresholdResultMask);

            foreach (var mask in ConnectedComponentsHelper.GetConnectedComponentsMasks(thresholdResultMask))
            {
                // Remove areas which belonged to the backgorund ("below threshold" areas)
                mask.SetTo(0, backgroundMask);
                if (mask.CountNonZero() > 0)
                    yield return mask;
            }
        }

        /// <summary>
        /// If given blob is not a single connected component, new blobIDs-s are assigned
        /// to every connected component.
        /// If the original blob is connected, its blobId is not changed.
        /// </summary>
        /// <param name="blobImage"></param>
        /// <param name="blobId">The blob to separate</param>
        /// <returns>Number of new blobs (1 is original is unchanged)</returns>
        public static int SeparateSegmentedPartsOfBlob(IBlobImage blobImage, int blobId)
        {
            var originalMask = blobImage.GetMask(blobId);

            var newMasks = GetThresholdResultMasks(originalMask, 0, 255, OpenCvSharp.ThresholdTypes.Binary)
                .ToArray();

            // Note: all blobs get new ID-s if they get separated
            if (newMasks.Length <= 1)
                return 1; // Nothing to separate, whole blob is connected

            foreach (var mask in newMasks)
                blobImage.SetBlobMask(mask, blobImage.GetNextUnusedBlobId());

            return newMasks.Length;
        }

        /// <summary>
        /// Merges the given blobId-s into a given blob.
        /// The blobID blobIdToExtend is assigned to all appearances of
        ///     all blobs having a blobId mentioned in blobIdsToMerge.
        /// </summary>
        /// <param name="blobImage"></param>
        /// <param name="blobIdToExtend">This blob will get extended with the other (merged) blobs.</param>
        /// <param name="blobIdsToMerge">Blobs which get merged into the one to extend.</param>
        public static void MergeBlobs(IBlobImage blobImage, int blobIdToExtend, int[] blobIdsToMerge)
        {
            var size = blobImage.Size;
            for (int y = 0; y < size.Height; y++)
                for (int x = 0; x < size.Width; x++)
                {
                    var currentBlobId = blobImage[y, x];
                    if (blobIdsToMerge.Contains(currentBlobId))
                        blobImage[y, x] = blobIdToExtend;
                }
        }

        public static IEnumerable<int> SegmentMask(IBlobImage blobImage, Mat componentMask)
        {
            var newBlobIds = new List<int>();
            var nextAvailableBlobId = blobImage.GetNextUnusedBlobId();

            Mat labeledImage = new Mat(componentMask.Size(), MatType.CV_32SC1);
            Cv2.ConnectedComponents(componentMask, labeledImage, PixelConnectivity.Connectivity4);
            var labeledImageIndexer = labeledImage.GetGenericIndexer<int>();
            var labelToBlobIdMapper = new Dictionary<int, int>();
            for (int x = 0; x < blobImage.Size.Width; x++)
            {
                for (int y = 0; y < blobImage.Size.Height; y++)
                {
                    int label = labeledImageIndexer[y, x];
                    if (label == 0)
                    {
                        continue;
                    }
                    if (!labelToBlobIdMapper.ContainsKey(label))
                    {
                        int blobId = nextAvailableBlobId;
                        labelToBlobIdMapper.Add(label, blobId);
                        blobImage[y, x] = blobId;
                        newBlobIds.Add(blobId);
                        nextAvailableBlobId++;
                    }
                    else
                    {
                        int blobId = labelToBlobIdMapper[label];
                        blobImage[y, x] = blobId;
                    }
                }
            }
            return newBlobIds;
        }
    }
}
