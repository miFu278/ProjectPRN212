using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace webAPI.Models;

public partial class BatterySwapContext : DbContext
{
    public BatterySwapContext()
    {
    }

    public BatterySwapContext(DbContextOptions<BatterySwapContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Battery> Battery { get; set; }
    public virtual DbSet<BatterySlot> BatterySlot { get; set; }
    public virtual DbSet<Battery_Type> Battery_Type { get; set; }
    public virtual DbSet<Booking> Booking { get; set; }
    public virtual DbSet<Charging_Station> Charging_Station { get; set; }
    public virtual DbSet<Comment> Comment { get; set; }
    public virtual DbSet<Dispatch_Log> Dispatch_Log { get; set; }
    public virtual DbSet<DriverPackage> DriverPackage { get; set; }
    public virtual DbSet<Package> Package { get; set; }
    public virtual DbSet<Password_Reset> Password_Reset { get; set; }
    public virtual DbSet<PaymentTransaction> PaymentTransaction { get; set; }
    public virtual DbSet<Station> Station { get; set; }
    public virtual DbSet<SwapTransaction> SwapTransaction { get; set; }
    public virtual DbSet<Users> Users { get; set; }
    public virtual DbSet<Vehicle> Vehicle { get; set; }
    public virtual DbSet<Vehicle_Model> Vehicle_Model { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // CHỈ dùng khi chưa được DI cấu hình (dev fallback).
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;Database=BatterySwapDBVer2;User Id=sa;Password=12345;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Battery>(entity =>
        {
            entity.HasKey(e => e.Battery_ID).HasName("PK__Battery__C1F93FD66640DEF3");

            entity.HasOne(d => d.Type).WithMany(p => p.Battery)
                .HasForeignKey(d => d.Type_ID)
                .HasConstraintName("FK_Battery_Type");
        });

        modelBuilder.Entity<BatterySlot>(entity =>
        {
            entity.HasKey(e => e.Slot_ID).HasName("PK__BatteryS__1AE2AAAEFE4D93C4");

            entity.HasIndex(e => new { e.ChargingStation_ID, e.Slot_Code }, "UQ_BatterySlot_ChargingStation_Slot")
                .IsUnique()
                .HasFilter("([Slot_Code] IS NOT NULL)");

            entity.Property(e => e.Last_Update).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Battery).WithMany(p => p.BatterySlot)
                .HasForeignKey(d => d.Battery_ID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BatterySlot_Battery");

            entity.HasOne(d => d.ChargingStation).WithMany(p => p.BatterySlot)
                .HasForeignKey(d => d.ChargingStation_ID)
                .HasConstraintName("FK_BatterySlot_ChargingStation");
        });

        modelBuilder.Entity<Battery_Type>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Battery___3214EC27F87D4ABC");
            entity.Property(e => e.ID).ValueGeneratedNever();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Booking_ID).HasName("PK__Booking__35ABFDE0C3CB7E04");

            entity.HasOne(d => d.ChargingStation).WithMany(p => p.Booking)
                .HasForeignKey(d => d.ChargingStation_ID)
                .HasConstraintName("FK_Booking_ChargingStation");

            entity.HasOne(d => d.Package).WithMany(p => p.Booking)
                .HasForeignKey(d => d.Package_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__Package__3A81B327");

            entity.HasOne(d => d.Slot).WithMany(p => p.Booking)
                .HasForeignKey(d => d.Slot_ID)
                .HasConstraintName("FK__Booking__Slot_ID__1EA48E88");

            entity.HasOne(d => d.Station).WithMany(p => p.Booking)
                .HasForeignKey(d => d.Station_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__Station__3C69FB99");

            entity.HasOne(d => d.User).WithMany(p => p.Booking)
                .HasForeignKey(d => d.User_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__User_ID__3B75D760");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.Booking)
                .HasForeignKey(d => d.Vehicle_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Booking__Vehicle__398D8EEE");
        });

        modelBuilder.Entity<Charging_Station>(entity =>
        {
            entity.HasKey(e => e.ChargingStation_ID).HasName("PK__Charging__8F51D5C40EF01B0C");

            entity.HasOne(d => d.Station).WithMany(p => p.Charging_Station)
                .HasForeignKey(d => d.Station_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ChargingStation_Station");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Comment_ID).HasName("PK__Comment__99FC143BAE085F6F");

            entity.HasOne(d => d.Swap).WithMany(p => p.Comment)
                .HasForeignKey(d => d.Swap_ID)
                .HasConstraintName("FK_Comment_Swap");

            entity.HasOne(d => d.User).WithMany(p => p.Comment)
                .HasForeignKey(d => d.User_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Comment__User_ID__3F466844");
        });

        modelBuilder.Entity<Dispatch_Log>(entity =>
        {
            entity.Property(e => e.Request_Time).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.BatteryType_Request).WithMany(p => p.Dispatch_Log)
                .HasForeignKey(d => d.BatteryType_Request_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dispatch_BatteryType_Request");

            entity.HasOne(d => d.Station_Request).WithMany(p => p.Dispatch_Log)
                .HasForeignKey(d => d.Station_Request_ID)
                .HasConstraintName("FK_Dispatch_Station_Request");
        });

        modelBuilder.Entity<DriverPackage>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__DriverPa__3214EC2700AEECB8");

            entity.HasOne(d => d.Package).WithMany(p => p.DriverPackage)
                .HasForeignKey(d => d.Package_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DriverPac__Packa__35BCFE0A");

            entity.HasOne(d => d.User).WithMany(p => p.DriverPackage)
                .HasForeignKey(d => d.User_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DriverPac__User___34C8D9D1");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.Package_ID).HasName("PK__Package__B7FCB94A36BE237E");

            entity.Property(e => e.MaxSoH).HasDefaultValue(100);
            entity.Property(e => e.Required_SoH).HasDefaultValue(0m);
            entity.Property(e => e.Status).HasDefaultValue("active");
        });

        modelBuilder.Entity<Password_Reset>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Password__3214EC27510A570D");

            entity.Property(e => e.Created_At).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Is_Used).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Password_Reset)
                .HasForeignKey(d => d.User_ID)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PasswordReset_Users");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__PaymentT__3214EC2732925836");

            entity.Property(e => e.Transaction_Time).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Package).WithMany(p => p.PaymentTransaction)
                .HasForeignKey(d => d.Package_ID)
                .HasConstraintName("FK__PaymentTr__Packa__46E78A0C");

            entity.HasOne(d => d.Station).WithMany(p => p.PaymentTransaction)
                .HasForeignKey(d => d.Station_ID)
                .HasConstraintName("FK__PaymentTr__Stati__45F365D3");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransaction)
                .HasForeignKey(d => d.User_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaymentTr__User___44FF419A");
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.HasKey(e => e.Station_ID).HasName("PK__Station__55F200EEBBB1B51A");

            entity.Property(e => e.Address).UseCollation("Vietnamese_100_CI_AI_SC");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).UseCollation("Vietnamese_100_CI_AI_SC");
        });

        modelBuilder.Entity<SwapTransaction>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__SwapTran__3214EC27FDCCE2D2");

            entity.Property(e => e.Swap_Time).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Booking).WithMany(p => p.SwapTransaction)
                .HasForeignKey(d => d.Booking_ID)
                .HasConstraintName("FK_SwapTransaction_Booking");

            entity.HasOne(d => d.ChargingStation).WithMany(p => p.SwapTransaction)
                .HasForeignKey(d => d.ChargingStation_ID)
                .HasConstraintName("FK_Swap_ChargingStation");

            entity.HasOne(d => d.Driver).WithMany(p => p.SwapTransactionDriver)
                .HasForeignKey(d => d.Driver_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SwapTrans__Drive__4BAC3F29");

            entity.HasOne(d => d.Payment).WithMany(p => p.SwapTransaction)
                .HasForeignKey(d => d.Payment_ID)
                .HasConstraintName("FK__SwapTrans__Payme__5070F446");

            entity.HasOne(d => d.Staff).WithMany(p => p.SwapTransactionStaff)
                .HasForeignKey(d => d.Staff_ID)
                .HasConstraintName("FK__SwapTrans__Staff__4CA06362");

            entity.HasOne(d => d.Station).WithMany(p => p.SwapTransaction)
                .HasForeignKey(d => d.Station_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SwapTrans__Stati__4D94879B");
        });

        modelBuilder.Entity<Users>(entity =>
        {
            entity.HasKey(e => e.ID).HasName("PK__Users__3214EC27EE7D6222");

            entity.Property(e => e.FullName).UseCollation("Vietnamese_100_CI_AI");
            entity.Property(e => e.Status).HasDefaultValue("Active");

            entity.HasOne(d => d.Station).WithMany(p => p.Users)
                .HasForeignKey(d => d.Station_ID)
                .HasConstraintName("FK__Users__Station_I__2A4B4B5E");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Vehicle_ID).HasName("PK__Vehicle__CE6D7CB5F4D7F5E0");

            entity.HasOne(d => d.Model).WithMany(p => p.Vehicle)
                .HasForeignKey(d => d.Model_ID)
                .HasConstraintName("FK_Vehicle_Model");

            entity.HasOne(d => d.User).WithMany(p => p.Vehicle)
                .HasForeignKey(d => d.User_ID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Vehicle__User_ID__2D27B809");
        });

        modelBuilder.Entity<Vehicle_Model>(entity =>
        {
            entity.HasKey(e => e.Model_ID).HasName("PK__Vehicle___1E82D1D3F79F7837");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}