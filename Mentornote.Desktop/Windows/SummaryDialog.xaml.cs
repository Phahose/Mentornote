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

namespace Mentornote.Desktop.Windows
{
    /// <summary>
    /// Interaction logic for SummaryDialog.xaml
    /// </summary>
    public partial class SummaryDialog : Window
    {
        public bool SaveClicked { get; private set; }
        public string SummaryText { get; private set; }

        public SummaryDialog(string summary)
        {
            InitializeComponent();
            SummaryText = summary;
            SummaryTextBlock.Text = summary;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveClicked = true;
            DialogResult = true;
            Close();
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            SaveClicked = false;
            DialogResult = false;
            Close();
        }
    }
}
