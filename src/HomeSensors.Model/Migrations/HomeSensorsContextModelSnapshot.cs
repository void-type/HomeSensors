﻿// <auto-generated />
using System;
using HomeSensors.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace HomeSensors.Model.Migrations
{
    [DbContext(typeof(HomeSensorsContext))]
    partial class HomeSensorsContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("HomeSensors.Model.TemperatureDevice", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<long?>("CurrentTemperatureLocationId")
                        .HasColumnType("bigint");

                    b.Property<string>("DeviceChannel")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DeviceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("DeviceModel")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsRetired")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.HasIndex("CurrentTemperatureLocationId");

                    b.ToTable("TemperatureDevices");
                });

            modelBuilder.Entity("HomeSensors.Model.TemperatureLocation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<double?>("MaxTemperatureLimit")
                        .HasColumnType("float");

                    b.Property<double?>("MinTemperatureLimit")
                        .HasColumnType("float");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("TemperatureLocations");
                });

            modelBuilder.Entity("HomeSensors.Model.TemperatureReading", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"), 1L, 1);

                    b.Property<double?>("DeviceBatteryLevel")
                        .HasColumnType("float");

                    b.Property<int?>("DeviceStatus")
                        .HasColumnType("int");

                    b.Property<double?>("Humidity")
                        .HasColumnType("float");

                    b.Property<double?>("TemperatureCelsius")
                        .HasColumnType("float");

                    b.Property<long>("TemperatureDeviceId")
                        .HasColumnType("bigint");

                    b.Property<long?>("TemperatureLocationId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("Time")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("TemperatureDeviceId");

                    b.HasIndex("TemperatureLocationId");

                    b.HasIndex("Time");

                    b.ToTable("TemperatureReadings");
                });

            modelBuilder.Entity("HomeSensors.Model.TemperatureDevice", b =>
                {
                    b.HasOne("HomeSensors.Model.TemperatureLocation", "CurrentTemperatureLocation")
                        .WithMany()
                        .HasForeignKey("CurrentTemperatureLocationId");

                    b.Navigation("CurrentTemperatureLocation");
                });

            modelBuilder.Entity("HomeSensors.Model.TemperatureReading", b =>
                {
                    b.HasOne("HomeSensors.Model.TemperatureDevice", "TemperatureDevice")
                        .WithMany("TemperatureReadings")
                        .HasForeignKey("TemperatureDeviceId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("HomeSensors.Model.TemperatureLocation", "TemperatureLocation")
                        .WithMany("TemperatureReadings")
                        .HasForeignKey("TemperatureLocationId");

                    b.Navigation("TemperatureDevice");

                    b.Navigation("TemperatureLocation");
                });

            modelBuilder.Entity("HomeSensors.Model.TemperatureDevice", b =>
                {
                    b.Navigation("TemperatureReadings");
                });

            modelBuilder.Entity("HomeSensors.Model.TemperatureLocation", b =>
                {
                    b.Navigation("TemperatureReadings");
                });
#pragma warning restore 612, 618
        }
    }
}