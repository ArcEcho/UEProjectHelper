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

        public AddFileDialog(IVsUIShell uiShell)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.uiShell = uiShell;

            InitializeComponent();

            DetectModules();
        }

        private void DetectModules()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var ModuleCollection = new ObservableCollection<DirectoryRecord>();

            string projectRootDirectory = UE4Helper.Instance.GetGameProjectRootDirectory();
            List<string> moduleSourceDirectories = new List<string>();

            // Add game module
            string projectName = UE4Helper.Instance.GetGameProjectName();
            string gameModuleSourceDirectory = System.IO.Path.Combine(projectRootDirectory, "Source", projectName);
            moduleSourceDirectories.Add(gameModuleSourceDirectory);

            //Add plugin modules, if exist.
            string pluginRootDirectory = System.IO.Path.Combine(projectRootDirectory, "Plugins");
            if (Directory.Exists(pluginRootDirectory))
            {
                DirectoryInfo diectoryInfo = new DirectoryInfo(pluginRootDirectory);
                foreach (var fileInfo in diectoryInfo.GetFiles("*.uplugin", SearchOption.AllDirectories))
                {
                    string pluginName = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name);
                    string pluginDirectory = System.IO.Path.GetDirectoryName(fileInfo.FullName);
                    string pluginModuleSourceDirectory = System.IO.Path.Combine(pluginDirectory, "Source", pluginName);

                    moduleSourceDirectories.Add(pluginModuleSourceDirectory);
                }
            }

            foreach (string moduleSourceDirectory in moduleSourceDirectories)
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DirectoryRecord item = (DirectoryRecord)directoryTreeView.SelectedItem;
            string targetDirectory = item.FullName;

            TextBlock_Tips.Text = targetDirectory;
            string inputFileName = TextBox_FileName.Text;


            string targetFileName = System.IO.Path.Combine(targetDirectory, inputFileName);
            List<string> targetFileNames = new List<string>();
            switch (ComboBox_ExtensionType.SelectedIndex)
            {
                case 0:
                    targetFileNames.Add(targetFileName + ".h");
                    targetFileNames.Add(targetFileName + ".cpp");
                    break;
                case 1:
                    targetFileNames.Add(targetFileName + ".h");
                    break;
                case 2:
                    targetFileNames.Add(targetFileName + ".cpp");
                    break;
                case 3:
                    string customizedExtension = ".txt";
                    targetFileNames.Add(targetFileName + customizedExtension);
                    break;
                default:
                    Close();
                    return;

            }


            List<string> existingFiles = new List<string>();
            foreach(var fileName in targetFileNames)
            {
                if(File.Exists(fileName))
                {
                    existingFiles.Add(fileName);
                    continue;
                }

                FileStream fs;
                fs = File.Create(fileName);
                fs.Close();
            }

            if(existingFiles.Count != 0)
            {
                string message = "The following files already exist in target directory:";;

                foreach(var existingFile in existingFiles)
                {
                    message += "\n" + System.IO.Path.GetFileName(existingFile);
                }

                TextBlock_Tips.Text = message;
                TextBlock_Tips.Foreground = Brushes.Red;

                Button_OK.IsEnabled = false;
                return;
            }

            Close();

            UE4Helper.Instance.RegenerateGameSolution();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ComboBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (TextBox_CustomizedExtension != null)
            {
                bool bCanInputCustomizedExtension = ComboBox_ExtensionType.SelectedIndex == 3;
                //TextBox_CustomizedExtension.IsEnabled = bCanInputCustomizedExtension;
                TextBox_CustomizedExtension.IsEnabled = false;

                TextBox_FileName_TextChanged(sender, e);
            }
        }

        private void TreeView_TargetDirectorySelected(object sender, RoutedEventArgs e)
        {
            if (!Grid_Settings.IsEnabled)
            {
                Grid_Settings.IsEnabled = true;

                ComboBox_SelectionChanged(sender, e);
            }
        }

        private void TextBox_FileName_TextChanged(object sender, RoutedEventArgs e)
        {
            string fileName = TextBox_FileName.Text;
            string failedReason;
            bool bValidFileName = UE4Helper.Instance.IsFileNameValid(fileName, out failedReason);

            if (Button_OK == null)
            {
                return;
            }

            Button_OK.IsEnabled = bValidFileName;

            if (TextBlock_Tips == null)
            {
                return;
            }

            if (!bValidFileName)
            {
                TextBlock_Tips.Text = failedReason;
                TextBlock_Tips.Foreground = Brushes.Red;
            }
            else
            {
                TextBlock_Tips.Text = "Tips: File name should not be longer than 30 characters and has no extension.";
                TextBlock_Tips.Foreground = Brushes.Green;
            }
        }
    }
}
