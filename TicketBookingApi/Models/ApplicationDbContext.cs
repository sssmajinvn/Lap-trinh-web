using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TicketBookingApi.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Binhluan> Binhluans { get; set; }

    public virtual DbSet<Combo> Combos { get; set; }

    public virtual DbSet<ComboItem> ComboItems { get; set; }

    public virtual DbSet<Daodien> Daodiens { get; set; }

    public virtual DbSet<Dienvien> Dienviens { get; set; }

    public virtual DbSet<Dondatve> Dondatves { get; set; }

    public virtual DbSet<Ghengoi> Ghengois { get; set; }

    public virtual DbSet<Hashtag> Hashtags { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }

    public virtual DbSet<KhuyenMaiPhim> KhuyenMaiPhims { get; set; }

    public virtual DbSet<KhuyenMaiUser> KhuyenMaiUsers { get; set; }

    public virtual DbSet<LichSuKhuyenMai> LichSuKhuyenMais { get; set; }

    public virtual DbSet<Lichchieu> Lichchieus { get; set; }

    public virtual DbSet<Nhanvien> Nhanviens { get; set; }

    public virtual DbSet<OrderConcession> OrderConcessions { get; set; }

    public virtual DbSet<Phim> Phims { get; set; }

    public virtual DbSet<Phongrapphim> Phongrapphims { get; set; }

    public virtual DbSet<Rapphim> Rapphims { get; set; }

    public virtual DbSet<Theloai> Theloais { get; set; }

    public virtual DbSet<Thongbao> Thongbaos { get; set; }

    public virtual DbSet<Thongtintaikhoan> Thongtintaikhoans { get; set; }

    public virtual DbSet<Thongtinthanhtoan> Thongtinthanhtoans { get; set; }

    public virtual DbSet<Vexemphim> Vexemphims { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=ep-cool-field-a1jhgki6-pooler.ap-southeast-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_m2z4fFZjQIyl;SslMode=Require;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Binhluan>(entity =>
        {
            entity.HasKey(e => e.Mabinhluan).HasName("binhluan_pkey");

            entity.ToTable("binhluan");

            entity.Property(e => e.Mabinhluan)
                .HasMaxLength(500)
                .HasColumnName("mabinhluan");
            entity.Property(e => e.Danhgia).HasColumnName("danhgia");
            entity.Property(e => e.IdKhach).HasColumnName("id_khach");
            entity.Property(e => e.IdNhanvien).HasColumnName("id_nhanvien");
            entity.Property(e => e.Maphim)
                .HasMaxLength(500)
                .HasColumnName("maphim");
            entity.Property(e => e.Noidung)
                .HasMaxLength(500)
                .HasColumnName("noidung");
            entity.Property(e => e.Noidungreply)
                .HasMaxLength(500)
                .HasColumnName("noidungreply");
            entity.Property(e => e.Thoidiemdanhgia)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("thoidiemdanhgia");

            entity.HasOne(d => d.IdKhachNavigation).WithMany(p => p.Binhluans)
                .HasForeignKey(d => d.IdKhach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("binhluan_id_khach_fkey");

            entity.HasOne(d => d.IdNhanvienNavigation).WithMany(p => p.Binhluans)
                .HasForeignKey(d => d.IdNhanvien)
                .HasConstraintName("binhluan_id_nhanvien_fkey");

            entity.HasOne(d => d.MaphimNavigation).WithMany(p => p.Binhluans)
                .HasForeignKey(d => d.Maphim)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("binhluan_maphim_fkey");
        });

        modelBuilder.Entity<Combo>(entity =>
        {
            entity.HasKey(e => e.ComboId).HasName("combos_pkey");

            entity.ToTable("combos");

            entity.Property(e => e.ComboId).HasColumnName("combo_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price).HasColumnName("price");
        });

        modelBuilder.Entity<ComboItem>(entity =>
        {
            entity.HasKey(e => e.ComboItemId).HasName("combo_items_pkey");

            entity.ToTable("combo_items");

            entity.Property(e => e.ComboItemId).HasColumnName("combo_item_id");
            entity.Property(e => e.ComboId).HasColumnName("combo_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Combo).WithMany(p => p.ComboItems)
                .HasForeignKey(d => d.ComboId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("combo_items_combo_id_fkey");

            entity.HasOne(d => d.Item).WithMany(p => p.ComboItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("combo_items_item_id_fkey");
        });

        modelBuilder.Entity<Daodien>(entity =>
        {
            entity.HasKey(e => e.Madaodien).HasName("daodien_pkey");

            entity.ToTable("daodien");

            entity.Property(e => e.Madaodien)
                .HasMaxLength(500)
                .HasColumnName("madaodien");
            entity.Property(e => e.Ngaysinh)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaysinh");
            entity.Property(e => e.Quoctich)
                .HasMaxLength(500)
                .HasColumnName("quoctich");
            entity.Property(e => e.Tendaodien)
                .HasMaxLength(500)
                .HasColumnName("tendaodien");
            entity.Property(e => e.Urlanhdaidien).HasColumnName("urlanhdaidien");
        });

        modelBuilder.Entity<Dienvien>(entity =>
        {
            entity.HasKey(e => e.Madienvien).HasName("dienvien_pkey");

            entity.ToTable("dienvien");

            entity.Property(e => e.Madienvien)
                .HasMaxLength(500)
                .HasColumnName("madienvien");
            entity.Property(e => e.Ngaysinh)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaysinh");
            entity.Property(e => e.Quoctich)
                .HasMaxLength(500)
                .HasColumnName("quoctich");
            entity.Property(e => e.Tendienvien)
                .HasMaxLength(500)
                .HasColumnName("tendienvien");
            entity.Property(e => e.Urlanhdaidien).HasColumnName("urlanhdaidien");

            entity.HasMany(d => d.Maphims).WithMany(p => p.Madienviens)
                .UsingEntity<Dictionary<string, object>>(
                    "PhimDienvien",
                    r => r.HasOne<Phim>().WithMany()
                        .HasForeignKey("Maphim")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_dienvien_maphim_fkey"),
                    l => l.HasOne<Dienvien>().WithMany()
                        .HasForeignKey("Madienvien")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_dienvien_madienvien_fkey"),
                    j =>
                    {
                        j.HasKey("Madienvien", "Maphim").HasName("phim_dienvien_pkey");
                        j.ToTable("phim_dienvien");
                        j.IndexerProperty<string>("Madienvien")
                            .HasMaxLength(500)
                            .HasColumnName("madienvien");
                        j.IndexerProperty<string>("Maphim")
                            .HasMaxLength(500)
                            .HasColumnName("maphim");
                    });
        });

        modelBuilder.Entity<Dondatve>(entity =>
        {
            entity.HasKey(e => e.Madondatve).HasName("dondatve_pkey");

            entity.ToTable("dondatve");

            entity.Property(e => e.Madondatve)
                .HasMaxLength(500)
                .HasColumnName("madondatve");
            entity.Property(e => e.IdKhach).HasColumnName("id_khach");
            entity.Property(e => e.Ngaydatve)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngaydatve");
            entity.Property(e => e.Tongtien)
                .HasDefaultValue(0)
                .HasColumnName("tongtien");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(500)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("trangthai");

            entity.HasOne(d => d.IdKhachNavigation).WithMany(p => p.Dondatves)
                .HasForeignKey(d => d.IdKhach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("dondatve_id_khach_fkey");
        });

        modelBuilder.Entity<Ghengoi>(entity =>
        {
            entity.HasKey(e => e.Maghe).HasName("ghengoi_pkey");

            entity.ToTable("ghengoi");

            entity.Property(e => e.Maghe)
                .HasMaxLength(500)
                .HasColumnName("maghe");
            entity.Property(e => e.Hesogiaghe)
                .HasDefaultValueSql("1")
                .HasColumnName("hesogiaghe");
            entity.Property(e => e.Loaighe)
                .HasMaxLength(500)
                .HasColumnName("loaighe");
            entity.Property(e => e.Mahangghe)
                .HasMaxLength(500)
                .HasColumnName("mahangghe");
            entity.Property(e => e.Maphong)
                .HasMaxLength(500)
                .HasColumnName("maphong");
            entity.Property(e => e.Soghe).HasColumnName("soghe");

            entity.HasOne(d => d.MaphongNavigation).WithMany(p => p.Ghengois)
                .HasForeignKey(d => d.Maphong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ghengoi_maphong_fkey");
        });

        modelBuilder.Entity<Hashtag>(entity =>
        {
            entity.HasKey(e => e.Mahashtag).HasName("hashtag_pkey");

            entity.ToTable("hashtag");

            entity.Property(e => e.Mahashtag)
                .HasMaxLength(500)
                .HasColumnName("mahashtag");
            entity.Property(e => e.Tenhashtag)
                .HasMaxLength(500)
                .HasColumnName("tenhashtag");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("items_pkey");

            entity.ToTable("items");

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.ItemType)
                .HasMaxLength(50)
                .HasColumnName("item_type");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.StockQuantity)
                .HasDefaultValue(0)
                .HasColumnName("stock_quantity");
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasDefaultValueSql("'thùng'::character varying")
                .HasColumnName("unit");
        });

        modelBuilder.Entity<KhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("khuyen_mai_pkey");

            entity.ToTable("khuyen_mai");

            entity.HasIndex(e => e.MaKhuyenMai, "khuyen_mai_ma_khuyen_mai_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApDungCho)
                .HasMaxLength(20)
                .HasDefaultValueSql("'TAT_CA_PHIM'::character varying")
                .HasColumnName("ap_dung_cho");
            entity.Property(e => e.ApDungUser)
                .HasMaxLength(20)
                .HasDefaultValueSql("'TAT_CA_USER'::character varying")
                .HasColumnName("ap_dung_user");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.GiaTriDonHangToiThieu)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("gia_tri_don_hang_toi_thieu");
            entity.Property(e => e.GiaTriGiam)
                .HasPrecision(10, 2)
                .HasColumnName("gia_tri_giam");
            entity.Property(e => e.GiamToiDa)
                .HasPrecision(10, 2)
                .HasColumnName("giam_toi_da");
            entity.Property(e => e.LoaiGiam)
                .HasMaxLength(20)
                .HasColumnName("loai_giam");
            entity.Property(e => e.MaKhuyenMai)
                .HasMaxLength(20)
                .HasColumnName("ma_khuyen_mai");
            entity.Property(e => e.MoTa).HasColumnName("mo_ta");
            entity.Property(e => e.NgayBatDau)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngay_bat_dau");
            entity.Property(e => e.NgayKetThuc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngay_ket_thuc");
            entity.Property(e => e.SoLanDungToiDaMoiUser)
                .HasDefaultValue(1)
                .HasColumnName("so_lan_dung_toi_da_moi_user");
            entity.Property(e => e.SoLuongDaDung)
                .HasDefaultValue(0)
                .HasColumnName("so_luong_da_dung");
            entity.Property(e => e.SoLuongMa).HasColumnName("so_luong_ma");
            entity.Property(e => e.SoLuongVeToiThieu)
                .HasDefaultValue(1)
                .HasColumnName("so_luong_ve_toi_thieu");
            entity.Property(e => e.TenKhuyenMai)
                .HasMaxLength(200)
                .HasColumnName("ten_khuyen_mai");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("trang_thai");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<KhuyenMaiPhim>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("khuyen_mai_phim_pkey");

            entity.ToTable("khuyen_mai_phim");

            entity.HasIndex(e => new { e.IdKhuyenMai, e.Maphim }, "khuyen_mai_phim_id_khuyen_mai_maphim_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdKhuyenMai).HasColumnName("id_khuyen_mai");
            entity.Property(e => e.Maphim)
                .HasMaxLength(500)
                .HasColumnName("maphim");

            entity.HasOne(d => d.IdKhuyenMaiNavigation).WithMany(p => p.KhuyenMaiPhims)
                .HasForeignKey(d => d.IdKhuyenMai)
                .HasConstraintName("khuyen_mai_phim_id_khuyen_mai_fkey");

            entity.HasOne(d => d.MaphimNavigation).WithMany(p => p.KhuyenMaiPhims)
                .HasForeignKey(d => d.Maphim)
                .HasConstraintName("khuyen_mai_phim_maphim_fkey");
        });

        modelBuilder.Entity<KhuyenMaiUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("khuyen_mai_user_pkey");

            entity.ToTable("khuyen_mai_user");

            entity.HasIndex(e => new { e.IdKhuyenMai, e.IdKhach }, "khuyen_mai_user_id_khuyen_mai_id_khach_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IdKhach).HasColumnName("id_khach");
            entity.Property(e => e.IdKhuyenMai).HasColumnName("id_khuyen_mai");

            entity.HasOne(d => d.IdKhachNavigation).WithMany(p => p.KhuyenMaiUsers)
                .HasForeignKey(d => d.IdKhach)
                .HasConstraintName("khuyen_mai_user_id_khach_fkey");

            entity.HasOne(d => d.IdKhuyenMaiNavigation).WithMany(p => p.KhuyenMaiUsers)
                .HasForeignKey(d => d.IdKhuyenMai)
                .HasConstraintName("khuyen_mai_user_id_khuyen_mai_fkey");
        });

        modelBuilder.Entity<LichSuKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lich_su_khuyen_mai_pkey");

            entity.ToTable("lich_su_khuyen_mai");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GiaTriGiamThucTe)
                .HasPrecision(10, 2)
                .HasColumnName("gia_tri_giam_thuc_te");
            entity.Property(e => e.IdKhach).HasColumnName("id_khach");
            entity.Property(e => e.IdKhuyenMai).HasColumnName("id_khuyen_mai");
            entity.Property(e => e.Madondatve)
                .HasMaxLength(500)
                .HasColumnName("madondatve");
            entity.Property(e => e.NgaySuDung)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngay_su_dung");

            entity.HasOne(d => d.IdKhachNavigation).WithMany(p => p.LichSuKhuyenMais)
                .HasForeignKey(d => d.IdKhach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lich_su_khuyen_mai_id_khach_fkey");

            entity.HasOne(d => d.IdKhuyenMaiNavigation).WithMany(p => p.LichSuKhuyenMais)
                .HasForeignKey(d => d.IdKhuyenMai)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lich_su_khuyen_mai_id_khuyen_mai_fkey");

            entity.HasOne(d => d.MadondatveNavigation).WithMany(p => p.LichSuKhuyenMais)
                .HasForeignKey(d => d.Madondatve)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lich_su_khuyen_mai_madondatve_fkey");
        });

        modelBuilder.Entity<Lichchieu>(entity =>
        {
            entity.HasKey(e => e.Malichchieu).HasName("lichchieu_pkey");

            entity.ToTable("lichchieu");

            entity.Property(e => e.Malichchieu)
                .HasMaxLength(500)
                .HasColumnName("malichchieu");
            entity.Property(e => e.Giave).HasColumnName("giave");
            entity.Property(e => e.Giochieu)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("giochieu");
            entity.Property(e => e.Gioketthuc)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("gioketthuc");
            entity.Property(e => e.Maphim)
                .HasMaxLength(500)
                .HasColumnName("maphim");
            entity.Property(e => e.Maphong)
                .HasMaxLength(500)
                .HasColumnName("maphong");
            entity.Property(e => e.Ngaychieu)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaychieu");

            entity.HasOne(d => d.MaphimNavigation).WithMany(p => p.Lichchieus)
                .HasForeignKey(d => d.Maphim)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lichchieu_maphim_fkey");

            entity.HasOne(d => d.MaphongNavigation).WithMany(p => p.Lichchieus)
                .HasForeignKey(d => d.Maphong)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lichchieu_maphong_fkey");
        });

        modelBuilder.Entity<Nhanvien>(entity =>
        {
            entity.HasKey(e => e.IdNhanvien).HasName("nhanvien_pkey");

            entity.ToTable("nhanvien");

            entity.HasIndex(e => e.Email, "nhanvien_email_key").IsUnique();

            entity.HasIndex(e => e.Manhanvien, "nhanvien_manhanvien_key").IsUnique();

            entity.HasIndex(e => e.Sdt, "nhanvien_sdt_key").IsUnique();

            entity.Property(e => e.IdNhanvien).HasColumnName("id_nhanvien");
            entity.Property(e => e.Anhdaidien)
                .HasMaxLength(500)
                .HasColumnName("anhdaidien");
            entity.Property(e => e.Email)
                .HasMaxLength(500)
                .HasColumnName("email");
            entity.Property(e => e.Gioitinh).HasColumnName("gioitinh");
            entity.Property(e => e.Hoten)
                .HasMaxLength(500)
                .HasColumnName("hoten");
            entity.Property(e => e.Manhanvien)
                .HasMaxLength(500)
                .HasColumnName("manhanvien");
            entity.Property(e => e.Matkhau)
                .HasMaxLength(500)
                .HasColumnName("matkhau");
            entity.Property(e => e.Ngaycapnhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaycapnhat");
            entity.Property(e => e.Ngaysinh)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaysinh");
            entity.Property(e => e.Ngaytao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaytao");
            entity.Property(e => e.Sdt)
                .HasMaxLength(50)
                .HasColumnName("sdt");
            entity.Property(e => e.Vaitro)
                .HasMaxLength(500)
                .HasColumnName("vaitro");

            entity.HasMany(d => d.Marapphims).WithMany(p => p.IdNhanviens)
                .UsingEntity<Dictionary<string, object>>(
                    "RapphimNhanvien",
                    r => r.HasOne<Rapphim>().WithMany()
                        .HasForeignKey("Marapphim")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("rapphim_nhanvien_marapphim_fkey"),
                    l => l.HasOne<Nhanvien>().WithMany()
                        .HasForeignKey("IdNhanvien")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("rapphim_nhanvien_id_nhanvien_fkey"),
                    j =>
                    {
                        j.HasKey("IdNhanvien", "Marapphim").HasName("rapphim_nhanvien_pkey");
                        j.ToTable("rapphim_nhanvien");
                        j.IndexerProperty<int>("IdNhanvien").HasColumnName("id_nhanvien");
                        j.IndexerProperty<string>("Marapphim")
                            .HasMaxLength(500)
                            .HasColumnName("marapphim");
                    });
        });

        modelBuilder.Entity<OrderConcession>(entity =>
        {
            entity.HasKey(e => e.IdOrderConcession).HasName("order_concessions_pkey");

            entity.ToTable("order_concessions");

            entity.Property(e => e.IdOrderConcession).HasColumnName("id_order_concession");
            entity.Property(e => e.ComboId).HasColumnName("combo_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Madondatve)
                .HasMaxLength(500)
                .HasColumnName("madondatve");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.ThanhTien).HasColumnName("thanh_tien");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price");

            entity.HasOne(d => d.Combo).WithMany(p => p.OrderConcessions)
                .HasForeignKey(d => d.ComboId)
                .HasConstraintName("order_concessions_combo_id_fkey");

            entity.HasOne(d => d.Item).WithMany(p => p.OrderConcessions)
                .HasForeignKey(d => d.ItemId)
                .HasConstraintName("order_concessions_item_id_fkey");

            entity.HasOne(d => d.MadondatveNavigation).WithMany(p => p.OrderConcessions)
                .HasForeignKey(d => d.Madondatve)
                .HasConstraintName("fk_dondatve");
        });

        modelBuilder.Entity<Phim>(entity =>
        {
            entity.HasKey(e => e.Maphim).HasName("phim_pkey");

            entity.ToTable("phim");

            entity.HasIndex(e => e.TmdbId, "phim_tmdb_id_key").IsUnique();

            entity.Property(e => e.Maphim)
                .HasMaxLength(500)
                .HasColumnName("maphim");
            entity.Property(e => e.Duoctaoboi)
                .HasMaxLength(500)
                .HasColumnName("duoctaoboi");
            entity.Property(e => e.Duoctaongay)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("duoctaongay");
            entity.Property(e => e.Gioihantuoi).HasColumnName("gioihantuoi");
            entity.Property(e => e.Mota).HasColumnName("mota");
            entity.Property(e => e.Ngayramat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngayramat");
            entity.Property(e => e.PosterUrl)
                .HasMaxLength(500)
                .HasColumnName("poster_url");
            entity.Property(e => e.Tenphim)
                .HasMaxLength(500)
                .HasColumnName("tenphim");
            entity.Property(e => e.Thoiluong).HasColumnName("thoiluong");
            entity.Property(e => e.TmdbId)
                .HasMaxLength(50)
                .HasColumnName("tmdb_id");
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(500)
                .HasColumnName("trailer_url");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(500)
                .HasDefaultValueSql("'coming_soon'::character varying")
                .HasColumnName("trangthai");

            entity.HasMany(d => d.Madaodiens).WithMany(p => p.Maphims)
                .UsingEntity<Dictionary<string, object>>(
                    "PhimDaodien",
                    r => r.HasOne<Daodien>().WithMany()
                        .HasForeignKey("Madaodien")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_daodien_madaodien_fkey"),
                    l => l.HasOne<Phim>().WithMany()
                        .HasForeignKey("Maphim")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_daodien_maphim_fkey"),
                    j =>
                    {
                        j.HasKey("Maphim", "Madaodien").HasName("phim_daodien_pkey");
                        j.ToTable("phim_daodien");
                        j.IndexerProperty<string>("Maphim")
                            .HasMaxLength(500)
                            .HasColumnName("maphim");
                        j.IndexerProperty<string>("Madaodien")
                            .HasMaxLength(500)
                            .HasColumnName("madaodien");
                    });

            entity.HasMany(d => d.Mahashtags).WithMany(p => p.Maphims)
                .UsingEntity<Dictionary<string, object>>(
                    "PhimHashtag",
                    r => r.HasOne<Hashtag>().WithMany()
                        .HasForeignKey("Mahashtag")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_hashtag_mahashtag_fkey"),
                    l => l.HasOne<Phim>().WithMany()
                        .HasForeignKey("Maphim")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_hashtag_maphim_fkey"),
                    j =>
                    {
                        j.HasKey("Maphim", "Mahashtag").HasName("phim_hashtag_pkey");
                        j.ToTable("phim_hashtag");
                        j.IndexerProperty<string>("Maphim")
                            .HasMaxLength(500)
                            .HasColumnName("maphim");
                        j.IndexerProperty<string>("Mahashtag")
                            .HasMaxLength(500)
                            .HasColumnName("mahashtag");
                    });

            entity.HasMany(d => d.Matheloais).WithMany(p => p.Maphims)
                .UsingEntity<Dictionary<string, object>>(
                    "PhimTheloai",
                    r => r.HasOne<Theloai>().WithMany()
                        .HasForeignKey("Matheloai")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_theloai_matheloai_fkey"),
                    l => l.HasOne<Phim>().WithMany()
                        .HasForeignKey("Maphim")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("phim_theloai_maphim_fkey"),
                    j =>
                    {
                        j.HasKey("Maphim", "Matheloai").HasName("phim_theloai_pkey");
                        j.ToTable("phim_theloai");
                        j.IndexerProperty<string>("Maphim")
                            .HasMaxLength(500)
                            .HasColumnName("maphim");
                        j.IndexerProperty<string>("Matheloai")
                            .HasMaxLength(500)
                            .HasColumnName("matheloai");
                    });
        });

        modelBuilder.Entity<Phongrapphim>(entity =>
        {
            entity.HasKey(e => e.Maphong).HasName("phongrapphim_pkey");

            entity.ToTable("phongrapphim");

            entity.Property(e => e.Maphong)
                .HasMaxLength(500)
                .HasColumnName("maphong");
            entity.Property(e => e.Marapphim)
                .HasMaxLength(500)
                .HasColumnName("marapphim");
            entity.Property(e => e.Soluongghe).HasColumnName("soluongghe");
            entity.Property(e => e.Tenphong)
                .HasMaxLength(500)
                .HasColumnName("tenphong");

            entity.HasOne(d => d.MarapphimNavigation).WithMany(p => p.Phongrapphims)
                .HasForeignKey(d => d.Marapphim)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("phongrapphim_marapphim_fkey");
        });

        modelBuilder.Entity<Rapphim>(entity =>
        {
            entity.HasKey(e => e.Marapphim).HasName("rapphim_pkey");

            entity.ToTable("rapphim");

            entity.Property(e => e.Marapphim)
                .HasMaxLength(500)
                .HasColumnName("marapphim");
            entity.Property(e => e.Diachi)
                .HasMaxLength(500)
                .HasColumnName("diachi");
            entity.Property(e => e.Tenrapphim)
                .HasMaxLength(500)
                .HasColumnName("tenrapphim");
        });

        modelBuilder.Entity<Theloai>(entity =>
        {
            entity.HasKey(e => e.Matheloai).HasName("theloai_pkey");

            entity.ToTable("theloai");

            entity.Property(e => e.Matheloai)
                .HasMaxLength(500)
                .HasColumnName("matheloai");
            entity.Property(e => e.Mota)
                .HasMaxLength(500)
                .HasColumnName("mota");
            entity.Property(e => e.Tentheloai)
                .HasMaxLength(500)
                .HasColumnName("tentheloai");
        });

        modelBuilder.Entity<Thongbao>(entity =>
        {
            entity.HasKey(e => e.Mathongbao).HasName("thongbao_pkey");

            entity.ToTable("thongbao");

            entity.Property(e => e.Mathongbao)
                .HasMaxLength(500)
                .HasColumnName("mathongbao");
            entity.Property(e => e.IdKhach).HasColumnName("id_khach");
            entity.Property(e => e.Madondatve)
                .HasMaxLength(500)
                .HasColumnName("madondatve");
            entity.Property(e => e.Maphim)
                .HasMaxLength(500)
                .HasColumnName("maphim");
            entity.Property(e => e.Ngaytao)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaytao");
            entity.Property(e => e.Noidung)
                .HasMaxLength(500)
                .HasColumnName("noidung");
            entity.Property(e => e.Thoidiemtb)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("thoidiemtb");
            entity.Property(e => e.Thoidiemxem)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("thoidiemxem");
            entity.Property(e => e.Tieude)
                .HasMaxLength(500)
                .HasColumnName("tieude");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(500)
                .HasColumnName("trangthai");

            entity.HasOne(d => d.IdKhachNavigation).WithMany(p => p.Thongbaos)
                .HasForeignKey(d => d.IdKhach)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("thongbao_id_khach_fkey");

            entity.HasOne(d => d.MadondatveNavigation).WithMany(p => p.Thongbaos)
                .HasForeignKey(d => d.Madondatve)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("thongbao_madondatve_fkey");

            entity.HasOne(d => d.MaphimNavigation).WithMany(p => p.Thongbaos)
                .HasForeignKey(d => d.Maphim)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("thongbao_maphim_fkey");
        });

        modelBuilder.Entity<Thongtintaikhoan>(entity =>
        {
            entity.HasKey(e => e.IdKhach).HasName("thongtintaikhoan_pkey");

            entity.ToTable("thongtintaikhoan");

            entity.HasIndex(e => e.Email, "thongtintaikhoan_email_key").IsUnique();

            entity.HasIndex(e => e.Mataikhoan, "thongtintaikhoan_mataikhoan_key").IsUnique();

            entity.HasIndex(e => e.Sdt, "thongtintaikhoan_sdt_key").IsUnique();

            entity.Property(e => e.IdKhach).HasColumnName("id_khach");
            entity.Property(e => e.Anhdaidien)
                .HasMaxLength(500)
                .HasColumnName("anhdaidien");
            entity.Property(e => e.Email)
                .HasMaxLength(500)
                .HasColumnName("email");
            entity.Property(e => e.Gioitinh).HasColumnName("gioitinh");
            entity.Property(e => e.HangThanhVien)
                .HasMaxLength(20)
                .HasDefaultValueSql("'BRONZE'::character varying")
                .HasColumnName("hang_thanh_vien");
            entity.Property(e => e.Hoten)
                .HasMaxLength(500)
                .HasColumnName("hoten");
            entity.Property(e => e.Mataikhoan)
                .HasMaxLength(500)
                .HasColumnName("mataikhoan");
            entity.Property(e => e.Matkhau)
                .HasMaxLength(500)
                .HasColumnName("matkhau");
            entity.Property(e => e.Ngaycapnhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaycapnhat");
            entity.Property(e => e.Ngaysinh)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaysinh");
            entity.Property(e => e.Ngaytao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("ngaytao");
            entity.Property(e => e.Sdt)
                .HasMaxLength(50)
                .HasColumnName("sdt");
            entity.Property(e => e.TongChiTieu).HasColumnName("tong_chi_tieu");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(20)
                .HasDefaultValueSql("'active'::character varying")
                .HasColumnName("trangthai");
        });

        modelBuilder.Entity<Thongtinthanhtoan>(entity =>
        {
            entity.HasKey(e => e.Mathanhtoan).HasName("thongtinthanhtoan_pkey");

            entity.ToTable("thongtinthanhtoan");

            entity.Property(e => e.Mathanhtoan)
                .HasMaxLength(500)
                .HasColumnName("mathanhtoan");
            entity.Property(e => e.Madondatve)
                .HasMaxLength(500)
                .HasColumnName("madondatve");
            entity.Property(e => e.Paymentgatewaytransactionid)
                .HasMaxLength(500)
                .HasColumnName("paymentgatewaytransactionid");
            entity.Property(e => e.Phuongthucthanhtoan)
                .HasMaxLength(500)
                .HasColumnName("phuongthucthanhtoan");
            entity.Property(e => e.Sotienthanhtoan).HasColumnName("sotienthanhtoan");
            entity.Property(e => e.Thoidiemthanhtoan)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("thoidiemthanhtoan");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(500)
                .HasColumnName("trangthai");

            entity.HasOne(d => d.MadondatveNavigation).WithMany(p => p.Thongtinthanhtoans)
                .HasForeignKey(d => d.Madondatve)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("thongtinthanhtoan_madondatve_fkey");
        });

        modelBuilder.Entity<Vexemphim>(entity =>
        {
            entity.HasKey(e => e.Mavexemphim).HasName("vexemphim_pkey");

            entity.ToTable("vexemphim");

            entity.Property(e => e.Mavexemphim)
                .HasMaxLength(500)
                .HasColumnName("mavexemphim");
            entity.Property(e => e.Giave).HasColumnName("giave");
            entity.Property(e => e.Madondatve)
                .HasMaxLength(500)
                .HasColumnName("madondatve");
            entity.Property(e => e.Maghe)
                .HasMaxLength(500)
                .HasColumnName("maghe");
            entity.Property(e => e.Malichchieu)
                .HasMaxLength(500)
                .HasColumnName("malichchieu");
            entity.Property(e => e.Qrcode)
                .HasMaxLength(1000)
                .HasColumnName("qrcode");
            entity.Property(e => e.Thoigianhethan)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("thoigianhethan");
            entity.Property(e => e.Thoigianphathanh)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("thoigianphathanh");
            entity.Property(e => e.Trangthai)
                .HasMaxLength(500)
                .HasColumnName("trangthai");

            entity.HasOne(d => d.MadondatveNavigation).WithMany(p => p.Vexemphims)
                .HasForeignKey(d => d.Madondatve)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vexemphim_madondatve_fkey");

            entity.HasOne(d => d.MagheNavigation).WithMany(p => p.Vexemphims)
                .HasForeignKey(d => d.Maghe)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vexemphim_maghe_fkey");

            entity.HasOne(d => d.MalichchieuNavigation).WithMany(p => p.Vexemphims)
                .HasForeignKey(d => d.Malichchieu)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vexemphim_malichchieu_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
