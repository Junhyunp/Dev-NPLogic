using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Services;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 등기부 Repository
    /// </summary>
    public class RegistryRepository
    {
        private readonly SupabaseService _supabaseService;
        private static readonly HttpClient _edgeFunctionsHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        private const string OcrRegistrySaveFunctionName = "ocr-registry-save";

        public RegistryRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        #region RegistryRun / BasicInfo / Gapgu / Eulgu (정제 산출물 스키마)

        /// <summary>
        /// OCR 데이터가 있는 모든 property_id를 반환 (사이드바 OCR 표시용)
        /// </summary>
        public async Task<HashSet<Guid>> GetAllOcrPropertyIdsAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRunTable>()
                    .Select("property_id")
                    .Get();

                return response.Models
                    .Where(r => r.PropertyId.HasValue)
                    .Select(r => r.PropertyId!.Value)
                    .ToHashSet();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegistryRepository] OCR property IDs 조회 실패: {ex.Message}");
                return new HashSet<Guid>();
            }
        }

        public async Task<List<RegistryRun>> GetRunsByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRunTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.DeedSeq, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToRegistryRun).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 세트 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<RegistryRun?> GetLatestRunByPropertyIdAsync(Guid propertyId)
        {
            var runs = await GetRunsByPropertyIdAsync(propertyId);
            return runs.FirstOrDefault();
        }

        /// <summary>
        /// 물건 ID 기준으로 모든 run의 basic_info를 조회 (여러 PDF = 여러 행)
        /// </summary>
        public async Task<List<RegistryBasicInfo>> GetBasicInfoListByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryBasicInfoTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.JibeonId, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryBasicInfo).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"basic_info 리스트 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건 ID 기준으로 모든 run의 갑구 행을 조회 (여러 PDF 합산)
        /// </summary>
        public async Task<List<RegistryGapguRow>> GetGapguRowsByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryGapguRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.SortIndex, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryGapguRow).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 리스트 조회 실패 (property): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건 ID 기준으로 모든 run의 을구 행을 조회 (여러 PDF 합산)
        /// </summary>
        public async Task<List<RegistryEulguRow>> GetEulguRowsByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryEulguRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.SortIndex, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryEulguRow).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 리스트 조회 실패 (property): {ex.Message}", ex);
            }
        }

        public async Task<RegistryBasicInfo?> GetBasicInfoByRunIdAsync(Guid registryRunId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryBasicInfoTable>()
                    .Where(x => x.RegistryRunId == registryRunId)
                    .Limit(1)
                    .Get();

                var model = response.Models.FirstOrDefault();
                return model == null ? null : MapToRegistryBasicInfo(model);
            }
            catch (Exception ex)
            {
                throw new Exception($"basic_info 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<List<RegistryGapguRow>> GetGapguRowsByRunIdAsync(Guid registryRunId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryGapguRowTable>()
                    .Where(x => x.RegistryRunId == registryRunId)
                    .Order(x => x.SortIndex, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryGapguRow).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 표 조회 실패: {ex.Message}", ex);
            }
        }

        public async Task<List<RegistryEulguRow>> GetEulguRowsByRunIdAsync(Guid registryRunId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryEulguRowTable>()
                    .Where(x => x.RegistryRunId == registryRunId)
                    .Order(x => x.SortIndex, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryEulguRow).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 표 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Supabase Edge Function(ocr-registry-save)을 호출하여
        /// PDF OCR → refined(basic_info/gapgu/eulgu) 생성 → registry_runs/basic_info/gapgu/eulgu 저장까지 수행합니다.
        /// </summary>
        public async Task<(RegistryRun Run, int SavedBasicInfo, int SavedGapguRows, int SavedEulguRows, string? RefinedVersion, string? RegistryAddress, List<string>? SummaryImages)>
            OcrRegistrySaveViaEdgeFunctionAsync(
                Guid propertyId,
                string pdfFilePath,
                bool includeSummaryImages = true,
                int summaryImageMaxPages = 3,
                CancellationToken cancellationToken = default)
        {
            if (propertyId == Guid.Empty)
                throw new ArgumentException("propertyId is empty.", nameof(propertyId));

            if (string.IsNullOrWhiteSpace(pdfFilePath))
                throw new ArgumentException("pdfFilePath is empty.", nameof(pdfFilePath));

            if (!File.Exists(pdfFilePath))
                throw new FileNotFoundException("PDF 파일을 찾을 수 없습니다.", pdfFilePath);

            await _supabaseService.EnsureValidSessionAsync(throwOnFailure: true);
            var session = _supabaseService.GetSession();
            var accessToken = session?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new Exception("Supabase 세션이 없습니다. 다시 로그인해주세요.");

            // 디버그: 토큰 유효성 확인
            System.Diagnostics.Debug.WriteLine($"[EdgeFunction] AccessToken length={accessToken.Length}, first20={accessToken[..Math.Min(20, accessToken.Length)]}...");
            System.Diagnostics.Debug.WriteLine($"[EdgeFunction] ExpiresAt={session!.ExpiresAt():O}, CreatedAt={session.CreatedAt:O}");
            System.Diagnostics.Debug.WriteLine($"[EdgeFunction] Url={_supabaseService.Url}/functions/v1/{OcrRegistrySaveFunctionName}");

            var url = $"{_supabaseService.Url}/functions/v1/{OcrRegistrySaveFunctionName}";
            var fileName = Path.GetFileName(pdfFilePath);

            System.Diagnostics.Debug.WriteLine($"[EdgeFunction] fileName={fileName}");

            using var fileStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "file", "upload.pdf");
            // NOTE: .NET은 한글/특수문자 파일명을 RFC 5987로 인코딩하여 Deno에서 깨짐.
            // 원본 파일명은 별도 form-data 필드(source_pdf_name)로 전달.
            content.Add(new StringContent(fileName), "source_pdf_name");
            content.Add(new StringContent(propertyId.ToString()), "property_id");
            content.Add(new StringContent(includeSummaryImages ? "true" : "false"), "include_summary_images");
            content.Add(new StringContent(summaryImageMaxPages.ToString()), "summary_image_max_pages");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("apikey", _supabaseService.Key);
            request.Content = content;

            using var response = await _edgeFunctionsHttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            OcrRegistrySaveEdgeResponse? edge;
            try
            {
                edge = JsonSerializer.Deserialize<OcrRegistrySaveEdgeResponse>(
                    responseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                throw new Exception($"Edge Function 응답 파싱 실패: {ex.Message}", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var msg = edge?.Error ?? responseText;
                System.Diagnostics.Debug.WriteLine($"[EdgeFunction] HTTP {(int)response.StatusCode}: {responseText[..Math.Min(500, responseText.Length)]}");
                throw new Exception($"Edge Function 호출 실패: {(int)response.StatusCode} - {msg}");
            }

            if (edge == null)
                throw new Exception("Edge Function 응답이 비어있습니다.");

            if (!edge.Success)
                throw new Exception(edge.Error ?? "Edge Function 처리 실패");

            if (edge.RegistryRun == null)
                throw new Exception("Edge Function 응답에 registry_run이 없습니다.");

            var run = new RegistryRun
            {
                Id = edge.RegistryRun.Id,
                PropertyId = edge.RegistryRun.PropertyId,
                PropertyNumber = edge.RegistryRun.PropertyNumber,
                DeedSeq = edge.RegistryRun.DeedSeq,
                JibeonId = edge.RegistryRun.JibeonId,
                SourcePdfName = fileName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return (
                run,
                edge.Saved?.BasicInfo ?? 0,
                edge.Saved?.GapguRows ?? 0,
                edge.Saved?.EulguRows ?? 0,
                edge.RefinedVersion,
                edge.RegistryAddress,
                edge.SummaryImages
            );
        }

        public async Task UpdateGapguRowAsync(RegistryGapguRow row)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                // JibunNumber 제외: merge가 결합한 값이므로 원본 보존
                var table = new RegistryGapguRowTable
                {
                    Id = row.Id,
                    RegistryRunId = row.RegistryRunId,
                    PropertyId = row.PropertyId,
                    RankNo = row.RankNo,
                    Purpose = row.Purpose,
                    Receipt = row.Receipt,
                    ReceiptDate = row.ReceiptDate,
                    RightHolder = row.RightHolder,
                    ClaimAmount = row.ClaimAmount,
                    NoteUserInput = row.NoteUserInput,
                    WageClaimEstimateUserInput = row.WageClaimEstimateUserInput,
                    TargetOwner = row.TargetOwner,
                    SortIndex = row.SortIndex,
                    UpdatedAt = DateTime.UtcNow
                };

                await client
                    .From<RegistryGapguRowTable>()
                    .Where(x => x.Id == row.Id)
                    .Update(table);
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 행 저장 실패: {ex.Message}", ex);
            }
        }

        public async Task UpdateEulguRowAsync(RegistryEulguRow row)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                // JibunNumber 제외: merge가 결합한 값이므로 원본 보존
                var table = new RegistryEulguRowTable
                {
                    Id = row.Id,
                    RegistryRunId = row.RegistryRunId,
                    PropertyId = row.PropertyId,
                    RankNo = row.RankNo,
                    Purpose = row.Purpose,
                    Receipt = row.Receipt,
                    ReceiptDate = row.ReceiptDate,
                    MortgageHolder = row.MortgageHolder,
                    MaxClaimAmount = row.MaxClaimAmount,
                    DebtorUserInput = row.DebtorUserInput,
                    CollateralTypeUserInput = row.CollateralTypeUserInput,
                    IsFactoryMortgageUserInput = row.IsFactoryMortgageUserInput,
                    TargetOwner = row.TargetOwner,
                    SortIndex = row.SortIndex,
                    UpdatedAt = DateTime.UtcNow
                };

                await client
                    .From<RegistryEulguRowTable>()
                    .Where(x => x.Id == row.Id)
                    .Update(table);
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 행 저장 실패: {ex.Message}", ex);
            }
        }

        public async Task<RegistryGapguRow> InsertGapguRowAsync(RegistryGapguRow row)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = new RegistryGapguRowTable
                {
                    Id = row.Id,
                    RegistryRunId = row.RegistryRunId,
                    PropertyId = row.PropertyId,
                    RankNo = row.RankNo,
                    Purpose = row.Purpose,
                    Receipt = row.Receipt,
                    ReceiptDate = row.ReceiptDate,
                    RightHolder = row.RightHolder,
                    ClaimAmount = row.ClaimAmount,
                    NoteUserInput = row.NoteUserInput,
                    WageClaimEstimateUserInput = row.WageClaimEstimateUserInput,
                    TargetOwner = row.TargetOwner,
                    JibunNumber = row.JibunNumber,
                    SortIndex = row.SortIndex,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await client.From<RegistryGapguRowTable>().Insert(table);
                var inserted = result.Models.First();
                row.Id = inserted.Id;
                row.CreatedAt = inserted.CreatedAt;
                row.UpdatedAt = inserted.UpdatedAt;
                return row;
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 행 추가 실패: {ex.Message}", ex);
            }
        }

        public async Task<RegistryEulguRow> InsertEulguRowAsync(RegistryEulguRow row)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = new RegistryEulguRowTable
                {
                    Id = row.Id,
                    RegistryRunId = row.RegistryRunId,
                    PropertyId = row.PropertyId,
                    RankNo = row.RankNo,
                    Purpose = row.Purpose,
                    Receipt = row.Receipt,
                    ReceiptDate = row.ReceiptDate,
                    MortgageHolder = row.MortgageHolder,
                    MaxClaimAmount = row.MaxClaimAmount,
                    DebtorUserInput = row.DebtorUserInput,
                    CollateralTypeUserInput = row.CollateralTypeUserInput,
                    IsFactoryMortgageUserInput = row.IsFactoryMortgageUserInput,
                    TargetOwner = row.TargetOwner,
                    JibunNumber = row.JibunNumber,
                    SortIndex = row.SortIndex,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await client.From<RegistryEulguRowTable>().Insert(table);
                var inserted = result.Models.First();
                row.Id = inserted.Id;
                row.CreatedAt = inserted.CreatedAt;
                row.UpdatedAt = inserted.UpdatedAt;
                return row;
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 행 추가 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteGapguRowAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<RegistryGapguRowTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 행 삭제 실패: {ex.Message}", ex);
            }
        }

        public async Task DeleteEulguRowAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client.From<RegistryEulguRowTable>().Where(x => x.Id == id).Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 행 삭제 실패: {ex.Message}", ex);
            }
        }

        private sealed class OcrRegistrySaveEdgeResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("registry_run")]
            public OcrRegistrySaveEdgeRegistryRun? RegistryRun { get; set; }

            [JsonPropertyName("saved")]
            public OcrRegistrySaveEdgeSaved? Saved { get; set; }

            [JsonPropertyName("refined_version")]
            public string? RefinedVersion { get; set; }

            [JsonPropertyName("registry_address")]
            public string? RegistryAddress { get; set; }

            [JsonPropertyName("summary_images")]
            public List<string>? SummaryImages { get; set; }
        }

        private sealed class OcrRegistrySaveEdgeRegistryRun
        {
            [JsonPropertyName("id")]
            public Guid Id { get; set; }

            [JsonPropertyName("property_id")]
            public Guid PropertyId { get; set; }

            [JsonPropertyName("property_number")]
            public string? PropertyNumber { get; set; }

            [JsonPropertyName("deed_seq")]
            public int DeedSeq { get; set; }

            [JsonPropertyName("jibeon_id")]
            public string? JibeonId { get; set; }
        }

        private sealed class OcrRegistrySaveEdgeSaved
        {
            [JsonPropertyName("basic_info")]
            public int BasicInfo { get; set; }

            [JsonPropertyName("gapgu_rows")]
            public int GapguRows { get; set; }

            [JsonPropertyName("eulgu_rows")]
            public int EulguRows { get; set; }
        }

        #endregion

        #region RegistryDocument (등기부 문서)

        /// <summary>
        /// 물건별 등기부 문서 조회
        /// </summary>
        public async Task<List<RegistryDocument>> GetDocumentsByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryDocumentTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToRegistryDocument).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부 문서 생성
        /// </summary>
        public async Task<RegistryDocument> CreateDocumentAsync(RegistryDocument document)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryDocumentTable(document);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryDocumentTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("등기부 문서 생성 후 데이터 조회 실패");

                return MapToRegistryDocument(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부 문서 수정
        /// </summary>
        public async Task<RegistryDocument> UpdateDocumentAsync(RegistryDocument document)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryDocumentTable(document);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryDocumentTable>()
                    .Where(x => x.Id == document.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("등기부 문서 수정 후 데이터 조회 실패");

                return MapToRegistryDocument(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 등기부 문서 삭제
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistryDocumentTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 문서 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region RegistryOwner (소유자 정보)

        /// <summary>
        /// 물건별 소유자 정보 조회
        /// </summary>
        public async Task<List<RegistryOwner>> GetOwnersByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryOwnerTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryOwner).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 소유자 정보 생성
        /// </summary>
        public async Task<RegistryOwner> CreateOwnerAsync(RegistryOwner owner)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryOwnerTable(owner);
                table.CreatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryOwnerTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("소유자 정보 생성 후 데이터 조회 실패");

                return MapToRegistryOwner(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 소유자 정보 수정
        /// </summary>
        public async Task<RegistryOwner> UpdateOwnerAsync(RegistryOwner owner)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryOwnerTable(owner);

                var response = await client
                    .From<RegistryOwnerTable>()
                    .Where(x => x.Id == owner.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("소유자 정보 수정 후 데이터 조회 실패");

                return MapToRegistryOwner(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 소유자 정보 삭제
        /// </summary>
        public async Task<bool> DeleteOwnerAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistryOwnerTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"소유자 정보 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region RegistryRight (권리 정보 - 갑구/을구)

        /// <summary>
        /// 전체 권리 정보 조회
        /// </summary>
        public async Task<List<RegistryRight>> GetAllRightsAsync(int limit = 500)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Limit(limit)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 권리 정보 조회 (갑구/을구 전체)
        /// </summary>
        public async Task<List<RegistryRight>> GetRightsByPropertyIdAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.RightOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 갑구 (소유권) 권리 조회
        /// </summary>
        public async Task<List<RegistryRight>> GetGapguRightsAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Where(x => x.Section == "갑구")
                    .Order(x => x.RightOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 권리 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 을구 (근저당/전세권) 권리 조회
        /// </summary>
        public async Task<List<RegistryRight>> GetEulguRightsAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Where(x => x.Section == "을구")
                    .Order(x => x.RightOrder, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToRegistryRight).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 권리 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리 정보 생성
        /// </summary>
        public async Task<RegistryRight> CreateRightAsync(RegistryRight right)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryRightTable(right);
                table.CreatedAt = DateTime.UtcNow;
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryRightTable>()
                    .Insert(table);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("권리 정보 생성 후 데이터 조회 실패");

                return MapToRegistryRight(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리 정보 수정
        /// </summary>
        public async Task<RegistryRight> UpdateRightAsync(RegistryRight right)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var table = MapToRegistryRightTable(right);
                table.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<RegistryRightTable>()
                    .Where(x => x.Id == right.Id)
                    .Update(table);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("권리 정보 수정 후 데이터 조회 실패");

                return MapToRegistryRight(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 권리 정보 삭제
        /// </summary>
        public async Task<bool> DeleteRightAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<RegistryRightTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"권리 정보 삭제 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region RegistrySummary (요약 표 3종 - 최신 1회 덮어쓰기)

        /// <summary>
        /// "1. 소유지분현황(갑구)" 행 조회
        /// </summary>
        public async Task<List<RegistryGapguOwnershipShareRow>> GetGapguOwnershipShareRowsAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryGapguOwnershipShareRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.SortIndex, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToGapguOwnershipShareRow).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"소유지분현황 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// "2. 소유지분을 제외한 소유권에 관한 사항(갑구)" 행 조회
        /// </summary>
        public async Task<List<RegistryGapguRightSummaryRow>> GetGapguRightSummaryRowsAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryGapguRightSummaryRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.SortIndex, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToGapguRightSummaryRow).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"갑구 요약 표 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// "3. (근)저당권 및 전세권 등(을구)" 행 조회
        /// </summary>
        public async Task<List<RegistryEulguRightSummaryRow>> GetEulguRightSummaryRowsAsync(Guid propertyId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<RegistryEulguRightSummaryRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Order(x => x.SortIndex, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToEulguRightSummaryRow).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"을구 요약 표 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건별 요약 표 3종을 최신 1회 기준으로 덮어쓰기 저장
        /// (기존 행 전체 삭제 후 신규 행 일괄 삽입)
        /// </summary>
        public async Task ReplaceRegistrySummaryAsync(
            Guid propertyId,
            List<RegistryGapguOwnershipShareRow> ownershipRows,
            List<RegistryGapguRightSummaryRow> gapguRows,
            List<RegistryEulguRightSummaryRow> eulguRows)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();

                // 1) 기존 데이터 삭제 (최신 1회 유지 정책)
                await client.From<RegistryGapguOwnershipShareRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Delete();

                await client.From<RegistryGapguRightSummaryRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Delete();

                await client.From<RegistryEulguRightSummaryRowTable>()
                    .Where(x => x.PropertyId == propertyId)
                    .Delete();

                var now = DateTime.UtcNow;

                // 2) 신규 데이터 삽입 (표 원본 순서 유지)
                if (ownershipRows != null && ownershipRows.Count > 0)
                {
                    var tables = ownershipRows.Select((r, idx) =>
                    {
                        r.Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id;
                        r.PropertyId = propertyId;
                        r.SortIndex ??= idx;
                        r.CreatedAt = now;
                        r.UpdatedAt = now;
                        return MapToGapguOwnershipShareRowTable(r);
                    }).ToList();

                    await client.From<RegistryGapguOwnershipShareRowTable>().Insert(tables);
                }

                if (gapguRows != null && gapguRows.Count > 0)
                {
                    var tables = gapguRows.Select((r, idx) =>
                    {
                        r.Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id;
                        r.PropertyId = propertyId;
                        r.SortIndex ??= idx;
                        r.CreatedAt = now;
                        r.UpdatedAt = now;
                        return MapToGapguRightSummaryRowTable(r);
                    }).ToList();

                    await client.From<RegistryGapguRightSummaryRowTable>().Insert(tables);
                }

                if (eulguRows != null && eulguRows.Count > 0)
                {
                    var tables = eulguRows.Select((r, idx) =>
                    {
                        r.Id = r.Id == Guid.Empty ? Guid.NewGuid() : r.Id;
                        r.PropertyId = propertyId;
                        r.SortIndex ??= idx;
                        r.CreatedAt = now;
                        r.UpdatedAt = now;
                        return MapToEulguRightSummaryRowTable(r);
                    }).ToList();

                    await client.From<RegistryEulguRightSummaryRowTable>().Insert(tables);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"등기부 요약 표 저장(덮어쓰기) 실패: {ex.Message}", ex);
            }
        }

        #endregion

        #region Mapping Methods

        private RegistryDocument MapToRegistryDocument(RegistryDocumentTable table)
        {
            return new RegistryDocument
            {
                Id = table.Id,
                PropertyId = table.PropertyId,
                FilePath = table.FilePath ?? string.Empty,
                FileName = table.FileName,
                FileSize = table.FileSize,
                OcrStatus = table.OcrStatus ?? "pending",
                OcrProcessedAt = table.OcrProcessedAt,
                OcrError = table.OcrError,
                RegistryType = table.RegistryType,
                RegistryNumber = table.RegistryNumber,
                ExtractedData = table.ExtractedData,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryDocumentTable MapToRegistryDocumentTable(RegistryDocument model)
        {
            return new RegistryDocumentTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                FilePath = model.FilePath,
                FileName = model.FileName,
                FileSize = model.FileSize,
                OcrStatus = model.OcrStatus,
                OcrProcessedAt = model.OcrProcessedAt,
                OcrError = model.OcrError,
                RegistryType = model.RegistryType,
                RegistryNumber = model.RegistryNumber,
                ExtractedData = model.ExtractedData,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        private RegistryOwner MapToRegistryOwner(RegistryOwnerTable table)
        {
            return new RegistryOwner
            {
                Id = table.Id,
                RegistryDocumentId = table.RegistryDocumentId,
                PropertyId = table.PropertyId,
                OwnerName = table.OwnerName,
                OwnerRegNo = table.OwnerRegNo,
                ShareRatio = table.ShareRatio,
                RegistrationDate = table.RegistrationDate,
                RegistrationCause = table.RegistrationCause,
                CreatedAt = table.CreatedAt
            };
        }

        private RegistryOwnerTable MapToRegistryOwnerTable(RegistryOwner model)
        {
            return new RegistryOwnerTable
            {
                Id = model.Id,
                RegistryDocumentId = model.RegistryDocumentId,
                PropertyId = model.PropertyId,
                OwnerName = model.OwnerName,
                OwnerRegNo = model.OwnerRegNo,
                ShareRatio = model.ShareRatio,
                RegistrationDate = model.RegistrationDate,
                RegistrationCause = model.RegistrationCause,
                CreatedAt = model.CreatedAt
            };
        }

        private RegistryRight MapToRegistryRight(RegistryRightTable table)
        {
            return new RegistryRight
            {
                Id = table.Id,
                PropertyId = table.PropertyId,
                Section = table.Section,
                RightType = table.RightType,
                RightOrder = table.RightOrder,
                RightHolder = table.RightHolder,
                ClaimAmount = table.ClaimAmount,
                RegistrationDate = table.RegistrationDate,
                RegistrationNumber = table.RegistrationNumber,
                RegistrationCause = table.RegistrationCause,
                Status = table.Status ?? "active",
                TargetOwner = table.TargetOwner,
                JibeonNumber = table.JibeonNumber,
                Notes = table.Notes,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryRightTable MapToRegistryRightTable(RegistryRight model)
        {
            return new RegistryRightTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                Section = model.Section,
                RightType = model.RightType,
                RightOrder = model.RightOrder,
                RightHolder = model.RightHolder,
                ClaimAmount = model.ClaimAmount,
                RegistrationDate = model.RegistrationDate,
                RegistrationNumber = model.RegistrationNumber,
                RegistrationCause = model.RegistrationCause,
                Status = model.Status,
                TargetOwner = model.TargetOwner,
                JibeonNumber = model.JibeonNumber,
                Notes = model.Notes,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        private RegistryGapguOwnershipShareRow MapToGapguOwnershipShareRow(RegistryGapguOwnershipShareRowTable table)
        {
            return new RegistryGapguOwnershipShareRow
            {
                Id = table.Id,
                PropertyId = table.PropertyId ?? Guid.Empty,
                RankNo = table.RankNo,
                OwnerName = table.OwnerName,
                OwnerRegNo = table.OwnerRegNo,
                ShareRatio = table.ShareRatio,
                Address = table.Address,
                SortIndex = table.SortIndex,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryGapguOwnershipShareRowTable MapToGapguOwnershipShareRowTable(RegistryGapguOwnershipShareRow model)
        {
            return new RegistryGapguOwnershipShareRowTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                RankNo = model.RankNo,
                OwnerName = model.OwnerName,
                OwnerRegNo = model.OwnerRegNo,
                ShareRatio = model.ShareRatio,
                Address = model.Address,
                SortIndex = model.SortIndex,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        private RegistryGapguRightSummaryRow MapToGapguRightSummaryRow(RegistryGapguRightSummaryRowTable table)
        {
            return new RegistryGapguRightSummaryRow
            {
                Id = table.Id,
                PropertyId = table.PropertyId ?? Guid.Empty,
                RankNo = table.RankNo,
                Purpose = table.Purpose,
                Receipt = table.Receipt,
                Details = table.Details,
                TargetOwner = table.TargetOwner,
                SortIndex = table.SortIndex,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryGapguRightSummaryRowTable MapToGapguRightSummaryRowTable(RegistryGapguRightSummaryRow model)
        {
            return new RegistryGapguRightSummaryRowTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                RankNo = model.RankNo,
                Purpose = model.Purpose,
                Receipt = model.Receipt,
                Details = model.Details,
                TargetOwner = model.TargetOwner,
                SortIndex = model.SortIndex,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        private RegistryEulguRightSummaryRow MapToEulguRightSummaryRow(RegistryEulguRightSummaryRowTable table)
        {
            return new RegistryEulguRightSummaryRow
            {
                Id = table.Id,
                PropertyId = table.PropertyId ?? Guid.Empty,
                RankNo = table.RankNo,
                Purpose = table.Purpose,
                Receipt = table.Receipt,
                Details = table.Details,
                TargetOwner = table.TargetOwner,
                SortIndex = table.SortIndex,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryEulguRightSummaryRowTable MapToEulguRightSummaryRowTable(RegistryEulguRightSummaryRow model)
        {
            return new RegistryEulguRightSummaryRowTable
            {
                Id = model.Id,
                PropertyId = model.PropertyId,
                RankNo = model.RankNo,
                Purpose = model.Purpose,
                Receipt = model.Receipt,
                Details = model.Details,
                TargetOwner = model.TargetOwner,
                SortIndex = model.SortIndex,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };
        }

        private RegistryRun MapToRegistryRun(RegistryRunTable table)
        {
            return new RegistryRun
            {
                Id = table.Id,
                PropertyId = table.PropertyId ?? Guid.Empty,
                PropertyNumber = table.PropertyNumber,
                DeedSeq = table.DeedSeq ?? 0,
                JibeonId = table.JibeonId,
                SourcePdfName = table.SourcePdfName,
                SummaryImagesBase64 = table.SummaryImagesBase64,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryBasicInfo MapToRegistryBasicInfo(RegistryBasicInfoTable table)
        {
            return new RegistryBasicInfo
            {
                Id = table.Id,
                RegistryRunId = table.RegistryRunId ?? Guid.Empty,
                PropertyId = table.PropertyId ?? Guid.Empty,
                JibeonId = table.JibeonId,
                RegistryAddress = table.RegistryAddress,
                DdAddress = table.DdAddress,
                IsAddressMatched = table.IsAddressMatched,
                CollateralType = table.CollateralType,
                LandAreaPyeong = table.LandAreaPyeong,
                BuildingAreaPyeong = table.BuildingAreaPyeong,
                OwnerName = table.OwnerName,
                OwnerRegNo = table.OwnerRegNo,
                ShareRatio = table.ShareRatio,
                OwnerAddress = table.OwnerAddress,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryGapguRow MapToRegistryGapguRow(RegistryGapguRowTable table)
        {
            return new RegistryGapguRow
            {
                Id = table.Id,
                RegistryRunId = table.RegistryRunId ?? Guid.Empty,
                PropertyId = table.PropertyId ?? Guid.Empty,
                RankNo = table.RankNo,
                Purpose = table.Purpose,
                Receipt = table.Receipt,
                ReceiptDate = table.ReceiptDate,
                RightHolder = table.RightHolder,
                ClaimAmount = table.ClaimAmount,
                NoteUserInput = table.NoteUserInput,
                WageClaimEstimateUserInput = table.WageClaimEstimateUserInput,
                TargetOwner = table.TargetOwner,
                JibunNumber = table.JibunNumber,
                SortIndex = table.SortIndex,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        private RegistryEulguRow MapToRegistryEulguRow(RegistryEulguRowTable table)
        {
            return new RegistryEulguRow
            {
                Id = table.Id,
                RegistryRunId = table.RegistryRunId ?? Guid.Empty,
                PropertyId = table.PropertyId ?? Guid.Empty,
                RankNo = table.RankNo,
                Purpose = table.Purpose,
                Receipt = table.Receipt,
                ReceiptDate = table.ReceiptDate,
                MortgageHolder = table.MortgageHolder,
                MaxClaimAmount = table.MaxClaimAmount,
                DebtorUserInput = table.DebtorUserInput,
                CollateralTypeUserInput = table.CollateralTypeUserInput,
                IsFactoryMortgageUserInput = table.IsFactoryMortgageUserInput,
                TargetOwner = table.TargetOwner,
                JibunNumber = table.JibunNumber,
                SortIndex = table.SortIndex,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        #endregion
    }

    #region Supabase Table Models

    /// <summary>
    /// Supabase registry_documents 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_documents")]
    internal class RegistryDocumentTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("file_path")]
        public string? FilePath { get; set; }

        [Postgrest.Attributes.Column("file_name")]
        public string? FileName { get; set; }

        [Postgrest.Attributes.Column("file_size")]
        public long? FileSize { get; set; }

        [Postgrest.Attributes.Column("ocr_status")]
        public string? OcrStatus { get; set; }

        [Postgrest.Attributes.Column("ocr_processed_at")]
        public DateTime? OcrProcessedAt { get; set; }

        [Postgrest.Attributes.Column("ocr_error")]
        public string? OcrError { get; set; }

        [Postgrest.Attributes.Column("registry_type")]
        public string? RegistryType { get; set; }

        [Postgrest.Attributes.Column("registry_number")]
        public string? RegistryNumber { get; set; }

        [Postgrest.Attributes.Column("extracted_data")]
        public string? ExtractedData { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Supabase registry_owners 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_owners")]
    internal class RegistryOwnerTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("registry_document_id")]
        public Guid? RegistryDocumentId { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("owner_name")]
        public string? OwnerName { get; set; }

        [Postgrest.Attributes.Column("owner_regno")]
        public string? OwnerRegNo { get; set; }

        [Postgrest.Attributes.Column("share_ratio")]
        public string? ShareRatio { get; set; }

        [Postgrest.Attributes.Column("registration_date")]
        public DateTime? RegistrationDate { get; set; }

        [Postgrest.Attributes.Column("registration_cause")]
        public string? RegistrationCause { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Supabase registry_rights 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_rights")]
    internal class RegistryRightTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("section")]
        public string Section { get; set; } = "갑구";

        [Postgrest.Attributes.Column("right_type")]
        public string? RightType { get; set; }

        [Postgrest.Attributes.Column("right_order")]
        public int? RightOrder { get; set; }

        [Postgrest.Attributes.Column("right_holder")]
        public string? RightHolder { get; set; }

        [Postgrest.Attributes.Column("claim_amount")]
        public decimal? ClaimAmount { get; set; }

        [Postgrest.Attributes.Column("registration_date")]
        public DateTime? RegistrationDate { get; set; }

        [Postgrest.Attributes.Column("registration_number")]
        public string? RegistrationNumber { get; set; }

        [Postgrest.Attributes.Column("registration_cause")]
        public string? RegistrationCause { get; set; }

        [Postgrest.Attributes.Column("status")]
        public string? Status { get; set; }

        [Postgrest.Attributes.Column("target_owner")]
        public string? TargetOwner { get; set; }

        [Postgrest.Attributes.Column("jibeon_number")]
        public string? JibeonNumber { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Supabase registry_gapgu_ownership_shares 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_gapgu_ownership_shares")]
    internal class RegistryGapguOwnershipShareRowTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("rank_no")]
        public string? RankNo { get; set; }

        [Postgrest.Attributes.Column("owner_name")]
        public string? OwnerName { get; set; }

        [Postgrest.Attributes.Column("owner_regno")]
        public string? OwnerRegNo { get; set; }

        [Postgrest.Attributes.Column("share_ratio")]
        public string? ShareRatio { get; set; }

        [Postgrest.Attributes.Column("address")]
        public string? Address { get; set; }

        [Postgrest.Attributes.Column("sort_index")]
        public int? SortIndex { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Supabase registry_gapgu_rights_summary 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_gapgu_rights_summary")]
    internal class RegistryGapguRightSummaryRowTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("rank_no")]
        public string? RankNo { get; set; }

        [Postgrest.Attributes.Column("purpose")]
        public string? Purpose { get; set; }

        [Postgrest.Attributes.Column("receipt")]
        public string? Receipt { get; set; }

        [Postgrest.Attributes.Column("details")]
        public string? Details { get; set; }

        [Postgrest.Attributes.Column("target_owner")]
        public string? TargetOwner { get; set; }

        [Postgrest.Attributes.Column("sort_index")]
        public int? SortIndex { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Supabase registry_eulgu_rights_summary 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("registry_eulgu_rights_summary")]
    internal class RegistryEulguRightSummaryRowTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("rank_no")]
        public string? RankNo { get; set; }

        [Postgrest.Attributes.Column("purpose")]
        public string? Purpose { get; set; }

        [Postgrest.Attributes.Column("receipt")]
        public string? Receipt { get; set; }

        [Postgrest.Attributes.Column("details")]
        public string? Details { get; set; }

        [Postgrest.Attributes.Column("target_owner")]
        public string? TargetOwner { get; set; }

        [Postgrest.Attributes.Column("sort_index")]
        public int? SortIndex { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    // ===== 정제 산출물 스키마 테이블 매핑 =====

    [Postgrest.Attributes.Table("registry_runs")]
    internal class RegistryRunTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("property_number")]
        public string? PropertyNumber { get; set; }

        [Postgrest.Attributes.Column("deed_seq")]
        public int? DeedSeq { get; set; }

        [Postgrest.Attributes.Column("jibeon_id")]
        public string? JibeonId { get; set; }

        [Postgrest.Attributes.Column("source_pdf_name")]
        public string? SourcePdfName { get; set; }

        [Postgrest.Attributes.Column("summary_images_base64")]
        public List<string>? SummaryImagesBase64 { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("registry_basic_info")]
    internal class RegistryBasicInfoTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("registry_run_id")]
        public Guid? RegistryRunId { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("jibeon_id")]
        public string? JibeonId { get; set; }

        [Postgrest.Attributes.Column("registry_address")]
        public string? RegistryAddress { get; set; }

        [Postgrest.Attributes.Column("dd_address")]
        public string? DdAddress { get; set; }

        [Postgrest.Attributes.Column("is_address_matched")]
        public bool? IsAddressMatched { get; set; }

        [Postgrest.Attributes.Column("collateral_type")]
        public string? CollateralType { get; set; }

        [Postgrest.Attributes.Column("land_area_pyeong")]
        public decimal? LandAreaPyeong { get; set; }

        [Postgrest.Attributes.Column("building_area_pyeong")]
        public decimal? BuildingAreaPyeong { get; set; }

        [Postgrest.Attributes.Column("owner_name")]
        public string? OwnerName { get; set; }

        [Postgrest.Attributes.Column("owner_regno")]
        public string? OwnerRegNo { get; set; }

        [Postgrest.Attributes.Column("share_ratio")]
        public string? ShareRatio { get; set; }

        [Postgrest.Attributes.Column("owner_address")]
        public string? OwnerAddress { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("registry_gapgu_rows")]
    internal class RegistryGapguRowTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("registry_run_id")]
        public Guid? RegistryRunId { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("rank_no")]
        public string? RankNo { get; set; }

        [Postgrest.Attributes.Column("purpose")]
        public string? Purpose { get; set; }

        [Postgrest.Attributes.Column("receipt")]
        public string? Receipt { get; set; }

        [Postgrest.Attributes.Column("receipt_date")]
        public DateTime? ReceiptDate { get; set; }

        [Postgrest.Attributes.Column("right_holder")]
        public string? RightHolder { get; set; }

        [Postgrest.Attributes.Column("claim_amount")]
        public decimal? ClaimAmount { get; set; }

        [Postgrest.Attributes.Column("note_user_input")]
        public string? NoteUserInput { get; set; }

        [Postgrest.Attributes.Column("wage_claim_estimate_user_input")]
        public string? WageClaimEstimateUserInput { get; set; }

        [Postgrest.Attributes.Column("target_owner")]
        public string? TargetOwner { get; set; }

        [Postgrest.Attributes.Column("jibun_number")]
        public string? JibunNumber { get; set; }

        [Postgrest.Attributes.Column("sort_index")]
        public int? SortIndex { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    [Postgrest.Attributes.Table("registry_eulgu_rows")]
    internal class RegistryEulguRowTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("registry_run_id")]
        public Guid? RegistryRunId { get; set; }

        [Postgrest.Attributes.Column("property_id")]
        public Guid? PropertyId { get; set; }

        [Postgrest.Attributes.Column("rank_no")]
        public string? RankNo { get; set; }

        [Postgrest.Attributes.Column("purpose")]
        public string? Purpose { get; set; }

        [Postgrest.Attributes.Column("receipt")]
        public string? Receipt { get; set; }

        [Postgrest.Attributes.Column("receipt_date")]
        public DateTime? ReceiptDate { get; set; }

        [Postgrest.Attributes.Column("mortgage_holder")]
        public string? MortgageHolder { get; set; }

        [Postgrest.Attributes.Column("max_claim_amount")]
        public decimal? MaxClaimAmount { get; set; }

        [Postgrest.Attributes.Column("debtor_user_input")]
        public string? DebtorUserInput { get; set; }

        [Postgrest.Attributes.Column("collateral_type_user_input")]
        public string? CollateralTypeUserInput { get; set; }

        [Postgrest.Attributes.Column("is_factory_mortgage_user_input")]
        public string? IsFactoryMortgageUserInput { get; set; }

        [Postgrest.Attributes.Column("target_owner")]
        public string? TargetOwner { get; set; }

        [Postgrest.Attributes.Column("jibun_number")]
        public string? JibunNumber { get; set; }

        [Postgrest.Attributes.Column("sort_index")]
        public int? SortIndex { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    #endregion
}

