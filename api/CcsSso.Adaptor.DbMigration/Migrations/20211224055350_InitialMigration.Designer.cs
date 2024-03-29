﻿// <auto-generated />
using System;
using CcsSso.Adaptor.DbPersistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CcsSso.Adaptor.DbMigration.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20211224055350_InitialMigration")]
    partial class InitialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.10")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConclaveAttributeMapping", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("AdapterConsumerEntityAttributeId")
                        .HasColumnType("integer");

                    b.Property<int>("ConclaveEntityAttributeId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("AdapterConsumerEntityAttributeId");

                    b.HasIndex("ConclaveEntityAttributeId");

                    b.ToTable("AdapterConclaveAttributeMapping");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ClientId")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ClientId")
                        .IsUnique();

                    b.ToTable("AdapterConsumer");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("AdapterConsumerId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AdapterConsumerId");

                    b.HasIndex("Name", "AdapterConsumerId")
                        .IsUnique();

                    b.ToTable("AdapterConsumerEntity");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntityAttribute", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("AdapterConsumerEntityId")
                        .HasColumnType("integer");

                    b.Property<string>("AttributeName")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("AdapterConsumerEntityId");

                    b.ToTable("AdapterConsumerEntityAttribute");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterFormat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("FomatFileType")
                        .HasColumnType("text");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.ToTable("AdapterFormat");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterSubscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("AdapterConsumerId")
                        .HasColumnType("integer");

                    b.Property<int>("AdapterFormatId")
                        .HasColumnType("integer");

                    b.Property<int>("ConclaveEntityId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("SubscriptionType")
                        .HasColumnType("text");

                    b.Property<string>("SubscriptionUrl")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AdapterConsumerId");

                    b.HasIndex("AdapterFormatId");

                    b.HasIndex("ConclaveEntityId");

                    b.ToTable("AdapterSubscription");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("ConclaveEntity");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntityAttribute", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("AttributeName")
                        .HasColumnType("text");

                    b.Property<int>("ConclaveEntityId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdatedOnUtc")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("ConclaveEntityId");

                    b.ToTable("ConclaveEntityAttribute");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConclaveAttributeMapping", b =>
                {
                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntityAttribute", "AdapterConsumerEntityAttribute")
                        .WithMany("AdapterConclaveAttributeMappings")
                        .HasForeignKey("AdapterConsumerEntityAttributeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntityAttribute", "ConclaveEntityAttribute")
                        .WithMany("AdapterConclaveAttributeMappings")
                        .HasForeignKey("ConclaveEntityAttributeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdapterConsumerEntityAttribute");

                    b.Navigation("ConclaveEntityAttribute");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntity", b =>
                {
                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumer", "AdapterConsumer")
                        .WithMany("AdapterConsumerEntities")
                        .HasForeignKey("AdapterConsumerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdapterConsumer");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntityAttribute", b =>
                {
                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntity", "AdapterConsumerEntity")
                        .WithMany("AdapterConsumerEntityAttributes")
                        .HasForeignKey("AdapterConsumerEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdapterConsumerEntity");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterSubscription", b =>
                {
                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumer", "AdapterConsumer")
                        .WithMany("AdapterSubscriptions")
                        .HasForeignKey("AdapterConsumerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.AdapterFormat", "AdapterFormat")
                        .WithMany("AdapterSubscriptions")
                        .HasForeignKey("AdapterFormatId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntity", "ConclaveEntity")
                        .WithMany("AdapterSubscriptions")
                        .HasForeignKey("ConclaveEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AdapterConsumer");

                    b.Navigation("AdapterFormat");

                    b.Navigation("ConclaveEntity");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntityAttribute", b =>
                {
                    b.HasOne("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntity", "ConclaveEntity")
                        .WithMany("ConclaveEntityAttributes")
                        .HasForeignKey("ConclaveEntityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ConclaveEntity");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumer", b =>
                {
                    b.Navigation("AdapterConsumerEntities");

                    b.Navigation("AdapterSubscriptions");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntity", b =>
                {
                    b.Navigation("AdapterConsumerEntityAttributes");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterConsumerEntityAttribute", b =>
                {
                    b.Navigation("AdapterConclaveAttributeMappings");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.AdapterFormat", b =>
                {
                    b.Navigation("AdapterSubscriptions");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntity", b =>
                {
                    b.Navigation("AdapterSubscriptions");

                    b.Navigation("ConclaveEntityAttributes");
                });

            modelBuilder.Entity("CcsSso.Adaptor.DbDomain.Entity.ConclaveEntityAttribute", b =>
                {
                    b.Navigation("AdapterConclaveAttributeMappings");
                });
#pragma warning restore 612, 618
        }
    }
}
