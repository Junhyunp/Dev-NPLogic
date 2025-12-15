using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NPLogic.Services
{
    /// <summary>
    /// 유사물건 추천 서비스
    /// Python recommend_processor.py를 호출하여 유사 경매 사례를 추천합니다.
    /// </summary>
    public class RecommendService : IDisposable
    {
        private bool _disposed;
        private readonly string _pythonPath;
        private readonly string _scriptPath;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Python 스크립트 기본 경로
        /// </summary>
        private const string DefaultPythonPath = "python";
        private const string ScriptFileName = "recommend_processor.py";

        public RecommendService(string? pythonPath = null, string? scriptPath = null)
        {
            _pythonPath = pythonPath ?? DefaultPythonPath;
            _scriptPath = scriptPath ?? FindScriptPath();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            };
        }

        /// <summary>
        /// 스크립트 경로 자동 탐색
        /// </summary>
        private string FindScriptPath()
        {
            var searchPaths = new List<string>
            {
                // 1. 앱 실행 디렉토리의 python 폴더
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", ScriptFileName),
                
                // 2. 개발 환경: 프로젝트 상대 경로
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", 
                    "python", ScriptFileName),
                
                // 3. 프로젝트 루트 python 폴더
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", 
                    "python", ScriptFileName),
            };

            foreach (var path in searchPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // 기본값 반환 (존재하지 않을 수 있음)
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python", ScriptFileName);
        }

        /// <summary>
        /// 유사물건 추천을 실행합니다.
        /// </summary>
        /// <param name="subject">대상 물건 정보</param>
        /// <param name="options">추천 옵션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>추천 결과</returns>
        public async Task<RecommendResult> RecommendAsync(
            RecommendSubject subject,
            RecommendOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= new RecommendOptions();

            if (!File.Exists(_scriptPath))
            {
                return new RecommendResult
                {
                    Success = false,
                    Error = $"추천 스크립트를 찾을 수 없습니다: {_scriptPath}"
                };
            }

            try
            {
                // 대상 물건 정보를 JSON으로 직렬화
                var subjectJson = JsonSerializer.Serialize(subject, _jsonOptions);

                // Python 프로세스 실행
                var result = await RunPythonProcessAsync(subjectJson, options, cancellationToken);
                return result;
            }
            catch (TaskCanceledException)
            {
                return new RecommendResult
                {
                    Success = false,
                    Error = "추천이 취소되었습니다."
                };
            }
            catch (Exception ex)
            {
                return new RecommendResult
                {
                    Success = false,
                    Error = $"추천 실행 오류: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Python 프로세스를 실행하고 결과를 반환합니다.
        /// </summary>
        private async Task<RecommendResult> RunPythonProcessAsync(
            string subjectJson,
            RecommendOptions options,
            CancellationToken cancellationToken)
        {
            var arguments = new StringBuilder();
            arguments.Append($"\"{_scriptPath}\"");
            arguments.Append($" --subject-json \"{EscapeJsonForCommandLine(subjectJson)}\"");

            if (options.RuleIndex.HasValue)
            {
                arguments.Append($" --rule-index {options.RuleIndex.Value}");
            }

            if (options.SimilarLand)
            {
                arguments.Append(" --similar-land");
            }

            if (!string.IsNullOrEmpty(options.RegionScope))
            {
                arguments.Append($" --region-scope {options.RegionScope}");
            }

            if (options.TopK > 0)
            {
                arguments.Append($" --topk {options.TopK}");
            }

            if (!string.IsNullOrEmpty(options.CandidatesSource))
            {
                arguments.Append($" --candidates-source {options.CandidatesSource}");
            }

            if (!string.IsNullOrEmpty(options.CandidatesPath))
            {
                arguments.Append($" --candidates-path \"{options.CandidatesPath}\"");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = arguments.ToString(),
                WorkingDirectory = Path.GetDirectoryName(_scriptPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Supabase 환경변수 전달
            if (!string.IsNullOrEmpty(options.SupabaseUrl))
            {
                startInfo.EnvironmentVariables["SUPABASE_URL"] = options.SupabaseUrl;
            }
            if (!string.IsNullOrEmpty(options.SupabaseKey))
            {
                startInfo.EnvironmentVariables["SUPABASE_KEY"] = options.SupabaseKey;
            }

            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // 프로세스 완료 또는 취소 대기
            var tcs = new TaskCompletionSource<bool>();
            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch { }
                tcs.TrySetCanceled();
            });

            var exitTask = Task.Run(() =>
            {
                process.WaitForExit();
                return true;
            }, cancellationToken);

            await Task.WhenAny(exitTask, tcs.Task);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();

            if (process.ExitCode != 0)
            {
                return new RecommendResult
                {
                    Success = false,
                    Error = !string.IsNullOrEmpty(error) ? error : $"프로세스 종료 코드: {process.ExitCode}"
                };
            }

            // JSON 결과 파싱
            try
            {
                var result = JsonSerializer.Deserialize<RecommendResult>(output, _jsonOptions);
                return result ?? new RecommendResult
                {
                    Success = false,
                    Error = "결과 파싱 실패"
                };
            }
            catch (JsonException ex)
            {
                return new RecommendResult
                {
                    Success = false,
                    Error = $"JSON 파싱 오류: {ex.Message}\n출력: {output}"
                };
            }
        }

        /// <summary>
        /// 명령줄용 JSON 이스케이프
        /// </summary>
        private static string EscapeJsonForCommandLine(string json)
        {
            // 쌍따옴표를 이스케이프
            return json.Replace("\"", "\\\"");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }

    #region Models

    /// <summary>
    /// 추천 대상 물건 정보
    /// </summary>
    public class RecommendSubject
    {
        [JsonPropertyName("property_id")]
        public string? PropertyId { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("usage")]
        public string? Usage { get; set; }

        [JsonPropertyName("region_big")]
        public string? RegionBig { get; set; }

        [JsonPropertyName("region_mid")]
        public string? RegionMid { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("building_area")]
        public double? BuildingArea { get; set; }

        [JsonPropertyName("land_area")]
        public double? LandArea { get; set; }

        [JsonPropertyName("building_appraisal_price")]
        public decimal? BuildingAppraisalPrice { get; set; }

        [JsonPropertyName("land_appraisal_price")]
        public decimal? LandAppraisalPrice { get; set; }

        [JsonPropertyName("auction_date")]
        public string? AuctionDate { get; set; }
    }

    /// <summary>
    /// 추천 옵션
    /// </summary>
    public class RecommendOptions
    {
        /// <summary>
        /// 적용할 규칙 번호 (1-based, null이면 전체)
        /// </summary>
        public int? RuleIndex { get; set; }

        /// <summary>
        /// 토지 유사 모드 (PLANT_WAREHOUSE_ETC, OTHER_BIG용)
        /// </summary>
        public bool SimilarLand { get; set; }

        /// <summary>
        /// 지역 범위 (big=시도, mid=시군구)
        /// </summary>
        public string RegionScope { get; set; } = "big";

        /// <summary>
        /// 반환할 최대 건수
        /// </summary>
        public int TopK { get; set; } = 10;

        /// <summary>
        /// 후보군 데이터 소스 (supabase, json, excel)
        /// </summary>
        public string CandidatesSource { get; set; } = "supabase";

        /// <summary>
        /// 후보군 JSON/Excel 파일 경로
        /// </summary>
        public string? CandidatesPath { get; set; }

        /// <summary>
        /// Supabase URL
        /// </summary>
        public string? SupabaseUrl { get; set; }

        /// <summary>
        /// Supabase API 키
        /// </summary>
        public string? SupabaseKey { get; set; }
    }

    /// <summary>
    /// 추천 결과
    /// </summary>
    public class RecommendResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("subject")]
        public RecommendSubjectInfo? Subject { get; set; }

        [JsonPropertyName("rule_results")]
        public Dictionary<string, List<RecommendCase>>? RuleResults { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("config")]
        public RecommendConfig? Config { get; set; }
    }

    /// <summary>
    /// 추천 결과의 대상 물건 요약 정보
    /// </summary>
    public class RecommendSubjectInfo
    {
        [JsonPropertyName("property_id")]
        public string? PropertyId { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("usage")]
        public string? Usage { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }
    }

    /// <summary>
    /// 추천된 경매 사례
    /// </summary>
    public class RecommendCase
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("case_no")]
        public string? CaseNo { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("usage")]
        public string? Usage { get; set; }

        [JsonPropertyName("auction_date")]
        public string? AuctionDate { get; set; }

        [JsonPropertyName("appraisal_price")]
        public decimal? AppraisalPrice { get; set; }

        [JsonPropertyName("winning_price")]
        public decimal? WinningPrice { get; set; }

        [JsonPropertyName("building_area")]
        public double? BuildingArea { get; set; }

        [JsonPropertyName("land_area")]
        public double? LandArea { get; set; }

        [JsonPropertyName("building_unit_price")]
        public decimal? BuildingUnitPrice { get; set; }

        [JsonPropertyName("land_unit_price")]
        public decimal? LandUnitPrice { get; set; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

        [JsonPropertyName("_rule_name")]
        public string? RuleName { get; set; }

        [JsonPropertyName("_rule_index")]
        public int? RuleIndex { get; set; }

        [JsonPropertyName("_category")]
        public string? Category { get; set; }

        /// <summary>
        /// 낙찰율 (낙찰가/감정가)
        /// </summary>
        public decimal? WinningRate => 
            AppraisalPrice > 0 ? (WinningPrice / AppraisalPrice) * 100 : null;
    }

    /// <summary>
    /// 추천 설정 정보
    /// </summary>
    public class RecommendConfig
    {
        [JsonPropertyName("rule_index")]
        public int? RuleIndex { get; set; }

        [JsonPropertyName("similar_land")]
        public bool SimilarLand { get; set; }

        [JsonPropertyName("region_scope")]
        public string? RegionScope { get; set; }

        [JsonPropertyName("topk")]
        public int TopK { get; set; }
    }

    #endregion
}

