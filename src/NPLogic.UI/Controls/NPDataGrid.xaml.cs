using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace NPLogic.UI.Controls
{
    public partial class NPDataGrid : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(NPDataGrid), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(NPDataGrid), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty AutoGenerateColumnsProperty =
            DependencyProperty.Register(nameof(AutoGenerateColumns), typeof(bool), typeof(NPDataGrid), new PropertyMetadata(false));

        public static readonly DependencyProperty CanUserAddRowsProperty =
            DependencyProperty.Register(nameof(CanUserAddRows), typeof(bool), typeof(NPDataGrid), new PropertyMetadata(false));

        public static readonly DependencyProperty CanUserDeleteRowsProperty =
            DependencyProperty.Register(nameof(CanUserDeleteRows), typeof(bool), typeof(NPDataGrid), new PropertyMetadata(false));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(NPDataGrid), new PropertyMetadata(true));

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(nameof(SelectionMode), typeof(DataGridSelectionMode), typeof(NPDataGrid), 
                new PropertyMetadata(DataGridSelectionMode.Single));

        public static readonly DependencyProperty ShowSearchBarProperty =
            DependencyProperty.Register(nameof(ShowSearchBar), typeof(bool), typeof(NPDataGrid), new PropertyMetadata(false));

        public static readonly DependencyProperty FilterContentProperty =
            DependencyProperty.Register(nameof(FilterContent), typeof(object), typeof(NPDataGrid), new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public bool AutoGenerateColumns
        {
            get => (bool)GetValue(AutoGenerateColumnsProperty);
            set => SetValue(AutoGenerateColumnsProperty, value);
        }

        public bool CanUserAddRows
        {
            get => (bool)GetValue(CanUserAddRowsProperty);
            set => SetValue(CanUserAddRowsProperty, value);
        }

        public bool CanUserDeleteRows
        {
            get => (bool)GetValue(CanUserDeleteRowsProperty);
            set => SetValue(CanUserDeleteRowsProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public DataGridSelectionMode SelectionMode
        {
            get => (DataGridSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public bool ShowSearchBar
        {
            get => (bool)GetValue(ShowSearchBarProperty);
            set => SetValue(ShowSearchBarProperty, value);
        }

        public object FilterContent
        {
            get => GetValue(FilterContentProperty);
            set => SetValue(FilterContentProperty, value);
        }

        public DataGrid DataGrid => InnerDataGrid;

        public NPDataGrid()
        {
            InitializeComponent();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Search functionality can be implemented by parent view via event or binding
            // This is a placeholder for extensibility
        }
    }
}

