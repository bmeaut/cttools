using AutoMapper;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services.Dto;
using Core.Services.Dto.Json;
using Core.Workspaces;
using System.Linq;

namespace Core.Services
{
    public static class DtoMapperConfiguration
    {
        public static MapperConfiguration Configuration = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MaterialSample, MaterialSampleDto>();
            cfg.CreateMap<Measurement, MeasurementDto>();
            cfg.CreateMap<IOperation, OperationDto>();
            cfg.CreateMap<Workspace, WorkspaceDto>()
                .ForMember(w => w.HasSavedSession, opt => opt.MapFrom<SessionContextResolver>());
            cfg.CreateMap<IHistoryStep, HistoryStepDto>();
            cfg.CreateMap<Status, StatusDto>();
            cfg.CreateMap<MaterialScan, MaterialScanDto>()
                .ForMember(ms => ms.ScanFilePaths, opt => opt.MapFrom<ScanFilesResolver>());
            cfg.CreateMap<UserGeneratedFile, UserGeneratedFileDto>();
            cfg.CreateMap<UserGeneratedFileDto, UserGeneratedFile>()
                .ForMember(ugf => ugf.MaterialSampleId, opt => opt.Ignore())
                .ForMember(ugf => ugf.MaterialSample, opt => opt.Ignore())
                .ForMember(ugf => ugf.CreatedAt, opt => opt.Ignore())
                .ForMember(ugf => ugf.UpdatedAt, opt => opt.Ignore());

            //JSON mapping
            cfg.CreateMap<Workspace, WorkspaceJson>();
            cfg.CreateMap<MaterialSample, MaterialSampleJson>();
            cfg.CreateMap<UserGeneratedFile, UserGeneratedFileJson>();
            cfg.CreateMap<MaterialScan, MaterialScanJson>();
            cfg.CreateMap<ScanFile, ScanFileJson>();
            cfg.CreateMap<Status, StatusJson>();
            cfg.CreateMap<Measurement, MeasurementJson>();
            cfg.CreateMap<OperationContext, OperationContextJson>();

        });
    }

    class ScanFilesResolver : IValueResolver<MaterialScan, MaterialScanDto, string[]>
    {
        public string[] Resolve(MaterialScan source, MaterialScanDto destination, string[] destMember, ResolutionContext context)
        {
            return source.ScanFiles.Select(sf => sf.FilePath).ToArray();
        }
    }

    class SessionContextResolver : IValueResolver<Workspace, WorkspaceDto, bool>
    {
        public bool Resolve(Workspace source, WorkspaceDto destination, bool destMember, ResolutionContext context)
        {
            return source.SessionContext != null;
        }
    }
}