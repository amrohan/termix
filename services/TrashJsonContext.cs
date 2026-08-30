using System.Text.Json.Serialization;
using termix.models;

namespace termix.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<TrashEntry>))]
public partial class TrashJsonContext : JsonSerializerContext
{
}