using System.Windows.Controls;

namespace NPLogic.Views.Loan.Sheets
{
    /// <summary>
    /// CombinedSheet.xaml - 일반+해지부보증 시트 (가장 복잡한 케이스)
    /// 섹션: 채권정보, 보증서요약, Loan Cap1, 보증여부별 합계, 배분대상금액 안분, 
    ///       1차 배당가능재원, 1차 안분비율, 2차 배당가능재원, 2차 안분비율, MCI 보증
    /// </summary>
    public partial class CombinedSheet : UserControl
    {
        public CombinedSheet()
        {
            InitializeComponent();
        }
    }
}
