using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Holons;

/// <summary>Parse HOLON.md identity files.</summary>
public static class IdentityParser
{
    /// <summary>Parsed holon identity.</summary>
    public record HolonIdentity
    {
        public string Uuid { get; init; } = "";
        public string GivenName { get; init; } = "";
        public string FamilyName { get; init; } = "";
        public string Motto { get; init; } = "";
        public string Composer { get; init; } = "";
        public string Clade { get; init; } = "";
        public string Status { get; init; } = "";
        public string Born { get; init; } = "";
        public string Lang { get; init; } = "";
        public List<string> Parents { get; init; } = new();
        public string Reproduction { get; init; } = "";
        public string GeneratedBy { get; init; } = "";
        public string ProtoStatus { get; init; } = "";
        public List<string> Aliases { get; init; } = new();
    }

    /// <summary>Parse a HOLON.md file.</summary>
    public static HolonIdentity ParseHolon(string path)
    {
        var text = File.ReadAllText(path);

        if (!text.StartsWith("---"))
            throw new FormatException($"{path}: missing YAML frontmatter");

        int endIdx = text.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIdx < 0)
            throw new FormatException($"{path}: unterminated frontmatter");

        var frontmatter = text[3..endIdx].Trim();

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<HolonIdentity>(frontmatter);
    }
}
