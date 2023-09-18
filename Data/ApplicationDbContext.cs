using Core;
using Core.Image;
using Core.Interfaces.Operation;
using Core.Interfaces.Workspaces;
using Core.Operation;
using Core.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.All,
        };

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Ignore<BaseEntity>();

            // Workspace
            builder.Entity<Workspace>()
                .Property(w => w.Name)
                .IsRequired(true);

            // Workspace - Status relationship
            builder.Entity<Workspace>()
                .HasMany(w => w.Statuses)
                .WithOne(s => s.Workspace)
                .HasForeignKey(s => s.WorkspaceId);
            builder.Entity<Workspace>()
                .HasOne(w => w.CurrentStatus)
                .WithOne()
                .HasForeignKey(typeof(Workspace), nameof(Workspace.CurrentStatusId))
                .IsRequired(false);

            // MaterialSample
            builder.Entity<MaterialSample>()
                .Property(ms => ms.Label)
                .IsRequired(true);
            builder.Entity<MaterialSample>()
                .Ignore(ms => ms.RawImages);
            builder.Entity<MaterialSample>()
                .Property(ms => ms.DicomLevel);
            builder.Entity<MaterialSample>()
                .Property(ms => ms.DicomRange);

            // MaterialSample - Status relationship
            builder.Entity<MaterialSample>()
                .HasMany(ms => ms.Statuses)
                .WithOne(s => s.MaterialSample)
                .HasForeignKey(s => s.MaterialSampleId);
            builder.Entity<MaterialSample>()
                .HasOne(ms => ms.CurrentStatus)
                .WithOne()
                .HasForeignKey(typeof(MaterialSample), nameof(MaterialSample.CurrentStatusId))
                .IsRequired(false);

            // MaterialScan
            builder.Entity<MaterialScan>()
                .Property(ms => ms.ScanFileFormat)
                .IsRequired(true);

            // Measurement
            builder.Entity<Measurement>()
                .Property(m => m.Name)
                .IsRequired(true);
            builder.Entity<Measurement>()
                .Ignore(m => m.BlobImages);

            // Measurement - BlobImageEntity
            builder.Ignore<BlobImageEntity>();
            builder.Entity<Measurement>().Property(m => m.BlobImageEntities).HasConversion(
                b => JsonConvert.SerializeObject(b, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                b => JsonConvert.DeserializeObject<IEnumerable<BlobImageEntity>>(b, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            );

            // Status
            builder.Entity<Status>()
                .Property(s => s.Name)
                .IsRequired(true);

            // ScanFile
            builder.Entity<ScanFile>()
                .Property(sf => sf.FilePath)
                .IsRequired(true);

            // UserGeneratedFile
            builder.Entity<UserGeneratedFile>()
                .Property(ugf => ugf.Path)
                .IsRequired(true);

            // OperationContext
            builder.Entity<OperationContext>()
                .Property(oc => oc.OperationName)
                .IsRequired(true);
            builder.Entity<OperationContext>()
                .Ignore(oc => oc.Artifacts);
            builder.Entity<OperationContext>()
                .Ignore(oc => oc.OperationRunEventArgs);
            builder.Entity<OperationContext>()
                .Ignore(oc => oc.BlobImages);
            builder.Entity<OperationContext>()
                .Ignore(oc => oc.RawImages);
            builder.Entity<OperationContext>()
                .Ignore(oc => oc.RawImageMetadata);

            // OperationContext - OperationProperties
            builder.Ignore<OperationProperties>();
            builder.Entity<OperationContext>().Property(oc => oc.OperationProperties).HasConversion(
                op => JsonConvert.SerializeObject(op, jsonSerializerSettings),
                op => JsonConvert.DeserializeObject<OperationProperties>(op, jsonSerializerSettings)
            );

            // OperationContext - InternalOutputs
            builder.Entity<OperationContext>().Property(oc => oc.InternalOutputs).HasConversion(
                op => JsonConvert.SerializeObject(op, jsonSerializerSettings),
                op => JsonConvert.DeserializeObject<Dictionary<string, InternalOutput>>(op, jsonSerializerSettings)
            );

            base.OnModelCreating(builder);
        }

        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<MaterialSample> MaterialSamples { get; set; }
        public DbSet<MaterialScan> MaterialScans { get; set; }
        public DbSet<ScanFile> ScanFiles { get; set; }
        public DbSet<Measurement> Measurements { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<SessionContext> SessionContexts { get; set; }
        public DbSet<UserGeneratedFile> UserGeneratedFiles { get; set; }
        public DbSet<OperationContext> OperationContexts { get; set; }
    }
}
