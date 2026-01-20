using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NPLogic.Services
{
    /// <summary>
    /// 데이터디스크 데이터 변환 서비스
    /// 상대방이 정의한 데이터 변환 규칙 적용
    /// </summary>
    public static class DataTransformService
    {
        /// <summary>
        /// 행 데이터에 모든 변환 규칙 적용
        /// </summary>
        public static void ApplyAllTransformations(Dictionary<string, object> row, BankType bankType, string sheetType)
        {
            // 자산유형 변환
            TransformAssetType(row);

            // 시트별 변환
            switch (sheetType)
            {
                case "채권일반정보":
                    TransformLoanData(row);
                    break;
                case "물건정보":
                    TransformPropertyData(row);
                    break;
                case "회생차주정보":
                    TransformRestructuringData(row, bankType);
                    break;
                case "신용보증서":
                    TransformGuaranteeData(row);
                    break;
            }
        }

        #region 자산유형 변환

        /// <summary>
        /// 자산유형: R/S → Regular/Special 변환
        /// </summary>
        public static void TransformAssetType(Dictionary<string, object> row)
        {
            var assetTypeKeys = new[] { "자산유형", "채권구분", "Pool 구분", "Pool" };
            
            foreach (var key in assetTypeKeys)
            {
                if (row.TryGetValue(key, out var value) && value != null)
                {
                    var strValue = value.ToString()?.Trim().ToUpper();
                    if (strValue == "R")
                    {
                        row[key] = "Regular";
                    }
                    else if (strValue == "S")
                    {
                        row[key] = "Special";
                    }
                }
            }
        }

        #endregion

        #region 채권일반정보 변환

        /// <summary>
        /// 채권일반정보 데이터 변환
        /// </summary>
        public static void TransformLoanData(Dictionary<string, object> row)
        {
            // 대출일련번호 변환: 숫자 → "차주일련번호-대출일련번호" 형식
            TransformLoanSerialNumber(row);

            // 이자율 자동 계산
            TransformInterestRates(row);

            // 채권액 합계 계산
            CalculateTotalClaimAmount(row);
        }

        /// <summary>
        /// 대출일련번호: 숫자 → "차주일련번호-대출일련번호" 형식
        /// </summary>
        private static void TransformLoanSerialNumber(Dictionary<string, object> row)
        {
            var loanSerialKeys = new[] { "대출일련번호", "채권일련번호", "계좌일련번호" };
            var borrowerNumberKeys = new[] { "차주일련번호", "차주번호" };

            string? borrowerNumber = null;
            foreach (var key in borrowerNumberKeys)
            {
                if (row.TryGetValue(key, out var value) && value != null)
                {
                    borrowerNumber = value.ToString()?.Trim();
                    break;
                }
            }

            foreach (var key in loanSerialKeys)
            {
                if (row.TryGetValue(key, out var value) && value != null)
                {
                    var strValue = value.ToString()?.Trim();
                    
                    // 숫자만 있는 경우 "차주일련번호-대출일련번호" 형식으로 변환
                    if (!string.IsNullOrEmpty(strValue) && 
                        !strValue.Contains("-") && 
                        IsNumeric(strValue) &&
                        !string.IsNullOrEmpty(borrowerNumber))
                    {
                        row[key] = $"{borrowerNumber}-{strValue}";
                        System.Diagnostics.Debug.WriteLine($"[TransformLoanSerialNumber] {key}: {strValue} → {row[key]}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// 이자율 자동 계산
        /// - 정상 이자율만 있는 경우: 연체 이자율 = 정상 + 3%
        /// - 연체 이자율만 있는 경우: 정상 이자율 = 연체 - 3%
        /// </summary>
        private static void TransformInterestRates(Dictionary<string, object> row)
        {
            var normalRateKeys = new[] { "정상이자율", "약정이자율(%)", "약정이자율" };
            var overdueRateKeys = new[] { "연체이자율", "Cutoff적용이자율(%)", "Cutoff적용이자율" };

            decimal? normalRate = null;
            decimal? overdueRate = null;
            string? normalRateKey = null;
            string? overdueRateKey = null;

            // 정상 이자율 찾기
            foreach (var key in normalRateKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var rate))
                {
                    normalRate = rate;
                    normalRateKey = key;
                    break;
                }
            }

            // 연체 이자율 찾기
            foreach (var key in overdueRateKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var rate))
                {
                    overdueRate = rate;
                    overdueRateKey = key;
                    break;
                }
            }

            // 자동 계산
            if (normalRate.HasValue && !overdueRate.HasValue)
            {
                // 정상만 있음 → 연체 = 정상 + 3%
                var calculatedOverdue = normalRate.Value + 0.03m;
                if (overdueRateKey != null)
                {
                    row[overdueRateKey] = calculatedOverdue;
                }
                else
                {
                    row["연체이자율"] = calculatedOverdue;
                }
                System.Diagnostics.Debug.WriteLine($"[TransformInterestRates] 연체이자율 자동 계산: {normalRate:P2} + 3% = {calculatedOverdue:P2}");
            }
            else if (!normalRate.HasValue && overdueRate.HasValue)
            {
                // 연체만 있음 → 정상 = 연체 - 3%
                var calculatedNormal = Math.Max(0, overdueRate.Value - 0.03m);
                if (normalRateKey != null)
                {
                    row[normalRateKey] = calculatedNormal;
                }
                else
                {
                    row["정상이자율"] = calculatedNormal;
                }
                System.Diagnostics.Debug.WriteLine($"[TransformInterestRates] 정상이자율 자동 계산: {overdueRate:P2} - 3% = {calculatedNormal:P2}");
            }
        }

        /// <summary>
        /// 채권액 합계 계산: 환산된 대출잔액 + 가지급금 + 미수이자
        /// </summary>
        private static void CalculateTotalClaimAmount(Dictionary<string, object> row)
        {
            var totalKeys = new[] { "채권액 합계", "채권액합계", "채권액합계(E=C+D)" };
            
            // 이미 값이 있는지 확인
            foreach (var key in totalKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var existing) && existing > 0)
                {
                    return; // 이미 값이 있으면 건너뛰기
                }
            }

            // 계산에 필요한 값 가져오기
            decimal loanBalance = 0;
            decimal advancePayment = 0;
            decimal accruedInterest = 0;

            var loanBalanceKeys = new[] { "환산된 대출잔액", "환산후대출원금잔액", "환산후원금잔액", "미상환원금잔액", "미상환원금잔액(C=A+B)" };
            var advanceKeys = new[] { "가지급금", "가지급금잔액", "가지급금잔액(B)", "가직브금잔액" };
            var interestKeys = new[] { "미수이자", "미수이자잔액", "미수이자잔액(D)" };

            foreach (var key in loanBalanceKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var dec))
                {
                    loanBalance = dec;
                    break;
                }
            }

            foreach (var key in advanceKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var dec))
                {
                    advancePayment = dec;
                    break;
                }
            }

            foreach (var key in interestKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var dec))
                {
                    accruedInterest = dec;
                    break;
                }
            }

            // 합계 계산
            var total = loanBalance + advancePayment + accruedInterest;
            if (total > 0)
            {
                row["채권액 합계"] = total;
                System.Diagnostics.Debug.WriteLine($"[CalculateTotalClaimAmount] 채권액 합계 자동 계산: {loanBalance:N0} + {advancePayment:N0} + {accruedInterest:N0} = {total:N0}");
            }
        }

        #endregion

        #region 물건정보 변환

        /// <summary>
        /// 물건정보 데이터 변환
        /// </summary>
        public static void TransformPropertyData(Dictionary<string, object> row)
        {
            // 선순위 소액보증금 합산
            CalculateSeniorSmallDeposit(row);

            // 선순위 임차보증금 합산
            CalculateSeniorLeaseDeposit(row);
        }

        /// <summary>
        /// 선순위 소액보증금 합산: 주택 + 상가
        /// </summary>
        private static void CalculateSeniorSmallDeposit(Dictionary<string, object> row)
        {
            var totalKeys = new[] { "선순위 소액보증금", "선순위소액보증금" };
            
            // 이미 값이 있는지 확인
            foreach (var key in totalKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var existing) && existing > 0)
                {
                    return;
                }
            }

            // 주택 + 상가 합산
            decimal housing = 0;
            decimal commercial = 0;

            var housingKeys = new[] { "선순위 주택 소액보증금", "선순위소액보증금(주택)", "소액임대차보증금(주택)" };
            var commercialKeys = new[] { "선순위 상가 소액보증금", "선순위소액보증금(상가)", "소액임대차보증금(상가)" };

            foreach (var key in housingKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var dec))
                {
                    housing = dec;
                    break;
                }
            }

            foreach (var key in commercialKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var dec))
                {
                    commercial = dec;
                    break;
                }
            }

            var total = housing + commercial;
            if (total > 0)
            {
                row["선순위 소액보증금"] = total;
                System.Diagnostics.Debug.WriteLine($"[CalculateSeniorSmallDeposit] 선순위 소액보증금 자동 계산: {housing:N0} + {commercial:N0} = {total:N0}");
            }
        }

        /// <summary>
        /// 선순위 임차보증금 합산: 주택 + 상가
        /// </summary>
        private static void CalculateSeniorLeaseDeposit(Dictionary<string, object> row)
        {
            var totalKeys = new[] { "선순위 임차보증금", "선순위임차보증금" };
            
            // 이미 값이 있는지 확인
            foreach (var key in totalKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var existing) && existing > 0)
                {
                    return;
                }
            }

            // 주택 + 상가 합산
            decimal housing = 0;
            decimal commercial = 0;

            var housingKeys = new[] { "선순위 주택 임차보증금", "선순위임차보증금(주택)", "선순위임대차보증금(주택)" };
            var commercialKeys = new[] { "선순위 상가 임차보증금", "선순위임차보증금(상가)", "선순위임대차보증금(상가)" };

            foreach (var key in housingKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var dec))
                {
                    housing = dec;
                    break;
                }
            }

            foreach (var key in commercialKeys)
            {
                if (row.TryGetValue(key, out var value) && TryParseDecimal(value, out var dec))
                {
                    commercial = dec;
                    break;
                }
            }

            var total = housing + commercial;
            if (total > 0)
            {
                row["선순위 임차보증금"] = total;
                System.Diagnostics.Debug.WriteLine($"[CalculateSeniorLeaseDeposit] 선순위 임차보증금 자동 계산: {housing:N0} + {commercial:N0} = {total:N0}");
            }
        }

        #endregion

        #region 회생차주정보 변환

        /// <summary>
        /// 회생차주정보 데이터 변환 (NH, SHB 특화)
        /// </summary>
        public static void TransformRestructuringData(Dictionary<string, object> row, BankType bankType)
        {
            if (bankType == BankType.NH || bankType == BankType.SHB)
            {
                // 상장/비상장 정규화
                NormalizeListingStatus(row);

                // 종업원수 정규화
                NormalizeEmployeeCount(row);
            }
        }

        /// <summary>
        /// 상장/비상장: 비상장/상장/공백 문자열만 추출
        /// </summary>
        private static void NormalizeListingStatus(Dictionary<string, object> row)
        {
            var keys = new[] { "상장/비상장", "상장여부" };

            foreach (var key in keys)
            {
                if (row.TryGetValue(key, out var value) && value != null)
                {
                    var strValue = value.ToString()?.Trim() ?? "";
                    
                    if (strValue.Contains("비상장"))
                    {
                        row[key] = "비상장";
                    }
                    else if (strValue.Contains("상장"))
                    {
                        row[key] = "상장";
                    }
                    else if (string.IsNullOrWhiteSpace(strValue))
                    {
                        row[key] = "";
                    }
                    
                    break;
                }
            }
        }

        /// <summary>
        /// 종업원수: 00명(~~~) → 00명만 추출
        /// </summary>
        private static void NormalizeEmployeeCount(Dictionary<string, object> row)
        {
            if (row.TryGetValue("종업원수", out var value) && value != null)
            {
                var strValue = value.ToString()?.Trim() ?? "";
                
                // "123명(기타정보)" 패턴에서 "123명"만 추출
                var match = Regex.Match(strValue, @"(\d+)\s*명");
                if (match.Success)
                {
                    row["종업원수"] = $"{match.Groups[1].Value}명";
                    System.Diagnostics.Debug.WriteLine($"[NormalizeEmployeeCount] 종업원수 정규화: {strValue} → {row["종업원수"]}");
                }
            }
        }

        #endregion

        #region 신용보증서 변환

        /// <summary>
        /// 신용보증서 데이터 변환
        /// </summary>
        public static void TransformGuaranteeData(Dictionary<string, object> row)
        {
            // 계좌일련번호 변환: 여러 숫자 → 각각 분리 필요 (복잡한 경우 별도 처리)
            TransformGuaranteeAccountSerial(row);
        }

        /// <summary>
        /// 신용보증서 계좌일련번호 변환
        /// - 여러 숫자인 경우 분리 필요 → "차주일련번호_계좌일련번호" 형식
        /// </summary>
        private static void TransformGuaranteeAccountSerial(Dictionary<string, object> row)
        {
            var serialKeys = new[] { "계좌일련번호", "관련대출채권일련번호" };
            var borrowerNumberKeys = new[] { "차주일련번호", "차주번호" };

            string? borrowerNumber = null;
            foreach (var key in borrowerNumberKeys)
            {
                if (row.TryGetValue(key, out var value) && value != null)
                {
                    borrowerNumber = value.ToString()?.Trim();
                    break;
                }
            }

            foreach (var key in serialKeys)
            {
                if (row.TryGetValue(key, out var value) && value != null)
                {
                    var strValue = value.ToString()?.Trim();
                    
                    // 숫자만 있는 경우 "차주일련번호_계좌일련번호" 형식으로 변환
                    if (!string.IsNullOrEmpty(strValue) && 
                        !strValue.Contains("_") && 
                        IsNumeric(strValue) &&
                        !string.IsNullOrEmpty(borrowerNumber))
                    {
                        row[key] = $"{borrowerNumber}_{strValue}";
                        System.Diagnostics.Debug.WriteLine($"[TransformGuaranteeAccountSerial] {key}: {strValue} → {row[key]}");
                    }
                    break;
                }
            }
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 숫자 여부 확인
        /// </summary>
        private static bool IsNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            
            return decimal.TryParse(value.Replace(",", ""), out _);
        }

        /// <summary>
        /// decimal 파싱 시도
        /// </summary>
        private static bool TryParseDecimal(object? value, out decimal result)
        {
            result = 0;
            if (value == null)
                return false;

            if (value is decimal d)
            {
                result = d;
                return true;
            }

            if (value is double dbl)
            {
                result = (decimal)dbl;
                return true;
            }

            if (value is int i)
            {
                result = i;
                return true;
            }

            if (value is long l)
            {
                result = l;
                return true;
            }

            var strValue = value.ToString()?.Replace(",", "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(strValue) || strValue == "-")
                return false;

            // 백분율 처리
            if (strValue.EndsWith("%"))
            {
                strValue = strValue.TrimEnd('%');
                if (decimal.TryParse(strValue, out var pctValue))
                {
                    result = pctValue / 100;
                    return true;
                }
            }

            return decimal.TryParse(strValue, out result);
        }

        #endregion
    }
}
