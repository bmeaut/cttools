using Core.Services;
using Xunit;

namespace Service.Test
{
    public class AutoMapperTest
    {
        [Fact]
        public void TestMappingConfiguration()
        {
            DtoMapperConfiguration.Configuration.AssertConfigurationIsValid();
        }
    }
}