-- Kết nối tới SQL Server mà CineBook.Api dùng và chọn đúng database trước khi chạy.
SET NOCOUNT ON;

SELECT @@SERVERNAME AS SqlServer, DB_NAME() AS DatabaseName, SYSUTCDATETIME() AS ReadAtUtc;

SELECT
    (SELECT COUNT_BIG(*) FROM dbo.Users) AS Accounts,
    (SELECT COUNT_BIG(*) FROM dbo.Bookings WHERE Status = N'Paid') AS PaidBookings,
    (SELECT COUNT_BIG(*) FROM dbo.Vouchers
     WHERE StartsAtUtc <= SYSUTCDATETIME() AND EndsAtUtc > SYSUTCDATETIME()) AS ActiveVouchers;

SELECT TOP (20)
    Id, FullName, MemberTier, City, CreatedAtUtc, [Version] AS ProfileVersion
FROM dbo.Users
ORDER BY [Version] DESC;

SELECT TOP (20)
    Code, Status, TotalAmount, CreatedAtUtc
FROM dbo.Bookings
ORDER BY CreatedAtUtc DESC, Id DESC;
