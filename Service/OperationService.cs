using AutoMapper;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Operation;
using Core.Operation;
using Core.Services;
using Core.Services.Dto;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Service
{
    public class OperationService : IOperationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly Dictionary<string, IOperation> _operations = new Dictionary<string, IOperation>();
        private double _runningOperationProgress = 1;
        public double RunningOperationProgress { get => _runningOperationProgress; set => _runningOperationProgress = value; }

        public OperationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void RegisterOperation(IOperation operation)
        {
            if (_operations.ContainsKey(operation.Name))
            {
                throw new OperationWithNameAlreadyRegisteredException(operation.Name);
            }

            _operations.Add(operation.Name, operation);
        }

        public IEnumerable<IOperation> GetAll()
        {
            var operations = _operations.Values;
            return operations;
        }

        public async Task<OperationContext> RunOperationAsync(OperationContext context, CancellationToken token, IProgress<double> progress)
        {
            var operation = GetOperation(context.OperationName);

            _runningOperationProgress = 0;

            var result = await operation.Run(context, progress, token);
            _runningOperationProgress = 1;

            if (token.IsCancellationRequested) return result;

            await AddOperationContextAsync(result);

            return result;
        }

        public async Task AddOperationContextAsync(OperationContext context)
        {
            await _unitOfWork.OperationContexts.AddAsync(context);
        }

        public void ParseOperations(string externalPluginPath)
        {
            // The method parameter points to the root directory, which contains Matlab and Python operations
            // Each operation resides in it's own folder and should define a few information about itself.
            //      - input parameters of the operation --> operation_properties.json
            //      - basic information about the operation, ex. Name, Creator, Version --> operation_info.json
            //      - matlab function containing the logic --> run.m returning JSON encoded OperationContext
            var matlabPlugins = Path.Combine(externalPluginPath, "Matlab");

            foreach (var dir in Directory.EnumerateDirectories(matlabPlugins))
            {
                string[] files = Directory.GetFiles(dir);
                string[] neededFiles = new string[] {
                    Path.Combine(dir, "operation_properties.json"),
                    Path.Combine(dir, "operation_info.json"),
                    Path.Combine(dir, "run.m"),
                };


                if (neededFiles.All(nf => files.Contains(nf)))
                {
                    var op = this.CreateMatlabOperation(dir);
                    this.RegisterOperation(op);
                    Debug.WriteLine("create matlab operation");
                }
                else
                {
                    continue; // needed files not present, do not load plugin
                }

            }
        }

        private class OperationInfo
        {
            public string Name { get; set; }
            public string Creator { get; set; }
            public string Version { get; set; }
        }

        private IOperation CreateMatlabOperation(string operationPath)
        {
            var op_info = JsonConvert.DeserializeObject<OperationInfo>(File.ReadAllText(Path.Combine(operationPath, "operation_info.json")));
            var className = $"{ op_info.Name.Replace(" ", String.Empty) }Properties";

            var assemblyName = new AssemblyName(className);
            var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var dynamicModule = dynamicAssembly.DefineDynamicModule("Main");

            var dynamicType = dynamicModule.DefineType(className,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout,
                typeof(OperationProperties));

            dynamicType.DefineDefaultConstructor(MethodAttributes.Public |
                                    MethodAttributes.SpecialName |
                                    MethodAttributes.RTSpecialName);


            var op_properties = JsonConvert.DeserializeObject<List<dynamic>>(File.ReadAllText(Path.Combine(operationPath, "operation_properties.json")));
            foreach (var prop in op_properties)
            {
                if (!(prop.ContainsKey("name") && prop.ContainsKey("type")))
                {
                    Debug.WriteLine("Can't parse operation properties"); // TODO: should throw an exception
                }
                var opName = prop.name.Value as string;
                var opType = prop.type.Value as string;

                Type propertyType = typeof(object);

                switch (opType) // TODO: maybe add bool or other type
                {
                    case "int":
                        propertyType = typeof(int);
                        break;
                    case "float":
                        propertyType = typeof(double);
                        break;
                    case "string":
                        propertyType = typeof(string);
                        break;
                    default:
                        break;
                }

                AddProperty(dynamicType, opName, propertyType);
            }

            Type propType = dynamicType.CreateType();

            var op = new MatlabOperation();
            op.Name = op_info.Name;
            op.OperationPath = operationPath;
            op.DefaultOperationProperties = (OperationProperties)Activator.CreateInstance(propType);
            return op;
        }

        private IOperation CreatePythonOperation()
        {
            throw new NotImplementedException();
        }

        private static void AddProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            var getMethod = typeBuilder.DefineMethod("get_" + propertyName,
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            var getMethodIL = getMethod.GetILGenerator();
            getMethodIL.Emit(OpCodes.Ldarg_0);
            getMethodIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getMethodIL.Emit(OpCodes.Ret);

            var setMethod = typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });
            var setMethodIL = setMethod.GetILGenerator();
            Label modifyProperty = setMethodIL.DefineLabel();
            Label exitSet = setMethodIL.DefineLabel();

            setMethodIL.MarkLabel(modifyProperty);
            setMethodIL.Emit(OpCodes.Ldarg_0);
            setMethodIL.Emit(OpCodes.Ldarg_1);
            setMethodIL.Emit(OpCodes.Stfld, fieldBuilder);
            setMethodIL.Emit(OpCodes.Nop);
            setMethodIL.MarkLabel(exitSet);
            setMethodIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethod);
            propertyBuilder.SetSetMethod(setMethod);
        }

        public OperationType GetOperationType(string operationName)
        {
            if (_operations.ContainsKey(operationName))
                return _operations[operationName].OperationType;
            throw new OperationWithNameNotFoundException(operationName);
        }

        private IOperation GetOperation(string operationName)
        {
            if (!_operations.ContainsKey(operationName))
                throw new OperationWithNameNotFoundException(operationName);
            return _operations[operationName];
        }

        public bool IsOperationCallableFromCanvas(string operationName, OperationProperties operationProperties)
        {
            var operation = GetOperation(operationName);
            return operation.IsCallableFromCanvas(operationProperties);
        }

        public bool IsOperationCallableFromButton(string operationName, OperationProperties operationProperties)
        {
            var operation = GetOperation(operationName);
            return operation.IsCallableFromButton(operationProperties);
        }
    }
}
