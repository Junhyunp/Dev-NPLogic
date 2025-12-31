using System.Windows;
using System.Windows.Controls;

namespace NPLogic.Views
{
    /// <summary>
    /// 비핵심 탭 - 차주개요/론/담보총괄/담보물건/선순위평가/경공매/회생개요/현금흐름집계/NPB
    /// </summary>
    public partial class NonCoreTab : UserControl
    {
        public NonCoreTab()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 서브탭 전환 이벤트
        /// </summary>
        private void SubTab_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string tag)
            {
                // 모든 컨텐츠 숨기기
                ContentBorrowerOverview.Visibility = Visibility.Collapsed;
                ContentLoan.Visibility = Visibility.Collapsed;
                ContentCollateralSummary.Visibility = Visibility.Collapsed;
                ContentCollateralProperty.Visibility = Visibility.Collapsed;
                ContentSeniorRights.Visibility = Visibility.Collapsed;
                ContentAuction.Visibility = Visibility.Collapsed;
                ContentRestructuring.Visibility = Visibility.Collapsed;
                ContentCashFlow.Visibility = Visibility.Collapsed;
                ContentNPB.Visibility = Visibility.Collapsed;

                // 선택된 탭 표시
                switch (tag)
                {
                    case "BorrowerOverview":
                        ContentBorrowerOverview.Visibility = Visibility.Visible;
                        break;
                    case "Loan":
                        ContentLoan.Visibility = Visibility.Visible;
                        break;
                    case "CollateralSummary":
                        ContentCollateralSummary.Visibility = Visibility.Visible;
                        break;
                    case "CollateralProperty":
                        ContentCollateralProperty.Visibility = Visibility.Visible;
                        break;
                    case "SeniorRights":
                        ContentSeniorRights.Visibility = Visibility.Visible;
                        break;
                    case "Auction":
                        ContentAuction.Visibility = Visibility.Visible;
                        break;
                    case "Restructuring":
                        ContentRestructuring.Visibility = Visibility.Visible;
                        break;
                    case "CashFlow":
                        ContentCashFlow.Visibility = Visibility.Visible;
                        break;
                    case "NPB":
                        ContentNPB.Visibility = Visibility.Visible;
                        break;
                }
            }
        }
    }
}
