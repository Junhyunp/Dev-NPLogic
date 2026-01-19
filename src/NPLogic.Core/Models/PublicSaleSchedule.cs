using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 공매일정(Ⅷ) 모델 - 엑셀 산출화면 기반
    /// </summary>
    public class PublicSaleSchedule
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        
        /// <summary>
        /// 공매번호
        /// </summary>
        public string? SaleNumber { get; set; }
        
        /// <summary>
        /// 공매일
        /// </summary>
        public DateTime? SaleDate { get; set; }
        
        /// <summary>
        /// 최저매각가
        /// </summary>
        public decimal? MinimumBid { get; set; }
        
        /// <summary>
        /// 낙찰가
        /// </summary>
        public decimal? SalePrice { get; set; }
        
        /// <summary>
        /// 상태: scheduled, completed, cancelled
        /// </summary>
        public string Status { get; set; } = "scheduled";

        // ========== 평가결과 (시나리오별) ==========
        
        /// <summary>
        /// 시나리오1 낙찰가액
        /// </summary>
        public decimal Scenario1WinningBid { get; set; }
        
        /// <summary>
        /// 시나리오2 낙찰가액
        /// </summary>
        public decimal Scenario2WinningBid { get; set; }
        
        /// <summary>
        /// 시나리오1 평가사유
        /// </summary>
        public string? Scenario1EvalReason { get; set; }
        
        /// <summary>
        /// 시나리오2 평가사유
        /// </summary>
        public string? Scenario2EvalReason { get; set; }

        // ========== 구분별 감정평가금액 ==========
        
        /// <summary>
        /// 토지 감정평가금액
        /// </summary>
        public decimal LandAppraisalValue { get; set; }
        
        /// <summary>
        /// 건물 감정평가금액
        /// </summary>
        public decimal BuildingAppraisalValue { get; set; }
        
        /// <summary>
        /// 기계 감정평가금액
        /// </summary>
        public decimal MachineryAppraisalValue { get; set; }
        
        /// <summary>
        /// 타행선순위 금액
        /// </summary>
        public decimal OtherBankSeniorValue { get; set; }
        
        /// <summary>
        /// 감정평가금액 총계
        /// </summary>
        public decimal TotalAppraisalValue { get; set; }

        // ========== 공매일정 상세 (1안/2안) ==========
        
        /// <summary>
        /// 1안 공매개시일
        /// </summary>
        public DateTime? Scenario1StartDate { get; set; }
        
        /// <summary>
        /// 2안 공매개시일
        /// </summary>
        public DateTime? Scenario2StartDate { get; set; }
        
        /// <summary>
        /// 1안 수익자변경비용
        /// </summary>
        public decimal Scenario1BeneficiaryChangeCost { get; set; }
        
        /// <summary>
        /// 2안 수익자변경비용
        /// </summary>
        public decimal Scenario2BeneficiaryChangeCost { get; set; }
        
        /// <summary>
        /// 1안 추정낙찰액
        /// </summary>
        public decimal Scenario1EstimatedWinningBid { get; set; }
        
        /// <summary>
        /// 2안 추정낙찰액
        /// </summary>
        public decimal Scenario2EstimatedWinningBid { get; set; }
        
        /// <summary>
        /// 1안 선순위차감
        /// </summary>
        public decimal Scenario1SeniorDeduction { get; set; }
        
        /// <summary>
        /// 2안 선순위차감
        /// </summary>
        public decimal Scenario2SeniorDeduction { get; set; }
        
        /// <summary>
        /// 1안 공매비용
        /// </summary>
        public decimal Scenario1SaleCost { get; set; }
        
        /// <summary>
        /// 2안 공매비용
        /// </summary>
        public decimal Scenario2SaleCost { get; set; }
        
        /// <summary>
        /// 1안 환가처분 보수
        /// </summary>
        public decimal Scenario1DisposalFee { get; set; }
        
        /// <summary>
        /// 2안 환가처분 보수
        /// </summary>
        public decimal Scenario2DisposalFee { get; set; }
        
        /// <summary>
        /// 1안 배당가능액-안분전
        /// </summary>
        public decimal Scenario1DistributableBefore { get; set; }
        
        /// <summary>
        /// 2안 배당가능액-안분전
        /// </summary>
        public decimal Scenario2DistributableBefore { get; set; }
        
        /// <summary>
        /// 1안 타채권자 배분
        /// </summary>
        public decimal Scenario1CreditorDistribution { get; set; }
        
        /// <summary>
        /// 2안 타채권자 배분
        /// </summary>
        public decimal Scenario2CreditorDistribution { get; set; }
        
        /// <summary>
        /// 1안 배당가능액-안분후
        /// </summary>
        public decimal Scenario1DistributableAfter { get; set; }
        
        /// <summary>
        /// 2안 배당가능액-안분후
        /// </summary>
        public decimal Scenario2DistributableAfter { get; set; }
        
        /// <summary>
        /// 1안 LoanCap
        /// </summary>
        public decimal Scenario1LoanCap { get; set; }
        
        /// <summary>
        /// 2안 LoanCap
        /// </summary>
        public decimal Scenario2LoanCap { get; set; }
        
        /// <summary>
        /// 1안 LoanCap 2
        /// </summary>
        public decimal Scenario1LoanCap2 { get; set; }
        
        /// <summary>
        /// 2안 LoanCap 2
        /// </summary>
        public decimal Scenario2LoanCap2 { get; set; }
        
        /// <summary>
        /// 1안 MortgageCap
        /// </summary>
        public decimal Scenario1MortgageCap { get; set; }
        
        /// <summary>
        /// 2안 MortgageCap
        /// </summary>
        public decimal Scenario2MortgageCap { get; set; }
        
        /// <summary>
        /// 1안 Cap반영배당액
        /// </summary>
        public decimal Scenario1CapAppliedDividend { get; set; }
        
        /// <summary>
        /// 2안 Cap반영배당액
        /// </summary>
        public decimal Scenario2CapAppliedDividend { get; set; }
        
        /// <summary>
        /// 1안 회수가능액
        /// </summary>
        public decimal Scenario1RecoverableAmount { get; set; }
        
        /// <summary>
        /// 2안 회수가능액
        /// </summary>
        public decimal Scenario2RecoverableAmount { get; set; }

        // ========== 공매비용 ==========
        
        /// <summary>
        /// 온비드수수료 (낙찰가 기준)
        /// </summary>
        public decimal OnbidFee { get; set; }
        
        /// <summary>
        /// 감정평가료 (감정평가금액 기준)
        /// </summary>
        public decimal AppraisalFee { get; set; }
        
        /// <summary>
        /// 공매비용 합계
        /// </summary>
        public decimal TotalSaleCost { get; set; }

        // ========== 환가처분보수 ==========
        
        /// <summary>
        /// 예상매각가
        /// </summary>
        public decimal ExpectedSalePrice { get; set; }
        
        /// <summary>
        /// 환가처분보수 금액
        /// </summary>
        public decimal DisposalFeeAmount { get; set; }

        // ========== 타채권자 배분 ==========
        
        /// <summary>
        /// 타채권자 배분 정보 (JSON)
        /// [{name, basis, ratio, amount1, amount2}]
        /// </summary>
        public string? CreditorDistributionsJson { get; set; }

        // ========== Lead time 설정 ==========
        
        /// <summary>
        /// 공매 Lead time (기본 11일)
        /// </summary>
        public int LeadTimeDays { get; set; } = 11;
        
        /// <summary>
        /// 공매 저감율 (기본 10%)
        /// </summary>
        public decimal DiscountRate { get; set; } = 0.10m;
        
        /// <summary>
        /// 초기 감정평가액 (Lead time 테이블 기준)
        /// </summary>
        public decimal InitialAppraisalValue { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ========== 연관 데이터 ==========
        
        /// <summary>
        /// 물건 정보 (조인용)
        /// </summary>
        public Property? Property { get; set; }

        // ========== 계산된 속성 ==========
        
        /// <summary>
        /// 상태 표시
        /// </summary>
        public string StatusDisplay => Status switch
        {
            "scheduled" => "예정",
            "completed" => "완료",
            "cancelled" => "취소",
            _ => Status
        };

        /// <summary>
        /// 낙찰률 (낙찰가/최저매각가)
        /// </summary>
        public decimal? BidRate => MinimumBid > 0 && SalePrice > 0 
            ? (SalePrice / MinimumBid) * 100 
            : null;

        /// <summary>
        /// 타채권자 배분 리스트 (역직렬화)
        /// </summary>
        public List<CreditorDistributionItem>? CreditorDistributions
        {
            get
            {
                if (string.IsNullOrEmpty(CreditorDistributionsJson))
                    return new List<CreditorDistributionItem>();
                try
                {
                    return JsonSerializer.Deserialize<List<CreditorDistributionItem>>(CreditorDistributionsJson);
                }
                catch
                {
                    return new List<CreditorDistributionItem>();
                }
            }
            set
            {
                CreditorDistributionsJson = value != null 
                    ? JsonSerializer.Serialize(value) 
                    : null;
            }
        }
    }

    /// <summary>
    /// 타채권자 배분 항목
    /// </summary>
    public class CreditorDistributionItem
    {
        /// <summary>
        /// 우선수익자명
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// 기준 (대출금액, 우선수익한도금액 등)
        /// </summary>
        public string? Basis { get; set; }
        
        /// <summary>
        /// 비율 (%)
        /// </summary>
        public decimal Ratio { get; set; }
        
        /// <summary>
        /// 1안 금액
        /// </summary>
        public decimal Amount1 { get; set; }
        
        /// <summary>
        /// 2안 금액
        /// </summary>
        public decimal Amount2 { get; set; }
    }
}
