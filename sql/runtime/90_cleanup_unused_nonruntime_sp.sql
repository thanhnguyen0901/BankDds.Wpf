USE NGANHANG_PUB;
GO
/*
  Cleanup non-runtime procedures after moving to UI-first architecture.
  Safe to run multiple times.

  Notes:
  - Do NOT drop transitional user CRUD SP yet (Phase D still uses them).
  - This script only removes procedures that are not part of runtime.
*/

IF OBJECT_ID(N'dbo.SP_CreateTransferTransaction', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.SP_CreateTransferTransaction;
    PRINT N'>>> Dropped dbo.SP_CreateTransferTransaction';
END
GO

IF OBJECT_ID(N'dbo.sp_SafeAddMergeProcArticle', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_SafeAddMergeProcArticle;
    PRINT N'>>> Dropped dbo.sp_SafeAddMergeProcArticle';
END
GO

IF OBJECT_ID(N'dbo.sp_SafeAddMergeViewArticle', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_SafeAddMergeViewArticle;
    PRINT N'>>> Dropped dbo.sp_SafeAddMergeViewArticle';
END
GO

/*
  Subscriber-only helper from legacy post-replication fixups.
  Run this block on subscriber databases only if that helper exists.
*/
IF OBJECT_ID(N'dbo.sp_SafeGrantExec', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_SafeGrantExec;
    PRINT N'>>> Dropped dbo.sp_SafeGrantExec';
END
GO
