﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sidergin_website.Data;

#nullable disable

namespace Sidergin_website.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250407120320_FixMigration")]
    partial class FixMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Faq", b =>
                {
                    b.Property<int>("FaqId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("faq_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("FaqId"));

                    b.Property<string>("Answer")
                        .IsRequired()
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)")
                        .HasColumnName("answer");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("Question")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)")
                        .HasColumnName("question");

                    b.HasKey("FaqId")
                        .HasName("PK__fqa__2FAB6E125DC366C2");

                    b.ToTable("faqs", (string)null);
                });

            modelBuilder.Entity("Sidergin_website.Models.Contact", b =>
                {
                    b.Property<int>("ContactId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("contact_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ContactId"));

                    b.Property<string>("Address")
                        .HasMaxLength(500)
                        .IsUnicode(false)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("address");

                    b.Property<DateTime?>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("email");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<string>("Zalo")
                        .HasMaxLength(50)
                        .IsUnicode(false)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("zalo");

                    b.HasKey("ContactId")
                        .HasName("PK__contacts__024E7A860F56AA40");

                    b.HasIndex(new[] { "Zalo" }, "UQ__contacts__97F75F7B13FEB32B")
                        .IsUnique()
                        .HasFilter("[zalo] IS NOT NULL");

                    b.HasIndex(new[] { "Email" }, "UQ__contacts__AB6E61648A508ACA")
                        .IsUnique();

                    b.HasIndex(new[] { "Phone" }, "UQ__contacts__B43B145F15C80E58")
                        .IsUnique();

                    b.ToTable("contacts", (string)null);
                });

            modelBuilder.Entity("Sidergin_website.Models.Order", b =>
                {
                    b.Property<int>("OrderId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("order_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("OrderId"));

                    b.Property<decimal>("CurrentPrice")
                        .HasColumnType("decimal(10, 2)")
                        .HasColumnName("current_price");

                    b.Property<DateTime?>("DeliveryDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("delivery_date")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("Notes")
                        .HasMaxLength(500)
                        .IsUnicode(false)
                        .HasColumnType("varchar(500)")
                        .HasColumnName("notes");

                    b.Property<DateTime?>("OrderDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("order_date")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("OrderStatus")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasDefaultValue("pending_payment")
                        .HasColumnName("order_status");

                    b.Property<string>("PaymentMethod")
                        .IsRequired()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("payment_method");

                    b.Property<string>("PaymentStatus")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasDefaultValue("pending")
                        .HasColumnName("payment_status");

                    b.Property<int>("Quantity")
                        .HasColumnType("int")
                        .HasColumnName("quantity");

                    b.Property<decimal?>("TotalAmount")
                        .HasColumnType("decimal(10, 2)")
                        .HasColumnName("total_amount");

                    b.Property<int>("UserId")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.Property<string>("VnpayTransactionId")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("vnpay_transaction_id");

                    b.HasKey("OrderId")
                        .HasName("PK__orders__465962295E83D09E");

                    b.HasIndex("UserId");

                    b.ToTable("orders", (string)null);
                });

            modelBuilder.Entity("Sidergin_website.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"));

                    b.Property<string>("Address")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)")
                        .HasColumnName("address");

                    b.Property<DateTime?>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("(getdate())");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("email");

                    b.Property<string>("Name")
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)")
                        .HasDefaultValue("customer")
                        .HasColumnName("name");

                    b.Property<string>("Password")
                        .HasMaxLength(255)
                        .IsUnicode(false)
                        .HasColumnType("varchar(255)")
                        .HasColumnName("password");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .IsUnicode(false)
                        .HasColumnType("varchar(20)")
                        .HasColumnName("phone");

                    b.Property<DateTime?>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("(getdate())");

                    b.HasKey("UserId")
                        .HasName("PK__users__B9BE370F56316043");

                    b.HasIndex(new[] { "Email" }, "UQ__users__AB6E6164FB2EF0E0")
                        .IsUnique();

                    b.HasIndex(new[] { "Phone" }, "UQ__users__B43B145F73D87F91")
                        .IsUnique()
                        .HasFilter("[phone] IS NOT NULL");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("Sidergin_website.Models.Order", b =>
                {
                    b.HasOne("Sidergin_website.Models.User", "User")
                        .WithMany("Orders")
                        .HasForeignKey("UserId")
                        .IsRequired()
                        .HasConstraintName("FK__orders__user_id__4316F928");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Sidergin_website.Models.User", b =>
                {
                    b.Navigation("Orders");
                });
#pragma warning restore 612, 618
        }
    }
}
