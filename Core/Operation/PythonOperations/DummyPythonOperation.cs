using Core.Interfaces.Operation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Operation
{
    public class DummyPythonOperation : PythonOperationBase
    {
        public override string Name => "DummyPythonOperation";
        protected override string Endpoint => "/runDemoOperation";

        public override OperationProperties DefaultOperationProperties => new EmptyOperationProperties();
    }
}
