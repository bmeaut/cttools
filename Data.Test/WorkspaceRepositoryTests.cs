using Core.Workspaces;
using Data.Repositories;
using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;

namespace Data.Test
{
    public class WorkspaceRepositoryTests
    {
        [Fact]
        public void GetAllAsync_ReturnsAllEntities()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceList = dbContext.Workspaces.ToList();
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var result = workspaceRepository.GetAllAsync().Result;

                result.Should().HaveCount(3);
                result.Should().Equal(workspaceList);
            }
        }

        [Fact]
        public async void AddAsync_AddsNewEntity()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var workspace = new Workspace
                {
                    Name = "test3",
                    Description = "Test workspace 3",
                };
                await workspaceRepository.AddAsync(workspace);
                dbContext.SaveChanges();

                var results = dbContext.Workspaces.ToList();
                results.Should().HaveCount(4);
                results.Should().Contain(workspace);
            }
        }

        [Fact]
        public async void AddRangeAsync_AddsNewEntities()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var workspaces = new List<Workspace>
                {
                    new Workspace
                    {
                        Name = "test3",
                        Description = "Test workspace 3",
                    },
                    new Workspace
                    {
                        Name = "test4",
                        Description = "Test workspace 4",
                    }
                };
                await workspaceRepository.AddRangeAsync(workspaces);
                dbContext.SaveChanges();

                var results = dbContext.Workspaces.ToList();
                results.Should().HaveCount(5);
                results.Should().Contain(workspaces);
            }
        }

        [Fact]
        public void Remove_RemovesEntity()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var workspace = dbContext.Workspaces.First();
                workspaceRepository.Remove(workspace);
                dbContext.SaveChanges();

                var results = dbContext.Workspaces.ToList();
                results.Should().HaveCount(2);
                results.Should().NotContain(workspace);
            }
        }

        [Fact]
        public void RemoveRange_RemovesEntities()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var workspaces = dbContext.Workspaces.ToList();
                workspaceRepository.RemoveRange(workspaces.GetRange(0, 2));
                dbContext.SaveChanges();

                var results = dbContext.Workspaces.ToList();
                results.Should().HaveCount(1);
                results.Should().Contain(workspaces[2]);
            }
        }

        [Fact]
        public void SingleOrDefaultAsync_ReturnsSingleCorrectEntity()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var workspaces = dbContext.Workspaces.ToList();
                var result = workspaceRepository.SingleOrDefaultAsync(w => w.Name == workspaces[0].Name).Result;

                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(workspaces[0]);
            }
        }

        [Fact]
        public async void SingleOrDefaultAsync_ThrowsInvalidOperationException()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);

                Func<Task> act = async ()
                    => await workspaceRepository.SingleOrDefaultAsync(w => w.Name.Contains("test"));
                await act.Should().ThrowAsync<InvalidOperationException>();
            }
        }

        [Fact]
        public async void SingleOrDefaultAsync_ThrowsArgumentNullException()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);

                Func<Task> act = async ()
                    => await workspaceRepository.SingleOrDefaultAsync(null);
                await act.Should().ThrowAsync<ArgumentNullException>();
            }
        }

        [Fact]
        public void SingleOrDefaultAsync_ReturnsNull()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var result = workspaceRepository.SingleOrDefaultAsync(w => w.Name == "not in list").Result;

                result.Should().BeNull();
            }
        }

        [Fact]
        public void Where_ReturnsSingleCorrectEntity()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var workspaces = dbContext.Workspaces.ToList();
                var result = workspaceRepository.Where(w => w.Name == workspaces[0].Name).ToList();

                result.Should().HaveCount(1);
                result.Should().Contain(workspaces[0]);
            }
        }

        [Fact]
        public void Where_ReturnsAllCorrectEntities()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var workspaces = dbContext.Workspaces.ToList().GetRange(0, 2);
                var result = workspaceRepository.Where(w => w.Name.Contains("test")).ToList();

                result.Should().HaveCount(2);
                result.Should().Contain(workspaces);
            }
        }

        [Fact]
        public void Where_ReturnsEmptyList()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);
                var result = workspaceRepository.Where(w => w.Name == "not in list").ToList();

                result.Should().BeEmpty();
            }
        }

        [Fact]
        public void Where_ThrowsArgumentNullException()
        {
            using (var dbContext = new ApplicationDbContextFactory().CreateContext())
            {
                var workspaceRepository = new Repository<Workspace>(dbContext);

                Action act = () => workspaceRepository.Where(null);

                act.Should().Throw<ArgumentNullException>();
            }
        }
    }
}
