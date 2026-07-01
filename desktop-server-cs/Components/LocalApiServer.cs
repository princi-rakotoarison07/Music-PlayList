using System.Text.Json;
using desktop_server_app.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace desktop_server_app.Components
{
    public class BlacklistRuleDto
    {
        public int Id { get; set; }
        public string RuleType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public static class LocalApiServer
    {
        private static readonly string JsonPath = Path.Combine(Environment.CurrentDirectory, "Config", "json", "blacklists.json");
        private static readonly object FileLock = new object();

        private static List<BlacklistRuleDto> LoadRules()
        {
            lock (FileLock)
            {
                if (!File.Exists(JsonPath)) return new List<BlacklistRuleDto>();
                try
                {
                    var json = File.ReadAllText(JsonPath);
                    return JsonSerializer.Deserialize<List<BlacklistRuleDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                catch
                {
                    return new List<BlacklistRuleDto>();
                }
            }
        }

        private static void SaveRules(List<BlacklistRuleDto> rules)
        {
            lock (FileLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(JsonPath)!);
                var json = JsonSerializer.Serialize(rules, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(JsonPath, json);
            }
        }

        public static Task StartAsync(string[] args, CancellationToken ct)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls("http://localhost:5001");

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            var app = builder.Build();

            app.UseCors();

            app.MapGet("/api/blacklist", () =>
            {
                return Results.Ok(LoadRules());
            });

            app.MapPost("/api/blacklist", (BlacklistRuleDto rule) =>
            {
                if (string.IsNullOrWhiteSpace(rule.RuleType) || string.IsNullOrWhiteSpace(rule.Value))
                    return Results.BadRequest();

                var rules = LoadRules();
                if (!rules.Any(r => r.RuleType == rule.RuleType && r.Value == rule.Value))
                {
                    rule.Id = rules.Any() ? rules.Max(r => r.Id) + 1 : 1;
                    rules.Insert(0, rule); // Add at the beginning
                    SaveRules(rules);
                }

                return Results.Ok(rule);
            });

            app.MapDelete("/api/blacklist/{id}", (int id) =>
            {
                var rules = LoadRules();
                var existing = rules.FirstOrDefault(r => r.Id == id);
                if (existing != null)
                {
                    rules.Remove(existing);
                    SaveRules(rules);
                }
                return Results.Ok();
            });

            AppLogger.Log("SYSTEM", "Local API Server started on http://localhost:5001");
            return app.RunAsync(ct);
        }
    }
}
