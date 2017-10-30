using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Shell;
using System.Globalization;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Collections.ObjectModel;

namespace UE4ProjectHelper
{
    class DirectoryRecord
    {
        public DirectoryInfo Info { get; set; }
        public string FullName { get; set; }

        public IEnumerable<FileInfo> Files
        {
            get
            {
                return Info.GetFiles();
            }
        }
        
        public IEnumerable<DirectoryRecord> Directories
        {
            get
            {
                return from directoryInfo in Info.GetDirectories("*", SearchOption.TopDirectoryOnly)
                       select new DirectoryRecord { Info = directoryInfo, FullName = directoryInfo.FullName };
            }
        }
    }


    /// <summary>
    /// Interaction logic for AddFileDialog.xaml
    /// </summary>
    public partial class AddFileDialog : Window
    {
        private IVsUIShell uiShell;
        private string projectFileName;

        public AddFileDialog(IVsUIShell uiShell, string projectFileName)
        {
            this.uiShell = uiShell;
            this.projectFileName = projectFileName;

            InitializeComponent();

            DetectModules();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "OpenAddFileDialogCommand";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                null,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void DetectModules()
        {
            var ModuleCollection = new ObservableCollection<DirectoryRecord>();

            string projectDirectory = System.IO.Path.GetDirectoryName(this.projectFileName);
            List<string> moduleSourceDirectories = new List<string>();

            // Add game module
            string projectName = System.IO.Path.GetFileNameWithoutExtension(this.projectFileName);
            string gameModuleSourceDirectory = System.IO.Path.Combine(projectDirectory, "Source", projectName);
            moduleSourceDirectories.Add(gameModuleSourceDirectory);

            //Add plugin modules, if exist.
            string pluginsDirectory = System.IO.Path.Combine(projectDirectory, "Plugins");
            if (Directory.Exists(pluginsDirectory))
            {
                DirectoryInfo diectoryInfo = new DirectoryInfo(pluginsDirectory);
                foreach (var fileInfo in diectoryInfo.GetFiles("*.uplugin", SearchOption.AllDirectories))
                {
                    string pluginName = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name);
                    string pluginDirectory = System.IO.Path.GetDirectoryName(fileInfo.FullName);
                    string pluginModuleSourceDirectory = System.IO.Path.Combine(pluginDirectory, "Source", pluginName);

                    moduleSourceDirectories.Add(pluginModuleSourceDirectory);
                }
            }

            foreach(string moduleSourceDirectory in moduleSourceDirectories)
            {
                ModuleCollection.Add(
                    new DirectoryRecord
                    {
                        Info = new DirectoryInfo(moduleSourceDirectory),
                        FullName = moduleSourceDirectory
                    }
                );

            }

            directoryTreeView.ItemsSource = ModuleCollection;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DirectoryRecord item = (DirectoryRecord)directoryTreeView.SelectedItem;
           
            myTextBlock.Text = item.FullName;
        }
    }
}
