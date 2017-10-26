using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using System.Diagnostics;
using Microsoft.Win32;
using EnvDTE;

namespace UE4ProjectHelper
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class RegenerateProjectFilesCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("e2f445d8-cc4d-4886-8c74-ea25a363148c");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly Package package;

		/// <summary>
		/// Initializes a new instance of the <see cref="RegenerateProjectFilesCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		private RegenerateProjectFilesCommand(Package package)
		{
			if (package == null)
			{
				throw new ArgumentNullException("package");
			}

			this.package = package;

			OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (commandService != null)
			{
				var menuCommandID = new CommandID(CommandSet, CommandId);
				var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
				commandService.AddCommand(menuItem);
			}
		}

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static RegenerateProjectFilesCommand Instance
		{
			get;
			private set;
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

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static void Initialize(Package package)
		{
			Instance = new RegenerateProjectFilesCommand(package);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void MenuItemCallback(object sender, EventArgs e)
		{
			RegistryKey targetKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\Unreal.ProjectFile\shell\rungenproj\command");
			if (targetKey.GetValueNames().Length != 1)
			{
				string message = string.Format(CultureInfo.CurrentCulture, "Error occurred when trying to read UnrealVersionSelector's path from register table.");
				string title = "UE4 Helper - RegenerateProjectFiles";

				VsShellUtilities.ShowMessageBox(
					this.ServiceProvider,
					message,
					title,
					OLEMSGICON.OLEMSGICON_CRITICAL,
					OLEMSGBUTTON.OLEMSGBUTTON_OK,
					OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
			}
			else
			{
				DTE dte = (DTE)this.ServiceProvider.GetService(typeof(DTE));
				if (dte.Solution.FullName.Length == 0 || dte.Solution.FullName == null)
				{
					string message = string.Format(CultureInfo.CurrentCulture, "You may have not opened any solution, please check!");
					string title = "RegenerateProjectFilesCommand";

					// Show a message box to prove we were here
					VsShellUtilities.ShowMessageBox(
						this.ServiceProvider,
						message,
						title,
						OLEMSGICON.OLEMSGICON_CRITICAL,
						OLEMSGBUTTON.OLEMSGBUTTON_OK,
						OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
				}
				else
				{
					string unrealVersionSelectorValueName = targetKey.GetValueNames()[0];
					string unrealVersionSelectorCommand = targetKey.GetValue(unrealVersionSelectorValueName).ToString();
					string fileName = unrealVersionSelectorCommand.Split(new string[] { " /projectfiles" }, StringSplitOptions.None)[0];

					string uprojectFileName = dte.Solution.FullName.Replace(".sln", ".uproject");

					System.Diagnostics.Process proc = new System.Diagnostics.Process();
					proc.StartInfo.FileName = fileName;
					proc.StartInfo.Arguments = "/projectfiles " + uprojectFileName;
					proc.Start();
				}
			}

			targetKey.Close();
		}
	}
}
