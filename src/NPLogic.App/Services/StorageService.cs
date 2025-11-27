using System;
using System.IO;
using System.Threading.Tasks;
using Supabase.Storage;

namespace NPLogic.Services
{
    /// <summary>
    /// Supabase Storage 서비스
    /// </summary>
    public class StorageService
    {
        private readonly SupabaseService _supabaseService;

        public StorageService(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        }

        /// <summary>
        /// 파일 업로드 (로컬 파일 경로)
        /// </summary>
        /// <param name="filePath">로컬 파일 경로</param>
        /// <param name="bucketName">버킷 이름</param>
        /// <param name="fileName">저장할 파일명</param>
        /// <param name="onProgress">진행률 콜백 (0-100)</param>
        public async Task<string> UploadFileAsync(
            string filePath,
            string bucketName,
            string fileName,
            Action<double>? onProgress = null)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("파일을 찾을 수 없습니다.", filePath);

                var client = _supabaseService.GetClient();
                
                // 파일 읽기
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                
                // 진행률 시뮬레이션 (Supabase C# 클라이언트에 실제 진행률 콜백이 없음)
                onProgress?.Invoke(30);

                // 파일 업로드
                var storagePath = $"{DateTime.UtcNow:yyyy/MM/dd}/{fileName}";
                await client.Storage
                    .From(bucketName)
                    .Upload(fileBytes, storagePath);

                onProgress?.Invoke(100);

                // 공개 URL 반환
                var publicUrl = client.Storage
                    .From(bucketName)
                    .GetPublicUrl(storagePath);

                return publicUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"파일 업로드 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 파일 업로드 (바이트 배열)
        /// </summary>
        /// <param name="bucketName">버킷 이름</param>
        /// <param name="storagePath">저장 경로</param>
        /// <param name="fileBytes">파일 바이트 배열</param>
        public async Task<string> UploadFileAsync(
            string bucketName,
            string storagePath,
            byte[] fileBytes)
        {
            try
            {
                var client = _supabaseService.GetClient();
                
                await client.Storage
                    .From(bucketName)
                    .Upload(fileBytes, storagePath);

                // 공개 URL 반환
                var publicUrl = client.Storage
                    .From(bucketName)
                    .GetPublicUrl(storagePath);

                return publicUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"파일 업로드 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 공개 URL 가져오기
        /// </summary>
        public Task<string> GetPublicUrlAsync(string bucketName, string storagePath)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var publicUrl = client.Storage
                    .From(bucketName)
                    .GetPublicUrl(storagePath);
                return Task.FromResult(publicUrl);
            }
            catch (Exception ex)
            {
                throw new Exception($"URL 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 파일 다운로드
        /// </summary>
        public async Task<byte[]> DownloadFileAsync(string bucketName, string filePath)
        {
            try
            {
                var client = _supabaseService.GetClient();
                
                var fileBytes = await client.Storage
                    .From(bucketName)
                    .Download(filePath, null);

                return fileBytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"파일 다운로드 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 파일 삭제
        /// </summary>
        public async Task<bool> DeleteFileAsync(string bucketName, string filePath)
        {
            try
            {
                var client = _supabaseService.GetClient();
                
                var pathList = new System.Collections.Generic.List<string> { filePath };
                await client.Storage
                    .From(bucketName)
                    .Remove(pathList);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"파일 삭제 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 버킷 목록 조회
        /// </summary>
        public async Task<System.Collections.Generic.List<Bucket>?> ListBucketsAsync()
        {
            try
            {
                var client = _supabaseService.GetClient();
                var buckets = await client.Storage.ListBuckets();
                return buckets;
            }
            catch (Exception ex)
            {
                throw new Exception($"버킷 목록 조회 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 파일 목록 조회
        /// </summary>
        public async Task<System.Collections.Generic.List<FileObject>?> ListFilesAsync(
            string bucketName,
            string? path = null)
        {
            try
            {
                var client = _supabaseService.GetClient();
                var files = await client.Storage
                    .From(bucketName)
                    .List(path ?? string.Empty);

                return files;
            }
            catch (Exception ex)
            {
                throw new Exception($"파일 목록 조회 실패: {ex.Message}", ex);
            }
        }
    }
}

