using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 경매일정(Ⅶ) 모델 - 엑셀 산출화면 기반
    /// </summary>
    public class AuctionSchedule
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        
        /// <summary>
        /// 일정 유형: auction(경매), public_sale(공매)
        /// </summary>
        public string ScheduleType { get; set; } = "auction";
        
        /// <summary>
        /// 사건번호
        /// </summary>
        public string? AuctionNumber { get; set; }
        
        /// <summary>
        /// 경매일 (기일)
        /// </summary>
        public DateTime? AuctionDate { get; set; }
        
        /// <summary>
        /// 입찰일
        /// </summary>
        public DateTime? BidDate { get; set; }
        
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

        // ========== 인터링/상계회수 ==========
        
        /// <summary>
        /// 인터링 원금상계
        /// </summary>
        public decimal InterimPrincipalOffset { get; set; }
        
        /// <summary>
        /// 인터링 원금회수
        /// </summary>
        public decimal InterimPrincipalRecovery { get; set; }
        
        /// <summary>
        /// 인터링 이자상계
        /// </summary>
        public decimal InterimInterestOffset { get; set; }
        
        /// <summary>
        /// 인터링 이자회수
        /// </summary>
        public decimal InterimInterestRecovery { get; set; }

        // ========== 대법원 경매 캡처 (A-002) ==========
        
        /// <summary>
        /// 사건내역 캡처 이미지 URL
        /// </summary>
        public string? CaseCaptureUrl { get; set; }
        
        /// <summary>
        /// 기일내역 캡처 이미지 URL
        /// </summary>
        public string? ScheduleCaptureUrl { get; set; }
        
        /// <summary>
        /// 문건송달내역 캡처 이미지 URL
        /// </summary>
        public string? DocumentCaptureUrl { get; set; }

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

        // ========== 차주사항 관련 ==========
        
        /// <summary>
        /// 차주사망 여부
        /// </summary>
        public bool IsBorrowerDeceased { get; set; }
        
        /// <summary>
        /// 소유주 전입 여부
        /// </summary>
        public bool IsOwnerMovedIn { get; set; }
        
        /// <summary>
        /// 개회 여부
        /// </summary>
        public bool IsSessionOpened { get; set; }
        
        /// <summary>
        /// 대출종류
        /// </summary>
        public string? LoanType { get; set; }
        
        /// <summary>
        /// 대위등기여부
        /// </summary>
        public bool HasSubrogationRegistration { get; set; }
        
        /// <summary>
        /// 대위등기주체
        /// </summary>
        public string? SubrogationEntity { get; set; }
        
        /// <summary>
        /// 주택담보대출 여부
        /// </summary>
        public bool IsHousingMortgageLoan { get; set; }
        
        /// <summary>
        /// 대위등기비용
        /// </summary>
        public decimal SubrogationCost { get; set; }
        
        /// <summary>
        /// 최종이수일
        /// </summary>
        public DateTime? FinalCompletionDate { get; set; }
        
        /// <summary>
        /// 관할법원
        /// </summary>
        public string? JurisdictionCourt { get; set; }
        
        /// <summary>
        /// 법원별 저감율
        /// </summary>
        public decimal CourtDiscountRate { get; set; }
        
        /// <summary>
        /// 경매신청여부: *1=경매개시예정, *2=경매개시결정, *3=3자경매신청, *4=중복경매신청
        /// </summary>
        public string AuctionRequestType { get; set; } = "*1";

        // ========== 시나리오별 일정 상세 ==========
        
        /// <summary>
        /// 시나리오1 경매신청일
        /// </summary>
        public DateTime? Scenario1AuctionRequestDate { get; set; }
        
        /// <summary>
        /// 시나리오2 경매신청일
        /// </summary>
        public DateTime? Scenario2AuctionRequestDate { get; set; }
        
        /// <summary>
        /// 시나리오1 경매절차 (개월)
        /// </summary>
        public int Scenario1ProcedureMonths { get; set; }
        
        /// <summary>
        /// 시나리오2 경매절차 (개월)
        /// </summary>
        public int Scenario2ProcedureMonths { get; set; }
        
        /// <summary>
        /// 시나리오1 예상1회차 (개월)
        /// </summary>
        public int Scenario1FirstRoundMonths { get; set; } = 6;
        
        /// <summary>
        /// 시나리오2 예상1회차 (개월)
        /// </summary>
        public int Scenario2FirstRoundMonths { get; set; } = 6;
        
        /// <summary>
        /// 시나리오1 법사가
        /// </summary>
        public decimal Scenario1LegalPrice { get; set; }
        
        /// <summary>
        /// 시나리오2 법사가
        /// </summary>
        public decimal Scenario2LegalPrice { get; set; }
        
        /// <summary>
        /// 시나리오1 예상낙찰회차
        /// </summary>
        public int? Scenario1ExpectedRound { get; set; }
        
        /// <summary>
        /// 시나리오2 예상낙찰회차
        /// </summary>
        public int? Scenario2ExpectedRound { get; set; }
        
        /// <summary>
        /// 시나리오1 최저낙찰가율
        /// </summary>
        public decimal Scenario1MinBidRate { get; set; }
        
        /// <summary>
        /// 시나리오2 최저낙찰가율
        /// </summary>
        public decimal Scenario2MinBidRate { get; set; }
        
        /// <summary>
        /// 시나리오1 최저법사가
        /// </summary>
        public decimal Scenario1MinLegalPrice { get; set; }
        
        /// <summary>
        /// 시나리오2 최저법사가
        /// </summary>
        public decimal Scenario2MinLegalPrice { get; set; }

        // ========== 비용 관련 (7개 항목) ==========
        
        /// <summary>
        /// 1. 신문공고료 (기본 220,000)
        /// </summary>
        public decimal NewspaperAdFee { get; set; } = 220000;
        
        /// <summary>
        /// 2. 현황조사수수료 (기본 70,000)
        /// </summary>
        public decimal SurveyFee { get; set; } = 70000;
        
        /// <summary>
        /// 3. 경매매각수수료
        /// </summary>
        public decimal AuctionSaleFee { get; set; }
        
        /// <summary>
        /// 4. 감정수수료
        /// </summary>
        public decimal AppraisalFee { get; set; }
        
        /// <summary>
        /// 5. 송달수수료
        /// </summary>
        public decimal DeliveryFee { get; set; }
        
        /// <summary>
        /// 6. 경매신청 등록/교육세
        /// </summary>
        public decimal RegistrationEducationFee { get; set; }
        
        /// <summary>
        /// 7. 기타비용 (기본 5,000)
        /// </summary>
        public decimal OtherCost { get; set; } = 5000;
        
        /// <summary>
        /// 부대비용율 가산 (기본 10%)
        /// </summary>
        public decimal AdditionalCostRate { get; set; } = 0.10m;
        
        /// <summary>
        /// 물건수 (현황조사수수료 계산용)
        /// </summary>
        public int PropertyCount { get; set; } = 1;
        
        /// <summary>
        /// 채권자수 (송달수수료 계산용)
        /// </summary>
        public int CreditorCount { get; set; } = 1;

        // ========== 배당/회수 관련 (시나리오별) ==========
        
        /// <summary>
        /// 시나리오1 선순위금액
        /// </summary>
        public decimal Scenario1SeniorDeduction { get; set; }
        
        /// <summary>
        /// 시나리오2 선순위금액
        /// </summary>
        public decimal Scenario2SeniorDeduction { get; set; }
        
        /// <summary>
        /// 시나리오1 경매수수료 및 제세
        /// </summary>
        public decimal Scenario1AuctionFees { get; set; }
        
        /// <summary>
        /// 시나리오2 경매수수료 및 제세
        /// </summary>
        public decimal Scenario2AuctionFees { get; set; }
        
        /// <summary>
        /// 시나리오1 낙찰후 배당가능재원
        /// </summary>
        public decimal Scenario1DistributableAfterSale { get; set; }
        
        /// <summary>
        /// 시나리오2 낙찰후 배당가능재원
        /// </summary>
        public decimal Scenario2DistributableAfterSale { get; set; }
        
        /// <summary>
        /// 시나리오1 선순위차감후 배당가능재원
        /// </summary>
        public decimal Scenario1DistributableAfterSenior { get; set; }
        
        /// <summary>
        /// 시나리오2 선순위차감후 배당가능재원
        /// </summary>
        public decimal Scenario2DistributableAfterSenior { get; set; }
        
        /// <summary>
        /// 시나리오1 Loan cap
        /// </summary>
        public decimal Scenario1LoanCap { get; set; }
        
        /// <summary>
        /// 시나리오2 Loan cap
        /// </summary>
        public decimal Scenario2LoanCap { get; set; }
        
        /// <summary>
        /// 시나리오1 Loan cap 2
        /// </summary>
        public decimal Scenario1LoanCap2 { get; set; }
        
        /// <summary>
        /// 시나리오2 Loan cap 2
        /// </summary>
        public decimal Scenario2LoanCap2 { get; set; }
        
        /// <summary>
        /// 시나리오1 Mortgage cap
        /// </summary>
        public decimal Scenario1MortgageCap { get; set; }
        
        /// <summary>
        /// 시나리오2 Mortgage cap
        /// </summary>
        public decimal Scenario2MortgageCap { get; set; }
        
        /// <summary>
        /// 시나리오1 Cap 반영 배당액
        /// </summary>
        public decimal Scenario1CapAppliedDividend { get; set; }
        
        /// <summary>
        /// 시나리오2 Cap 반영 배당액
        /// </summary>
        public decimal Scenario2CapAppliedDividend { get; set; }
        
        /// <summary>
        /// 시나리오1 가산:예납경매수수료 회수
        /// </summary>
        public decimal Scenario1PrepaidFeeRecovery { get; set; }
        
        /// <summary>
        /// 시나리오2 가산:예납경매수수료 회수
        /// </summary>
        public decimal Scenario2PrepaidFeeRecovery { get; set; }
        
        /// <summary>
        /// 시나리오1 배당 회수가능액
        /// </summary>
        public decimal Scenario1DividendRecoverable { get; set; }
        
        /// <summary>
        /// 시나리오2 배당 회수가능액
        /// </summary>
        public decimal Scenario2DividendRecoverable { get; set; }

        // ========== 경매예납금액 회수액 추정 ==========
        
        /// <summary>
        /// 실제납입한 경매예납금액
        /// </summary>
        public decimal ActualPaidDeposit { get; set; }
        
        /// <summary>
        /// 예납금액수수료 회수비율 (기본 90%)
        /// </summary>
        public decimal DepositRecoveryRate { get; set; } = 0.90m;

        // ========== 3자선행/중복경매 관련 ==========
        
        /// <summary>
        /// 3자선행경매신청금액
        /// </summary>
        public decimal ThirdPartyAuctionAmount { get; set; }
        
        /// <summary>
        /// 중복경매신청금액
        /// </summary>
        public decimal DuplicateAuctionAmount { get; set; }
        
        /// <summary>
        /// 3자선행경매 비용 상세 (JSON)
        /// </summary>
        public string? ThirdPartyAuctionCostsJson { get; set; }
        
        /// <summary>
        /// 중복경매 비용 상세 (JSON)
        /// </summary>
        public string? DuplicateAuctionCostsJson { get; set; }
        
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
        /// 일정 유형 표시 (경매/공매)
        /// </summary>
        public string ScheduleTypeDisplay => ScheduleType switch
        {
            "auction" => "경매",
            "public_sale" => "공매",
            _ => ScheduleType
        };

        /// <summary>
        /// 낙찰률 (낙찰가/최저매각가)
        /// </summary>
        public decimal? BidRate => MinimumBid > 0 && SalePrice > 0 
            ? (SalePrice / MinimumBid) * 100 
            : null;

        /// <summary>
        /// 감정평가금액 총계
        /// </summary>
        public decimal TotalAppraisalValue => 
            LandAppraisalValue + BuildingAppraisalValue + MachineryAppraisalValue + OtherBankSeniorValue;

        /// <summary>
        /// 경매예납금액 합계 (7개 비용)
        /// </summary>
        public decimal TotalAuctionCost =>
            NewspaperAdFee + SurveyFee + AuctionSaleFee + AppraisalFee + 
            DeliveryFee + RegistrationEducationFee + OtherCost;

        /// <summary>
        /// 부대비용 포함 총액
        /// </summary>
        public decimal TotalCostWithAdditional => TotalAuctionCost * (1 + AdditionalCostRate);

        /// <summary>
        /// 경매신청여부 표시
        /// </summary>
        public string AuctionRequestTypeDisplay => AuctionRequestType switch
        {
            "*1" => "경매개시예정",
            "*2" => "경매개시결정",
            "*3" => "3자경매신청",
            "*4" => "중복경매신청",
            _ => AuctionRequestType
        };
    }
}
