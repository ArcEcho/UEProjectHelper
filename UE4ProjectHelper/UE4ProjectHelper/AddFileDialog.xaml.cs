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
using Microsoft.VisualStudio.Shell.Interop;

namespace UE4ProjectHelper
{
    /// <summary>
    /// Interaction logic for AddFileDialog.xaml
    /// </summary>
    public partial class AddFileDialog : Window
    {
        private IVsUIShell shell;

        public AddFileDialog(IVsUIShell uiShell)
        {
            shell = uiShell;

            InitializeComponent();
        }
    }
}
