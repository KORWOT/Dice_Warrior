using System;
using System.IO;

namespace FateDice
{
    public static class MetaFailure
    {
        // Retain InvalidDataException compatibility for existing local save consumers.
        // The typed cause carries the recovery distinction; never parse localized messages.
        public static InvalidDataException InvalidFile(MetaFailureKind kind, string message) =>
            new InvalidDataException(message, new MetaCommandException(message, kind));

        public static MetaFailureKind Classify(Exception error)
        {
            if (error is MetaCommandException command) return command.Kind;
            if (error is InvalidDataException)
                return error.InnerException is MetaCommandException cause &&
                    (cause.Kind == MetaFailureKind.UnsupportedSchema || cause.Kind == MetaFailureKind.ForeignProfile)
                    ? cause.Kind : MetaFailureKind.Corrupt;
            if (error is IOException || error is UnauthorizedAccessException) return MetaFailureKind.TemporaryIO;
            return MetaFailureKind.Rejected;
        }

        public static string Message(MetaFailureKind kind)
        {
            switch (kind)
            {
                case MetaFailureKind.Stale: return "다른 진행이 먼저 저장되었습니다. 저장된 여정을 다시 불러와 주세요.";
                case MetaFailureKind.TemporaryIO: return "저장을 읽거나 기록하지 못했습니다. 저장 공간과 접근 권한을 확인한 뒤 다시 시도해 주세요.";
                case MetaFailureKind.Corrupt: return "저장 파일이 손상되었거나 내용이 올바르지 않습니다. 원본은 보존했습니다.";
                case MetaFailureKind.UnsupportedSchema: return "지원하지 않는 저장 버전입니다. 이 저장을 지원하는 게임 버전이 필요합니다.";
                case MetaFailureKind.ForeignProfile: return "다른 플레이어의 저장입니다. 현재 프로필의 저장을 확인해 주세요.";
                default: return "요청 조건을 확인한 뒤 다시 시도해 주세요.";
            }
        }
    }
}
