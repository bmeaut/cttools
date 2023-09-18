using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Operation;
using Core.Workspaces;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        private IWorkspaceRepository _workspaceRepository;
        private IRepository<MaterialSample> _materialSampleRepository;
        private IRepository<MaterialScan> _materialScanRepository;
        private IRepository<ScanFile> _scanFileRepository;
        private IRepository<Measurement> _measurementRepository;
        private IRepository<Status> _statusRepository;
        private IRepository<SessionContext> _sessionContextRepository;
        private IRepository<UserGeneratedFile> _userGeneratedFileRepository;
        private IRepository<OperationContext> _operationContextRepository;

        //public UnitOfWork(IServiceScopeFactory factory)
        //{
        //    _context = factory.CreateScope().ServiceProvider
        //                .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
        //                .CreateDbContext();
        //}

        //TODO: DbContextFactory
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        // If no special functions are needed we can use the default repository implementation.
        public IWorkspaceRepository Workspaces => _workspaceRepository ??= new WorkspaceRepository(_context);
        public IRepository<MaterialSample> MaterialSamples => _materialSampleRepository ??= new MaterialSampleRepository(_context);
        public IRepository<MaterialScan> MaterialScans => _materialScanRepository ??= new Repository<MaterialScan>(_context);
        public IRepository<ScanFile> ScanFiles => _scanFileRepository ??= new Repository<ScanFile>(_context);
        public IRepository<Measurement> Measurements => _measurementRepository ??= new Repository<Measurement>(_context);
        public IRepository<Status> Statuses => _statusRepository ??= new Repository<Status>(_context);
        public IRepository<SessionContext> SessionContexts => _sessionContextRepository ??= new Repository<SessionContext>(_context);
        public IRepository<UserGeneratedFile> UserGeneratedFiles => _userGeneratedFileRepository ??= new Repository<UserGeneratedFile>(_context);
        public IRepository<OperationContext> OperationContexts => _operationContextRepository ??= new Repository<OperationContext>(_context);

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _context.DisposeAsync();
        }
    }
}
