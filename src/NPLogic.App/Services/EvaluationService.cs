using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NPLogic.Core.Models;
using NPLogic.Data.Repositories;
using Supabase;

namespace NPLogic.Services
{
    /// <summary>
    /// 평가 통합 서비스
    /// </summary>
    public class EvaluationService
    {
        private readonly Client _supabase;
        private readonly EvaluationCaseRepository _caseRepo;
        private readonly EvaluationLandParcelRepository _landParcelRepo;
        private readonly EvaluationMachineryRepository _machineryRepo;
        private readonly EvaluationRentalAnalysisRepository _rentalRepo;
        private readonly EvaluationCommercialDataRepository _commercialRepo;
        private readonly EvaluationManagementFeeRepository _managementFeeRepo;
        private readonly RecoveryStrategyService _recoveryService;
        private readonly MapService _mapService;
        
        public EvaluationService(Client supabase)
        {
            _supabase = supabase ?? throw new ArgumentNullException(nameof(supabase));
            _caseRepo = new EvaluationCaseRepository(supabase);
            _landParcelRepo = new EvaluationLandParcelRepository(supabase);
            _machineryRepo = new EvaluationMachineryRepository(supabase);
            _rentalRepo = new EvaluationRentalAnalysisRepository(supabase);
            _commercialRepo = new EvaluationCommercialDataRepository(supabase);
            _managementFeeRepo = new EvaluationManagementFeeRepository(supabase);
            _recoveryService = new RecoveryStrategyService();
            _mapService = new MapService();
        }
        
        #region 사례 평가 관련
        
        /// <summary>
        /// 평가 사례 추가
        /// </summary>
        public async Task<EvaluationCase> AddCaseAsync(EvaluationCase evalCase)
        {
            return await _caseRepo.SaveAsync(evalCase);
        }
        
        /// <summary>
        /// 평가 사례 목록 조회
        /// </summary>
        public async Task<List<EvaluationCase>> GetCasesAsync(Guid evaluationId)
        {
            return await _caseRepo.GetByEvaluationIdAsync(evaluationId);
        }
        
        /// <summary>
        /// 적용할 사례 설정
        /// </summary>
        public async Task SetAppliedCaseAsync(Guid evaluationId, Guid caseId)
        {
            await _caseRepo.SetAppliedCaseAsync(evaluationId, caseId);
        }
        
        /// <summary>
        /// 사례 삭제
        /// </summary>
        public async Task DeleteCaseAsync(Guid caseId)
        {
            await _caseRepo.DeleteAsync(caseId);
        }
        
        #endregion
        
        #region 지번별 평가 (공장/창고)
        
        /// <summary>
        /// 지번별 평가 목록 조회
        /// </summary>
        public async Task<List<EvaluationLandParcel>> GetLandParcelsAsync(Guid evaluationId)
        {
            return await _landParcelRepo.GetByEvaluationIdAsync(evaluationId);
        }
        
        /// <summary>
        /// 지번별 평가 저장
        /// </summary>
        public async Task SaveLandParcelsAsync(List<EvaluationLandParcel> parcels)
        {
            await _landParcelRepo.SaveBatchAsync(parcels);
        }
        
        /// <summary>
        /// 지번별 평가 합계 계산
        /// </summary>
        public async Task<LandParcelSummary> CalculateLandParcelSummaryAsync(Guid evaluationId)
        {
            var parcels = await GetLandParcelsAsync(evaluationId);
            return new LandParcelSummary
            {
                TotalLandArea = parcels.Where(p => p.ParcelType == "토지").Sum(p => p.AreaPyeong ?? 0),
                TotalBuildingArea = parcels.Where(p => p.ParcelType == "건물").Sum(p => p.AreaPyeong ?? 0),
                TotalAppraisalValue = parcels.Sum(p => p.AppraisalValue ?? 0),
                ParcelCount = parcels.Count
            };
        }
        
        #endregion
        
        #region 기계기구 평가 (공장)
        
        /// <summary>
        /// 기계기구 목록 조회
        /// </summary>
        public async Task<List<EvaluationMachinery>> GetMachineryAsync(Guid evaluationId)
        {
            return await _machineryRepo.GetByEvaluationIdAsync(evaluationId);
        }
        
        /// <summary>
        /// 기계기구 저장
        /// </summary>
        public async Task SaveMachineryAsync(List<EvaluationMachinery> machinery)
        {
            await _machineryRepo.SaveBatchAsync(machinery);
        }
        
        /// <summary>
        /// 기계인정율 계산 (기계 감정가 / 부동산 감정가)
        /// </summary>
        public async Task<decimal> CalculateMachineryRecognitionRateAsync(Guid evaluationId, decimal realEstateAppraisalValue)
        {
            if (realEstateAppraisalValue <= 0) return 0;
            
            var machinery = await GetMachineryAsync(evaluationId);
            var totalMachineryValue = machinery.Sum(m => m.AppraisalValue ?? 0);
            
            return totalMachineryValue / realEstateAppraisalValue;
        }
        
        #endregion
        
        #region 임대 분석 (상가/주택/토지)
        
        /// <summary>
        /// 임대호가분석 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetRentalQuotesAsync(Guid evaluationId)
        {
            return await _rentalRepo.GetRentalQuotesAsync(evaluationId);
        }
        
        /// <summary>
        /// 수익가치 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetIncomeValueAsync(Guid evaluationId)
        {
            return await _rentalRepo.GetIncomeValueAnalysisAsync(evaluationId);
        }
        
        /// <summary>
        /// 탐문결과 조회
        /// </summary>
        public async Task<List<EvaluationRentalAnalysis>> GetInquiryResultsAsync(Guid evaluationId)
        {
            return await _rentalRepo.GetInquiryAnalysisAsync(evaluationId);
        }
        
        /// <summary>
        /// 임대 분석 저장
        /// </summary>
        public async Task SaveRentalAnalysisAsync(List<EvaluationRentalAnalysis> analyses)
        {
            await _rentalRepo.SaveBatchAsync(analyses);
        }
        
        #endregion
        
        #region 상권/임대동향 (상가)
        
        /// <summary>
        /// 상권정보 조회
        /// </summary>
        public async Task<EvaluationCommercialData?> GetCommercialDataAsync(Guid evaluationId)
        {
            return await _commercialRepo.GetByEvaluationIdAsync(evaluationId);
        }
        
        /// <summary>
        /// 상권정보 저장
        /// </summary>
        public async Task<EvaluationCommercialData> SaveCommercialDataAsync(EvaluationCommercialData data)
        {
            return await _commercialRepo.SaveAsync(data);
        }
        
        /// <summary>
        /// 층별효용비율 적용
        /// </summary>
        public decimal ApplyFloorUtility(decimal baseValue, string floor, EvaluationCommercialData commercialData)
        {
            decimal utilityRate = floor switch
            {
                "B1" or "B2" or "지하1층" or "지하2층" => commercialData.FloorUtilityBasement ?? 0.5m,
                "1" or "1층" => commercialData.FloorUtility1F ?? 1.0m,
                "2" or "2층" => commercialData.FloorUtility2F ?? 0.8m,
                _ => commercialData.FloorUtility3FUp ?? 0.6m
            };
            
            return baseValue * utilityRate;
        }
        
        #endregion
        
        #region 선순위 관리비
        
        /// <summary>
        /// 선순위 관리비 조회
        /// </summary>
        public async Task<EvaluationManagementFee?> GetManagementFeeAsync(Guid evaluationId)
        {
            return await _managementFeeRepo.GetByEvaluationIdAsync(evaluationId);
        }
        
        /// <summary>
        /// 선순위 관리비 저장
        /// </summary>
        public async Task<EvaluationManagementFee> SaveManagementFeeAsync(EvaluationManagementFee fee)
        {
            return await _managementFeeRepo.SaveAsync(fee);
        }
        
        /// <summary>
        /// 추정관리비 계산
        /// </summary>
        public async Task<EvaluationManagementFee> CalculateManagementFeeAsync(
            Guid evaluationId,
            string? phone,
            decimal arrears,
            decimal monthlyFee,
            int scenario1Months,
            int scenario2Months)
        {
            return await _managementFeeRepo.CalculateAndSaveAsync(
                evaluationId, phone, arrears, monthlyFee, scenario1Months, scenario2Months);
        }
        
        #endregion
        
        #region 회수전략
        
        /// <summary>
        /// 회수전략 요약 계산
        /// </summary>
        public RecoveryStrategySummary CalculateRecoveryStrategy(
            decimal appraisalValue,
            decimal scenario1Rate,
            decimal scenario2Rate,
            decimal? seniorRights = null,
            decimal? opb = null,
            InterimRecoveryData? interimData = null)
        {
            return _recoveryService.CalculateRecoveryStrategy(
                appraisalValue, scenario1Rate, scenario2Rate, seniorRights, opb, interimData);
        }
        
        #endregion
        
        #region 지도 서비스
        
        /// <summary>
        /// API 키 설정
        /// </summary>
        public void SetMapApiKeys(string? kakaoApiKey = null, string? naverClientId = null, string? naverClientSecret = null)
        {
            _mapService.SetApiKeys(kakaoApiKey, naverClientId, naverClientSecret);
        }
        
        /// <summary>
        /// 주소 좌표 변환
        /// </summary>
        public async Task<(double? Latitude, double? Longitude)> GeoCodeAddressAsync(string address)
        {
            return await _mapService.GeoCodeAddressKakaoAsync(address);
        }
        
        /// <summary>
        /// 사례지도 HTML 생성
        /// </summary>
        public string GenerateCaseMapHtml(double centerLat, double centerLng, List<MapMarker> markers)
        {
            return _mapService.GenerateKakaoMapHtml(centerLat, centerLng, markers);
        }
        
        /// <summary>
        /// 로드뷰 HTML 생성
        /// </summary>
        public string GenerateRoadViewHtml(double latitude, double longitude)
        {
            return _mapService.GenerateKakaoRoadViewHtml(latitude, longitude);
        }
        
        /// <summary>
        /// 외부 지도 열기
        /// </summary>
        public void OpenExternalMap(double latitude, double longitude, string? label = null, bool useKakao = true)
        {
            if (useKakao)
                _mapService.OpenKakaoMapInBrowser(latitude, longitude, label);
            else
                _mapService.OpenNaverMapInBrowser(latitude, longitude, label);
        }
        
        #endregion
        
        #region 유틸리티
        
        /// <summary>
        /// 평가 유형 자동 선택
        /// </summary>
        public string GetEvaluationType(string propertyType)
        {
            if (string.IsNullOrEmpty(propertyType)) return "apartment";
            
            var type = propertyType.ToLower();
            
            if (type.Contains("아파트") || type.Contains("오피스텔"))
                return "apartment";
            if (type.Contains("다세대") || type.Contains("연립") || type.Contains("빌라"))
                return "multifamily";
            if (type.Contains("공장") || type.Contains("창고") || type.Contains("제조"))
                return "factory";
            if (type.Contains("상가") || type.Contains("근린") || type.Contains("판매") || type.Contains("지식산업"))
                return "commercial";
            if (type.Contains("주택") || type.Contains("토지") || type.Contains("대지") || type.Contains("임야"))
                return "houseland";
            
            return "apartment"; // 기본값
        }
        
        /// <summary>
        /// 거리 계산 (km)
        /// </summary>
        public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            return MapService.CalculateDistanceKm(lat1, lon1, lat2, lon2);
        }
        
        #endregion
    }
    
    /// <summary>
    /// 지번별 평가 합계 요약
    /// </summary>
    public class LandParcelSummary
    {
        public decimal TotalLandArea { get; set; }
        public decimal TotalBuildingArea { get; set; }
        public decimal TotalAppraisalValue { get; set; }
        public int ParcelCount { get; set; }
    }
}
