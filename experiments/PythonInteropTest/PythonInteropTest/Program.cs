using Python.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PythonInteropTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Python.NET fork supporting .net standard 2.0: https://github.com/henon/pythonnet_netstandard
            var pythonInstallDir = @"C:\Python\Python38";
            var pythonDLLPath = Path.Combine(pythonInstallDir, "python38.dll");

            string path = pythonInstallDir + ";" + Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDLLPath, EnvironmentVariableTarget.Process);

            using (Py.GIL())
            {
                using (Py.CreateScope("add_operation"))
                {
                    new AddOperationPlugin().Run();
                }

                using (Py.CreateScope("auto_venv"))
                {
                    new AutoVenvPlugin().Run();
                }
            }

            //var pluginName = "auto_venv";

            ////Set `virtualEnvPath` to `pythonInstallDir` to avoid using a virutal environmnet
            //var pluginDir = Path.Combine(@"C:\Users\BorosIstván\source\repos\PythonInteropTest\PythonInteropTest\Plugins", pluginName);
            //var virtualEnvPath = Path.Combine(pluginDir, "env");
            //var requirementsPath = Path.Combine(pluginDir, "requirements.txt");

            //Environment.SetEnvironmentVariable("PYTHONHOME", virtualEnvPath, EnvironmentVariableTarget.Process);

            //var lib = new[]
            //{
            //    pluginDir,
            //    Path.Combine(pythonInstallDir, "Lib"),
            //    Path.Combine(pythonInstallDir, "DLLs"),
            //    Path.Combine(virtualEnvPath, "Lib"),
            //    Path.Combine(virtualEnvPath, @"Lib\site-packages"),
            //};
            //string paths = string.Join(";", lib);
            //Environment.SetEnvironmentVariable("PYTHONPATH", paths, EnvironmentVariableTarget.Process);

            //#region virtual environment setup and update
            //// Check presence of virtual environment, if not found create it
            ////if (!File.Exists(Path.Combine(virtualEnvPath, "Scripts", "activate")))
            ////{
            ////    Console.WriteLine("Creating venv");
            ////    using (Py.GIL())
            ////    {
            ////        dynamic sys = Py.Import("sys");
            ////        dynamic subprocess = Py.Import("subprocess");
            ////        subprocess.run($"python -m venv { virtualEnvPath }");
            ////        subprocess.run($"{ Path.Combine(virtualEnvPath, "Scripts", "python") } -m pip install --upgrade -r { requirementsPath }");
            ////    }
            ////    //return;
            ////}
            ////else
            ////{
            ////    Console.WriteLine("Updating packages");
            ////    //using (Py.GIL())
            ////    //{
            ////    //    dynamic sys = Py.Import("sys");
            ////    //    dynamic subprocess = Py.Import("subprocess");
            ////    //    subprocess.run($"{ Path.Combine(virtualEnvPath, "Scripts", "python") } -m pip install --upgrade -r { requirementsPath }");
            ////    //}
            ////}
            //#endregion

            //PythonEngine.PythonHome = virtualEnvPath;
            //PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);

            //using (Py.GIL())
            //{
            //    Console.WriteLine(PythonEngine.Version);
            //    Console.WriteLine(PythonEngine.IsInitialized);
            //    Console.WriteLine(PythonEngine.PythonPath);

            //    dynamic plugin = Py.Import("plugin");

            //    var input = new Test
            //    {
            //        A = 1,
            //        B = 2,
            //        C = 3,
            //        D = 4,
            //        Text = "Test text"
            //    };

            //    var res = plugin.run(input);
            //    Console.WriteLine(res.GetType());
            //    Console.WriteLine(res);

            //}
        }

        class Test
        {
            public int A { get; set; }
            public int B { get; set; }
            public int C { get; set; }
            public int D { get; set; }
            public string Text { get; set; }
        }

        interface IPlugin
        {
            void Run();
        }

        class AddOperationPlugin : IPlugin
        {
            static string pythonInstallDir = @"C:\Python\Python38";
            static string pythonDLLPath = Path.Combine(pythonInstallDir, "python38.dll");

            static string pluginName = "add_operation";

            //Set `virtualEnvPath` to `pythonInstallDir` to avoid using a virutal environmnet
            static string pluginDir = Path.Combine(@"C:\Users\BorosIstván\source\repos\PythonInteropTest\PythonInteropTest\Plugins", pluginName);
            static string virtualEnvPath = Path.Combine(pluginDir, "env");
            static string requirementsPath = Path.Combine(pluginDir, "requirements.txt");

            static string[] lib = new string[]
            {
                pluginDir,
                Path.Combine(pythonInstallDir, "Lib"),
                Path.Combine(pythonInstallDir, "DLLs"),
                Path.Combine(virtualEnvPath, "Lib"),
                Path.Combine(virtualEnvPath, @"Lib\site-packages"),
            };
            static string paths = string.Join(";", lib);

            public AddOperationPlugin()
            {
                Environment.SetEnvironmentVariable("PYTHONHOME", virtualEnvPath, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONPATH", paths, EnvironmentVariableTarget.Process);
                PythonEngine.PythonHome = virtualEnvPath;
                PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
            }
            public void Run()
            {
                using (Py.GIL())
                {
                    Console.WriteLine(PythonEngine.Version);
                    Console.WriteLine(PythonEngine.IsInitialized);
                    Console.WriteLine(PythonEngine.PythonPath);

                    dynamic plugin = Py.Import("plugin");

                    var input = new Test
                    {
                        A = 1,
                        B = 2,
                        C = 3,
                        D = 4,
                        Text = "Test text 2"
                    };

                    var res = plugin.run(input);
                    Console.WriteLine(res.GetType());
                    Console.WriteLine(res);

                }
            }
        }

        class AutoVenvPlugin : IPlugin
        {
            static string pythonInstallDir = @"C:\Python\Python38";
            static string pythonDLLPath = Path.Combine(pythonInstallDir, "python38.dll");

            static string pluginName = "auto_venv";

            //Set `virtualEnvPath` to `pythonInstallDir` to avoid using a virutal environmnet
            static string pluginDir = Path.Combine(@"C:\Users\BorosIstván\source\repos\PythonInteropTest\PythonInteropTest\Plugins", pluginName);
            static string virtualEnvPath = Path.Combine(pluginDir, "env");
            static string requirementsPath = Path.Combine(pluginDir, "requirements.txt");

            static string[] lib = new string[]
            {
                pluginDir,
                Path.Combine(pythonInstallDir, "Lib"),
                Path.Combine(pythonInstallDir, "DLLs"),
                Path.Combine(virtualEnvPath, "Lib"),
                Path.Combine(virtualEnvPath, @"Lib\site-packages"),
            };
            static string paths = string.Join(";", lib);

            public AutoVenvPlugin()
            {
                Environment.SetEnvironmentVariable("PYTHONHOME", virtualEnvPath, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("PYTHONPATH", paths, EnvironmentVariableTarget.Process);
                PythonEngine.PythonHome = virtualEnvPath;
                PythonEngine.PythonPath = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.Process);
            }
            public void Run()
            {
                using (Py.GIL())
                {
                    Console.WriteLine(PythonEngine.Version);
                    Console.WriteLine(PythonEngine.IsInitialized);
                    Console.WriteLine(PythonEngine.PythonPath);

                    dynamic plugin = Py.Import("plugin");

                    var input = new Test
                    {
                        A = 1,
                        B = 2,
                        C = 3,
                        D = 4,
                        Text = "Test text 2"
                    };

                    var res = plugin.run(input);
                    Console.WriteLine(res.GetType());
                    Console.WriteLine(res);

                }
            }
        }
    }
}

