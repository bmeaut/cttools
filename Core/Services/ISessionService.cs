using Core.Image;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface ISessionService
    {
        public Workspace GetCurrentWorkspace();

        public void SetCurrentWorkspace(Workspace workspace);

        public MaterialSample GetCurrentMaterialSample();

        public void SetCurrentMaterialSample(MaterialSample materialSample);

        public Measurement GetCurrentMeasurement();

        public void SetCurrentMeasurement(Measurement measurement);

        public int GetCurrentLayer();

        public void SetCurrentLayer(int layer);

        public Task<SessionContext> SaveCurrentSessionAsync();
    }
}
