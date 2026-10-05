using System.ComponentModel.DataAnnotations;

namespace DocumentProcessor.Web.Configuration;

public sealed class BedrockOptions
{
    public const string SectionName = "Bedrock";

    [Required]
    public string Region { get; init; } = "us-east-1";

    [Required]
    public string SummarizationModelId { get; init; } = "global.anthropic.claude-sonnet-5";

    [Range(1, 128_000)]
    public int MaxTokens { get; init; } = 2000;

    /// <summary>Characters of extracted text sent to the model.</summary>
    [Range(1_000, 200_000)]
    public int MaxInputCharacters { get; init; } = 10_000;

    [Range(1, 100)]
    public int MaxPdfPages { get; init; } = 5;
}
