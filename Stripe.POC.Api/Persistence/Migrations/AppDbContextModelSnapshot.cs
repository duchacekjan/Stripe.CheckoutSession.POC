﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using POC.Api.Persistence;

#nullable disable

namespace POC.Api.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.6");

            modelBuilder.Entity("POC.Api.Persistence.Entities.CheckoutSession", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClientSecret")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<long>("OrderId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SessionId")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OrderId")
                        .IsUnique();

                    b.ToTable("CheckoutSessions", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Event", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Events", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Order", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("BasketId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("BasketId")
                        .IsUnique();

                    b.ToTable("Orders", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.OrderItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("OrderId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.ToTable("OrderItems", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Payment", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<long>("OrderId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PaymentIntentId")
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<string>("SessionId")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("SessionId")
                        .IsUnique();

                    b.ToTable("Payments", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.PaymentHistory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("NewStatus")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OldStatus")
                        .HasColumnType("INTEGER");

                    b.Property<long>("PaymentId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("PaymentId");

                    b.ToTable("PaymentHistories", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Performance", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<uint>("DurationMinutes")
                        .HasColumnType("INTEGER");

                    b.Property<long>("EventId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("PerformanceDate")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EventId");

                    b.ToTable("Performances", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Price", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Amount")
                        .HasPrecision(18, 2)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Prices", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Seat", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<uint>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("OrderItemId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("PerformanceId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("PriceId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Row")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OrderItemId");

                    b.HasIndex("PerformanceId");

                    b.HasIndex("PriceId");

                    b.ToTable("Seats", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Voucher", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("InitialAmount")
                        .HasColumnType("TEXT");

                    b.Property<long>("SeatId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SeatId")
                        .IsUnique();

                    b.ToTable("Vouchers", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.VoucherHistory", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Amount")
                        .HasColumnType("TEXT");

                    b.Property<long>("OrderId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("VoucherId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("OrderId");

                    b.HasIndex("VoucherId");

                    b.ToTable("VoucherHistories", (string)null);
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.CheckoutSession", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.Order", "Order")
                        .WithOne("CheckoutSession")
                        .HasForeignKey("POC.Api.Persistence.Entities.CheckoutSession", "OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Order");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.OrderItem", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.Order", "Order")
                        .WithMany("OrderItems")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Order");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Payment", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.Order", "Order")
                        .WithMany("Payments")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Order");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.PaymentHistory", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.Payment", "Payment")
                        .WithMany("History")
                        .HasForeignKey("PaymentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Payment");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Performance", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.Event", "Event")
                        .WithMany("Performances")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Event");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Seat", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.OrderItem", "OrderItem")
                        .WithMany("Seats")
                        .HasForeignKey("OrderItemId");

                    b.HasOne("POC.Api.Persistence.Entities.Performance", "Performance")
                        .WithMany("Seats")
                        .HasForeignKey("PerformanceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("POC.Api.Persistence.Entities.Price", "Price")
                        .WithMany()
                        .HasForeignKey("PriceId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("OrderItem");

                    b.Navigation("Performance");

                    b.Navigation("Price");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Voucher", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.Seat", "Seat")
                        .WithOne()
                        .HasForeignKey("POC.Api.Persistence.Entities.Voucher", "SeatId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Seat");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.VoucherHistory", b =>
                {
                    b.HasOne("POC.Api.Persistence.Entities.Order", "Order")
                        .WithMany("Vouchers")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("POC.Api.Persistence.Entities.Voucher", "Voucher")
                        .WithMany("History")
                        .HasForeignKey("VoucherId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Order");

                    b.Navigation("Voucher");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Event", b =>
                {
                    b.Navigation("Performances");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Order", b =>
                {
                    b.Navigation("CheckoutSession")
                        .IsRequired();

                    b.Navigation("OrderItems");

                    b.Navigation("Payments");

                    b.Navigation("Vouchers");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.OrderItem", b =>
                {
                    b.Navigation("Seats");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Payment", b =>
                {
                    b.Navigation("History");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Performance", b =>
                {
                    b.Navigation("Seats");
                });

            modelBuilder.Entity("POC.Api.Persistence.Entities.Voucher", b =>
                {
                    b.Navigation("History");
                });
#pragma warning restore 612, 618
        }
    }
}
