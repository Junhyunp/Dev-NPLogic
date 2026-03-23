using System;

namespace NPLogic.Core.Models
{
    /// <summary>
    /// 물건별 탭별 비고 (1 property + 1 tab = 1 note)
    /// </summary>
    public class PropertyNote
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string TabName { get; set; } = string.Empty;
        public string NoteText { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
