using Core.Exceptions;
using Core.Image;
using Core.Interfaces;
using Core.Services;
using Core.Workspaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class SessionService : ISessionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWorkspaceService _workspaceService;

        private Workspace _currentWorkspace;
        private MaterialSample _currentMaterialSample;
        private Measurement _currentMeasurement;
        private int _currentLayerIndex = 0;

        public SessionService(
            IUnitOfWork unitOfWork,
            IWorkspaceService workspaceService)
        {
            _unitOfWork = unitOfWork;
            _workspaceService = workspaceService;
        }

        public Workspace GetCurrentWorkspace()
        {
            return _currentWorkspace;
        }

        public void SetCurrentWorkspace(Workspace workspace)
        {
            _currentWorkspace = workspace;
        }

        public MaterialSample GetCurrentMaterialSample()
        {
            return _currentMaterialSample;
        }

        public void SetCurrentMaterialSample(MaterialSample materialSample)
        {
            if (_currentWorkspace == null)
            {
                throw new NoWorkspaceOpenedException();
            }

            var materialSampleIds = _currentWorkspace.MaterialSamples.Select(ms => ms.Id);
            if (!materialSampleIds.Contains(materialSample.Id))
            {
                throw new EntityHasNoRelationWithOtherEntityException<Workspace, MaterialSample>(_currentWorkspace.Id, materialSample.Id);
            }

            if (_currentMeasurement != null)
                _currentMeasurement.MaterialSample = materialSample;
            _currentMaterialSample = materialSample;
        }

        public Measurement GetCurrentMeasurement()
        {
            return _currentMeasurement;
        }

        public void SetCurrentMeasurement(Measurement measurement)
        {
            if (_currentWorkspace == null)
            {
                throw new NoWorkspaceOpenedException();
            }
            if (_currentMaterialSample == null)
            {
                throw new NoMaterialSampleOpenedException();
            }

            var measurementIds = _currentMaterialSample.Measurements.Select(m => m.Id);
            if (!measurementIds.Contains(measurement.Id))
            {
                throw new EntityHasNoRelationWithOtherEntityException<MaterialSample, Measurement>(_currentMaterialSample.Id, measurement.Id);
            }

            _currentMeasurement = measurement;
        }

        public int GetCurrentLayer()
        {
            return _currentLayerIndex;
        }

        public void SetCurrentLayer(int layer)
        {
            if (_currentWorkspace == null)
            {
                throw new NoWorkspaceOpenedException();
            }
            if (_currentMaterialSample == null)
            {
                throw new NoMaterialSampleOpenedException();
            }
            if (_currentMeasurement == null)
            {
                throw new NoMeasurementOpenedException();
            }

            var numOfLayers = _currentMeasurement.MaterialSample.RawImages.NumberOfLayers;
            if ((layer >= numOfLayers) || (layer < 0))
            {
                throw new LayerIndexOutOfBoundsException(layer);
            }

            _currentLayerIndex = layer;
        }

        public async Task<SessionContext> SaveCurrentSessionAsync()
        {
            if (_currentWorkspace == null)
            {
                throw new NoWorkspaceOpenedException();
            }

            var sessionContext = _currentWorkspace.SessionContext;
            if (sessionContext == null)
            {
                sessionContext = new SessionContext
                {
                    CurrentLayerIndex = _currentLayerIndex,
                    CurrentMeasurement = _currentMeasurement
                };
                await _unitOfWork.SessionContexts.AddAsync(sessionContext);
            }
            else
            {
                sessionContext.CurrentLayerIndex = _currentLayerIndex;
                sessionContext.CurrentMeasurement = _currentMeasurement;
                _unitOfWork.SessionContexts.Update(sessionContext);
                await _unitOfWork.CommitAsync();
            }

            _currentWorkspace.SessionContext = sessionContext;
            await _workspaceService.UpdateWorkspaceAsync(_currentWorkspace);

            return sessionContext;
        }
    }
}
