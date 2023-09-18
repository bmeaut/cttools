using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    class OperationDrawAttributes : IOperationDrawAttributesService
    {
        public bool DrawRunEnabled(OperationDto operation, OperationProperties properties)
        {
            throw new NotImplementedException();
        }

        // Deprecated
        public DrawStyle GetDrawAttribute(OperationDto operation, OperationProperties properties)
        {
            try
            {
                return DummyDrawStyleStrategy(operation, properties);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception while getting drawattributes.\n{e}");
            }
            return DrawStyle.Ink;
        }

        private DrawStyle DummyDrawStyleStrategy(OperationDto operation, OperationProperties properties)
        {
            if (operation.Name == "Manual edit")
            {
                var props = properties as ManualEditOperationProperties;
                if (props.Mode == ManualEditOperationProperties.ModeEnum.AddNew)
                    return DrawStyle.Rectangle;
                if (props.Mode == ManualEditOperationProperties.ModeEnum.Subtract)
                    return DrawStyle.Ellipsis;
            }
            return DrawStyle.Ink;
        }
    }
}
