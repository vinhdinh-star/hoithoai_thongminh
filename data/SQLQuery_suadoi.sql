USE app_quanlykhoVLXD;
GO
-- Sửa 3 câu SQL mẫu để chạy được trên SQL Server (dùng khi đã chạy script cũ rồi)
UPDATE CauHoiSQL SET SQLQuery = N'SELECT LEFT(NgayNhap, 7) AS Thang, COUNT(*) AS SoLuotNhap FROM PhieuNhap GROUP BY LEFT(NgayNhap, 7);' WHERE ID = 44;
UPDATE CauHoiSQL SET SQLQuery = N'SELECT LEFT(NgayGiaoDich, 7) AS Thang, COUNT(*) AS TongGiaoDich FROM (SELECT NgayNhap AS NgayGiaoDich FROM PhieuNhap UNION ALL SELECT NgayXuat AS NgayGiaoDich FROM PhieuXuat) t GROUP BY LEFT(NgayGiaoDich, 7);' WHERE ID = 99;
UPDATE CauHoiSQL SET SQLQuery = N'SELECT COUNT(DISTINCT MaNV) AS SoNhanVienHoatDong FROM (SELECT MaNV FROM PhieuNhap UNION SELECT MaNV FROM PhieuXuat) t;' WHERE ID = 100;
GO