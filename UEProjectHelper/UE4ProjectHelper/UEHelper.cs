using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell.Interop;

namespace UEProjectHelper
{
    class UEHelper
    {
        private readonly Package package;

        public static UEHelper Instance
        {
            get;
            private set;
        }

        private UEHelper(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

        }

        public static void Initialize(Package package)
        {
            if (Instance == null)
            {
                Instance = new UEHelper(package);
            }
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        public void ShowErrorMessage(string message)
        {
            string title = "UE Helper";

            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public bool HasAnySolutionOpened()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
            if (dte == null)
            {
                return false;
            }    


            return !(dte.Solution.FullName.Length == 0 || dte.Solution.FullName == null);
        }

        public bool IsUEGameSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return File.Exists(GetUProjectFileName());
        }

        public string GetUProjectFileName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
            if(dte == null)
            {
                return String.Empty;
            }

            string uprojectFileName = dte.Solution.FullName.Replace(".sln", ".uproject");
            return uprojectFileName;
        }

        public string GetSolutionDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
            if (dte == null)
            {
                return String.Empty;
            }

            return Path.GetDirectoryName(dte.Solution.FullName);
        }

        public string GetGameProjectName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return Path.GetFileNameWithoutExtension(GetUProjectFileName());
        }

        public string GetGameProjectRootDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string uprojectFileName = GetUProjectFileName();
            return Path.GetDirectoryName(uprojectFileName);
        }

        public void RegenerateGameSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            RegistryKey targetKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Unreal.ProjectFile\shell\rungenproj\command");
            if (targetKey.GetValueNames().Length != 1)
            {
                string message = string.Format(CultureInfo.CurrentCulture, "No UnrealEngineVersionSelector detected.");
                ShowErrorMessage(message);
            }
            else
            {
                string unrealVersionSelectorValueName = targetKey.GetValueNames()[0];
                string unrealVersionSelectorCommand = targetKey.GetValue(unrealVersionSelectorValueName).ToString();
                string unrealVersionSelectorFileName = unrealVersionSelectorCommand.Split(new string[] { " /projectfiles" }, StringSplitOptions.None)[0];
                string uprojectFileName = GetUProjectFileName();

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = unrealVersionSelectorFileName;
                proc.StartInfo.Arguments = "/projectfiles " + uprojectFileName;
                proc.Start();
            }

            targetKey.Close();
        }

        public bool IsUEEngineSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
            if (dte == null)
            {
                return false;
            }

            string solutionName = Path.GetFileName(dte.Solution.FileName);
            return solutionName == "UE5.sln" || solutionName == "UE.sln";
        }

        public bool IsGenerateProjectFilesScriptExists()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string GenerateProjectFilesScriptFilepath = Path.Combine(GetSolutionDirectory(), "GenerateProjectFiles.bat");
            return File.Exists(GenerateProjectFilesScriptFilepath);
        }

        public void RegenerateEngineSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!IsGenerateProjectFilesScriptExists())
            {
                string message = string.Format(CultureInfo.CurrentCulture, "This seems to be a engine solution, but the GenerateProject.bat file not exists. ");
                ShowErrorMessage(message);
                return;
            }
            else
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "GenerateProjectFiles.bat";
                proc.Start();
            }
        }

        public void ShowDebugString(String inString)
        {
            string message = string.Format(CultureInfo.CurrentCulture, "{0}", inString);
            string title = "Show Debug String";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private bool IsFileNameContainsOnlyLegalCharacters(string testName, out string outIllegalCharacters)
        {
            bool bContainsIllegalCharacters = false;
            outIllegalCharacters = "";
            
            for (int CharIdx = 0; CharIdx < testName.Length; ++CharIdx)
            {
                char targetChar = testName[CharIdx];
                if (!Char.IsLetter(targetChar) && targetChar != '_' && !Char.IsNumber(targetChar))
                {
                    if (!outIllegalCharacters.Contains(targetChar))
                    {
                        outIllegalCharacters += targetChar;
                    }

                    bContainsIllegalCharacters = true;
                }
            }

            return !bContainsIllegalCharacters;
        }

        public bool IsFileNameValid(string fileName, out string  failedReason)
        {
            failedReason = "";

            if (fileName.Length == 0)
            {
                failedReason = "File name should not be empty.";
                return false;
            }

            if (fileName.Contains(" "))
            {
                failedReason = "File name should not contain blank space character.";
                return false;
            }

            if (!Char.IsLetter(fileName[0]))
            {
                failedReason = "File name must begin with an alphabetic character.";
                return false;
            }

            string illegalNameCharacters;
            if (!IsFileNameContainsOnlyLegalCharacters(fileName, out illegalNameCharacters))
            {
                failedReason = "File name may not contain the following characters: " + illegalNameCharacters;
                return false;
            }

            return true;
        }
        
        public bool IsFilePathValid(string filePath)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            for (int i = 0; i < invalidChars.Length; i++)
            {
                if (filePath.Contains(invalidChars[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

