using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NPLogic.Services
{
    /// <summary>
    /// 국세청 사업자등록정보 상태조회 서비스
    /// 공공데이터포털 API: https://api.odcloud.kr/api/nts-businessman/v1/status
    /// </summary>
    public class BusinessRegistrationService
    {
        private readonly HttpClient _httpClient;
        private string? _serviceKey;

        private const string StatusApiUrl = "https://api.odcloud.kr/api/nts-businessman/v1/status";

        public BusinessRegistrationService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            LoadApiKey();
        }

        /// <summary>
        /// API 키 로드 (appsettings.env → 환경변수 → app_config)
        /// </summary>
        private void LoadApiKey()
        {
            try
            {
                // 1. appsettings.env 파일에서 로드
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var envPath = Path.Combine(basePath, "appsettings.env");
                if (File.Exists(envPath))
                {
                    foreach (var line in File.ReadAllLines(envPath))
                    {
                        if (line.StartsWith("NTS_BUSINESS_API_KEY="))
                        {
                            _serviceKey = line.Substring("NTS_BUSINESS_API_KEY=".Length).Trim();
                            if (!string.IsNullOrEmpty(_serviceKey))
                            {
                                Debug.WriteLine("[BusinessRegistrationService] appsettings.env에서 API 키 로드 완료");
                                return;
                            }
                        }
                    }
                }

                // 2. 환경변수에서 로드
                _serviceKey = Environment.GetEnvironmentVariable("NTS_BUSINESS_API_KEY");
                if (!string.IsNullOrEmpty(_serviceKey))
                {
                    Debug.WriteLine("[BusinessRegistrationService] 환경변수에서 API 키 로드 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BusinessRegistrationService] API 키 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// API 키 설정 여부
        /// </summary>
        public bool HasApiKey => !string.IsNullOrEmpty(_serviceKey);

        /// <summary>
        /// API 키 수동 설정
        /// </summary>
        public void SetApiKey(string key)
        {
            _serviceKey = key;
        }

        /// <summary>
        /// 사업자등록번호로 상태 조회
        /// </summary>
        /// <param name="businessNumber">사업자등록번호 (10자리, 하이픈 포함 가능)</param>
        /// <returns>조회 결과 또는 null</returns>
        public async Task<BusinessStatusResult?> GetStatusAsync(string businessNumber)
        {
            if (string.IsNullOrWhiteSpace(businessNumber))
                return null;

            if (string.IsNullOrEmpty(_serviceKey))
            {
                Debug.WriteLine("[BusinessRegistrationService] API 키가 설정되지 않음");
                return null;
            }

            // 하이픈 제거, 숫자만 추출
            var cleanNumber = businessNumber.Replace("-", "").Replace(" ", "").Trim();
            if (cleanNumber.Length != 10)
            {
                Debug.WriteLine($"[BusinessRegistrationService] 사업자번호 형식 오류: {cleanNumber} ({cleanNumber.Length}자리)");
                return null;
            }

            try
            {
                var url = $"{StatusApiUrl}?serviceKey={Uri.EscapeDataString(_serviceKey)}";

                var requestBody = new { b_no = new[] { cleanNumber } };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                Debug.WriteLine($"[BusinessRegistrationService] 상태조회 요청: {cleanNumber}");

                var response = await _httpClient.PostAsync(url, jsonContent);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[BusinessRegistrationService] API 응답 오류: {response.StatusCode} - {content}");
                    return null;
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                // match_cnt 확인
                if (!root.TryGetProperty("match_cnt", out var matchCnt) || matchCnt.GetInt32() == 0)
                {
                    // data 배열에서 에러 메시지 확인
                    if (root.TryGetProperty("data", out var dataArr) && dataArr.GetArrayLength() > 0)
                    {
                        var firstItem = dataArr[0];
                        var taxType = firstItem.TryGetProperty("tax_type", out var tt) ? tt.GetString() : null;
                        if (!string.IsNullOrEmpty(taxType) && taxType.Contains("등록되지 않은"))
                        {
                            return new BusinessStatusResult
                            {
                                BusinessNumber = cleanNumber,
                                Status = "미등록",
                                StatusCode = "00",
                                TaxType = taxType
                            };
                        }
                    }
                    Debug.WriteLine("[BusinessRegistrationService] 조회 결과 없음");
                    return null;
                }

                if (root.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    var item = data[0];
                    var result = new BusinessStatusResult
                    {
                        BusinessNumber = item.TryGetProperty("b_no", out var bno) ? bno.GetString() ?? cleanNumber : cleanNumber,
                        Status = item.TryGetProperty("b_stt", out var bstt) ? bstt.GetString() ?? "" : "",
                        StatusCode = item.TryGetProperty("b_stt_cd", out var bsttCd) ? bsttCd.GetString() ?? "" : "",
                        TaxType = item.TryGetProperty("tax_type", out var taxTypeVal) ? taxTypeVal.GetString() ?? "" : "",
                        TaxTypeCode = item.TryGetProperty("tax_type_cd", out var taxTypeCd) ? taxTypeCd.GetString() ?? "" : "",
                        ClosingDate = item.TryGetProperty("end_dt", out var endDt) ? endDt.GetString() ?? "" : "",
                        InvoiceApplyDate = item.TryGetProperty("invoice_apply_dt", out var invDt) ? invDt.GetString() ?? "" : ""
                    };

                    Debug.WriteLine($"[BusinessRegistrationService] 조회 성공: {result.Status} ({result.TaxType})");
                    return result;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BusinessRegistrationService] 조회 실패: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    /// 사업자등록 상태조회 결과
    /// </summary>
    public class BusinessStatusResult
    {
        /// <summary>사업자등록번호</summary>
        public string BusinessNumber { get; set; } = "";

        /// <summary>사업자 상태 (계속사업자, 휴업자, 폐업자)</summary>
        public string Status { get; set; } = "";

        /// <summary>상태코드 (01=계속, 02=휴업, 03=폐업)</summary>
        public string StatusCode { get; set; } = "";

        /// <summary>과세유형 (부가가치세 일반과세자 등)</summary>
        public string TaxType { get; set; } = "";

        /// <summary>과세유형코드</summary>
        public string TaxTypeCode { get; set; } = "";

        /// <summary>폐업일자 (YYYYMMDD)</summary>
        public string ClosingDate { get; set; } = "";

        /// <summary>세금계산서적용일자</summary>
        public string InvoiceApplyDate { get; set; } = "";

        /// <summary>사업 중인지 여부</summary>
        public bool IsActive => StatusCode == "01";

        /// <summary>휴업 여부</summary>
        public bool IsSuspended => StatusCode == "02";

        /// <summary>폐업 여부</summary>
        public bool IsClosed => StatusCode == "03";

        /// <summary>표시용 요약 텍스트</summary>
        public string DisplaySummary
        {
            get
            {
                if (string.IsNullOrEmpty(Status)) return "조회 실패";
                var summary = Status;
                if (!string.IsNullOrEmpty(TaxType) && !TaxType.Contains("등록되지 않은"))
                    summary += $" | {TaxType}";
                if (IsClosed && !string.IsNullOrEmpty(ClosingDate) && ClosingDate.Length == 8)
                    summary += $" | 폐업일: {ClosingDate.Substring(0, 4)}-{ClosingDate.Substring(4, 2)}-{ClosingDate.Substring(6, 2)}";
                return summary;
            }
        }
    }
}
