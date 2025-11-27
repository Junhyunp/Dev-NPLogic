using System;
using NPLogic.Core.Models;

namespace NPLogic.Core.Services
{
    /// <summary>
    /// 권리분석 40+ 케이스 판단 로직 룰 엔진
    /// 기획서 선순위(Ⅴ).md 기준 구현
    /// </summary>
    public class RightAnalysisRuleEngine
    {
        /// <summary>
        /// 모든 룰 적용
        /// </summary>
        public void ApplyRules(RightAnalysis analysis, Property? property)
        {
            if (analysis == null) return;

            // 물건 유형 결정
            var propertyType = DeterminePropertyType(property);

            // 1. 선순위 근저당권 판단
            ApplySeniorMortgageRules(analysis);

            // 2. 유치권 판단
            ApplyLienRules(analysis);

            // 3. 선순위 소액보증금/임차보증금 판단
            ApplyDepositRules(analysis, propertyType);

            // 4. 선순위 임금채권 판단
            ApplyWageClaimRules(analysis);

            // 5. 당해세 및 선순위 조세채권 판단
            ApplyTaxRules(analysis);
        }

        /// <summary>
        /// 물건 유형 판단 (주택/토지/상가)
        /// </summary>
        private PropertyCategory DeterminePropertyType(Property? property)
        {
            if (property == null) return PropertyCategory.Residential;

            var type = property.PropertyType?.ToLower() ?? "";

            // 토지
            if (type.Contains("토지") || type.Contains("land"))
                return PropertyCategory.Land;

            // 상가/공장
            if (type.Contains("상가") || type.Contains("공장") || type.Contains("commercial") || type.Contains("factory"))
                return PropertyCategory.Commercial;

            // 주택 (아파트, 빌라, 다세대, 다가구, 단독주택 등)
            return PropertyCategory.Residential;
        }

        #region 1. 선순위 근저당권 판단

        private void ApplySeniorMortgageRules(RightAnalysis analysis)
        {
            // DD 금액이 있으면 반영
            if (analysis.SeniorMortgageDd > 0)
            {
                analysis.SeniorMortgageReflected = analysis.SeniorMortgageDd;
                analysis.SeniorMortgageReason = "선순위 근저당권 반영함.";
            }
            else
            {
                analysis.SeniorMortgageReflected = 0;
                analysis.SeniorMortgageReason = "해당사항 없음.";
            }
        }

        #endregion

        #region 2. 유치권 판단

        private void ApplyLienRules(RightAnalysis analysis)
        {
            // DD 금액이 있으면 반영
            if (analysis.LienDd > 0)
            {
                analysis.LienReflected = analysis.LienDd;
                analysis.LienReason = "유치권 확인 반영함.";
            }
            else
            {
                analysis.LienReflected = 0;
                analysis.LienReason = "해당사항 없음.";
            }
        }

        #endregion

        #region 3. 선순위 소액보증금/임차보증금 판단 (40+ 케이스)

        private void ApplyDepositRules(RightAnalysis analysis, PropertyCategory propertyType)
        {
            var auctionStatus = analysis.GetAuctionStatus();
            var claimPassed = analysis.ClaimDeadlinePassed;
            var hasSurvey = analysis.SurveyReportSubmitted ?? false;
            var hasTenant = analysis.HasTenant ?? false;
            var tenantClaimed = analysis.TenantClaimSubmitted ?? false;
            var tenantBeforeMortgage = analysis.TenantDateBeforeMortgage ?? false;
            var hasTenantRegistry = analysis.HasTenantRegistry;
            var hasCommercialLease = analysis.HasCommercialLease;
            var addressMatch = analysis.AddressMatch ?? false;

            switch (propertyType)
            {
                case PropertyCategory.Residential:
                    ApplyResidentialDepositRules(analysis, auctionStatus, claimPassed, hasSurvey, 
                        hasTenant, tenantClaimed, tenantBeforeMortgage, hasTenantRegistry, addressMatch);
                    break;

                case PropertyCategory.Land:
                    ApplyLandDepositRules(analysis, auctionStatus, claimPassed, hasSurvey);
                    break;

                case PropertyCategory.Commercial:
                    ApplyCommercialDepositRules(analysis, auctionStatus, claimPassed, hasSurvey, 
                        hasTenant, tenantClaimed, tenantBeforeMortgage, hasCommercialLease, addressMatch);
                    break;
            }
        }

        /// <summary>
        /// 주택 (아파트, 근린주택, 단독주택, 다세대, 다가구) 케이스
        /// </summary>
        private void ApplyResidentialDepositRules(RightAnalysis analysis, AuctionStatusEnum auctionStatus,
            bool claimPassed, bool hasSurvey, bool hasTenant, bool tenantClaimed, 
            bool tenantBeforeMortgage, bool hasTenantRegistry, bool addressMatch)
        {
            // 경매개시 + 배당요구종기일 경과
            if (auctionStatus == AuctionStatusEnum.Opened && claimPassed)
            {
                if (hasSurvey && hasTenant)
                {
                    if (tenantClaimed)
                    {
                        // Case 1: 경매개시 + 종기일경과 + 현황조사서O + 임차인O + 배당요구신청O
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_R1";
                        analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 현황조사서상 임차인 확인되며 경매열람자료상 임차인 배당요구신청금액 확인반영함.";
                    }
                    else if (!tenantBeforeMortgage)
                    {
                        // Case 2: 임차인 배당요구신청X + 임차일 근저당설정일 이후
                        analysis.SmallDepositReflected = 0;
                        analysis.SmallDepositCase = "CASE_R2";
                        analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 현황조사서상 임차인 확인되며 임차인 배당요구신청 없어 미반영함.";
                    }
                    else
                    {
                        // Case 3: 임차인 배당요구신청X + 임차일 근저당설정일 이전
                        analysis.LeaseDepositReflected = analysis.LeaseDepositDd;
                        analysis.SmallDepositReflected = 0;
                        analysis.SmallDepositCase = "CASE_R3";
                        analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 현황조사서상 임차인 확인되며 임차인 배당요구신청 없어 미반영함.";
                        analysis.LeaseDepositReason = "임차일 고려하여 선순위 임차보증금 반영함.";
                    }
                }
                else if (hasSurvey && !hasTenant)
                {
                    // Case 4: 현황조사서O + 임차인X
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_R4";
                    analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 제시된 경매열람자료 현황조사서상 임차인 없어 미반영함.";
                }
            }
            // 경매개시 + 배당요구종기일 미경과
            else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed)
            {
                if (hasSurvey && hasTenant)
                {
                    if (tenantClaimed)
                    {
                        // Case 5
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_R5";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서상 임차인 확인되며 경매열람자료상 임차인 배당요구신청금액 확인 반영함.";
                    }
                    else if (!tenantBeforeMortgage)
                    {
                        // Case 6: 임차일 근저당설정일 이후
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_R6";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서상 임차인 확인되어 선순위소액보증금 반영함.";
                    }
                    else
                    {
                        // Case 7: 임차일 근저당설정일 이전
                        analysis.LeaseDepositReflected = analysis.LeaseDepositDd;
                        analysis.SmallDepositCase = "CASE_R7";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서상 선순위 임차인 확인되어 선순위보증금 반영함.";
                    }
                }
                else if (hasSurvey && !hasTenant)
                {
                    // Case 8
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_R8";
                    analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 제시된 경매열람자료 현황조사서상 임차인 없어 미반영함.";
                }
                else if (!hasSurvey && hasTenantRegistry)
                {
                    // 현황조사서X + 전입세대열람O
                    if (hasTenant && !tenantBeforeMortgage)
                    {
                        // Case 9
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_R9";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않음. 전입세대열람서상 임차인 확인되어 소액보증금 추정 반영함.";
                    }
                    else if (hasTenant && tenantBeforeMortgage)
                    {
                        // Case 10
                        analysis.LeaseDepositReflected = analysis.LeaseDepositDd;
                        analysis.SmallDepositCase = "CASE_R10";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않음. 전입세대열람서상 선순위 임차인 확인되어 선순위보증금 추정 반영함.";
                    }
                    else
                    {
                        // Case 11: 전입인X
                        analysis.SmallDepositReflected = 0;
                        analysis.SmallDepositCase = "CASE_R11";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않음. 전입세대열람서상 임차인 확인되지 않아 미반영함.";
                    }
                }
                else if (!hasSurvey && !hasTenantRegistry)
                {
                    // 현황조사서X + 전입세대열람X
                    if (!addressMatch)
                    {
                        // Case 12: 주소 불일치 → 보수적 반영
                        analysis.SmallDepositReflected = EstimateSmallDeposit(analysis.HousingOfficialPrice);
                        analysis.SmallDepositCase = "CASE_R12";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않았으며, 전입세대열람 제공되지 않음. 물건지주소지와 소유자주소지 불일치하여 보수적으로 소액보증금 추정반영함.";
                    }
                    else
                    {
                        // Case 13: 주소 일치 → 미반영
                        analysis.SmallDepositReflected = 0;
                        analysis.SmallDepositCase = "CASE_R13";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않았으며, 전입세대열람 제공되지 않음. 물건지주소지와 소유자주소지 일치하여 반영하지 않음.";
                    }
                }
            }
            // 경매미개시
            else if (auctionStatus == AuctionStatusEnum.NotOpened)
            {
                if (hasTenantRegistry && hasTenant)
                {
                    if (!tenantBeforeMortgage)
                    {
                        // Case 14
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_R14";
                        analysis.SmallDepositReason = "경매미개시 물건으로, 전입세대열람서상 전입인(임차인) 확인되어 소액보증금 추정 반영함.";
                    }
                    else
                    {
                        // Case 15
                        analysis.LeaseDepositReflected = analysis.LeaseDepositDd;
                        analysis.SmallDepositCase = "CASE_R15";
                        analysis.SmallDepositReason = "경매미개시 물건으로, 전입세대열람서상 전입인(임차인) 확인되며, 전입일이 근저당권설정일 이전으로 임차보증금 추정반영함.";
                    }
                }
                else if (hasTenantRegistry && !hasTenant)
                {
                    // Case 16
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_R16";
                    analysis.SmallDepositReason = "경매미개시 물건으로, 전입세대열람서상 전입인(임차인) 확인되지 않아 미반영함.";
                }
                else if (!hasTenantRegistry)
                {
                    if (!addressMatch)
                    {
                        // Case 17
                        analysis.SmallDepositReflected = EstimateSmallDeposit(analysis.HousingOfficialPrice);
                        analysis.SmallDepositCase = "CASE_R17";
                        analysis.SmallDepositReason = "경매미개시 물건으로 전입세대열람 제공되지 않음. 물건지주소지와 소유자주소지 불일치하여 보수적으로 소액보증금 추정반영함.";
                    }
                    else
                    {
                        // Case 18
                        analysis.SmallDepositReflected = 0;
                        analysis.SmallDepositCase = "CASE_R18";
                        analysis.SmallDepositReason = "경매미개시 물건으로 전입세대열람 제공되지 않음. 물건지주소지와 소유자주소지 일치하여 미반영함.";
                    }
                }
            }
        }

        /// <summary>
        /// 토지 케이스
        /// </summary>
        private void ApplyLandDepositRules(RightAnalysis analysis, AuctionStatusEnum auctionStatus,
            bool claimPassed, bool hasSurvey)
        {
            if (auctionStatus == AuctionStatusEnum.Opened && claimPassed && hasSurvey)
            {
                // Case L1
                analysis.SmallDepositReflected = 0;
                analysis.SmallDepositCase = "CASE_L1";
                analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 제시된 경매열람자료 현황조사서상 임차인 없음 확인반영함.";
            }
            else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed && hasSurvey)
            {
                // Case L2
                analysis.SmallDepositReflected = 0;
                analysis.SmallDepositCase = "CASE_L2";
                analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 제시된 경매열람자료 현황조사서상 임차인 없음 확인반영함.";
            }
            else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed && !hasSurvey)
            {
                // Case L3
                analysis.SmallDepositReflected = 0;
                analysis.SmallDepositCase = "CASE_L3";
                analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않았으나, 토지임을 고려시 임차인 없을것으로 추정되어 미반영함.";
            }
            else if (auctionStatus == AuctionStatusEnum.NotOpened)
            {
                // Case L4
                analysis.SmallDepositReflected = 0;
                analysis.SmallDepositCase = "CASE_L4";
                analysis.SmallDepositReason = "경매미개시 물건으로 토지임을 고려시 임차인 없을것으로 추정되어 미반영함.";
            }
        }

        /// <summary>
        /// 상가/공장 케이스
        /// </summary>
        private void ApplyCommercialDepositRules(RightAnalysis analysis, AuctionStatusEnum auctionStatus,
            bool claimPassed, bool hasSurvey, bool hasTenant, bool tenantClaimed, 
            bool tenantBeforeMortgage, bool hasCommercialLease, bool addressMatch)
        {
            // 경매개시 + 배당요구종기일 경과
            if (auctionStatus == AuctionStatusEnum.Opened && claimPassed)
            {
                if (hasSurvey && hasTenant)
                {
                    if (tenantClaimed)
                    {
                        // Case C1
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_C1";
                        analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 현황조사서상 임차인 확인되며 경매열람자료상 임차인 배당요구신청금액 확인반영함.";
                    }
                    else if (!tenantBeforeMortgage)
                    {
                        // Case C2
                        analysis.SmallDepositReflected = 0;
                        analysis.SmallDepositCase = "CASE_C2";
                        analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 현황조사서상 임차인 확인되며 임차인 배당요구신청 없어 미반영함.";
                    }
                    else
                    {
                        // Case C3
                        analysis.LeaseDepositReflected = analysis.LeaseDepositDd;
                        analysis.SmallDepositCase = "CASE_C3";
                        analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 현황조사서상 임차인 확인되며 임차인 배당요구신청 없어 미반영함.";
                        analysis.LeaseDepositReason = "임차일 고려하여 선순위 임차보증금 반영함.";
                    }
                }
                else if (hasSurvey && !hasTenant)
                {
                    // Case C4
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_C4";
                    analysis.SmallDepositReason = "배당요구종기일 경과 물건으로 제시된 경매열람자료 현황조사서상 임차인 없어 미반영함.";
                }
            }
            // 경매개시 + 배당요구종기일 미경과
            else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed)
            {
                if (hasSurvey && hasTenant)
                {
                    if (tenantClaimed)
                    {
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_C5";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서상 임차인 확인되며 경매열람자료상 임차인 배당요구신청금액 확인 반영함.";
                    }
                    else if (!tenantBeforeMortgage)
                    {
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_C6";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서상 임차인 확인되어 선순위소액보증금 반영함.";
                    }
                    else
                    {
                        analysis.LeaseDepositReflected = analysis.LeaseDepositDd;
                        analysis.SmallDepositCase = "CASE_C7";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서상 선순위 임차인 확인되어 선순위보증금 반영함.";
                    }
                }
                else if (hasSurvey && !hasTenant)
                {
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_C8";
                    analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 제시된 경매열람자료 현황조사서상 임차인 없어 미반영함.";
                }
                else if (!hasSurvey && hasCommercialLease)
                {
                    if (hasTenant && !tenantBeforeMortgage)
                    {
                        analysis.SmallDepositReflected = analysis.SmallDepositDd;
                        analysis.SmallDepositCase = "CASE_C9";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않음. 상가임대차열람서상 임차인 확인되어 소액보증금 추정 반영함.";
                    }
                    else if (hasTenant && tenantBeforeMortgage)
                    {
                        analysis.LeaseDepositReflected = analysis.LeaseDepositDd;
                        analysis.SmallDepositCase = "CASE_C10";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않음. 상가임대차열람서상 선순위 임차인 확인되어 선순위보증금 추정 반영함.";
                    }
                    else
                    {
                        analysis.SmallDepositReflected = 0;
                        analysis.SmallDepositCase = "CASE_C11";
                        analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않음. 상가임대차열람서상 임차인 확인되지 않아 미반영함.";
                    }
                }
                else if (!hasSurvey && !hasCommercialLease)
                {
                    // 상가 특성상 보증금 없을 것으로 추정
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_C12";
                    analysis.SmallDepositReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않았으며, 상가임대차열람서 제공되지 않음. 상가임대 특성상 선순위보증금 없을것으로 추정되어 미반영함.";
                }
            }
            // 경매미개시
            else if (auctionStatus == AuctionStatusEnum.NotOpened)
            {
                if (hasCommercialLease && hasTenant)
                {
                    analysis.SmallDepositReflected = analysis.SmallDepositDd;
                    analysis.SmallDepositCase = "CASE_C13";
                    analysis.SmallDepositReason = "경매미개시 물건으로, 상가임대차열람서상 임차인 확인되어 소액보증금 추정 반영함.";
                }
                else if (hasCommercialLease && !hasTenant)
                {
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_C14";
                    analysis.SmallDepositReason = "경매미개시 물건으로, 상가임대차열람서상 임차인 확인되지 않아 미반영함.";
                }
                else
                {
                    analysis.SmallDepositReflected = 0;
                    analysis.SmallDepositCase = "CASE_C15";
                    analysis.SmallDepositReason = "경매미개시 물건으로 상가임대차열람서 제공되지 않음. 상가임대 특성상 선순위보증금은 없을것으로 예상되어 미반영함.";
                }
            }
        }

        /// <summary>
        /// 소액보증금 추정 (공시가격 기준)
        /// </summary>
        private decimal EstimateSmallDeposit(decimal? housingOfficialPrice)
        {
            // 2024년 기준 소액임차인 최우선변제금 (서울 기준)
            // 실제로는 지역별로 다름
            const decimal defaultSmallDeposit = 55000000m; // 5,500만원

            if (housingOfficialPrice.HasValue)
            {
                // 공시가격 기준으로 추정 (임의 로직)
                if (housingOfficialPrice.Value > 900000000) // 9억 초과
                    return 55000000; // 5,500만원
                else if (housingOfficialPrice.Value > 600000000) // 6억 초과
                    return 37000000; // 3,700만원
                else
                    return 25000000; // 2,500만원
            }

            return defaultSmallDeposit;
        }

        #endregion

        #region 4. 선순위 임금채권 판단

        private void ApplyWageClaimRules(RightAnalysis analysis)
        {
            var debtorType = analysis.GetDebtorType();
            var auctionStatus = analysis.GetAuctionStatus();
            var claimPassed = analysis.ClaimDeadlinePassed;
            var hasSurvey = analysis.SurveyReportSubmitted ?? false;
            var hasWageClaim = analysis.HasWageClaim;
            var wageClaimed = analysis.WageClaimSubmitted;
            var wageSeizure = analysis.WageClaimEstimatedSeizure;

            // 개인인 경우 임금채권 발생 여지 없음
            if (debtorType == DebtorTypeEnum.Individual)
            {
                if (auctionStatus == AuctionStatusEnum.Opened && claimPassed && !wageClaimed)
                {
                    analysis.WageClaimReflected = 0;
                    analysis.WageClaimReason = "경매개시되어 배당요구종기일 경과물건으로 임금채권 배당요구신청 확인되지 않는바, 미반영함";
                }
                else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed && hasSurvey && !hasWageClaim)
                {
                    analysis.WageClaimReflected = 0;
                    analysis.WageClaimReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서상 임금채권 확인되지 않으며, 임금채권 배당요구도 없어 미반영함.";
                }
                else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed && !hasSurvey)
                {
                    analysis.WageClaimReflected = 0;
                    analysis.WageClaimReason = "경매개시되어 배당요구종기일 미경과 물건으로 현황조사서 제출되지 않았으나, 개인채무자로 임금채권 발생여지 없어 미반영함.";
                }
                else
                {
                    analysis.WageClaimReflected = 0;
                    analysis.WageClaimReason = "경매미개시 물건으로 개인채무자로 임금채권 발생여지 없어 미반영함.";
                }
            }
            // 개인사업자/법인인 경우
            else
            {
                if (auctionStatus == AuctionStatusEnum.Opened && claimPassed)
                {
                    if (wageClaimed)
                    {
                        analysis.WageClaimReflected = analysis.WageClaimDd;
                        analysis.WageClaimReason = "경매개시되어 배당요구종기일 경과물건으로 임금채권 배당요구신청 확인되어 경매열람자료 확인반영함.";
                    }
                    else
                    {
                        analysis.WageClaimReflected = 0;
                        analysis.WageClaimReason = "경매개시되어 배당요구종기일 경과물건으로 임금채권 배당요구신청 확인되지 않는바, 미반영함";
                    }
                }
                else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed)
                {
                    if (wageClaimed)
                    {
                        analysis.WageClaimReflected = analysis.WageClaimDd;
                        analysis.WageClaimReason = "경매개시되어 배당요구종기일 미경과물건으로 임금채권자의 배당요구신청 있는바, 배당요구종기일 고려하여 반영함.";
                    }
                    else if (wageSeizure)
                    {
                        analysis.WageClaimReflected = analysis.WageClaimDd;
                        analysis.WageClaimReason = "경매개시되어 배당요구종기일 미경과물건으로 임금채권 배당요구신청 없으나, 관련 가압류 확인되는바, 근로복지공단 임금자료 등 고려하여 반영함.";
                    }
                    else if (hasWageClaim)
                    {
                        analysis.WageClaimReflected = analysis.WageClaimDd;
                        analysis.WageClaimReason = "경매개시되어 배당요구종기일 미경과물건으로 임금채권 배당요구신청 없고, 관련 가압류 없으나, 근로복지공단 임금자료 등 고려하여 반영함.";
                    }
                    else
                    {
                        analysis.WageClaimReflected = 0;
                        analysis.WageClaimReason = "경매개시되어 배당요구종기일 미경과물건으로 임금채권 배당요구신청 없으나, 관련 가압류 없으며, 임금채권 없을것으로 예상되어 미반영함.";
                    }
                }
                else
                {
                    // 경매미개시
                    if (wageSeizure)
                    {
                        analysis.WageClaimReflected = analysis.WageClaimDd;
                        analysis.WageClaimReason = "경매미개시 물건으로, 임금채권 추정가압류 확인되는바 근로복지공단 임금자료 등 고려하여 추정반영함.";
                    }
                    else if (hasWageClaim)
                    {
                        analysis.WageClaimReflected = analysis.WageClaimDd;
                        analysis.WageClaimReason = "경매미개시 물건으로, 임금채권 추정가압류 확인되지 않으나, 근로복지공단 임금자료 등 고려하여 추정반영함";
                    }
                    else
                    {
                        analysis.WageClaimReflected = 0;
                        analysis.WageClaimReason = "경매미개시 물건으로, 임금채권 추정가압류 확인되지 않으며, 임금채권 없을것으로 예상되어 미반영함.";
                    }
                }
            }
        }

        #endregion

        #region 5. 당해세 및 선순위 조세채권 판단

        private void ApplyTaxRules(RightAnalysis analysis)
        {
            var auctionStatus = analysis.GetAuctionStatus();
            var claimPassed = analysis.ClaimDeadlinePassed;
            var hasTaxClaim = analysis.HasTaxClaim;
            var hasSeniorTaxClaim = analysis.HasSeniorTaxClaim;

            if (auctionStatus == AuctionStatusEnum.Opened && claimPassed)
            {
                if (hasTaxClaim && hasSeniorTaxClaim)
                {
                    analysis.CurrentTaxReflected = analysis.CurrentTaxDd;
                    analysis.SeniorTaxReflected = analysis.SeniorTaxDd;
                    analysis.CurrentTaxReason = "경매개시되어 배당요구종기일 경과물건으로 경매열람자료상 당해세 교부청구 및 선순위조세 교부청구 반영함.";
                    analysis.SeniorTaxReason = "경매열람자료상 선순위조세 교부청구서 반영함.";
                }
                else if (hasTaxClaim)
                {
                    analysis.CurrentTaxReflected = analysis.CurrentTaxDd;
                    analysis.SeniorTaxReflected = 0;
                    analysis.CurrentTaxReason = "경매개시되어 배당요구종기일 경과물건으로 경매열람자료상 당해세 교부청구 확인반영함.";
                    analysis.SeniorTaxReason = "해당사항 없음.";
                }
                else if (hasSeniorTaxClaim)
                {
                    analysis.CurrentTaxReflected = 0;
                    analysis.SeniorTaxReflected = analysis.SeniorTaxDd;
                    analysis.CurrentTaxReason = "해당사항 없음.";
                    analysis.SeniorTaxReason = "경매개시되어 배당요구종기일 경과물건으로 경매열람자료상 선순위조세 교부청구서 반영함.";
                }
                else
                {
                    analysis.CurrentTaxReflected = 0;
                    analysis.SeniorTaxReflected = 0;
                    analysis.CurrentTaxReason = "해당사항 없음.";
                    analysis.SeniorTaxReason = "해당사항 없음.";
                }
            }
            else if (auctionStatus == AuctionStatusEnum.Opened && !claimPassed)
            {
                if (hasTaxClaim && hasSeniorTaxClaim)
                {
                    analysis.CurrentTaxReflected = analysis.CurrentTaxDd;
                    analysis.SeniorTaxReflected = analysis.SeniorTaxDd;
                    analysis.CurrentTaxReason = "경매개시되어 배당요구종기일 미경과물건으로 경매열람자료상 당해세 교부청구 및 선순위조세 교부청구 반영함.";
                    analysis.SeniorTaxReason = "경매열람자료상 선순위조세 교부청구서 반영함.";
                }
                else if (hasTaxClaim)
                {
                    analysis.CurrentTaxReflected = analysis.CurrentTaxDd;
                    analysis.SeniorTaxReflected = 0;
                    analysis.CurrentTaxReason = "경매개시되어 배당요구종기일 미경과물건으로 경매열람자료상 당해세 교부청구 확인반영함.";
                    analysis.SeniorTaxReason = "해당사항 없음.";
                }
                else if (hasSeniorTaxClaim)
                {
                    analysis.CurrentTaxReflected = 0;
                    analysis.SeniorTaxReflected = analysis.SeniorTaxDd;
                    analysis.CurrentTaxReason = "해당사항 없음.";
                    analysis.SeniorTaxReason = "경매개시되어 배당요구종기일 미경과물건으로 경매열람자료상 선순위조세 교부청구서 반영함.";
                }
                else
                {
                    // 당해세 추정 반영
                    analysis.CurrentTaxReflected = EstimateCurrentTax(analysis.InitialAppraisalValue);
                    analysis.SeniorTaxReflected = 0;
                    analysis.CurrentTaxReason = "경매개시되어 배당요구종기일 미경과물건으로 당해세 외 선순위 조세 없을것으로 예상되어 당해세 추정반영함.";
                    analysis.SeniorTaxReason = "해당사항 없음.";
                }
            }
            else
            {
                // 경매미개시 → 당해세 추정
                analysis.CurrentTaxReflected = EstimateCurrentTax(analysis.InitialAppraisalValue);
                analysis.SeniorTaxReflected = 0;
                analysis.CurrentTaxReason = "경매미개시 물건으로 당해세 외 선순위 조세 없을것으로 예상되어 당해세 추정반영함.";
                analysis.SeniorTaxReason = "해당사항 없음.";
            }
        }

        /// <summary>
        /// 당해세 추정 (감정가 기준)
        /// </summary>
        private decimal EstimateCurrentTax(decimal? appraisalValue)
        {
            // 간단한 추정 로직 (실제로는 더 복잡)
            if (appraisalValue.HasValue)
            {
                // 감정가의 약 0.3% 추정
                return Math.Round(appraisalValue.Value * 0.003m, 0);
            }
            return 500000; // 기본 50만원
        }

        #endregion
    }

    /// <summary>
    /// 물건 카테고리
    /// </summary>
    public enum PropertyCategory
    {
        Residential,  // 주택 (아파트, 빌라, 다세대, 다가구, 단독주택 등)
        Land,         // 토지
        Commercial    // 상가, 공장
    }
}

