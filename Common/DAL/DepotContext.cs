using System.Text.Json;
using Common.DAL.Models;
using Common.Services;
using Microsoft.EntityFrameworkCore;

namespace Common.DAL
{
    public class DepotContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Translation> Translations { get; set; }
        public DbSet<Setting> Settings { get; set; }

        private bool _isLoaded { get; set; }

        public const string UsersPath = @"Json\Users.json";
        public const string ToursPath = @"Json\Tours.json";
        public const string GroupsPath = @"Json\Groups.json";
        public const string TicketsPath = @"Json\Tickets.json";
        public const string TranslationsPath = @"Json\Translations.json";
        public const string SettingsPath = @"Json\Settings.json";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: "Depot");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(b => b.Id);
            modelBuilder.Entity<Ticket>().HasKey(b => b.Id);
            modelBuilder.Entity<Tour>().HasKey(b => b.Id);
            modelBuilder.Entity<Group>().HasKey(b => b.Id);
            modelBuilder.Entity<Translation>().HasKey(b => b.Id);
            modelBuilder.Entity<Setting>().HasKey(b => b.Id);
        }

        public async void LoadContext()
        {
            if(_isLoaded) return;

            LoadJson(Users, UsersPath);
            LoadJson(Tours, ToursPath);
            LoadJson(Groups, GroupsPath);
            LoadJson(Tickets, TicketsPath);
            LoadJson(Translations, TranslationsPath);
            LoadJson(Settings, SettingsPath);

            _isLoaded = true;

            await SaveChangesAsync();
        }

        /// <summary>
        /// Do not run this in production, this is for testing purposes only.
        /// </summary>
        public void Purge()
        {
            if (!_isLoaded) return;

            Users.RemoveRange(Users);
            Tours.RemoveRange(Tours);
            Groups.RemoveRange(Groups);
            Tickets.RemoveRange(Tickets);
            Translations.RemoveRange(Translations);
            Settings.RemoveRange(Settings);

            _isLoaded = false;
        }

        public override int SaveChanges()
        {
            int changes = base.SaveChanges();

            File.WriteAllText(UsersPath, JsonSerializer.Serialize(Users.ToList()));
            File.WriteAllText(TicketsPath, JsonSerializer.Serialize(Tickets.ToList()));
            File.WriteAllText(ToursPath, JsonSerializer.Serialize(Tours.ToList()));
            File.WriteAllText(GroupsPath, JsonSerializer.Serialize(Groups.ToList()));
            File.WriteAllText(TranslationsPath, JsonSerializer.Serialize(Translations.ToList()));
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings.ToList()));

            return changes;
        }

        private void LoadJson<T>(DbSet<T> dbSet, string jsonFile) where T : DbEntity
        {
            if (File.Exists(jsonFile))
            {
                var objs = JsonSerializer.Deserialize<List<T>>(File.ReadAllText(jsonFile));
                if (objs != null)
                {
                    dbSet.AddRange(objs);
                }
            }
        }
    }
}
