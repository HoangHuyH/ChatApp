using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ChatApp.Web.Data;
using ChatApp.Web.Models.Entities;

namespace ChatApp.Web.Services
{
    public class DatabaseInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DatabaseInitializer> _logger;
        private readonly IWebHostEnvironment _env;

        public DatabaseInitializer(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DatabaseInitializer> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _env = env;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Apply migrations
                await _context.Database.MigrateAsync();
                
                // Execute our custom SQL script to fix the Friendship table structure
                await ExecuteSqlScriptAsync();

                // Create test users if the database is empty
                if (!_userManager.Users.Any())
                {
                    _logger.LogInformation("Seeding test users");
                    
                    // Create test users
                    var users = new[]
                    {
                        new User 
                        { 
                            UserName = "john@example.com", 
                            Email = "john@example.com",
                            DisplayName = "John Doe",
                            CreatedAt = DateTime.UtcNow
                        },
                        new User 
                        { 
                            UserName = "jane@example.com", 
                            Email = "jane@example.com",
                            DisplayName = "Jane Smith",
                            CreatedAt = DateTime.UtcNow
                        },
                        new User 
                        { 
                            UserName = "bob@example.com", 
                            Email = "bob@example.com",
                            DisplayName = "Bob Johnson",
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    string defaultPassword = "Password123!";

                    foreach (var user in users)
                    {
                        await _userManager.CreateAsync(user, defaultPassword);
                    }

                    // Groups and other data is created in the SQL script
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Test users seeded successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }
        
        private async Task ExecuteSqlScriptAsync()
        {
            try
            {
                string sqlFilePath = Path.Combine(_env.ContentRootPath, "Data", "InitializeDatabase.sql");
                
                if (File.Exists(sqlFilePath))
                {
                    string sqlScript = await File.ReadAllTextAsync(sqlFilePath);
                    
                    // Split script into individual commands by semicolon
                    var commands = sqlScript
                        .Split(';')
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Select(c => c.Trim() + ";")
                        .ToList();
                    
                    var connection = _context.Database.GetDbConnection() as SqliteConnection;
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }
                    
                    foreach (var command in commands)
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = command;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    
                    _logger.LogInformation("Custom SQL script executed successfully");
                }
                else
                {
                    _logger.LogWarning("SQL script not found at path: {Path}", sqlFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL script");
            }
        }
    }
}