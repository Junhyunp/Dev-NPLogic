using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;

namespace NPLogic.Data.Repositories
{
    /// <summary>
    /// 물건 Repository
    /// </summary>
    public class PropertyRepository
    {
        private readonly Services.SupabaseService _supabaseService;

        public PropertyRepository(Services.SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 모든 물건 조회
        /// </summary>
        public async Task<List<Property>> GetAllAsync()
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToProperty).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ID로 물건 조회
        /// </summary>
        public async Task<Property?> GetByIdAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.Id == id)
                    .Single();

                return response == null ? null : MapToProperty(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로젝트 ID로 물건 목록 조회
        /// </summary>
        public async Task<List<Property>> GetByProjectIdAsync(string projectId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.ProjectId == projectId)
                    .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToProperty).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로젝트별 물건 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램 ID로 물건 목록 조회
        /// </summary>
        public async Task<List<Property>> GetByProgramIdAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.ProgramId == programId)
                    .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                    .Get();

                return response.Models.Select(MapToProperty).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램별 물건 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 담당자로 물건 목록 조회
        /// </summary>
        public async Task<List<Property>> GetByAssignedUserAsync(Guid userId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.AssignedTo == userId)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToProperty).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"담당자별 물건 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 상태별 물건 목록 조회
        /// </summary>
        public async Task<List<Property>> GetByStatusAsync(string status)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.Status == status)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToProperty).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"상태별 물건 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건 유형별 조회
        /// </summary>
        public async Task<List<Property>> GetByTypeAsync(string propertyType)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.PropertyType == propertyType)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Get();

                return response.Models.Select(MapToProperty).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"유형별 물건 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 차주번호와 물건번호로 물건 조회 (등기부등본정보 연결용)
        /// borrower_number는 borrowers 테이블에 있으므로 borrower_id를 먼저 조회한 후 물건 조회
        /// </summary>
        public async Task<Property?> GetByBorrowerAndPropertyNumberAsync(string borrowerNumber, string? propertyNumber)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();

                // 1. borrowers 테이블에서 차주번호로 borrower_id 조회
                var borrowerResponse = await client
                    .From<BorrowerTable>()
                    .Where(x => x.BorrowerNumber == borrowerNumber)
                    .Single();

                if (borrowerResponse == null)
                    return null;

                var borrowerId = borrowerResponse.Id;

                // 2. 물건 테이블에서 borrower_id로 조회
                var query = client
                    .From<PropertyTable>()
                    .Where(x => x.BorrowerId == borrowerId);

                // 물건번호가 있으면 추가 필터
                if (!string.IsNullOrEmpty(propertyNumber))
                {
                    var response = await query.Get();
                    var match = response.Models.FirstOrDefault(x =>
                        x.PropertyNumber == propertyNumber ||
                        x.CollateralNumber == propertyNumber);
                    return match == null ? null : MapToProperty(match);
                }
                else
                {
                    // 물건번호 없으면 해당 차주의 첫 번째 물건 반환
                    var response = await query
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Limit(1)
                        .Get();
                    return response.Models.Count > 0 ? MapToProperty(response.Models.First()) : null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PropertyRepository] GetByBorrowerAndPropertyNumberAsync 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Borrowers 테이블 (borrower_number 조회용)
        /// </summary>
        [Postgrest.Attributes.Table("borrowers")]
        private class BorrowerTable : Postgrest.Models.BaseModel
        {
            [Postgrest.Attributes.PrimaryKey("id", false)]
            public Guid Id { get; set; }

            [Postgrest.Attributes.Column("borrower_number")]
            public string? BorrowerNumber { get; set; }
        }

        /// <summary>
        /// 검색 (주소, 물건번호)
        /// </summary>
        public async Task<List<Property>> SearchAsync(string searchText)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                // Supabase의 텍스트 검색 사용
                var response = await client
                    .From<PropertyTable>()
                    .Get();

                // 클라이언트 측 필터링 (Supabase C# 클라이언트의 한계)
                var filtered = response.Models.Where(x =>
                    (!string.IsNullOrEmpty(x.PropertyNumber) && x.PropertyNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.AddressFull) && x.AddressFull.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.AddressRoad) && x.AddressRoad.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.AddressJibun) && x.AddressJibun.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                return filtered.Select(MapToProperty).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 검색 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 복합 필터 조회
        /// </summary>
        public async Task<List<Property>> GetFilteredAsync(
            string? projectId = null,
            string? propertyType = null,
            string? status = null,
            Guid? assignedTo = null,
            string? searchText = null)
        {
            try
            {
                // 모든 속성을 가져와서 클라이언트 측에서 필터링
                var allProperties = await GetAllAsync();

                // 필터 적용
                if (!string.IsNullOrWhiteSpace(projectId))
                    allProperties = allProperties.Where(p => p.ProjectId == projectId).ToList();

                if (!string.IsNullOrWhiteSpace(propertyType))
                    allProperties = allProperties.Where(p => p.PropertyType == propertyType).ToList();

                if (!string.IsNullOrWhiteSpace(status))
                    allProperties = allProperties.Where(p => p.Status == status).ToList();

                if (assignedTo.HasValue)
                    allProperties = allProperties.Where(p => p.AssignedTo == assignedTo.Value).ToList();

                // 텍스트 검색
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    allProperties = allProperties.Where(p =>
                        (!string.IsNullOrEmpty(p.PropertyNumber) && p.PropertyNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.AddressFull) && p.AddressFull.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.AddressRoad) && p.AddressRoad.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(p.AddressJibun) && p.AddressJibun.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                return allProperties;
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 필터 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 페이지네이션된 물건 목록 조회 (클라이언트 사이드 - Legacy)
        /// </summary>
        public async Task<(List<Property> Items, int TotalCount)> GetPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? projectId = null,
            string? propertyType = null,
            string? status = null,
            Guid? assignedTo = null,
            string? searchText = null)
        {
            try
            {
                var allItems = await GetFilteredAsync(projectId, propertyType, status, assignedTo, searchText);
                var totalCount = allItems.Count;

                var pagedItems = allItems
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (pagedItems, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception($"페이지네이션 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 서버 사이드 페이지네이션된 물건 목록 조회
        /// Supabase Range를 사용하여 DB에서 필요한 범위만 조회
        /// </summary>
        public async Task<(List<Property> Items, int TotalCount)> GetPagedServerSideAsync(
            int page = 1,
            int pageSize = 50,
            Guid? programId = null,
            string? status = null,
            Guid? assignedTo = null,
            List<Guid>? filterProgramIds = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                // 페이지네이션 범위 계산
                int from = (page - 1) * pageSize;
                int to = from + pageSize - 1;
                
                // 조건에 따라 쿼리 실행 (Supabase C# 클라이언트의 체이닝 제약으로 인해 분기)
                Postgrest.Responses.ModeledResponse<PropertyTable> response;
                
                if (programId.HasValue && !string.IsNullOrEmpty(status) && assignedTo.HasValue)
                {
                    response = await client
                        .From<PropertyTable>()
                        .Where(x => x.ProgramId == programId)
                        .Where(x => x.Status == status)
                        .Where(x => x.AssignedTo == assignedTo)
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Range(from, to)
                        .Get();
                }
                else if (programId.HasValue && !string.IsNullOrEmpty(status))
                {
                    response = await client
                        .From<PropertyTable>()
                        .Where(x => x.ProgramId == programId)
                        .Where(x => x.Status == status)
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Range(from, to)
                        .Get();
                }
                else if (programId.HasValue && assignedTo.HasValue)
                {
                    response = await client
                        .From<PropertyTable>()
                        .Where(x => x.ProgramId == programId)
                        .Where(x => x.AssignedTo == assignedTo)
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Range(from, to)
                        .Get();
                }
                else if (programId.HasValue)
                {
                    response = await client
                        .From<PropertyTable>()
                        .Where(x => x.ProgramId == programId)
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Range(from, to)
                        .Get();
                }
                else if (assignedTo.HasValue)
                {
                    response = await client
                        .From<PropertyTable>()
                        .Where(x => x.AssignedTo == assignedTo)
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Range(from, to)
                        .Get();
                }
                else if (filterProgramIds != null && filterProgramIds.Count > 0)
                {
                    // PM 권한: 담당 프로그램 목록으로 필터링 - 클라이언트 측 필터링 사용
                    var allResponse = await client
                        .From<PropertyTable>()
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Get();
                    
                    var filteredItems = allResponse.Models
                        .Where(x => x.ProgramId.HasValue && filterProgramIds.Contains(x.ProgramId.Value))
                        .Skip(from)
                        .Take(pageSize)
                        .ToList();
                    
                    var totalFiltered = allResponse.Models
                        .Count(x => x.ProgramId.HasValue && filterProgramIds.Contains(x.ProgramId.Value));
                    
                    return (filteredItems.Select(MapToProperty).ToList(), totalFiltered);
                }
                else
                {
                    response = await client
                        .From<PropertyTable>()
                        .Order(x => x.PropertyNumber, Postgrest.Constants.Ordering.Ascending)
                        .Range(from, to)
                        .Get();
                }
                
                var items = response.Models.Select(MapToProperty).ToList();
                
                // Total Count 조회 (별도 쿼리) - 페이지네이션 정보 필요
                int totalCount = await GetTotalCountAsync(programId, status, assignedTo);
                
                return (items, totalCount);
            }
            catch (Exception ex)
            {
                throw new Exception($"서버 사이드 페이지네이션 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 조건에 맞는 전체 개수 조회 (서버 사이드 페이지네이션용)
        /// </summary>
        private async Task<int> GetTotalCountAsync(Guid? programId, string? status, Guid? assignedTo)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                Postgrest.Responses.ModeledResponse<PropertyTable> response;
                
                // 조건에 따라 Count 쿼리 실행
                if (programId.HasValue && !string.IsNullOrEmpty(status) && assignedTo.HasValue)
                {
                    response = await client.From<PropertyTable>()
                        .Select("id")
                        .Where(x => x.ProgramId == programId)
                        .Where(x => x.Status == status)
                        .Where(x => x.AssignedTo == assignedTo)
                        .Get();
                }
                else if (programId.HasValue && !string.IsNullOrEmpty(status))
                {
                    response = await client.From<PropertyTable>()
                        .Select("id")
                        .Where(x => x.ProgramId == programId)
                        .Where(x => x.Status == status)
                        .Get();
                }
                else if (programId.HasValue && assignedTo.HasValue)
                {
                    response = await client.From<PropertyTable>()
                        .Select("id")
                        .Where(x => x.ProgramId == programId)
                        .Where(x => x.AssignedTo == assignedTo)
                        .Get();
                }
                else if (programId.HasValue)
                {
                    response = await client.From<PropertyTable>()
                        .Select("id")
                        .Where(x => x.ProgramId == programId)
                        .Get();
                }
                else if (assignedTo.HasValue)
                {
                    response = await client.From<PropertyTable>()
                        .Select("id")
                        .Where(x => x.AssignedTo == assignedTo)
                        .Get();
                }
                else
                {
                    response = await client.From<PropertyTable>()
                        .Select("id")
                        .Get();
                }
                
                return response.Models.Count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 물건 생성
        /// </summary>
        public async Task<Property> CreateAsync(Property property)
        {
            try
            {
                // 주소 앞뒤 공란 제거
                TrimAddressFields(property);
                
                var client = await _supabaseService.GetClientAsync();
                var propertyTable = MapToPropertyTable(property);
                propertyTable.CreatedAt = DateTime.UtcNow;
                propertyTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<PropertyTable>()
                    .Insert(propertyTable);

                var created = response.Models.FirstOrDefault();
                if (created == null)
                    throw new Exception("물건 생성 후 데이터 조회 실패");

                // Postgrest 클라이언트가 nullable Guid를 제대로 직렬화하지 않는 문제 해결
                // ProgramId가 있는데 Insert 결과에 반영되지 않았으면 별도로 Update
                if (property.ProgramId.HasValue && created.ProgramId != property.ProgramId)
                {
                    await client
                        .From<PropertyTable>()
                        .Where(x => x.Id == created.Id)
                        .Set(x => x.ProgramId, property.ProgramId)
                        .Update();
                    
                    created.ProgramId = property.ProgramId;
                }

                return MapToProperty(created);
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 생성 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건 수정
        /// </summary>
        public async Task<Property> UpdateAsync(Property property)
        {
            try
            {
                // 주소 앞뒤 공란 제거
                TrimAddressFields(property);
                
                var client = await _supabaseService.GetClientAsync();
                var propertyTable = MapToPropertyTable(property);
                propertyTable.UpdatedAt = DateTime.UtcNow;

                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.Id == property.Id)
                    .Update(propertyTable);

                var updated = response.Models.FirstOrDefault();
                if (updated == null)
                    throw new Exception("물건 수정 후 데이터 조회 실패");

                return MapToProperty(updated);
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 수정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건 삭제
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<PropertyTable>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건번호로 조회
        /// </summary>
        public async Task<Property?> GetByPropertyNumberAsync(string propertyNumber)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var response = await client
                    .From<PropertyTable>()
                    .Where(x => x.PropertyNumber == propertyNumber)
                    .Single();

                return response == null ? null : MapToProperty(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Upsert (있으면 업데이트, 없으면 생성) - 물건번호 기준
        /// </summary>
        public async Task<Property> UpsertByPropertyNumberAsync(Property property)
        {
            try
            {
                if (string.IsNullOrEmpty(property.PropertyNumber))
                    throw new ArgumentException("물건번호가 필요합니다.");

                var existing = await GetByPropertyNumberAsync(property.PropertyNumber);
                
                if (existing != null)
                {
                    property.Id = existing.Id;
                    return await UpdateAsync(property);
                }
                else
                {
                    if (property.Id == Guid.Empty)
                        property.Id = Guid.NewGuid();
                    return await CreateAsync(property);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"물건 Upsert 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 일괄 Upsert
        /// </summary>
        public async Task<(int Created, int Updated, int Failed)> BulkUpsertAsync(List<Property> properties)
        {
            int created = 0, updated = 0, failed = 0;

            foreach (var property in properties)
            {
                try
                {
                    if (string.IsNullOrEmpty(property.PropertyNumber))
                    {
                        failed++;
                        continue;
                    }

                    var existing = await GetByPropertyNumberAsync(property.PropertyNumber);
                    if (existing != null)
                    {
                        property.Id = existing.Id;
                        await UpdateAsync(property);
                        updated++;
                    }
                    else
                    {
                        if (property.Id == Guid.Empty)
                            property.Id = Guid.NewGuid();
                        await CreateAsync(property);
                        created++;
                    }
                }
                catch
                {
                    failed++;
                }
            }

            return (created, updated, failed);
        }

        /// <summary>
        /// 물건 담당자 할당
        /// </summary>
        public async Task<bool> AssignToUserAsync(Guid propertyId, Guid userId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var update = new PropertyTable
                {
                    AssignedTo = userId,
                    UpdatedAt = DateTime.UtcNow
                };

                await client
                    .From<PropertyTable>()
                    .Where(x => x.Id == propertyId)
                    .Update(update);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"담당자 할당 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 물건 상태 변경
        /// </summary>
        public async Task<bool> UpdateStatusAsync(Guid propertyId, string status)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                var update = new PropertyTable
                {
                    Status = status,
                    UpdatedAt = DateTime.UtcNow
                };

                await client
                    .From<PropertyTable>()
                    .Where(x => x.Id == propertyId)
                    .Update(update);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"상태 변경 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 통계 - 전체/상태별 개수
        /// </summary>
        public async Task<PropertyStatistics> GetStatisticsAsync(string? projectId = null, Guid? assignedTo = null)
        {
            try
            {
                var properties = await GetFilteredAsync(projectId: projectId, assignedTo: assignedTo);

                return new PropertyStatistics
                {
                    TotalCount = properties.Count,
                    PendingCount = properties.Count(x => x.Status == "pending"),
                    ProcessingCount = properties.Count(x => x.Status == "processing"),
                    CompletedCount = properties.Count(x => x.Status == "completed")
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"통계 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램별 통계 집계 조회 (전체 물건 로드 없이)
        /// 최소한의 필드만 선택하여 메모리/네트워크 사용 최소화
        /// </summary>
        public async Task<List<ProgramStatistics>> GetProgramStatisticsAsync(
            List<Guid>? filterProgramIds = null,
            Guid? assignedTo = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                // 최소한의 필드만 선택하여 조회
                Postgrest.Responses.ModeledResponse<PropertyTable> response;
                
                if (assignedTo.HasValue)
                {
                    response = await client
                        .From<PropertyTable>()
                        .Select("program_id, status")
                        .Where(x => x.AssignedTo == assignedTo)
                        .Get();
                }
                else
                {
                    response = await client
                        .From<PropertyTable>()
                        .Select("program_id, status")
                        .Get();
                }
                
                // 클라이언트에서 그룹화 (최소 데이터로)
                var allModels = response.Models.Where(x => x.ProgramId.HasValue);
                
                // PM 권한 필터링
                if (filterProgramIds != null && filterProgramIds.Count > 0)
                {
                    allModels = allModels.Where(x => filterProgramIds.Contains(x.ProgramId!.Value));
                }
                
                var stats = allModels
                    .GroupBy(x => x.ProgramId!.Value)
                    .Select(g => new ProgramStatistics
                    {
                        ProgramId = g.Key,
                        TotalCount = g.Count(),
                        CompletedCount = g.Count(x => x.Status == "completed")
                    })
                    .ToList();
                
                return stats;
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램별 통계 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 서버 사이드 통계 조회 (프로그램별, Count만)
        /// 간소화된 버전 - 한번 조회 후 클라이언트에서 집계
        /// </summary>
        public async Task<PropertyStatistics> GetStatisticsServerSideAsync(
            Guid? programId = null,
            string? status = null,
            Guid? assignedTo = null,
            List<Guid>? filterProgramIds = null)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                // 최소 필드만 조회 (status와 program_id)
                Postgrest.Responses.ModeledResponse<PropertyTable> response;
                
                if (programId.HasValue && assignedTo.HasValue)
                {
                    response = await client.From<PropertyTable>()
                        .Select("status, program_id")
                        .Where(x => x.ProgramId == programId)
                        .Where(x => x.AssignedTo == assignedTo)
                        .Get();
                }
                else if (programId.HasValue)
                {
                    response = await client.From<PropertyTable>()
                        .Select("status, program_id")
                        .Where(x => x.ProgramId == programId)
                        .Get();
                }
                else if (assignedTo.HasValue)
                {
                    response = await client.From<PropertyTable>()
                        .Select("status, program_id")
                        .Where(x => x.AssignedTo == assignedTo)
                        .Get();
                }
                else
                {
                    response = await client.From<PropertyTable>()
                        .Select("status, program_id")
                        .Get();
                }
                
                var models = response.Models.AsEnumerable();
                
                // filterProgramIds가 있으면 추가 필터링
                if (filterProgramIds != null && filterProgramIds.Count > 0 && !programId.HasValue)
                {
                    models = models.Where(x => x.ProgramId.HasValue && filterProgramIds.Contains(x.ProgramId.Value));
                }
                
                var modelList = models.ToList();
                
                return new PropertyStatistics
                {
                    TotalCount = modelList.Count,
                    PendingCount = modelList.Count(x => x.Status == "pending"),
                    ProcessingCount = modelList.Count(x => x.Status == "processing"),
                    CompletedCount = modelList.Count(x => x.Status == "completed")
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"서버 사이드 통계 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 주소 필드의 앞뒤 공란 제거
        /// </summary>
        private void TrimAddressFields(Property property)
        {
            property.AddressFull = property.AddressFull?.Trim();
            property.AddressRoad = property.AddressRoad?.Trim();
            property.AddressJibun = property.AddressJibun?.Trim();
            property.AddressDetail = property.AddressDetail?.Trim();
        }

        /// <summary>
        /// PropertyTable -> Property 매핑
        /// </summary>
        private Property MapToProperty(PropertyTable table)
        {
            return new Property
            {
                Id = table.Id,
                ProjectId = table.ProjectId,
                ProgramId = table.ProgramId,
                BorrowerId = table.BorrowerId,
                PropertyNumber = table.PropertyNumber,
                PropertyType = table.PropertyType,
                AddressFull = table.AddressFull,
                AddressRoad = table.AddressRoad,
                AddressJibun = table.AddressJibun,
                AddressDetail = table.AddressDetail,
                LandArea = table.LandArea,
                BuildingArea = table.BuildingArea,
                Floors = table.Floors,
                CompletionDate = table.CompletionDate,
                AppraisalValue = table.AppraisalValue,
                MinimumBid = table.MinimumBid,
                SalePrice = table.SalePrice,
                Opb = table.Opb,
                Latitude = table.Latitude,
                Longitude = table.Longitude,
                Status = table.Status ?? "pending",
                AssignedTo = table.AssignedTo,
                // 대시보드 진행 관리 필드
                DebtorName = table.DebtorName,
                CollateralNumber = table.CollateralNumber,
                AgreementDoc = table.AgreementDoc,
                GuaranteeDoc = table.GuaranteeDoc,
                AuctionStart1 = table.AuctionStart1,
                AuctionStart2 = table.AuctionStart2,
                AuctionDocs = table.AuctionDocs,
                TenantDocs = table.TenantDocs,
                SeniorRightsReview = table.SeniorRightsReview,
                AppraisalConfirmed = table.AppraisalConfirmed,
                HasCommercialDistrictData = table.HasCommercialDistrictData,
                // F-001: 필터용 필드
                OwnerMoveIn = table.OwnerMoveIn,
                BorrowerResiding = table.BorrowerResiding,
                AuctionScheduleDate = table.AuctionScheduleDate,
                QaUnansweredCount = table.QaUnansweredCount,
                RightsAnalysisStatus = table.RightsAnalysisStatus ?? "pending",
                // 물건정보 시트 대표컬럼
                AssetType = table.AssetType,
                BorrowerNumber = table.BorrowerNumber,
                BorrowerName = table.BorrowerName,
                AddressProvince = table.AddressProvince,
                AddressCity = table.AddressCity,
                AddressDistrict = table.AddressDistrict,
                Pnu = table.Pnu,
                JointCollateralAmount = table.JointCollateralAmount,
                // 선순위 정보
                SeniorMortgageAmount = table.SeniorMortgageAmount,
                SeniorHousingSmallDeposit = table.SeniorHousingSmallDeposit,
                SeniorCommercialSmallDeposit = table.SeniorCommercialSmallDeposit,
                SeniorSmallDeposit = table.SeniorSmallDeposit,
                SeniorHousingLeaseDeposit = table.SeniorHousingLeaseDeposit,
                SeniorCommercialLeaseDeposit = table.SeniorCommercialLeaseDeposit,
                SeniorLeaseDeposit = table.SeniorLeaseDeposit,
                SeniorWageClaim = table.SeniorWageClaim,
                SeniorCurrentTax = table.SeniorCurrentTax,
                SeniorTaxClaim = table.SeniorTaxClaim,
                SeniorEtc = table.SeniorEtc,
                SeniorTotal = table.SeniorTotal,
                // 감정평가 정보
                AppraisalType = table.AppraisalType,
                AppraisalDate = table.AppraisalDate,
                AppraisalAgency = table.AppraisalAgency,
                LandAppraisalValue = table.LandAppraisalValue,
                BuildingAppraisalValue = table.BuildingAppraisalValue,
                MachineryAppraisalValue = table.MachineryAppraisalValue,
                ExcludedAppraisal = table.ExcludedAppraisal,
                KbPrice = table.KbPrice,
                // 경매 기본 정보
                AuctionStarted = table.AuctionStarted,
                AuctionCourt = table.AuctionCourt,
                // 경매 선행 정보
                PrecedentAuctionApplicant = table.PrecedentAuctionApplicant,
                PrecedentAuctionStartDate = table.PrecedentAuctionStartDate,
                PrecedentCaseNumber = table.PrecedentCaseNumber,
                PrecedentClaimDeadline = table.PrecedentClaimDeadline,
                PrecedentClaimAmount = table.PrecedentClaimAmount,
                // 경매 후행 정보
                SubsequentAuctionApplicant = table.SubsequentAuctionApplicant,
                SubsequentAuctionStartDate = table.SubsequentAuctionStartDate,
                SubsequentCaseNumber = table.SubsequentCaseNumber,
                SubsequentClaimDeadline = table.SubsequentClaimDeadline,
                SubsequentClaimAmount = table.SubsequentClaimAmount,
                // 경매 기일/결과 정보
                InitialCourtValue = table.InitialCourtValue,
                FirstAuctionDate = table.FirstAuctionDate,
                FinalAuctionRound = table.FinalAuctionRound,
                FinalAuctionResult = table.FinalAuctionResult,
                FinalAuctionDate = table.FinalAuctionDate,
                NextAuctionDate = table.NextAuctionDate,
                WinningBidAmount = table.WinningBidAmount,
                FinalMinimumBid = table.FinalMinimumBid,
                NextMinimumBid = table.NextMinimumBid,
                Notes = table.Notes,
                CreatedBy = table.CreatedBy,
                CreatedAt = table.CreatedAt,
                UpdatedAt = table.UpdatedAt
            };
        }

        /// <summary>
        /// Property -> PropertyTable 매핑
        /// </summary>
        private PropertyTable MapToPropertyTable(Property property)
        {
            return new PropertyTable
            {
                Id = property.Id,
                ProjectId = property.ProjectId,
                ProgramId = property.ProgramId,
                BorrowerId = property.BorrowerId,
                PropertyNumber = property.PropertyNumber,
                PropertyType = property.PropertyType,
                AddressFull = property.AddressFull,
                AddressRoad = property.AddressRoad,
                AddressJibun = property.AddressJibun,
                AddressDetail = property.AddressDetail,
                LandArea = property.LandArea,
                BuildingArea = property.BuildingArea,
                Floors = property.Floors,
                CompletionDate = property.CompletionDate,
                AppraisalValue = property.AppraisalValue,
                MinimumBid = property.MinimumBid,
                SalePrice = property.SalePrice,
                Opb = property.Opb,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                Status = property.Status,
                AssignedTo = property.AssignedTo,
                // 대시보드 진행 관리 필드
                DebtorName = property.DebtorName,
                CollateralNumber = property.CollateralNumber,
                AgreementDoc = property.AgreementDoc,
                GuaranteeDoc = property.GuaranteeDoc,
                AuctionStart1 = property.AuctionStart1,
                AuctionStart2 = property.AuctionStart2,
                AuctionDocs = property.AuctionDocs,
                TenantDocs = property.TenantDocs,
                SeniorRightsReview = property.SeniorRightsReview,
                AppraisalConfirmed = property.AppraisalConfirmed,
                HasCommercialDistrictData = property.HasCommercialDistrictData,
                // F-001: 필터용 필드
                OwnerMoveIn = property.OwnerMoveIn,
                BorrowerResiding = property.BorrowerResiding,
                AuctionScheduleDate = property.AuctionScheduleDate,
                QaUnansweredCount = property.QaUnansweredCount,
                RightsAnalysisStatus = property.RightsAnalysisStatus,
                // 물건정보 시트 대표컬럼
                AssetType = property.AssetType,
                BorrowerNumber = property.BorrowerNumber,
                BorrowerName = property.BorrowerName,
                AddressProvince = property.AddressProvince,
                AddressCity = property.AddressCity,
                AddressDistrict = property.AddressDistrict,
                Pnu = property.Pnu,
                JointCollateralAmount = property.JointCollateralAmount,
                // 선순위 정보
                SeniorMortgageAmount = property.SeniorMortgageAmount,
                SeniorHousingSmallDeposit = property.SeniorHousingSmallDeposit,
                SeniorCommercialSmallDeposit = property.SeniorCommercialSmallDeposit,
                SeniorSmallDeposit = property.SeniorSmallDeposit,
                SeniorHousingLeaseDeposit = property.SeniorHousingLeaseDeposit,
                SeniorCommercialLeaseDeposit = property.SeniorCommercialLeaseDeposit,
                SeniorLeaseDeposit = property.SeniorLeaseDeposit,
                SeniorWageClaim = property.SeniorWageClaim,
                SeniorCurrentTax = property.SeniorCurrentTax,
                SeniorTaxClaim = property.SeniorTaxClaim,
                SeniorEtc = property.SeniorEtc,
                SeniorTotal = property.SeniorTotal,
                // 감정평가 정보
                AppraisalType = property.AppraisalType,
                AppraisalDate = property.AppraisalDate,
                AppraisalAgency = property.AppraisalAgency,
                LandAppraisalValue = property.LandAppraisalValue,
                BuildingAppraisalValue = property.BuildingAppraisalValue,
                MachineryAppraisalValue = property.MachineryAppraisalValue,
                ExcludedAppraisal = property.ExcludedAppraisal,
                KbPrice = property.KbPrice,
                // 경매 기본 정보
                AuctionStarted = property.AuctionStarted,
                AuctionCourt = property.AuctionCourt,
                // 경매 선행 정보
                PrecedentAuctionApplicant = property.PrecedentAuctionApplicant,
                PrecedentAuctionStartDate = property.PrecedentAuctionStartDate,
                PrecedentCaseNumber = property.PrecedentCaseNumber,
                PrecedentClaimDeadline = property.PrecedentClaimDeadline,
                PrecedentClaimAmount = property.PrecedentClaimAmount,
                // 경매 후행 정보
                SubsequentAuctionApplicant = property.SubsequentAuctionApplicant,
                SubsequentAuctionStartDate = property.SubsequentAuctionStartDate,
                SubsequentCaseNumber = property.SubsequentCaseNumber,
                SubsequentClaimDeadline = property.SubsequentClaimDeadline,
                SubsequentClaimAmount = property.SubsequentClaimAmount,
                // 경매 기일/결과 정보
                InitialCourtValue = property.InitialCourtValue,
                FirstAuctionDate = property.FirstAuctionDate,
                FinalAuctionRound = property.FinalAuctionRound,
                FinalAuctionResult = property.FinalAuctionResult,
                FinalAuctionDate = property.FinalAuctionDate,
                NextAuctionDate = property.NextAuctionDate,
                WinningBidAmount = property.WinningBidAmount,
                FinalMinimumBid = property.FinalMinimumBid,
                NextMinimumBid = property.NextMinimumBid,
                Notes = property.Notes,
                CreatedBy = property.CreatedBy,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt
            };
        }

        /// <summary>
        /// 진행 체크박스 필드 업데이트 (개별 필드)
        /// </summary>
        public async Task<bool> UpdateProgressFieldAsync(Guid propertyId, string fieldName, object value)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                
                // Supabase RPC를 사용하여 동적 필드 업데이트
                var property = await GetByIdAsync(propertyId);
                if (property == null) return false;

                // 필드별 업데이트
                switch (fieldName.ToLower())
                {
                    case "agreement_doc":
                        property.AgreementDoc = Convert.ToBoolean(value);
                        break;
                    case "guarantee_doc":
                        property.GuaranteeDoc = Convert.ToBoolean(value);
                        break;
                    case "auction_start_1":
                        property.AuctionStart1 = value?.ToString();
                        break;
                    case "auction_start_2":
                        property.AuctionStart2 = value?.ToString();
                        break;
                    case "auction_docs":
                        property.AuctionDocs = Convert.ToBoolean(value);
                        break;
                    case "tenant_docs":
                        property.TenantDocs = Convert.ToBoolean(value);
                        break;
                    case "senior_rights_review":
                        property.SeniorRightsReview = Convert.ToBoolean(value);
                        break;
                    case "appraisal_confirmed":
                        property.AppraisalConfirmed = Convert.ToBoolean(value);
                        break;
                    case "has_commercial_district_data":
                        property.HasCommercialDistrictData = Convert.ToBoolean(value);
                        break;
                    case "opb":
                        property.Opb = value == null ? null : Convert.ToDecimal(value);
                        break;
                    case "rights_analysis_status":
                        property.RightsAnalysisStatus = value?.ToString() ?? "pending";
                        break;
                    default:
                        return false;
                }

                await UpdateAsync(property);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"진행 필드 업데이트 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 프로그램의 모든 물건 삭제
        /// </summary>
        public async Task DeleteByProgramIdAsync(Guid programId)
        {
            try
            {
                var client = await _supabaseService.GetClientAsync();
                await client
                    .From<PropertyTable>()
                    .Where(x => x.ProgramId == programId)
                    .Delete();
            }
            catch (Exception ex)
            {
                throw new Exception($"프로그램 물건 일괄 삭제 실패: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 물건 통계
    /// </summary>
    public class PropertyStatistics
    {
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int CompletedCount { get; set; }
    }

    /// <summary>
    /// 프로그램별 통계
    /// </summary>
    public class ProgramStatistics
    {
        public Guid ProgramId { get; set; }
        public string ProgramName { get; set; } = "";
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public int ProgressPercent => TotalCount > 0 ? (int)Math.Round((double)CompletedCount / TotalCount * 100) : 0;
    }

    /// <summary>
    /// Supabase properties 테이블 매핑
    /// </summary>
    [Postgrest.Attributes.Table("properties")]
    internal class PropertyTable : Postgrest.Models.BaseModel
    {
        [Postgrest.Attributes.PrimaryKey("id", false)]
        public Guid Id { get; set; }

        [Postgrest.Attributes.Column("project_id")]
        public string? ProjectId { get; set; }

        [Postgrest.Attributes.Column("program_id")]
        public Guid? ProgramId { get; set; }

        [Postgrest.Attributes.Column("borrower_id")]
        public Guid? BorrowerId { get; set; }

        [Postgrest.Attributes.Column("property_number")]
        public string? PropertyNumber { get; set; }

        [Postgrest.Attributes.Column("property_type")]
        public string? PropertyType { get; set; }

        [Postgrest.Attributes.Column("address_full")]
        public string? AddressFull { get; set; }

        [Postgrest.Attributes.Column("address_road")]
        public string? AddressRoad { get; set; }

        [Postgrest.Attributes.Column("address_jibun")]
        public string? AddressJibun { get; set; }

        [Postgrest.Attributes.Column("address_detail")]
        public string? AddressDetail { get; set; }

        [Postgrest.Attributes.Column("land_area")]
        public decimal? LandArea { get; set; }

        [Postgrest.Attributes.Column("building_area")]
        public decimal? BuildingArea { get; set; }

        [Postgrest.Attributes.Column("floors")]
        public string? Floors { get; set; }

        [Postgrest.Attributes.Column("completion_date")]
        public DateTime? CompletionDate { get; set; }

        [Postgrest.Attributes.Column("appraisal_value")]
        public decimal? AppraisalValue { get; set; }

        [Postgrest.Attributes.Column("minimum_bid")]
        public decimal? MinimumBid { get; set; }

        [Postgrest.Attributes.Column("sale_price")]
        public decimal? SalePrice { get; set; }

        /// <summary>
        /// OPB (Outstanding Principal Balance, 대출잔액)
        /// Phase 6.4
        /// </summary>
        [Postgrest.Attributes.Column("opb")]
        public decimal? Opb { get; set; }

        [Postgrest.Attributes.Column("latitude")]
        public decimal? Latitude { get; set; }

        [Postgrest.Attributes.Column("longitude")]
        public decimal? Longitude { get; set; }

        [Postgrest.Attributes.Column("status")]
        public string? Status { get; set; }

        [Postgrest.Attributes.Column("assigned_to")]
        public Guid? AssignedTo { get; set; }

        // ========== 대시보드 진행 관리 필드 ==========

        [Postgrest.Attributes.Column("debtor_name")]
        public string? DebtorName { get; set; }

        [Postgrest.Attributes.Column("collateral_number")]
        public string? CollateralNumber { get; set; }

        [Postgrest.Attributes.Column("agreement_doc")]
        public bool AgreementDoc { get; set; }

        [Postgrest.Attributes.Column("guarantee_doc")]
        public bool GuaranteeDoc { get; set; }

        [Postgrest.Attributes.Column("auction_start_1")]
        public string? AuctionStart1 { get; set; }

        [Postgrest.Attributes.Column("auction_start_2")]
        public string? AuctionStart2 { get; set; }

        [Postgrest.Attributes.Column("auction_docs")]
        public bool AuctionDocs { get; set; }

        [Postgrest.Attributes.Column("tenant_docs")]
        public bool TenantDocs { get; set; }

        [Postgrest.Attributes.Column("senior_rights_review")]
        public bool SeniorRightsReview { get; set; }

        [Postgrest.Attributes.Column("appraisal_confirmed")]
        public bool AppraisalConfirmed { get; set; }

        /// <summary>
        /// 상권 데이터 확보 여부 (상가/아파트형공장 전용)
        /// Phase 6.5
        /// </summary>
        [Postgrest.Attributes.Column("has_commercial_district_data")]
        public bool HasCommercialDistrictData { get; set; }

        // ========== F-001: 필터용 필드 ==========

        /// <summary>
        /// 소유자 전입 여부
        /// </summary>
        [Postgrest.Attributes.Column("owner_move_in")]
        public bool OwnerMoveIn { get; set; }

        /// <summary>
        /// 차주 거주 여부
        /// </summary>
        [Postgrest.Attributes.Column("borrower_residing")]
        public bool BorrowerResiding { get; set; }

        // ========== 물건정보 시트 대표컬럼 ==========

        [Postgrest.Attributes.Column("asset_type")]
        public string? AssetType { get; set; }

        [Postgrest.Attributes.Column("borrower_number")]
        public string? BorrowerNumber { get; set; }

        [Postgrest.Attributes.Column("borrower_name")]
        public string? BorrowerName { get; set; }

        [Postgrest.Attributes.Column("address_province")]
        public string? AddressProvince { get; set; }

        [Postgrest.Attributes.Column("address_city")]
        public string? AddressCity { get; set; }

        [Postgrest.Attributes.Column("address_district")]
        public string? AddressDistrict { get; set; }

        [Postgrest.Attributes.Column("pnu")]
        public string? Pnu { get; set; }

        [Postgrest.Attributes.Column("joint_collateral_amount")]
        public decimal? JointCollateralAmount { get; set; }

        // ========== 선순위 정보 ==========

        [Postgrest.Attributes.Column("senior_mortgage_amount")]
        public decimal? SeniorMortgageAmount { get; set; }

        [Postgrest.Attributes.Column("senior_housing_small_deposit")]
        public decimal? SeniorHousingSmallDeposit { get; set; }

        [Postgrest.Attributes.Column("senior_commercial_small_deposit")]
        public decimal? SeniorCommercialSmallDeposit { get; set; }

        [Postgrest.Attributes.Column("senior_small_deposit")]
        public decimal? SeniorSmallDeposit { get; set; }

        [Postgrest.Attributes.Column("senior_housing_lease_deposit")]
        public decimal? SeniorHousingLeaseDeposit { get; set; }

        [Postgrest.Attributes.Column("senior_commercial_lease_deposit")]
        public decimal? SeniorCommercialLeaseDeposit { get; set; }

        [Postgrest.Attributes.Column("senior_lease_deposit")]
        public decimal? SeniorLeaseDeposit { get; set; }

        [Postgrest.Attributes.Column("senior_wage_claim")]
        public decimal? SeniorWageClaim { get; set; }

        [Postgrest.Attributes.Column("senior_current_tax")]
        public decimal? SeniorCurrentTax { get; set; }

        [Postgrest.Attributes.Column("senior_tax_claim")]
        public decimal? SeniorTaxClaim { get; set; }

        [Postgrest.Attributes.Column("senior_etc")]
        public decimal? SeniorEtc { get; set; }

        [Postgrest.Attributes.Column("senior_total")]
        public decimal? SeniorTotal { get; set; }

        // ========== 감정평가 정보 ==========

        [Postgrest.Attributes.Column("appraisal_type")]
        public string? AppraisalType { get; set; }

        [Postgrest.Attributes.Column("appraisal_date")]
        public DateTime? AppraisalDate { get; set; }

        [Postgrest.Attributes.Column("appraisal_agency")]
        public string? AppraisalAgency { get; set; }

        [Postgrest.Attributes.Column("land_appraisal_value")]
        public decimal? LandAppraisalValue { get; set; }

        [Postgrest.Attributes.Column("building_appraisal_value")]
        public decimal? BuildingAppraisalValue { get; set; }

        [Postgrest.Attributes.Column("machinery_appraisal_value")]
        public decimal? MachineryAppraisalValue { get; set; }

        [Postgrest.Attributes.Column("excluded_appraisal")]
        public decimal? ExcludedAppraisal { get; set; }

        [Postgrest.Attributes.Column("kb_price")]
        public decimal? KbPrice { get; set; }

        // ========== 경매 기본 정보 ==========

        [Postgrest.Attributes.Column("auction_started")]
        public bool AuctionStarted { get; set; }

        [Postgrest.Attributes.Column("auction_court")]
        public string? AuctionCourt { get; set; }

        // ========== 경매 선행 정보 ==========

        [Postgrest.Attributes.Column("precedent_auction_applicant")]
        public string? PrecedentAuctionApplicant { get; set; }

        [Postgrest.Attributes.Column("precedent_auction_start_date")]
        public DateTime? PrecedentAuctionStartDate { get; set; }

        [Postgrest.Attributes.Column("precedent_case_number")]
        public string? PrecedentCaseNumber { get; set; }

        [Postgrest.Attributes.Column("precedent_claim_deadline")]
        public DateTime? PrecedentClaimDeadline { get; set; }

        [Postgrest.Attributes.Column("precedent_claim_amount")]
        public decimal? PrecedentClaimAmount { get; set; }

        // ========== 경매 후행 정보 ==========

        [Postgrest.Attributes.Column("subsequent_auction_applicant")]
        public string? SubsequentAuctionApplicant { get; set; }

        [Postgrest.Attributes.Column("subsequent_auction_start_date")]
        public DateTime? SubsequentAuctionStartDate { get; set; }

        [Postgrest.Attributes.Column("subsequent_case_number")]
        public string? SubsequentCaseNumber { get; set; }

        [Postgrest.Attributes.Column("subsequent_claim_deadline")]
        public DateTime? SubsequentClaimDeadline { get; set; }

        [Postgrest.Attributes.Column("subsequent_claim_amount")]
        public decimal? SubsequentClaimAmount { get; set; }

        // ========== 경매 기일/결과 정보 ==========

        [Postgrest.Attributes.Column("initial_court_value")]
        public decimal? InitialCourtValue { get; set; }

        [Postgrest.Attributes.Column("first_auction_date")]
        public DateTime? FirstAuctionDate { get; set; }

        [Postgrest.Attributes.Column("final_auction_round")]
        public int? FinalAuctionRound { get; set; }

        [Postgrest.Attributes.Column("final_auction_result")]
        public string? FinalAuctionResult { get; set; }

        [Postgrest.Attributes.Column("final_auction_date")]
        public DateTime? FinalAuctionDate { get; set; }

        [Postgrest.Attributes.Column("next_auction_date")]
        public DateTime? NextAuctionDate { get; set; }

        [Postgrest.Attributes.Column("winning_bid_amount")]
        public decimal? WinningBidAmount { get; set; }

        [Postgrest.Attributes.Column("final_minimum_bid")]
        public decimal? FinalMinimumBid { get; set; }

        [Postgrest.Attributes.Column("next_minimum_bid")]
        public decimal? NextMinimumBid { get; set; }

        [Postgrest.Attributes.Column("notes")]
        public string? Notes { get; set; }

        [Postgrest.Attributes.Column("auction_schedule_date")]
        public DateTime? AuctionScheduleDate { get; set; }

        [Postgrest.Attributes.Column("qa_unanswered_count")]
        public int QaUnansweredCount { get; set; }

        [Postgrest.Attributes.Column("rights_analysis_status")]
        public string? RightsAnalysisStatus { get; set; }

        [Postgrest.Attributes.Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

