using System;
using System.Collections.Generic;
using System.Text.Json;
using NPLogic.Core.Models;

namespace NPLogic.Services
{
    /// <summary>
    /// Property 상태 스냅샷
    /// </summary>
    public class PropertySnapshot
    {
        /// <summary>
        /// 스냅샷 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// JSON 직렬화된 Property 데이터
        /// </summary>
        public string JsonData { get; set; } = "";

        /// <summary>
        /// 스냅샷 설명 (예: "탭 이동 전")
        /// </summary>
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Undo 서비스 - 3단계 되돌리기 기능 지원
    /// 피드백 반영: 자동저장 + 되돌리기 (최대 3스텝)
    /// </summary>
    public class UndoService
    {
        private static UndoService? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// 최대 Undo 스택 크기
        /// </summary>
        private const int MaxUndoSteps = 3;

        /// <summary>
        /// Property ID별 Undo 스택
        /// </summary>
        private readonly Dictionary<Guid, Stack<PropertySnapshot>> _undoStacks = new();

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static UndoService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new UndoService();
                    }
                }
                return _instance;
            }
        }

        private UndoService() { }

        /// <summary>
        /// 현재 Property 상태를 스냅샷으로 저장
        /// </summary>
        /// <param name="property">저장할 Property</param>
        /// <param name="description">스냅샷 설명</param>
        public void SaveSnapshot(Property property, string description = "")
        {
            if (property == null || property.Id == Guid.Empty)
                return;

            // 해당 Property의 스택이 없으면 생성
            if (!_undoStacks.ContainsKey(property.Id))
            {
                _undoStacks[property.Id] = new Stack<PropertySnapshot>();
            }

            var stack = _undoStacks[property.Id];

            // 최대 크기 초과 시 가장 오래된 것 제거
            if (stack.Count >= MaxUndoSteps)
            {
                // Stack은 LIFO이므로, 임시 리스트로 변환 후 가장 오래된 것 제거
                var items = new List<PropertySnapshot>(stack);
                items.RemoveAt(items.Count - 1); // 가장 오래된 것 제거
                stack.Clear();
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    stack.Push(items[i]);
                }
            }

            // JSON 직렬화
            var jsonData = JsonSerializer.Serialize(property, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var snapshot = new PropertySnapshot
            {
                CreatedAt = DateTime.Now,
                JsonData = jsonData,
                Description = description
            };

            stack.Push(snapshot);
        }

        /// <summary>
        /// Undo 가능 여부 확인
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Undo 가능 여부</returns>
        public bool CanUndo(Guid propertyId)
        {
            return _undoStacks.ContainsKey(propertyId) && _undoStacks[propertyId].Count > 0;
        }

        /// <summary>
        /// Undo 스택 개수 반환
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>Undo 가능 횟수</returns>
        public int GetUndoCount(Guid propertyId)
        {
            if (!_undoStacks.ContainsKey(propertyId))
                return 0;
            return _undoStacks[propertyId].Count;
        }

        /// <summary>
        /// 마지막 스냅샷으로 되돌리기
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>복원된 Property (null이면 실패)</returns>
        public Property? Undo(Guid propertyId)
        {
            if (!CanUndo(propertyId))
                return null;

            var snapshot = _undoStacks[propertyId].Pop();

            try
            {
                var property = JsonSerializer.Deserialize<Property>(snapshot.JsonData);
                return property;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 특정 Property의 Undo 스택 초기화
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        public void ClearUndoStack(Guid propertyId)
        {
            if (_undoStacks.ContainsKey(propertyId))
            {
                _undoStacks[propertyId].Clear();
            }
        }

        /// <summary>
        /// 모든 Undo 스택 초기화
        /// </summary>
        public void ClearAll()
        {
            _undoStacks.Clear();
        }

        /// <summary>
        /// 마지막 스냅샷 정보 조회 (Peek)
        /// </summary>
        /// <param name="propertyId">Property ID</param>
        /// <returns>마지막 스냅샷 정보 (없으면 null)</returns>
        public PropertySnapshot? PeekSnapshot(Guid propertyId)
        {
            if (!CanUndo(propertyId))
                return null;
            return _undoStacks[propertyId].Peek();
        }
    }
}
