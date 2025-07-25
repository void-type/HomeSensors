﻿// <auto-generated />
using System;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace HomeSensors.Model.Migrations
{
    [DbContext(typeof(HomeSensorsContext))]
    [Migration("20250704225742_ThermostatAction")]
    partial class ThermostatAction
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("HomeSensors.Model.Data.Models.Category", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.HasIndex("Order");

                    b.ToTable("Category", (string)null);
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureDevice", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<bool>("IsRetired")
                        .HasColumnType("bit");

                    b.Property<string>("MqttTopic")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("TemperatureLocationId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("MqttTopic")
                        .IsUnique();

                    b.HasIndex("TemperatureLocationId");

                    b.ToTable("TemperatureDevice", (string)null);
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureLocation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long?>("CategoryId")
                        .HasColumnType("bigint");

                    b.Property<bool>("IsHidden")
                        .HasColumnType("bit");

                    b.Property<double?>("MaxTemperatureLimitCelsius")
                        .HasColumnType("float");

                    b.Property<double?>("MinTemperatureLimitCelsius")
                        .HasColumnType("float");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("TemperatureLocation", (string)null);
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureReading", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<double?>("DeviceBatteryLevel")
                        .HasColumnType("float");

                    b.Property<int?>("DeviceStatus")
                        .HasColumnType("int");

                    b.Property<double?>("Humidity")
                        .HasColumnType("float");

                    b.Property<bool>("IsSummary")
                        .HasColumnType("bit");

                    b.Property<double?>("TemperatureCelsius")
                        .HasColumnType("float");

                    b.Property<long>("TemperatureDeviceId")
                        .HasColumnType("bigint");

                    b.Property<long>("TemperatureLocationId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Time")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("TemperatureDeviceId");

                    b.HasIndex("Time")
                        .HasFilter("[IsSummary] = 0");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("Time"), new[] { "TemperatureLocationId", "TemperatureCelsius", "Humidity" });

                    b.HasIndex("TemperatureLocationId", "Time");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("TemperatureLocationId", "Time"), new[] { "TemperatureCelsius", "Humidity" });

                    b.HasIndex("IsSummary", "TemperatureDeviceId", "Time");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("IsSummary", "TemperatureDeviceId", "Time"), new[] { "TemperatureCelsius", "Humidity", "TemperatureLocationId" });

                    b.ToTable("TemperatureReading", (string)null);
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.ThermostatAction", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<string>("EntityId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTimeOffset?>("LastChanged")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("LastUpdated")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("State")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("EntityId", "LastChanged")
                        .IsUnique()
                        .HasFilter("[EntityId] IS NOT NULL AND [LastChanged] IS NOT NULL");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("EntityId", "LastChanged"), new[] { "LastUpdated", "State" });

                    b.ToTable("ThermostatAction", (string)null);
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureDevice", b =>
                {
                    b.HasOne("HomeSensors.Model.Data.Models.TemperatureLocation", "TemperatureLocation")
                        .WithMany()
                        .HasForeignKey("TemperatureLocationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TemperatureLocation");
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureLocation", b =>
                {
                    b.HasOne("HomeSensors.Model.Data.Models.Category", "Category")
                        .WithMany("TemperatureLocations")
                        .HasForeignKey("CategoryId");

                    b.Navigation("Category");
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureReading", b =>
                {
                    b.HasOne("HomeSensors.Model.Data.Models.TemperatureDevice", "TemperatureDevice")
                        .WithMany("TemperatureReadings")
                        .HasForeignKey("TemperatureDeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("HomeSensors.Model.Data.Models.TemperatureLocation", "TemperatureLocation")
                        .WithMany("TemperatureReadings")
                        .HasForeignKey("TemperatureLocationId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("TemperatureDevice");

                    b.Navigation("TemperatureLocation");
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.Category", b =>
                {
                    b.Navigation("TemperatureLocations");
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureDevice", b =>
                {
                    b.Navigation("TemperatureReadings");
                });

            modelBuilder.Entity("HomeSensors.Model.Data.Models.TemperatureLocation", b =>
                {
                    b.Navigation("TemperatureReadings");
                });
#pragma warning restore 612, 618
        }
    }
}
