﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using UMS.Infrastructure.Persistence;

#nullable disable

namespace UMS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250612151752_AddRefreshTokens")]
    partial class AddRefreshTokens
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("UMS.Domain.Users.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("id");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at_utc");

                    b.Property<string>("DeviceId")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)")
                        .HasColumnName("device_id");

                    b.Property<DateTime>("ExpiresAtUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("expires_at_utc");

                    b.Property<DateTime?>("RevokedAtUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("revoked_at_utc");

                    b.Property<string>("Token")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)")
                        .HasColumnName("token");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("pk_refresh_tokens");

                    b.HasIndex("Token")
                        .IsUnique()
                        .HasDatabaseName("ix_refresh_tokens_token");

                    b.HasIndex("UserId")
                        .HasDatabaseName("ix_refresh_tokens_user_id");

                    b.ToTable("refresh_tokens");
                });

            modelBuilder.Entity("UMS.Domain.Users.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("id");

                    b.Property<string>("ActivationToken")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)")
                        .HasColumnName("activation_token");

                    b.Property<DateTime?>("ActivationTokenExpiryUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("activation_token_expiry_utc");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at_utc");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("created_by");

                    b.Property<DateTime?>("DeletedAtUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("deleted_at_utc");

                    b.Property<Guid?>("DeletedBy")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("deleted_by");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("email");

                    b.Property<string>("FirstName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("first_name");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeleted")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deleted");

                    b.Property<DateTime?>("LastLoginAtUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("last_login_at_utc");

                    b.Property<DateTime?>("LastModifiedAtUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("last_modified_at_utc");

                    b.Property<Guid?>("LastModifiedBy")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("last_modified_by");

                    b.Property<string>("LastName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("last_name");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)")
                        .HasColumnName("password_hash");

                    b.Property<string>("PasswordResetToken")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)")
                        .HasColumnName("password_reset_token");

                    b.Property<DateTime?>("PasswordResetTokenExpiryUtc")
                        .HasColumnType("datetime2")
                        .HasColumnName("password_reset_token_expiry_utc");

                    b.Property<string>("UserCode")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)")
                        .HasColumnName("user_code");

                    b.HasKey("Id")
                        .HasName("pk_users");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasDatabaseName("ix_users_email");

                    b.HasIndex("UserCode")
                        .IsUnique()
                        .HasDatabaseName("ix_users_user_code");

                    b.ToTable("users");
                });

            modelBuilder.Entity("UMS.Infrastructure.Persistence.Entities.EntitySequence", b =>
                {
                    b.Property<string>("EntityTypePrefix")
                        .HasMaxLength(4)
                        .HasColumnType("nvarchar(4)")
                        .HasColumnName("entity_type_prefix");

                    b.Property<DateTime>("SequenceDate")
                        .HasColumnType("date")
                        .HasColumnName("sequence_date");

                    b.Property<int>("LastValue")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0)
                        .HasColumnName("last_value");

                    b.HasKey("EntityTypePrefix", "SequenceDate")
                        .HasName("pk_entity_sequences");

                    b.ToTable("entity_sequences");
                });

            modelBuilder.Entity("UMS.Domain.Users.RefreshToken", b =>
                {
                    b.HasOne("UMS.Domain.Users.User", null)
                        .WithMany("RefreshTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_refresh_tokens__users_user_id");
                });

            modelBuilder.Entity("UMS.Domain.Users.User", b =>
                {
                    b.Navigation("RefreshTokens");
                });
#pragma warning restore 612, 618
        }
    }
}
