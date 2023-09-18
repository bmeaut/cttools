using Data;
using Data.Test;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Test
{
    public abstract class ServiceTestBase : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        protected readonly UnitOfWork _unitOfWork;

        public ServiceTestBase()
        {
            _dbContext = new ApplicationDbContextFactory().CreateContext();
            _unitOfWork = new UnitOfWork(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
