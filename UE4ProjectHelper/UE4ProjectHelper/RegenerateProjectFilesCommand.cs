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
            ThreadHelper.ThrowIfNotOnUIThread();

            UE4Helper.Initialize(this.package);

            if(!UE4Helper.Instance.HasAnySolutionOpened())
            {
                string message = string.Format(CultureInfo.CurrentCulture, "You may have not opened any solution, please check!");
				UE4Helper.Instance.ShowErrorMessage(message);
                return;
            }

			if (UE4Helper.Instance.IsUEGameSolution())
            {
				UE4Helper.Instance.RegenerateGameSolution();
            }
			else if (UE4Helper.Instance.IsUEEngineSolution())
			{ 
				UE4Helper.Instance.RegenerateEngineSolution();
			}
			else
            {
                string message = string.Format(CultureInfo.CurrentCulture, "This solution is not a valid UE4 game solution or engine solution.");
				UE4Helper.Instance.ShowErrorMessage(message);
            }
		}
	}
}
