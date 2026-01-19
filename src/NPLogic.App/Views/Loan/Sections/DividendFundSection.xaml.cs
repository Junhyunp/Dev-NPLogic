using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views.Loan.Sections
{
    public partial class DividendFundSection : UserControl
    {
        public static readonly DependencyProperty SheetTypeProperty =
            DependencyProperty.Register(nameof(SheetType), typeof(string), typeof(DividendFundSection), new PropertyMetadata("Guarantee"));

        public static readonly DependencyProperty PhaseProperty =
            DependencyProperty.Register(nameof(Phase), typeof(string), typeof(DividendFundSection), new PropertyMetadata("First"));

        public string SheetType
        {
            get => (string)GetValue(SheetTypeProperty);
            set => SetValue(SheetTypeProperty, value);
        }

        public string Phase
        {
            get => (string)GetValue(PhaseProperty);
            set => SetValue(PhaseProperty, value);
        }

        public DividendFundSection()
        {
            InitializeComponent();
        }
    }
}
