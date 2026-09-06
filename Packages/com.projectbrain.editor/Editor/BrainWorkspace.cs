using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProjectBrain
{
    [Serializable] public sealed class BrainFileStamp { public string path; public string sha256; }
    [Serializable] public sealed class BrainSnapshot { public BrainFileStamp[] files; public string[] limitations; }
    [Serializable] public sealed class BrainChange { public string path; public string kind; public bool allowed; }

    public sealed class BrainWorkspace
    {
        public readonly string Root;
        public BrainWorkspace(string root) { Root = Path.GetFullPath(root); }
        public static void Require(bool value, string message) { if (!value) throw new InvalidDataException(message); }
        public static string Hash(string text) => HashBytes(Encoding.UTF8.GetBytes(text));
        public static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        public static bool InScope(string path) => new[] { "Assets/", "Packages/", "ProjectSettings/" }.Any(p => path.StartsWith(p, StringComparison.Ordinal));
        public string Resolve(string path, bool rejectLinks = true)
        {
            Require(!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) && !path.Contains("\\") && !path.Contains(":") && !path.Split('/').Any(p => p == ".." || p == "." || p == ""), "프로젝트 상대 경로가 필요합니다: " + path);
            var full = Path.GetFullPath(Path.Combine(Root, path));
            Require(full.StartsWith(Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "프로젝트 밖 경로입니다.");
            var current = Root;
            foreach (var part in path.Split('/'))
            {
                current = Path.Combine(current, part);
                if (rejectLinks && (File.Exists(current) || Directory.Exists(current))) Require((File.GetAttributes(current) & FileAttributes.ReparsePoint) == 0, "링크 경로는 지원하지 않습니다: " + path);
            }
            return full;
        }
        public string FileHash(string path) { var full = Resolve(path); return File.Exists(full) ? HashBytes(File.ReadAllBytes(full)) : ""; }
        public BrainSnapshot Capture()
        {
            var files = new List<BrainFileStamp>();
            var limits = new List<string>();
            foreach (var scope in new[] { "Assets", "Packages", "ProjectSettings" }) Scan(scope, files, limits);
            var manifest = Path.Combine(Root, "Packages", "manifest.json");
            if (File.Exists(manifest))
            {
                try
                {
                    using (var parsed = JsonDocument.Parse(File.ReadAllText(manifest)))
                        if (parsed.RootElement.TryGetProperty("dependencies", out var deps))
                            foreach (var dep in deps.EnumerateObject())
                                if (dep.Value.GetString().StartsWith("file:", StringComparison.OrdinalIgnoreCase)) limits.Add("file-package-not-certified: " + dep.Name);
                }
                catch (Exception e) { limits.Add("manifest-unreadable: " + e.Message); }
            }
            return new BrainSnapshot { files = files.OrderBy(f => f.path, StringComparer.Ordinal).ToArray(), limitations = limits.ToArray() };
        }
        private void Scan(string relative, List<BrainFileStamp> files, List<string> limits)
        {
            try
            {
                var full = Resolve(relative);
                if (!Directory.Exists(full)) { limits.Add("missing-scope: " + relative); return; }
                foreach (var entry in Directory.GetFileSystemEntries(full).OrderBy(p => p, StringComparer.Ordinal))
                {
                    var name = Path.GetFileName(entry);
                    if (name == ".git") continue;
                    var path = relative + "/" + name;
                    if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0) { limits.Add("link-not-read: " + path); continue; }
                    if (Directory.Exists(entry)) Scan(path, files, limits);
                    else
                    {
                        try { files.Add(new BrainFileStamp { path = path, sha256 = FileHash(path) }); }
                        catch (Exception e) { limits.Add("file-not-read: " + path + ": " + e.Message); }
                    }
                }
            }
            catch (Exception e) { limits.Add("scope-not-read: " + relative + ": " + e.Message); }
        }
        public void ValidateAllowed(string[] paths)
        {
            Require(paths != null && paths.Length > 0, "허용 경로를 명시하세요.");
            foreach (var path in paths) { Require(InScope(path), "감시 범위 밖 허용 경로: " + path); Resolve(path.TrimEnd('/'), false); }
            Require(paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() == paths.Length, "중복 허용 경로입니다.");
        }
        public static BrainChange[] Changes(BrainSnapshot before, BrainSnapshot after, string[] allowed)
        {
            var a = before.files.ToDictionary(f => f.path, StringComparer.Ordinal);
            var b = after.files.ToDictionary(f => f.path, StringComparer.Ordinal);
            return a.Keys.Union(b.Keys).OrderBy(p => p, StringComparer.Ordinal)
                .Where(p => !a.ContainsKey(p) || !b.ContainsKey(p) || a[p].sha256 != b[p].sha256)
                .Select(p => new BrainChange { path = p, kind = !a.ContainsKey(p) ? "added" : !b.ContainsKey(p) ? "deleted" : "modified", allowed = allowed.Any(x => x.EndsWith("/", StringComparison.Ordinal) ? p.StartsWith(x, StringComparison.Ordinal) : p == x) }).ToArray();
        }
    }
}
