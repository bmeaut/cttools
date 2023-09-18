using Dicom;
using Dicom.Imaging;
using Dicom.IO.Buffer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Core.SteelFibers.Converters
{
    public class PngToDicomConverter
    {
        public PngToDicomConverter()
        {
        }

        /// <summary>
        ///  Konvertálja a képeket .dcm fájlokká. Beolvassa az adott nevű képesorozat elemeit, majd a megadott helyre és névvel
        ///  elmenti a DICOM fájlokat. A fájlok <név><sorszám>.<kiterjesztés> formában lókerülnek beolvasásra és mentésre, a sorszámot NEM kell megadni.
        /// </summary>
        /// <param name="inputFilesName"> A beolvasandó képek útvonala és neve (kiterjeszéts nélkül) </param>
        /// <param name="inputFileExtension"> A beolvasandó képek kiterjesztése </param>
        /// <param name="outputFilesName"> A DICOM fájlok helye és neve (kiterjeszéts nélkül)</param>
        /// <param name="imageCount"> A képek száma </param>
        public void ConvertImages(string inputFilesName, string inputFileExtension, string outputFilesName, int imageCount)
        {
            for (int i = 0; i < imageCount; i++)
            {
                Bitmap bitmap = new Bitmap(inputFilesName + i + inputFileExtension);
                bitmap = GetValidImage(bitmap);
                int rows, columns;
                byte[] pixels = GetPixels(bitmap, out rows, out columns);
                MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);
                DicomDataset dataset = new DicomDataset();
                FillDataset(dataset);
                dataset.Add(DicomTag.Rows, rows.ToString());
                dataset.Add(DicomTag.Columns, columns.ToString());
                dataset.Add(DicomTag.InstanceNumber, $"{i + 1}");
                dataset.Add(DicomTag.ImagePositionPatient, @"-105.8\-230.8\" + $"-{i * 1}");    // az első kettő szám tetszőleges
                DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
                pixelData.BitsStored = 8;
                pixelData.SamplesPerPixel = 3;
                pixelData.HighBit = 7;
                pixelData.PixelRepresentation = 0;
                pixelData.PlanarConfiguration = 0;
                pixelData.AddFrame(buffer);

                DicomFile dicomfile = new DicomFile(dataset);
                dicomfile.Save(outputFilesName + i + ".dcm");
            }
        }
        private void FillDataset(DicomDataset dataset)
        {
            //type 1 attributes.
            dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
            dataset.Add(DicomTag.StudyInstanceUID, DicomUID.ComputedRadiographyImageStorage);
            dataset.Add(DicomTag.SeriesInstanceUID, DicomUID.ComputedRadiographyImageStorage);
            dataset.Add(DicomTag.SOPInstanceUID, DicomUID.ComputedRadiographyImageStorage);

            //type 2 attributes
            dataset.Add(DicomTag.PatientID, "1");
            dataset.Add(DicomTag.PatientName, $"Generated steel fiber reinforced concrete sample");
            dataset.Add(DicomTag.PatientBirthDate, "00000000");
            dataset.Add(DicomTag.PatientSex, "O");
            dataset.Add(DicomTag.StudyDate, DateTime.Now);
            dataset.Add(DicomTag.StudyTime, DateTime.Now);
            dataset.Add(DicomTag.AccessionNumber, string.Empty);
            dataset.Add(DicomTag.ReferringPhysicianName, "Inputfile generator for steel fiber reinforced concrete samples");
            dataset.Add(DicomTag.StudyID, "1");
            dataset.Add(DicomTag.SeriesNumber, "1");
            dataset.Add(DicomTag.AcquisitionNumber, "1");
            dataset.Add(DicomTag.PixelSpacing, @"0.390625\0.390625");       // a pixelek közötti távolság
            //dataset.Add(DicomTag.ModalitiesInStudy, "CR");
            dataset.Add(DicomTag.Modality, "CT");
            dataset.Add(DicomTag.NumberOfStudyRelatedInstances, "1");
            dataset.Add(DicomTag.NumberOfStudyRelatedSeries, "1");
            dataset.Add(DicomTag.NumberOfSeriesRelatedInstances, "1");
            dataset.Add(DicomTag.ImageLaterality, "U");
            dataset.Add(DicomTag.SeriesDescription, "Steel fiber reinforced concrete");
            dataset.Add(DicomTag.InstitutionName, "BME-VIK AUT");
            dataset.Add(DicomTag.BitsAllocated, "16");
            dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
        }

        private Bitmap GetValidImage(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Bitmap old = bitmap;
                using (old)
                {
                    bitmap = new Bitmap(old.Width, old.Height, PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.DrawImage(old, 0, 0, old.Width, old.Height);
                    }
                }
            }
            return bitmap;
        }
        private byte[] GetPixels(Bitmap image, out int rows, out int columns)
        {
            rows = image.Height;
            columns = image.Width;

            if (rows % 2 != 0 && columns % 2 != 0)
                --columns;

            BitmapData data = image.LockBits(new Rectangle(0, 0, columns, rows), ImageLockMode.ReadOnly, image.PixelFormat);
            IntPtr bmpData = data.Scan0;
            try
            {
                int stride = columns * 3;
                int size = rows * stride;
                byte[] pixelData = new byte[size];
                for (int i = 0; i < rows; ++i)
                    Marshal.Copy(new IntPtr(bmpData.ToInt64() + i * data.Stride), pixelData, i * stride, stride);

                //swap BGR to RGB
                BlueRedSwap(pixelData);
                return pixelData;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
        private void BlueRedSwap(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 3)
            {
                byte temp = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = temp;
            }
        }
    }
}
