using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views.Loan.Sections
{
    public partial class ProrationSection : UserControl
    {
        public static readonly DependencyProperty PhaseProperty =
            DependencyProperty.Register(nameof(Phase), typeof(string), typeof(ProrationSection), new PropertyMetadata("First"));

        public string Phase
        {
            get => (string)GetValue(PhaseProperty);
            set => SetValue(PhaseProperty, value);
        }

        public ProrationSection()
        {
            InitializeComponent();
        }
    }
}
