using System.Windows;

namespace DataSync.Windows.Dialog
{

    public partial class WinDialogLog : Window
    {
        public Log Log { get; set; }
        public WinDialogLog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxLog.Text = string.Format("Dato: {0}\n" +
                                            "Kunde: {1}\n\n" +
                                            "Fejl: {2}\n" +
                                            "Besked: {3}", Log.DateCreated.ToString(),Log.Customer.Name, Log.Name, Log.Info);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}