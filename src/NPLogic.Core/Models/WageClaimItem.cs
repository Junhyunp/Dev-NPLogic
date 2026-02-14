using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 임금채권 항목
    /// </summary>
    public class WageClaimItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? PropertyId { get; set; }

        /// <summary>순번</summary>
        public int SequenceNumber { get; set; }

        /// <summary>성명</summary>
        public string EmployeeName { get; set; } = "";

        /// <summary>생년월일</summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>가압류일자</summary>
        public DateTime? SeizureDate { get; set; }

        /// <summary>가압류금액</summary>
        public decimal? SeizureAmount { get; set; }

        /// <summary>배당요구일자</summary>
        public DateTime? ClaimDate { get; set; }

        private decimal? _wageAmount;
        /// <summary>임금</summary>
        public decimal? WageAmount
        {
            get => _wageAmount;
            set { _wageAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmount)); }
        }

        private decimal? _severanceAmount;
        /// <summary>퇴직금</summary>
        public decimal? SeveranceAmount
        {
            get => _severanceAmount;
            set { _severanceAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmount)); }
        }

        private decimal? _otherAmount;
        /// <summary>기타</summary>
        public decimal? OtherAmount
        {
            get => _otherAmount;
            set { _otherAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmount)); }
        }

        /// <summary>배당요구액 합계 (임금 + 퇴직금 + 기타)</summary>
        public decimal TotalAmount => (WageAmount ?? 0) + (SeveranceAmount ?? 0) + (OtherAmount ?? 0);

        private decimal? _threeMonthWage;
        /// <summary>3개월 임금 (최종 3개월분 임금)</summary>
        public decimal? ThreeMonthWage
        {
            get => _threeMonthWage;
            set { _threeMonthWage = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReflectedAmount)); }
        }

        private decimal? _threeYearSeverance;
        /// <summary>3년분 퇴직금</summary>
        public decimal? ThreeYearSeverance
        {
            get => _threeYearSeverance;
            set { _threeYearSeverance = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReflectedAmount)); }
        }

        private decimal? _substitutePayment;
        /// <summary>체당금지급액</summary>
        public decimal? SubstitutePayment
        {
            get => _substitutePayment;
            set { _substitutePayment = value; OnPropertyChanged(); OnPropertyChanged(nameof(ReflectedAmount)); }
        }

        /// <summary>반영금액 합계 (3개월 임금 + 3년분 퇴직금 + 체당금지급액)</summary>
        public decimal ReflectedAmount => (ThreeMonthWage ?? 0) + (ThreeYearSeverance ?? 0) + (SubstitutePayment ?? 0);

        /// <summary>비고</summary>
        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
