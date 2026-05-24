using System.Windows;
using MahApps.Metro.Controls;

namespace BWB_Auswertung.Views
{
    public partial class ManuelleVerbindungDialog : MetroWindow
    {
        public string HostInput
        {
            get => HostTextBox.Text;
            set => HostTextBox.Text = value;
        }

        public string PortInput
        {
            get => PortTextBox.Text;
            set => PortTextBox.Text = value;
        }

        public ManuelleVerbindungDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HostTextBox.Text))
            {
                MessageBox.Show("Bitte IP-Adresse oder Hostname eingeben.", "Eingabe fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(PortTextBox.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Bitte gültigen Port eingeben.", "Eingabe ungültig", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
