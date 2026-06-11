// File: Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using ClinicApp.Web.Models;

namespace ClinicApp.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Your tables
        public DbSet<Clinic> Clinics { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Treatment> Treatments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MaterialHistory> MaterialHistories { get; set; }
        public DbSet<TreatmentPlan> TreatmentPlans { get; set; }
        public DbSet<PatientTooth> PatientTeeth { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<PatientFile> PatientFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Patient → Files relationship
            modelBuilder.Entity<PatientFile>()
                .HasOne(f => f.Patient)
                .WithMany(p => p.Files)
                .HasForeignKey(f => f.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient → Teeth relationship
            modelBuilder.Entity<PatientTooth>()
                .HasOne(t => t.Patient)
                .WithMany(p => p.Teeth)
                .HasForeignKey(t => t.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Treatment → TreatmentPlan relationship
            modelBuilder.Entity<Treatment>()
                .HasOne(t => t.TreatmentPlan)
                .WithMany(p => p.Treatments)
                .HasForeignKey(t => t.TreatmentPlanId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique index for PatientTooth
            modelBuilder.Entity<PatientTooth>()
                .HasIndex(t => new { t.PatientId, t.ToothNumber })
                .IsUnique();

            // 🆕 إعدادات Appointment
            modelBuilder.Entity<Appointment>(entity =>
            {
                // علاقة مع Patient
                entity.HasOne(a => a.Patient)
                    .WithMany(p => p.Appointments)
                    .HasForeignKey(a => a.PatientId)
                    .OnDelete(DeleteBehavior.Restrict);

                // علاقة مع Doctor
                entity.HasOne(a => a.Doctor)
                    .WithMany()
                    .HasForeignKey(a => a.DoctorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // علاقة مع Clinic
                entity.HasOne(a => a.Clinic)
                    .WithMany(c => c.Appointments)
                    .HasForeignKey(a => a.ClinicId)
                    .OnDelete(DeleteBehavior.Cascade);

                // علاقة مع CreatedBy
                entity.HasOne(a => a.CreatedBy)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // علاقة مع UpdatedBy
                entity.HasOne(a => a.UpdatedBy)
                    .WithMany()
                    .HasForeignKey(a => a.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes للأداء
                entity.HasIndex(a => a.StartTime);
                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => new { a.DoctorId, a.StartTime });
            });

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Appointment)
                .WithMany()
                .HasForeignKey(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DoctorSchedule>(entity =>
            {
                entity.HasOne(s => s.Doctor)
                    .WithMany()
                    .HasForeignKey(s => s.DoctorId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.Clinic)
                    .WithMany()
                    .HasForeignKey(s => s.ClinicId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(s => new { s.DoctorId, s.DayOfWeek }).IsUnique();
            });
        }
    }
}