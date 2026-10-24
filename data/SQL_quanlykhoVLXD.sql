-- 1. Tạo database trước
CREATE DATABASE app_quanlykhoVLXD;
GO

-- 2. Chuyển sang sử dụng database vừa tạo
USE app_quanlykhoVLXD;
GO

-- 3. Tạo bảng Nhân Viên
CREATE TABLE NhanVien (
    MaNV NVARCHAR(50) PRIMARY KEY,
    HoTen NVARCHAR(100),
    ChucVu NVARCHAR(100)
);

-- Bảng Nhà Cung Cấp
CREATE TABLE NhaCungCap (
    MaNCC NVARCHAR(50) PRIMARY KEY,
    TenNCC NVARCHAR(200),
    DiaChi NVARCHAR(250),
    DienThoai NVARCHAR(50)
);

-- Bảng Vật Tư
CREATE TABLE VatTu (
    MaVT NVARCHAR(50) PRIMARY KEY,
    TenVT NVARCHAR(200),
    DonViTinh NVARCHAR(50),
    GiaGoc FLOAT
);

-- Bảng Kho
CREATE TABLE Kho (
    MaKho NVARCHAR(50) PRIMARY KEY,
    TenKho NVARCHAR(200),
    DiaChi NVARCHAR(250)
);

-- Bảng Tồn Kho
CREATE TABLE TonKho (
    MaKho NVARCHAR(50),
    MaVT NVARCHAR(50),
    SoLuongTon INT,
    PRIMARY KEY (MaKho, MaVT)
);

-- Bảng Phiếu Nhập
CREATE TABLE PhieuNhap (
    MaPN NVARCHAR(50) PRIMARY KEY,
    NgayNhap NVARCHAR(50),
    MaNCC NVARCHAR(50),
    MaNV NVARCHAR(50)
);

-- Bảng Chi Tiết Phiếu Nhập
CREATE TABLE ChiTietPhieuNhap (
    MaPN NVARCHAR(50),
    MaVT NVARCHAR(50),
    SoLuong INT,
    DonGia FLOAT,
    PRIMARY KEY (MaPN, MaVT)
);

-- Bảng Phiếu Xuất
CREATE TABLE PhieuXuat (
    MaPX NVARCHAR(50) PRIMARY KEY,
    NgayXuat NVARCHAR(50),
    NguoiNhan NVARCHAR(200),
    MaNV NVARCHAR(50)
);

-- Bảng Chi Tiết Phiếu Xuất
CREATE TABLE ChiTietPhieuXuat (
    MaPX NVARCHAR(50),
    MaVT NVARCHAR(50),
    SoLuong INT,
    DonGia FLOAT,
    PRIMARY KEY (MaPX, MaVT)
);
GO

-- 4. Thêm dữ liệu mẫu
INSERT INTO NhanVien VALUES ('NV01', N'Nguyen Van A', N'Thu kho');
INSERT INTO NhanVien VALUES ('NV02', N'Tran Thi B', N'Nhan vien ban hang');

INSERT INTO NhaCungCap VALUES ('NCC01', N'Cong ty VLXD Mien Trung', N'Quy Nhon', '0351234567');
INSERT INTO NhaCungCap VALUES ('NCC02', N'Dai ly Sat Thep Nam A', 'Da Nang', '0905111222');

INSERT INTO VatTu VALUES ('VT01', N'Xi mang Hoang Thach', N'Bao', 85000);
INSERT INTO VatTu VALUES ('VT02', N'Sat phi 10 Pomina', N'Cay', 115000);
INSERT INTO VatTu VALUES ('VT03', N'Gach ong 2 lo', N'Vien', 1200);

INSERT INTO Kho VALUES ('K01', N'Kho Tong Quy Nhon', N'150 An Duong Vuong');
INSERT INTO Kho VALUES ('K02', N'Kho Phu', N'12 Tran Hung Dao');

INSERT INTO TonKho VALUES ('K01', 'VT01', 500);
INSERT INTO TonKho VALUES ('K01', 'VT02', 200);
INSERT INTO TonKho VALUES ('K02', 'VT03', 1000);

INSERT INTO PhieuNhap VALUES ('PN01', '2026-06-01', 'NCC01', 'NV01');
INSERT INTO PhieuNhap VALUES ('PN02', '2026-06-05', 'NCC02', 'NV01');

INSERT INTO ChiTietPhieuNhap VALUES ('PN01', 'VT01', 500, 80000);
INSERT INTO ChiTietPhieuNhap VALUES ('PN02', 'VT02', 200, 110000);

INSERT INTO PhieuXuat VALUES ('PX01', '2026-06-10', N'Cong ty Xay dung ABC', 'NV02');
INSERT INTO ChiTietPhieuXuat VALUES ('PX01', 'VT01', 50, 90000);
GO