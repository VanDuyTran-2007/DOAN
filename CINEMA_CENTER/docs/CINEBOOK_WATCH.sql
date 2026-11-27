-- Chọn đúng database CineBook trong SSMS, rồi chạy và mở tab Messages.
-- Mặc định đọc lại mỗi 5 giây trong khoảng 1 phút. Có thể bấm Cancel để dừng sớm.
SET NOCOUNT ON;

DECLARE @Remaining int = 12;
DECLARE @Message nvarchar(500);

WHILE @Remaining > 0
BEGIN
    SELECT @Message = CONCAT(
        CONVERT(varchar(19), SYSDATETIME(), 120), N' | ', DB_NAME(),
        N' | Tài khoản: ', (SELECT COUNT_BIG(*) FROM dbo.Users),
        N' | Phiên bản hồ sơ mới nhất: ',
            (SELECT COALESCE(MAX(CONVERT(bigint, [Version])), 0)
             FROM dbo.Users),
        N' | Đơn đã thanh toán: ',
            (SELECT COUNT_BIG(*) FROM dbo.Bookings WHERE Status = N'Paid')
    );

    RAISERROR(N'%s', 0, 1, @Message) WITH NOWAIT;
    SET @Remaining -= 1;

    IF @Remaining > 0
        WAITFOR DELAY '00:00:05';
END;
