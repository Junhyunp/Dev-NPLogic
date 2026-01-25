using System;
using System.Collections.Generic;
using System.Linq;

namespace NPLogic.Services
{
    /// <summary>
    /// IBK Excel 시트별 컬럼 매핑 설정
    /// </summary>
    public static class SheetMappingConfig
    {
        /// <summary>
        /// 시트 유형별 컬럼 매핑 가져오기
        /// </summary>
        public static List<ColumnMappingRule> GetMappingRules(SheetType sheetType)
        {
            return sheetType switch
            {
                SheetType.BorrowerGeneral => GetBorrowerGeneralMappings(),
                SheetType.BorrowerRestructuring => GetBorrowerRestructuringMappings(),
                SheetType.Loan => GetLoanMappings(),
                SheetType.Property => GetPropertyMappings(),
                SheetType.RegistryDetail => GetRegistryDetailMappings(),
                SheetType.Guarantee => GetGuaranteeMappings(),
                _ => new List<ColumnMappingRule>()
            };
        }

        /// <summary>
        /// Sheet A: 차주일반정보 → borrowers
        /// </summary>
        private static List<ColumnMappingRule> GetBorrowerGeneralMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("자산유형", "asset_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", true, ColumnDataType.String),
                new("관련차주", "related_borrower", false, ColumnDataType.String),
                new("차주형태", "borrower_type", false, ColumnDataType.String),
                
                // 금액 정보
                new("대출원금잔액", "opb", false, ColumnDataType.Decimal),
                new("가지급금", "advance_payment", false, ColumnDataType.Decimal),
                new("미상환원금잔액", "unpaid_principal", false, ColumnDataType.Decimal),
                new("미수이자", "accrued_interest", false, ColumnDataType.Decimal),
                new("차주별 근저당권설정액", "mortgage_amount", false, ColumnDataType.Decimal),
                new("차주별 선순위 근저당설정액", "senior_mortgage_amount", false, ColumnDataType.Decimal),
                
                // 비고
                new("비고", "notes", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Sheet A-1: 회생차주정보 → borrower_restructuring
        /// </summary>
        private static List<ColumnMappingRule> GetBorrowerRestructuringMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 정보 (차주 조회용 - borrowers 테이블 참조)
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("자산유형", "asset_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", false, ColumnDataType.String),
                new("관련차주", "related_borrower", false, ColumnDataType.String),

                // 회생 정보
                new("인가/미인가", "approval_status", false, ColumnDataType.String),
                new("세부진행단계", "progress_stage", false, ColumnDataType.String),
                new("세부 진행단계", "progress_stage", false, ColumnDataType.String),
                new("관할법원", "court_name", false, ColumnDataType.String),
                new("회생사건번호", "case_number", false, ColumnDataType.String),

                // 날짜 정보
                new("회생신청일", "filing_date", false, ColumnDataType.Date),
                new("보전처분일", "preservation_date", false, ColumnDataType.Date),
                new("개시결정일", "commencement_date", false, ColumnDataType.Date),
                new("채권신고일", "claim_filing_date", false, ColumnDataType.Date),
                new("인가/폐지결정일", "approval_dismissal_date", false, ColumnDataType.Date),

                // 회사 정보
                new("업종", "industry", false, ColumnDataType.String),
                new("상장/비상장", "listing_status", false, ColumnDataType.String),
                new("상장여부", "listing_status", false, ColumnDataType.String),
                new("종업원수", "employee_count", false, ColumnDataType.Integer),
                new("설립일", "establishment_date", false, ColumnDataType.Date),

                // 기타
                new("회생탈락권", "excluded_claim", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Sheet B: 채권일반정보 → loans
        /// </summary>
        private static List<ColumnMappingRule> GetLoanMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", false, ColumnDataType.String),
                new("대출일련번호", "account_serial", true, ColumnDataType.String),

                // 대출 정보
                new("대출과목", "loan_type", false, ColumnDataType.String),
                new("계좌번호", "account_number", false, ColumnDataType.String),

                // 이자율
                new("이자율", "normal_interest_rate", false, ColumnDataType.Decimal),
                new("정상이자율", "normal_interest_rate", false, ColumnDataType.Decimal),
                new("연체이자율", "overdue_interest_rate", false, ColumnDataType.Decimal),

                // 날짜
                new("최초대출일", "initial_loan_date", false, ColumnDataType.Date),
                new("대출만기일", "maturity_date", false, ColumnDataType.Date),
                new("최종이수일", "last_interest_date", false, ColumnDataType.Date),

                // 금액
                new("통화표시", "currency", false, ColumnDataType.String),
                new("최초대출금액", "initial_loan_amount", false, ColumnDataType.Decimal),
                new("최초대출원금", "initial_loan_amount", false, ColumnDataType.Decimal),
                new("대출금잔액", "loan_principal_balance", false, ColumnDataType.Decimal),
                new("대출원금잔액", "loan_principal_balance", false, ColumnDataType.Decimal),
                new("환산된 대출잔액", "converted_loan_balance", false, ColumnDataType.Decimal),
                new("환산대출잔액", "converted_loan_balance", false, ColumnDataType.Decimal),
                new("미상환원금잔액", "unpaid_principal", false, ColumnDataType.Decimal),
                new("미상환원금", "unpaid_principal", false, ColumnDataType.Decimal),
                new("가지급금", "advance_payment", false, ColumnDataType.Decimal),
                new("미수이자", "accrued_interest", false, ColumnDataType.Decimal),
                new("채권액 합계", "total_claim_amount", false, ColumnDataType.Decimal),
                new("채권액합계", "total_claim_amount", false, ColumnDataType.Decimal),
            };
        }

        /// <summary>
        /// Sheet C-1: 담보재산정보_물건정보 → properties
        /// </summary>
        private static List<ColumnMappingRule> GetPropertyMappings()
        {
            return new List<ColumnMappingRule>
            {
                // ========== 기본 정보 ==========
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("Pool 구분", "pool_type", false, ColumnDataType.String),
                new("Pool", "pool_type", false, ColumnDataType.String),
                new("자산유형", "asset_type", false, ColumnDataType.String),
                new("차주구분", "borrower_category", false, ColumnDataType.String),
                new("채권구분", "borrower_category", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "debtor_name", false, ColumnDataType.String),
                new("물건번호", "collateral_number", false, ColumnDataType.String),
                new("물건 일련번호", "property_number", false, ColumnDataType.String),
                new("물건일련번호", "property_number", false, ColumnDataType.String),
                new("Assign", "assigned_to_name", false, ColumnDataType.String),

                // Property 일련번호 (내부용, property_number와 별도)
                new("Property 일련번호", "property_number", false, ColumnDataType.String),
                new("Property일련번호", "property_number", false, ColumnDataType.String),

                // ========== 주소 정보 ==========
                new("담보소재지1", "address_province", false, ColumnDataType.String),
                new("담보소재지 1", "address_province", false, ColumnDataType.String),
                new("(특별광역시/도)", "address_province", false, ColumnDataType.String),
                new("(특별,광역시/도)", "address_province", false, ColumnDataType.String),
                new("담보소재지2", "address_city", false, ColumnDataType.String),
                new("담보소재지 2", "address_city", false, ColumnDataType.String),
                new("(시/군/구)", "address_city", false, ColumnDataType.String),
                new("담보소재지3", "address_district", false, ColumnDataType.String),
                new("담보소재지 3", "address_district", false, ColumnDataType.String),
                new("(동/리/읍/면)", "address_district", false, ColumnDataType.String),
                new("(동/읍/면/리)", "address_district", false, ColumnDataType.String),
                new("담보소재지4", "address_detail", false, ColumnDataType.String),
                new("담보소재지 4", "address_detail", false, ColumnDataType.String),
                new("(나머지/상세지번/기타재산내역)", "address_detail", false, ColumnDataType.String),
                new("담보소재지4 (나머지상세지번/기타재산내역)", "address_detail", false, ColumnDataType.String),

                // ========== 물건 타입 및 면적 ==========
                new("물건 종류", "property_type", false, ColumnDataType.String),
                new("물건종류", "property_type", false, ColumnDataType.String),
                new("Property Type", "property_type", false, ColumnDataType.String),
                new("물건 대지면적", "land_area", false, ColumnDataType.Decimal),
                new("물건대지면적", "land_area", false, ColumnDataType.Decimal),
                new("Property-대지면적", "land_area", false, ColumnDataType.Decimal),
                new("Property- 대지면적", "land_area", false, ColumnDataType.Decimal),
                new("물건 건물면적", "building_area", false, ColumnDataType.Decimal),
                new("물건건물면적", "building_area", false, ColumnDataType.Decimal),
                new("Property-건물면적", "building_area", false, ColumnDataType.Decimal),
                new("Property- 건물면적", "building_area", false, ColumnDataType.Decimal),
                new("물건 기타", "machinery_value", false, ColumnDataType.Decimal),
                new("물건기타", "machinery_value", false, ColumnDataType.Decimal),
                new("물건 기타 (기계기구 등)", "machinery_value", false, ColumnDataType.Decimal),
                new("Property-기타(기계기구등)", "machinery_value", false, ColumnDataType.Decimal),
                new("Property- 기타(기계기구등)", "machinery_value", false, ColumnDataType.Decimal),
                new("소유자명", "owner_name", false, ColumnDataType.String),
                new("권리유형", "right_type", false, ColumnDataType.String),

                // ========== 근저당/공담 설정 정보 ==========
                new("공담 물건 금액", "joint_collateral_amount", false, ColumnDataType.Decimal),
                new("공담물건금액", "joint_collateral_amount", false, ColumnDataType.Decimal),
                new("공담 물건금액", "joint_collateral_amount", false, ColumnDataType.Decimal),
                new("물건별 선순위 설정액", "senior_mortgage_amount", false, ColumnDataType.Decimal),
                new("물건별선순위설정액", "senior_mortgage_amount", false, ColumnDataType.Decimal),
                new("Property 설정 순위", "mortgage_rank", false, ColumnDataType.String),
                new("Property설정 순위", "mortgage_rank", false, ColumnDataType.String),
                new("Property별 근저당권설정액", "mortgage_amount", false, ColumnDataType.Decimal),
                new("Property별근저당권설정액", "mortgage_amount", false, ColumnDataType.Decimal),
                new("환산후 Property별 근저당권설정액", "mortgage_amount_converted", false, ColumnDataType.Decimal),

                // ========== 선순위 정보 ==========
                new("선순위 주택 소액보증금", "senior_housing_small_deposit", false, ColumnDataType.Decimal),
                new("선순위 주택소액보증금", "senior_housing_small_deposit", false, ColumnDataType.Decimal),
                new("선순위주택소액보증금", "senior_housing_small_deposit", false, ColumnDataType.Decimal),
                new("선순위 상가 소액보증금", "senior_commercial_small_deposit", false, ColumnDataType.Decimal),
                new("선순위 상가소액보증금", "senior_commercial_small_deposit", false, ColumnDataType.Decimal),
                new("선순위상가소액보증금", "senior_commercial_small_deposit", false, ColumnDataType.Decimal),
                new("선순위 소액보증금", "senior_small_deposit", false, ColumnDataType.Decimal),
                new("선순위소액보증금", "senior_small_deposit", false, ColumnDataType.Decimal),
                new("선순위\n소액보증금", "senior_small_deposit", false, ColumnDataType.Decimal),
                new("선순위 주택 임차보증금", "senior_housing_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위 주택임차보증금", "senior_housing_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위주택임차보증금", "senior_housing_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위 상가 임차보증금", "senior_commercial_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위 상가임차보증금", "senior_commercial_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위상가임차보증금", "senior_commercial_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위 임차보증금", "senior_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위임차보증금", "senior_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위\n임차보증금", "senior_lease_deposit", false, ColumnDataType.Decimal),
                new("선순위 임금채권", "senior_wage_claim", false, ColumnDataType.Decimal),
                new("선순위임금채권", "senior_wage_claim", false, ColumnDataType.Decimal),
                new("선순위\n임금채권", "senior_wage_claim", false, ColumnDataType.Decimal),
                new("선순위 당해세", "senior_current_tax", false, ColumnDataType.Decimal),
                new("선순위당해세", "senior_current_tax", false, ColumnDataType.Decimal),
                new("선순위\n당해세", "senior_current_tax", false, ColumnDataType.Decimal),
                new("선순위 조세채권", "senior_tax_claim", false, ColumnDataType.Decimal),
                new("선순위조세채권", "senior_tax_claim", false, ColumnDataType.Decimal),
                new("선순위\n조세채권", "senior_tax_claim", false, ColumnDataType.Decimal),
                new("선순위 기타", "senior_etc", false, ColumnDataType.Decimal),
                new("선순위기타", "senior_etc", false, ColumnDataType.Decimal),
                new("기타 선순위", "senior_etc", false, ColumnDataType.Decimal),
                new("기타선순위", "senior_etc", false, ColumnDataType.Decimal),
                new("선순위 합계", "senior_total", false, ColumnDataType.Decimal),
                new("선순위합계", "senior_total", false, ColumnDataType.Decimal),
                new("합계", "senior_total", false, ColumnDataType.Decimal),

                // ========== 감정평가 정보 ==========
                new("감정평가구분", "appraisal_type", false, ColumnDataType.String),
                new("감정평가일자", "appraisal_date", false, ColumnDataType.Date),
                new("감정평가일", "appraisal_date", false, ColumnDataType.Date),
                new("감정평가기관", "appraisal_agency", false, ColumnDataType.String),
                new("토지감정평가액", "land_appraisal_value", false, ColumnDataType.Decimal),
                new("토지 감정평가액", "land_appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액\n_대지", "land_appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액_대지", "land_appraisal_value", false, ColumnDataType.Decimal),
                new("건물감정평가액", "building_appraisal_value", false, ColumnDataType.Decimal),
                new("건물 감정평가액", "building_appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액\n_건물", "building_appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액_건물", "building_appraisal_value", false, ColumnDataType.Decimal),
                new("기계평가액", "machinery_appraisal_value", false, ColumnDataType.Decimal),
                new("기계 평가액", "machinery_appraisal_value", false, ColumnDataType.Decimal),
                new("기계감정평가액", "machinery_appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액\n_기계기구", "machinery_appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액_기계기구", "machinery_appraisal_value", false, ColumnDataType.Decimal),
                new("제시외", "excluded_appraisal", false, ColumnDataType.Decimal),
                new("제시외 및 기타", "excluded_appraisal", false, ColumnDataType.Decimal),
                new("감정평가액\n_제시외 및 기타", "excluded_appraisal", false, ColumnDataType.Decimal),
                new("감정평가액_제시외 및 기타", "excluded_appraisal", false, ColumnDataType.Decimal),
                new("감정평가액합계", "appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액 합계", "appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액\n_합계", "appraisal_value", false, ColumnDataType.Decimal),
                new("감정평가액_합계", "appraisal_value", false, ColumnDataType.Decimal),
                new("KB아파트시세", "kb_price", false, ColumnDataType.Decimal),
                new("KB 아파트시세", "kb_price", false, ColumnDataType.Decimal),
                new("KB시세", "kb_price", false, ColumnDataType.Decimal),

                // ========== 경매 기본 정보 ==========
                new("경매개시여부", "auction_started", false, ColumnDataType.Boolean),
                new("경매 개시여부", "auction_started", false, ColumnDataType.Boolean),
                new("경매\n개시여부", "auction_started", false, ColumnDataType.Boolean),
                new("경매 관할법원", "auction_court", false, ColumnDataType.String),
                new("경매관할법원", "auction_court", false, ColumnDataType.String),

                // ========== 경매 정보 (선행) ==========
                new("경매신청기관(선행)", "precedent_auction_applicant", false, ColumnDataType.String),
                new("경매신청기관 (선행)", "precedent_auction_applicant", false, ColumnDataType.String),
                new("경매신청기관\n(선행)", "precedent_auction_applicant", false, ColumnDataType.String),
                new("경매개시일자(선행)", "precedent_auction_start_date", false, ColumnDataType.Date),
                new("경매개시일자 (선행)", "precedent_auction_start_date", false, ColumnDataType.Date),
                new("경매개시일자\n(선행)", "precedent_auction_start_date", false, ColumnDataType.Date),
                new("경매사건번호(선행)", "precedent_case_number", false, ColumnDataType.String),
                new("경매사건번호 (선행)", "precedent_case_number", false, ColumnDataType.String),
                new("경매사건번호\n(선행)", "precedent_case_number", false, ColumnDataType.String),
                new("배당요구종기일(선행)", "precedent_claim_deadline", false, ColumnDataType.Date),
                new("배당요구종기일 (선행)", "precedent_claim_deadline", false, ColumnDataType.Date),
                new("배당요구종기일\n(선행)", "precedent_claim_deadline", false, ColumnDataType.Date),
                new("청구금액(선행)", "precedent_claim_amount", false, ColumnDataType.Decimal),
                new("청구금액 (선행)", "precedent_claim_amount", false, ColumnDataType.Decimal),

                // ========== 경매 정보 (후행) ==========
                new("경매신청기관(후행)", "subsequent_auction_applicant", false, ColumnDataType.String),
                new("경매신청기관 (후행)", "subsequent_auction_applicant", false, ColumnDataType.String),
                new("경매개시일자(후행)", "subsequent_auction_start_date", false, ColumnDataType.Date),
                new("경매개시일자 (후행)", "subsequent_auction_start_date", false, ColumnDataType.Date),
                new("경매사건번호(후행)", "subsequent_case_number", false, ColumnDataType.String),
                new("경매사건번호 (후행)", "subsequent_case_number", false, ColumnDataType.String),
                new("배당요구종기일(후행)", "subsequent_claim_deadline", false, ColumnDataType.Date),
                new("배당요구종기일 (후행)", "subsequent_claim_deadline", false, ColumnDataType.Date),
                new("청구금액(후행)", "subsequent_claim_amount", false, ColumnDataType.Decimal),
                new("청구금액 (후행)", "subsequent_claim_amount", false, ColumnDataType.Decimal),

                // ========== 경매 진행 정보 ==========
                new("최초법사가", "initial_court_value", false, ColumnDataType.Decimal),
                new("최초 법사가", "initial_court_value", false, ColumnDataType.Decimal),
                new("최초경매기일", "first_auction_date", false, ColumnDataType.Date),
                new("최초 경매기일", "first_auction_date", false, ColumnDataType.Date),
                new("최종경매회차", "final_auction_round", false, ColumnDataType.Integer),
                new("최종 경매회차", "final_auction_round", false, ColumnDataType.Integer),
                new("최종경매결과", "final_auction_result", false, ColumnDataType.String),
                new("최종 경매결과", "final_auction_result", false, ColumnDataType.String),
                new("최종경매기일", "final_auction_date", false, ColumnDataType.Date),
                new("최종 경매기일", "final_auction_date", false, ColumnDataType.Date),
                new("차기경매기일", "next_auction_date", false, ColumnDataType.Date),
                new("차기 경매기일", "next_auction_date", false, ColumnDataType.Date),
                new("낙찰금액", "winning_bid_amount", false, ColumnDataType.Decimal),
                new("매각금액", "winning_bid_amount", false, ColumnDataType.Decimal),
                new("최종경매일의 최저입찰금액", "final_minimum_bid", false, ColumnDataType.Decimal),
                new("최종경매일의최저입찰금액", "final_minimum_bid", false, ColumnDataType.Decimal),
                new("차후최종경매일의 최저입찰금액", "next_minimum_bid", false, ColumnDataType.Decimal),
                new("차후예정경매일의 최저입찰금액", "next_minimum_bid", false, ColumnDataType.Decimal),
                new("차후예정경매일의최저입찰금액", "next_minimum_bid", false, ColumnDataType.Decimal),

                // ========== 경매사건번호 (대시보드 "사건번호" 표시용) ==========
                new("경매사건번호", "property_number", false, ColumnDataType.String),
                new("경매사건번호(IBK)", "property_number", false, ColumnDataType.String),
                new("경매사건번호 (IBK)", "property_number", false, ColumnDataType.String),
                new("경매사건번호\n(IBK)", "property_number", false, ColumnDataType.String),

                // 비고
                new("비고", "notes", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Sheet C-2: 등기부등본정보 → registry_sheet_data
        /// </summary>
        private static List<ColumnMappingRule> GetRegistryDetailMappings()
        {
            return new List<ColumnMappingRule>
            {
                // 기본 정보
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", false, ColumnDataType.String),
                new("물건번호", "property_number", false, ColumnDataType.String),
                new("물건일련번호", "property_number", false, ColumnDataType.String),
                new("Property 일련번호", "property_number", false, ColumnDataType.String),
                new("Property일련번호", "property_number", false, ColumnDataType.String),

                // 지번번호
                new("지번번호", "jibun_number", false, ColumnDataType.String),
                new("지번", "jibun_number", false, ColumnDataType.String),

                // 주소 정보
                new("담보소재지1", "address_province", false, ColumnDataType.String),
                new("담보소재지 1", "address_province", false, ColumnDataType.String),
                new("(특별광역시/도)", "address_province", false, ColumnDataType.String),
                new("(특별,광역시/도)", "address_province", false, ColumnDataType.String),
                new("담보소재지2", "address_city", false, ColumnDataType.String),
                new("담보소재지 2", "address_city", false, ColumnDataType.String),
                new("(시/군/구)", "address_city", false, ColumnDataType.String),
                new("담보소재지3", "address_district", false, ColumnDataType.String),
                new("담보소재지 3", "address_district", false, ColumnDataType.String),
                new("(동/리/읍/면)", "address_district", false, ColumnDataType.String),
                new("(동/읍/면/리)", "address_district", false, ColumnDataType.String),
                new("담보소재지4", "address_detail", false, ColumnDataType.String),
                new("담보소재지 4", "address_detail", false, ColumnDataType.String),
                new("(나머지/상세지번/기타재산내역)", "address_detail", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Sheet D: 신용보증서 → credit_guarantees
        /// </summary>
        private static List<ColumnMappingRule> GetGuaranteeMappings()
        {
            return new List<ColumnMappingRule>
            {
                // ========== 기본 조회용 정보 (차주/대출 식별용) ==========
                new("일련번호", "serial_number", false, ColumnDataType.Integer),
                new("자산유형", "asset_type", false, ColumnDataType.String),
                new("채권구분", "asset_type", false, ColumnDataType.String),
                new("차주일련번호", "borrower_number", true, ColumnDataType.String),
                new("차주명", "borrower_name", false, ColumnDataType.String),

                // 관련 대출 정보 (대표컬럼: 계좌일련번호, 관련 대출채권 계좌번호)
                new("계좌일련번호", "loan_account_serial", false, ColumnDataType.String),
                new("대출일련번호", "loan_account_serial", false, ColumnDataType.String),
                new("관련대출채권일련번호", "loan_account_serial", false, ColumnDataType.String),
                new("관련 대출채권 일련번호", "loan_account_serial", false, ColumnDataType.String),
                new("대출계좌번호", "related_loan_account_number", false, ColumnDataType.String),
                new("계좌번호", "related_loan_account_number", false, ColumnDataType.String),
                new("관련대출채권계좌번호", "related_loan_account_number", false, ColumnDataType.String),
                new("관련 대출채권 계좌번호", "related_loan_account_number", false, ColumnDataType.String),
                new("보증관련계좌번호", "related_loan_account_number", false, ColumnDataType.String),

                // ========== 보증 기본 정보 ==========
                new("보증번호", "guarantee_number", false, ColumnDataType.String),
                new("보증서번호", "guarantee_number", false, ColumnDataType.String),
                new("신용보증번호", "guarantee_number", false, ColumnDataType.String),
                new("보증종류", "guarantee_type", false, ColumnDataType.String),
                new("보증유형", "guarantee_type", false, ColumnDataType.String),
                new("보증형태", "guarantee_type", false, ColumnDataType.String),
                new("보증서종류", "guarantee_type", false, ColumnDataType.String),
                new("보증기관", "guarantee_institution", false, ColumnDataType.String),
                new("보증인", "guarantee_institution", false, ColumnDataType.String),
                new("신용보증기관", "guarantee_institution", false, ColumnDataType.String),
                new("보증기관명", "guarantee_institution", false, ColumnDataType.String),

                // ========== 보증 금액 정보 ==========
                new("보증금액", "guarantee_amount", false, ColumnDataType.Decimal),
                new("보증한도", "guarantee_amount", false, ColumnDataType.Decimal),
                new("신용보증금액", "guarantee_amount", false, ColumnDataType.Decimal),
                new("보증비율", "guarantee_ratio", false, ColumnDataType.Decimal),
                new("보증률", "guarantee_ratio", false, ColumnDataType.Decimal),
                new("보증율", "guarantee_ratio", false, ColumnDataType.Decimal),
                new("보증잔액", "converted_guarantee_balance", false, ColumnDataType.Decimal),
                new("환산보증잔액", "converted_guarantee_balance", false, ColumnDataType.Decimal),
                new("환산 보증잔액", "converted_guarantee_balance", false, ColumnDataType.Decimal),
                new("환산후보증잔액", "converted_guarantee_balance", false, ColumnDataType.Decimal),
                new("환산후 보증잔액", "converted_guarantee_balance", false, ColumnDataType.Decimal),
                new("환산된 보증잔액", "converted_guarantee_balance", false, ColumnDataType.Decimal),

                // ========== 대위변제 정보 ==========
                new("대위변제금액", "subrogation_amount", false, ColumnDataType.Decimal),
                new("대위변제액", "subrogation_amount", false, ColumnDataType.Decimal),
                new("대위변제", "subrogation_amount", false, ColumnDataType.Decimal),
                new("기대위변제금액", "prior_subrogation_amount", false, ColumnDataType.Decimal),
                new("기대위변제액", "prior_subrogation_amount", false, ColumnDataType.Decimal),
                new("기수령 대위변제금액", "prior_subrogation_amount", false, ColumnDataType.Decimal),

                // ========== 상태 정보 ==========
                new("유효여부", "is_valid", false, ColumnDataType.Boolean),
                new("보증유효", "is_valid", false, ColumnDataType.Boolean),
                new("보증유효여부", "is_valid", false, ColumnDataType.Boolean),
                new("해지여부", "is_terminated", false, ColumnDataType.Boolean),
                new("보증해지", "is_terminated", false, ColumnDataType.Boolean),
                new("보증해지여부", "is_terminated", false, ColumnDataType.Boolean),
                new("수령여부", "is_received", false, ColumnDataType.Boolean),
                new("대위변제수령여부", "is_received", false, ColumnDataType.Boolean),

                // ========== 날짜 정보 ==========
                new("발급일", "issue_date", false, ColumnDataType.Date),
                new("보증발급일", "issue_date", false, ColumnDataType.Date),
                new("보증서발급일", "issue_date", false, ColumnDataType.Date),
                new("만기일", "expiry_date", false, ColumnDataType.Date),
                new("보증만기일", "expiry_date", false, ColumnDataType.Date),
                new("보증기간만료일", "expiry_date", false, ColumnDataType.Date),
                new("대위변제예정일", "subrogation_expected_date", false, ColumnDataType.Date),
                new("대위변제 예정일", "subrogation_expected_date", false, ColumnDataType.Date),
                new("수령일", "received_date", false, ColumnDataType.Date),
                new("대위변제수령일", "received_date", false, ColumnDataType.Date),
                new("대위변제 수령일", "received_date", false, ColumnDataType.Date),

                // ========== MCI 정보 ==========
                new("MCI채권번호", "mci_bond_number", false, ColumnDataType.String),
                new("MCI 채권번호", "mci_bond_number", false, ColumnDataType.String),
                new("MCI번호", "mci_bond_number", false, ColumnDataType.String),
                new("MCI최초금액", "mci_initial_amount", false, ColumnDataType.Decimal),
                new("MCI 최초금액", "mci_initial_amount", false, ColumnDataType.Decimal),
                new("MCI최초잔액", "mci_initial_amount", false, ColumnDataType.Decimal),
                new("MCI잔액", "mci_balance", false, ColumnDataType.Decimal),
                new("MCI 잔액", "mci_balance", false, ColumnDataType.Decimal),
                new("MCI현재잔액", "mci_balance", false, ColumnDataType.Decimal),

                // ========== 기타 ==========
                new("비고", "notes", false, ColumnDataType.String),
            };
        }

        /// <summary>
        /// Excel 컬럼명으로 매핑 규칙 찾기 (유사 매칭 지원)
        /// </summary>
        public static ColumnMappingRule? FindMappingRule(List<ColumnMappingRule> rules, string excelColumnName)
        {
            if (string.IsNullOrWhiteSpace(excelColumnName))
                return null;

            // 줄바꿈을 공백으로 변환하고 정리
            var normalizedName = NormalizeColumnName(excelColumnName);

            // 1. 정확한 매칭 (양쪽 모두 normalize해서 비교)
            var exactMatch = rules.FirstOrDefault(r => 
                NormalizeColumnName(r.ExcelColumnName).Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return exactMatch;

            // 2. 포함 매칭 (Excel 컬럼명이 규칙의 컬럼명을 포함)
            var containsMatch = rules.FirstOrDefault(r => 
                normalizedName.Contains(NormalizeColumnName(r.ExcelColumnName), StringComparison.OrdinalIgnoreCase));
            if (containsMatch != null)
                return containsMatch;

            // 3. 역방향 포함 매칭 (규칙의 컬럼명이 Excel 컬럼명을 포함)
            var reverseMatch = rules.FirstOrDefault(r => 
                NormalizeColumnName(r.ExcelColumnName).Contains(normalizedName, StringComparison.OrdinalIgnoreCase));
            
            return reverseMatch;
        }

        /// <summary>
        /// 컬럼명 정규화 (줄바꿈, 연속 공백 처리)
        /// </summary>
        private static string NormalizeColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return string.Empty;

            return columnName
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("  ", " ")
                .Trim();
        }
    }

    /// <summary>
    /// 컬럼 매핑 규칙
    /// </summary>
    public class ColumnMappingRule
    {
        public string ExcelColumnName { get; set; }
        public string DbColumnName { get; set; }
        public bool IsRequired { get; set; }
        public ColumnDataType DataType { get; set; }

        public ColumnMappingRule(string excelColumnName, string dbColumnName, bool isRequired, ColumnDataType dataType)
        {
            ExcelColumnName = excelColumnName;
            DbColumnName = dbColumnName;
            IsRequired = isRequired;
            DataType = dataType;
        }

        /// <summary>
        /// 값 변환
        /// </summary>
        public object? ConvertValue(object? rawValue)
        {
            if (rawValue == null || (rawValue is string s && string.IsNullOrWhiteSpace(s)))
                return null;

            try
            {
                return DataType switch
                {
                    ColumnDataType.String => rawValue.ToString()?.Trim(),
                    ColumnDataType.Integer => Convert.ToInt32(rawValue),
                    ColumnDataType.Decimal => ParseDecimal(rawValue),
                    ColumnDataType.Date => ParseDate(rawValue),
                    ColumnDataType.Boolean => ParseBoolean(rawValue),
                    _ => rawValue
                };
            }
            catch
            {
                return null;
            }
        }

        private static decimal? ParseDecimal(object value)
        {
            var str = value.ToString()?.Replace(",", "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(str) || str == "-")
                return null;
            
            // 백분율 처리
            if (str.EndsWith("%"))
            {
                str = str.TrimEnd('%');
                if (decimal.TryParse(str, out var pctValue))
                    return pctValue / 100;
            }

            if (decimal.TryParse(str, out var decValue))
                return decValue;

            return null;
        }

        private static DateTime? ParseDate(object value)
        {
            if (value is DateTime dt)
                return dt;

            var str = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(str) || str == "-")
                return null;

            // 다양한 날짜 형식 처리
            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyy.MM.dd",
                "yyyyMMdd",
                "MM/dd/yyyy",
                "dd/MM/yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(str, format, null, System.Globalization.DateTimeStyles.None, out var parsed))
                    return parsed;
            }

            if (DateTime.TryParse(str, out var fallback))
                return fallback;

            // Excel 날짜 숫자 (OADate)
            if (double.TryParse(str, out var oaDate) && oaDate > 0 && oaDate < 100000)
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch { }
            }

            return null;
        }

        private static bool? ParseBoolean(object value)
        {
            var str = value.ToString()?.Trim().ToLower();
            if (string.IsNullOrEmpty(str))
                return null;

            return str switch
            {
                "y" or "yes" or "true" or "1" or "예" or "○" => true,
                "n" or "no" or "false" or "0" or "아니오" or "x" => false,
                _ => null
            };
        }
    }

    /// <summary>
    /// 컬럼 데이터 타입
    /// </summary>
    public enum ColumnDataType
    {
        String,
        Integer,
        Decimal,
        Date,
        Boolean
    }
}

