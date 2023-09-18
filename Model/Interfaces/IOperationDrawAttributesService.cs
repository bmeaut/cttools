using Core.Interfaces.Operation;
using Core.Services.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public enum DrawStyle
    {
        Ink,
        Ellipsis,
        Rectangle
    }
    public interface IOperationDrawAttributesService
    {
        // public DrawStyle GetDrawAttribute(OperationDto operation, OperationProperties properties);
        public bool DrawRunEnabled(OperationDto operation, OperationProperties properties);
    }
}
