using System.Windows;

namespace IqooNeo10
{
    public partial class RoleSelectionWindow : Window
    {
        public RoleSelectionWindow()
        {
            InitializeComponent();
        }

        private void Buyer_Click(object sender, RoutedEventArgs e)
        {
            new MarketWindow(username: null, canEdit: false, showContact: true, openedFromAdmin: false).Show();
            Close();
        }

        private void SellerOrAdmin_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow(isSellerOrAdmin: true).Show();
            Close();
        }
    }
}
