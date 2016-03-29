﻿using OpenRnD.Harness.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenRnD.Harness.IISExpress
{
    public class IISExpressHarness : IDisposable
    {
        public string ProjectPath { get; }

        public int ServerPort { get; }
        public Process ServerProcess { get; }


        public IISExpressHarness(string projectPath, int serverPort)
        {
            string fullName = new DirectoryInfo(Path.Combine(projectPath, "Web.config")).FullName;

            if (!File.Exists(fullName))
            {
                throw new Exception($"No Web.config file found in the project path directory ({fullName}).");
            }

            if (serverPort < 1 || serverPort > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException($"The server port is invalid.");
            }

            ProjectPath = projectPath;
            ServerPort = serverPort;

            PIDUtilities.KillByPID("pid.txt", "iisexpress");
            ProcessStartInfo startInfo = CreateServerStartInfo();
            ServerProcess = StartServer(startInfo);
            PIDUtilities.StorePID("pid.txt", ServerProcess.Id);
        }

        private ProcessStartInfo CreateServerStartInfo()
        {
            string iisExpressPath = GetIISExpressPath(IISExpressBitness.x86);
            CheckForIISExpressPath(iisExpressPath);

            string testAssemblyLocationPath = Assembly.GetExecutingAssembly().Location;
            string testAssemblyProjectPath = Path.GetDirectoryName(testAssemblyLocationPath);
            string targetProjectPath = Path.Combine(testAssemblyProjectPath, ProjectPath);
            string targetProjectPathNormalized = new DirectoryInfo(targetProjectPath).FullName;
            string iisExpressArguments = string.Format(@"/path:""{0}"" /port:{1}", targetProjectPathNormalized, ServerPort);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = iisExpressPath,
                Arguments = iisExpressArguments
            };

            return startInfo;
        }

        private void CheckForIISExpressPath(string iisExpressPath)
        {
            if(!File.Exists(iisExpressPath))
            {
                throw new IOException($"IIS Express path not found ({iisExpressPath}).");
            }
        }

        private Process StartServer(ProcessStartInfo startInfo)
        {
            return Process.Start(startInfo);
        }

        public void Dispose()
        {
            ProcessTerminator.Terminate(ServerProcess);
        }

        private string GetIISExpressPath(IISExpressBitness bitness)
        {
            Environment.SpecialFolder folder;

            if(bitness == IISExpressBitness.x86)
            {
                folder = Environment.SpecialFolder.ProgramFilesX86;
            }
            else if(bitness == IISExpressBitness.x64)
            {
                folder = Environment.SpecialFolder.ProgramFiles;
            }
            else
            {
                throw new ArgumentException();
            }

            string iisExpressPath = Path.Combine(Environment.GetFolderPath(folder), @"IIS Express\iisexpress.exe");

            return iisExpressPath;
        }

        const string iisExpressPath = @"IIS Express\IISExpress.exe";
    }
}
