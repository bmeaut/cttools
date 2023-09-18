using Cv4s.Common.Interfaces;

namespace Cv4s.Common.Services
{
    public class TestService : ITestService
    {
        public void Log()
        {
            Console.WriteLine($"{nameof(TestService)} is called");
        }
    }
}