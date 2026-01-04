using System;

namespace NPLogic.Data.Exceptions
{
    /// <summary>
    /// 세션 만료 시 발생하는 예외
    /// 토큰 갱신에 실패했을 때 이 예외가 발생하며,
    /// 전역 예외 핸들러에서 감지하여 로그인 화면으로 리다이렉트합니다.
    /// </summary>
    public class SessionExpiredException : Exception
    {
        public SessionExpiredException()
            : base("세션이 만료되었습니다. 다시 로그인해주세요.")
        {
        }

        public SessionExpiredException(string message)
            : base(message)
        {
        }

        public SessionExpiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

