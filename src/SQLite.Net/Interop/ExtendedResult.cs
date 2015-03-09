using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace SQLite.Net.Interop
{
    [PublicAPI]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ExtendedResult
    {
        IOErrorRead = (Result.IOError | (1 << 8)),
        IOErrorShortRead = (Result.IOError | (2 << 8)),
        IOErrorWrite = (Result.IOError | (3 << 8)),
        IOErrorFsync = (Result.IOError | (4 << 8)),
        IOErrorDirFSync = (Result.IOError | (5 << 8)),
        IOErrorTruncate = (Result.IOError | (6 << 8)),
        IOErrorFStat = (Result.IOError | (7 << 8)),
        IOErrorUnlock = (Result.IOError | (8 << 8)),
        IOErrorRdlock = (Result.IOError | (9 << 8)),
        IOErrorDelete = (Result.IOError | (10 << 8)),
        IOErrorBlocked = (Result.IOError | (11 << 8)),
        IOErrorNoMem = (Result.IOError | (12 << 8)),
        IOErrorAccess = (Result.IOError | (13 << 8)),
        IOErrorCheckReservedLock = (Result.IOError | (14 << 8)),
        IOErrorLock = (Result.IOError | (15 << 8)),
        IOErrorClose = (Result.IOError | (16 << 8)),
        IOErrorDirClose = (Result.IOError | (17 << 8)),
        IOErrorSHMOpen = (Result.IOError | (18 << 8)),
        IOErrorSHMSize = (Result.IOError | (19 << 8)),
        IOErrorSHMLock = (Result.IOError | (20 << 8)),
        IOErrorSHMMap = (Result.IOError | (21 << 8)),
        IOErrorSeek = (Result.IOError | (22 << 8)),
        IOErrorDeleteNoEnt = (Result.IOError | (23 << 8)),
        IOErrorMMap = (Result.IOError | (24 << 8)),
        LockedSharedcache = (Result.Locked | (1 << 8)),
        BusyRecovery = (Result.Busy | (1 << 8)),
        CannottOpenNoTempDir = (Result.CannotOpen | (1 << 8)),
        CannotOpenIsDir = (Result.CannotOpen | (2 << 8)),
        CannotOpenFullPath = (Result.CannotOpen | (3 << 8)),
        CorruptVTab = (Result.Corrupt | (1 << 8)),
        ReadonlyRecovery = (Result.ReadOnly | (1 << 8)),
        ReadonlyCannotLock = (Result.ReadOnly | (2 << 8)),
        ReadonlyRollback = (Result.ReadOnly | (3 << 8)),
        AbortRollback = (Result.Abort | (2 << 8)),
        ConstraintCheck = (Result.Constraint | (1 << 8)),
        ConstraintCommitHook = (Result.Constraint | (2 << 8)),
        ConstraintForeignKey = (Result.Constraint | (3 << 8)),
        ConstraintFunction = (Result.Constraint | (4 << 8)),
        ConstraintNotNull = (Result.Constraint | (5 << 8)),
        ConstraintPrimaryKey = (Result.Constraint | (6 << 8)),
        ConstraintTrigger = (Result.Constraint | (7 << 8)),
        ConstraintUnique = (Result.Constraint | (8 << 8)),
        ConstraintVTab = (Result.Constraint | (9 << 8)),
        NoticeRecoverWAL = (Result.Notice | (1 << 8)),
        NoticeRecoverRollback = (Result.Notice | (2 << 8))
    }
}