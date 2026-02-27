using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NPLogic.Services
{
    /// <summary>
    /// 외부 Supabase 프로젝트에서 실거래가 데이터를 조회하는 서비스
    /// RPC 함수(get_trades)를 통해 조회하며, 서비스 계정 인증 필요
    /// </summary>
    public class TradeService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const string BaseUrl = "https://gkchpqzbpnmhzcjhsodf.supabase.co";
        private const string ApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImdrY2hwcXpicG5taHpjamhzb2RmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzE0OTE0MDcsImV4cCI6MjA4NzA2NzQwN30.nTz2apTANkaFOTVPbCRdQLSMAbKA52ZSgE0z_iqh-fM";
        private const string ServiceEmail = "wpf-service@nplogic-map.com";
        private const string ServicePassword = "NpL0gic#Wpf2026!svc";
        private const string ClientUserId = "NPLogic-WPF";

        private static string? _accessToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly SemaphoreSlim _loginLock = new(1, 1);

        /// <summary>
        /// 서비스 계정으로 로그인하여 access_token 획득
        /// </summary>
        private static async Task EnsureAuthenticatedAsync()
        {
            // 토큰이 유효하면 (만료 5분 전 여유) 스킵
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                return;

            await _loginLock.WaitAsync();
            try
            {
                // 더블 체크
                if (_accessToken != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                    return;

                Debug.WriteLine("[TradeService] 서비스 계정 로그인 시도...");

                var loginUrl = $"{BaseUrl}/auth/v1/token?grant_type=password";
                var body = JsonSerializer.Serialize(new { email = ServiceEmail, password = ServicePassword });

                using var request = new HttpRequestMessage(HttpMethod.Post, loginUrl);
                request.Headers.Add("apikey", ApiKey);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[TradeService] 로그인 실패: {response.StatusCode} - {responseJson}");
                    return;
                }

                using var doc = JsonDocument.Parse(responseJson);
                _accessToken = doc.RootElement.GetProperty("access_token").GetString();
                var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

                Debug.WriteLine($"[TradeService] 로그인 성공 (만료: {expiresIn}초 후)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TradeService] 로그인 예외: {ex.Message}");
            }
            finally
            {
                _loginLock.Release();
            }
        }

        /// <summary>
        /// PNU로 실거래가 조회 (RPC: get_trades)
        /// </summary>
        public async Task<List<TradeRecord>> GetTradesByPnuAsync(string pnu, string? category = null, string? clientUserId = null, int limit = 100)
        {
            try
            {
                await EnsureAuthenticatedAsync();
                if (string.IsNullOrEmpty(_accessToken))
                {
                    Debug.WriteLine("[TradeService] 인증 토큰 없음 - 조회 불가");
                    return new();
                }

                var rpcUrl = $"{BaseUrl}/rest/v1/rpc/get_trades";

                var bodyObj = new Dictionary<string, object?>
                {
                    ["p_pnu"] = pnu,
                    ["p_client_user_id"] = clientUserId ?? ClientUserId,
                    ["p_limit"] = limit
                };
                if (!string.IsNullOrEmpty(category))
                    bodyObj["p_category"] = category;

                var body = JsonSerializer.Serialize(bodyObj);

                Debug.WriteLine($"[TradeService] RPC 요청: pnu={pnu}, category={category ?? "(전체)"}");

                using var request = new HttpRequestMessage(HttpMethod.Post, rpcUrl);
                request.Headers.Add("apikey", ApiKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[TradeService] RPC 실패: {response.StatusCode} - {json}");
                    return new();
                }

                var result = JsonSerializer.Deserialize<List<TradeRecord>>(json, _jsonOptions) ?? new();
                Debug.WriteLine($"[TradeService] 응답: {result.Count}건");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TradeService] 실거래가 조회 실패: {ex.Message}");
                return new();
            }
        }
    }

    /// <summary>
    /// 실거래가 DB 레코드
    /// </summary>
    public class TradeRecord
    {
        [JsonPropertyName("area")]
        public double Area { get; set; }

        [JsonPropertyName("deal_date")]
        public int DealDate { get; set; }

        [JsonPropertyName("deal_amount")]
        public int DealAmount { get; set; }

        [JsonPropertyName("floor")]
        public string? Floor { get; set; }

        [JsonPropertyName("reg_date")]
        public string? RegDate { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("dong")]
        public string? Dong { get; set; }

        /// <summary>등기여부 (reg_date가 null이면 미등기)</summary>
        public bool IsRegistered => !string.IsNullOrEmpty(RegDate);
    }
}
