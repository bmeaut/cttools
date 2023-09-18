using Core.Image;
using Core.Interfaces.Image;
using Core.Workspaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Text;

namespace Data.Test
{
    public class ApplicationDbContextFactory : IDisposable
    {
        private DbConnection _connection;

        private DbContextOptions<ApplicationDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection).Options;
        }

        public ApplicationDbContext CreateContext()
        {
            ApplicationDbContext context = null;
            if (_connection == null)
            {
                _connection = new SqliteConnection("DataSource=:memory:;Cache=Shared;");
                _connection.Open();

                var options = CreateOptions();
                context = new ApplicationDbContext(options);
                context.Database.EnsureCreated();
            }

            if (context == null)
            {
                context = new ApplicationDbContext(CreateOptions());
            }
            Seed(context);

            return context;
        }

        public void Seed(ApplicationDbContext context)
        {
            var workspaces = new List<Workspace>
            {
                new Workspace
                {
                    Name = "test0",
                    Description = "Test workspace 0"
                },
                new Workspace
                {
                    Name = "test1",
                    Description = "Test workspace 1",
                },
                new Workspace
                {
                    Name = "workspace2",
                    Description = "Test workspace 2",
                }
            };
            context.Workspaces.AddRange(workspaces);

            var materialScans = new List<MaterialScan>
            {
                new MaterialScan(),
                new MaterialScan()
            };

            var materialSamples = new List<MaterialSample>
            {
                new MaterialSample
                {
                    Label = "Test material 1",
                    Workspace = workspaces[0],
                    MaterialScan = materialScans[0],
                    RawImages = new TestRawImageSource()
                },
                new MaterialSample
                {
                    Label = "Test material 2",
                    Workspace = workspaces[1],
                    MaterialScan = materialScans[1],
                    RawImages = new TestRawImageSource()
                },
                new MaterialSample
                {
                    Label = "Test material 3",
                    Workspace = workspaces[2],
                    RawImages = new TestRawImageSource()
                }
            };
            context.MaterialScans.AddRange(materialScans);
            context.MaterialSamples.AddRange(materialSamples);

            var measurements = new List<Measurement>
            {
                new Measurement
                {
                    Name = "Test measurement 1",
                    MaterialSample = materialSamples[0],
                    BlobImages = new TestBlobImageSource()
                },
                new Measurement
                {
                    Name = "Test measurement 2",
                    MaterialSample = materialSamples[0],
                    BlobImages = new TestBlobImageSource()
                },
                new Measurement
                {
                    Name = "Test measurement 3",
                    MaterialSample = materialSamples[1],
                    BlobImages = new TestBlobImageSource()
                },
                new Measurement
                {
                    Name = "Test measurement 4",
                    MaterialSample = materialSamples[2],
                    BlobImages = new TestBlobImageSource()
                }
            };
            context.Measurements.AddRange(measurements);
            context.SaveChanges();

            var workspaceStatuses = new List<Status>
            {
                new Status
                {
                    Name = "Created",
                    Workspace = workspaces[0]
                },
                new Status
                {
                    Name =  "Updated",
                    Workspace = workspaces[0]
                }
            };
            context.Statuses.AddRange(workspaceStatuses);
            workspaces[0].CurrentStatus = workspaceStatuses[^1];

            var materialSampleStatuses = new List<Status>
            {
                new Status
                {
                    Name = "Started",
                    MaterialSample = materialSamples[0]
                },
                new Status
                {
                    Name = "Finished",
                    MaterialSample = materialSamples[0]
                }
            };
            context.Statuses.AddRange(materialSampleStatuses);
            materialSamples[0].CurrentStatus = materialSampleStatuses[^1];
            context.SaveChanges();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        private class TestRawImageSource : IRawImageSource
        {
            public Bitmap this[int idx] => new Bitmap(ImageWidth, ImageHeight);

            public int NumberOfLayers => 10;

            public int ImageWidth => 100;

            public int ImageHeight => 100;


            public double XResolution { get; set; } = 100;
            public double YResolution { get; set; } = 100;
            public double ZResolution { get; set; } = 1;
            public int DicomLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public int DicomRange { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public int GetDicomPixelValue(int x, int y, int z)
            {
                throw new NotImplementedException();
            }

            public IDictionary<int, Bitmap> ToDictionary()
            {
                return new Dictionary<int, Bitmap>();
            }
        }

        private class TestBlobImageSource : IBlobImageSource
        {
            public BlobImage this[int idx] => new BlobImage(100, 100);

            public IDictionary<int, BlobImage> ToDictionary()
            {
                return new Dictionary<int, BlobImage>
                {
                    { 0, this[0] }
                };
            }

            public IEnumerable<BlobImageEntity> GetBlobImageEntities()
            {
                return new List<BlobImageEntity>
                {
                    new BlobImageEntity
                    {
                        LayerIndex = 0,
                        Image = new int[100,100],
                        Tags = new Dictionary<int, List<Tag>>()
                    }
                };
            }
        }
    }
}
