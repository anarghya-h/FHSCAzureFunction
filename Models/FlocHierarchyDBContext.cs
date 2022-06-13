using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FHSCAzureFunction.Models
{
    public class FlocHierarchyDBContext : DbContext
    {
        public FlocHierarchyDBContext()
        {
        }

        public FlocHierarchyDBContext(DbContextOptions<FlocHierarchyDBContext> options)
            : base(options)
        {
        }
        
        //Tables in the Database
        public DbSet<EquipmentDataFromGsap> EQUIPMENT_DATA_FROM_GSAP { get; set; }
        public DbSet<EquipmentDetails> EQUIPMENT_DETAILS { get; set; }
        public DbSet<Floc1> FLOC_1_DETAILS { get; set; }
        public DbSet<Floc2> FLOC_2_DETAILS { get; set; }
        public DbSet<Floc3> FLOC_3_DETAILS { get; set; }
        public DbSet<Floc4> FLOC_4_DETAILS { get; set; }
        public DbSet<GsaporiginalData> GSAP_ORIGINAL_DATA { get; set; }
        public DbSet<JobDetails> JOB_DETAILS { get; set; }
        public DbSet<CsvColMapper> CSV_COL_MAPPER { get; set; }
        public DbSet<Datacharts> JOB_SUMMARY { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GsaporiginalData>()
                .HasKey(c => new { c.JobId, c.FunctionalLocation });

            modelBuilder.Entity<EquipmentDataFromGsap>()
                .HasKey(c => new { c.JobId, c.Equipment });

            modelBuilder.Entity<EquipmentDetails>()
                .HasKey(c => new { c.JobId, c.Equipment });

            modelBuilder.Entity<Floc1>()
                .HasKey(c => new { c.JobId, c.TerminalCode });

            modelBuilder.Entity<Floc2>()
                .HasKey(c => new { c.JobId, c.FlocLevel2Name });

            modelBuilder.Entity<Floc3>()
                .HasKey(c => new { c.JobId, c.FlocLevel3Name });

            modelBuilder.Entity<Floc4>()
                .HasKey(c => new { c.JobId, c.FunctionalLocation });           

        }
    }
}
