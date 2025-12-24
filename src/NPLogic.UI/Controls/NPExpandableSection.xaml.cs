using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Controls
{
    /// <summary>
    /// 접기/펼치기가 가능한 섹션 컨트롤
    /// </summary>
    public partial class NPExpandableSection : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// 섹션 헤더 텍스트
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(NPExpandableSection),
                new PropertyMetadata("Section"));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// 섹션 펼침/접힘 상태
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(
                nameof(IsExpanded),
                typeof(bool),
                typeof(NPExpandableSection),
                new PropertyMetadata(true, OnIsExpandedChanged));

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// 섹션 내용 (ContentProperty로 지정)
        /// </summary>
        public new static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                nameof(Content),
                typeof(object),
                typeof(NPExpandableSection),
                new PropertyMetadata(null));

        public new object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        #endregion

        public NPExpandableSection()
        {
            InitializeComponent();
            UpdateVisualState();
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NPExpandableSection section)
            {
                section.UpdateVisualState();
            }
        }

        private void HeaderBorder_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        private void UpdateVisualState()
        {
            if (ContentArea != null)
            {
                ContentArea.Visibility = IsExpanded ? Visibility.Visible : Visibility.Collapsed;
            }

            if (ToggleIcon != null)
            {
                ToggleIcon.Text = IsExpanded ? "▼" : "▶";
            }

            if (HintText != null)
            {
                HintText.Text = IsExpanded ? "접기" : "펼치기";
            }
        }
    }
}










