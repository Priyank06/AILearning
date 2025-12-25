using PoC1_LegacyAnalyzer_Web.Models.GroundTruth;
using PoC1_LegacyAnalyzer_Web.Services.GroundTruth;

namespace PoC1_LegacyAnalyzer_Web.SampleData
{
    /// <summary>
    /// Sample ground truth dataset for legacy C# code analysis
    /// </summary>
    public static class LegacyCodeBenchmark
    {
        /// <summary>
        /// Create a sample benchmark dataset with known legacy code issues
        /// </summary>
        public static GroundTruthDataset CreateSampleDataset()
        {
            var builder = new GroundTruthDatasetBuilder(
                "Legacy C# Benchmark v1.0",
                "Benchmark dataset containing common legacy .NET Framework issues for validation");

            // Sample File 1: Legacy Data Access Layer with SQL Injection
            var file1Content = @"using System;
using System.Data;
using System.Data.SqlClient;

namespace LegacyApp.DataAccess
{
    public class UserRepository
    {
        private string connectionString = ""Server=localhost;Database=MyDB;Trusted_Connection=True;"";

        // ISSUE: SQL Injection vulnerability
        public DataTable GetUserByUsername(string username)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string sql = ""SELECT * FROM Users WHERE Username = '"" + username + ""'"";  // Line 14
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // ISSUE: Using legacy DataSet (performance + memory inefficiency)
        public DataSet GetAllUsers()  // Line 25
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(""SELECT * FROM Users"", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                return ds;
            }
        }

        // ISSUE: No async/await (blocking I/O)
        public DataTable GetActiveUsers()  // Line 38
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();  // Blocking operation
                SqlCommand cmd = new SqlCommand(""SELECT * FROM Users WHERE IsActive = 1"", conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }
    }
}";

            builder.AddFile("UserRepository.cs", "CSharp", file1Content, "DataAccess/UserRepository.cs")
                .AddSecurityIssue(
                    "UserRepository.cs",
                    "SQL Injection",
                    "SQL query constructed using string concatenation with user input",
                    "CRITICAL",
                    "UserRepository.GetUserByUsername",
                    14,
                    "string sql = \"SELECT * FROM Users WHERE Username = '\" + username + \"'\";"
                )
                .AddPerformanceIssue(
                    "UserRepository.cs",
                    "Legacy Data Access",
                    "Using DataSet instead of modern ORM (Entity Framework) - causes memory overhead",
                    "MEDIUM",
                    "UserRepository.GetAllUsers",
                    25,
                    "DataSet ds = new DataSet();"
                )
                .AddPerformanceIssue(
                    "UserRepository.cs",
                    "Synchronous I/O",
                    "Blocking database operation without async/await - hurts scalability",
                    "HIGH",
                    "UserRepository.GetActiveUsers",
                    38,
                    "conn.Open();  // Blocking operation"
                );

            // Sample File 2: Legacy ASP.NET Web Forms with global state
            var file2Content = @"using System;
using System.Web;
using System.Web.UI;

namespace LegacyApp.Web
{
    public partial class UserProfile : Page
    {
        // ISSUE: Using HttpContext.Current (global state anti-pattern)
        protected void Page_Load(object sender, EventArgs e)
        {
            var userId = HttpContext.Current.Session[""UserId""];  // Line 12
            if (userId == null)
            {
                Response.Redirect(""Login.aspx"");
            }

            LoadUserData(userId.ToString());
        }

        // ISSUE: Using Session state (global state)
        private void LoadUserData(string userId)
        {
            HttpContext.Current.Session[""CurrentUser""] = userId;  // Line 23
            // ... more code
        }

        // ISSUE: ViewState usage (performance impact)
        protected void SaveButton_Click(object sender, EventArgs e)
        {
            ViewState[""UserModified""] = true;  // Line 30
            // ... save logic
        }
    }
}";

            builder.AddFile("UserProfile.aspx.cs", "CSharp", file2Content, "Web/UserProfile.aspx.cs")
                .AddArchitectureIssue(
                    "UserProfile.aspx.cs",
                    "Global State",
                    "Using HttpContext.Current creates tight coupling and makes testing difficult",
                    "HIGH",
                    "UserProfile.Page_Load",
                    12,
                    "var userId = HttpContext.Current.Session[\"UserId\"];"
                )
                .AddArchitectureIssue(
                    "UserProfile.aspx.cs",
                    "Global State",
                    "Direct Session state manipulation violates dependency injection principles",
                    "MEDIUM",
                    "UserProfile.LoadUserData",
                    23,
                    "HttpContext.Current.Session[\"CurrentUser\"] = userId;"
                )
                .AddPerformanceIssue(
                    "UserProfile.aspx.cs",
                    "ViewState Overhead",
                    "ViewState increases page size and network transfer time",
                    "MEDIUM",
                    "UserProfile.SaveButton_Click",
                    30,
                    "ViewState[\"UserModified\"] = true;"
                );

            // Sample File 3: God Object anti-pattern
            var file3Content = @"using System;
using System.Collections.Generic;

namespace LegacyApp.Business
{
    // ISSUE: God Object - too many responsibilities
    public class UserManager  // Line 7
    {
        // Authentication
        public bool AuthenticateUser(string username, string password) { return true; }
        public void LogoutUser(int userId) { }

        // Authorization
        public bool HasPermission(int userId, string permission) { return true; }
        public void GrantPermission(int userId, string permission) { }

        // User CRUD
        public void CreateUser(string username) { }
        public void UpdateUser(int userId, string name) { }
        public void DeleteUser(int userId) { }

        // Email notifications
        public void SendWelcomeEmail(int userId) { }
        public void SendPasswordResetEmail(string email) { }

        // Reporting
        public List<string> GetUserActivityReport(int userId) { return new List<string>(); }
        public void ExportUsersToCSV(string filePath) { }

        // Password management
        public void ResetPassword(int userId) { }
        public bool ValidatePassword(string password) { return true; }

        // Session management
        public void CreateSession(int userId) { }
        public void InvalidateSession(string sessionId) { }
    }
}";

            builder.AddFile("UserManager.cs", "CSharp", file3Content, "Business/UserManager.cs")
                .AddArchitectureIssue(
                    "UserManager.cs",
                    "SOLID Violations",
                    "God Object with too many responsibilities - violates Single Responsibility Principle",
                    "HIGH",
                    "UserManager",
                    7,
                    "public class UserManager"
                );

            // Sample File 4: Hardcoded credentials
            var file4Content = @"using System;
using System.Net.Mail;

namespace LegacyApp.Services
{
    public class EmailService
    {
        // ISSUE: Hardcoded credentials (security vulnerability)
        private string smtpServer = ""smtp.company.com"";
        private string smtpUsername = ""admin@company.com"";  // Line 11
        private string smtpPassword = ""P@ssw0rd123"";  // Line 12 - CRITICAL

        public void SendEmail(string to, string subject, string body)
        {
            using (SmtpClient client = new SmtpClient(smtpServer))
            {
                client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                MailMessage message = new MailMessage(""noreply@company.com"", to, subject, body);
                client.Send(message);  // Synchronous send
            }
        }
    }
}";

            builder.AddFile("EmailService.cs", "CSharp", file4Content, "Services/EmailService.cs")
                .AddSecurityIssue(
                    "EmailService.cs",
                    "Hardcoded Credentials",
                    "SMTP password hardcoded in source code - major security risk",
                    "CRITICAL",
                    "EmailService",
                    12,
                    "private string smtpPassword = \"P@ssw0rd123\";"
                );

            // Sample File 5: No exception handling
            var file5Content = @"using System;
using System.IO;

namespace LegacyApp.Utilities
{
    public class FileProcessor
    {
        // ISSUE: No exception handling - application will crash on file errors
        public string ReadConfigFile(string fileName)  // Line 9
        {
            string content = File.ReadAllText(fileName);  // Can throw FileNotFoundException
            return content;
        }

        // ISSUE: Resource leak - File stream not properly disposed
        public void WriteLogFile(string message)  // Line 16
        {
            FileStream fs = new FileStream(""app.log"", FileMode.Append);
            StreamWriter writer = new StreamWriter(fs);
            writer.WriteLine(message);
            // Missing: writer.Dispose() and fs.Dispose()
        }
    }
}";

            builder.AddFile("FileProcessor.cs", "CSharp", file5Content, "Utilities/FileProcessor.cs")
                .AddSecurityIssue(
                    "FileProcessor.cs",
                    "Missing Error Handling",
                    "No exception handling can lead to information disclosure through stack traces",
                    "MEDIUM",
                    "FileProcessor.ReadConfigFile",
                    9,
                    "string content = File.ReadAllText(fileName);"
                )
                .AddPerformanceIssue(
                    "FileProcessor.cs",
                    "Resource Leaks",
                    "File streams not properly disposed - can cause file handle exhaustion",
                    "HIGH",
                    "FileProcessor.WriteLogFile",
                    16,
                    "FileStream fs = new FileStream(\"app.log\", FileMode.Append);"
                );

            return builder
                .WithTags("legacy", "dotnet-framework", "security", "performance", "architecture")
                .Build();
        }
    }
}
