using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using DocumentProcessor.Web.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentProcessor.Web.Services;

public sealed class BedrockDocumentSummarizer(
    IAmazonBedrockRuntime bedrock,
    IOptions<BedrockOptions> options) : IDocumentSummarizer
{
    private readonly BedrockOptions _options = options.Value;

    public async Task<string> SummarizeAsync(
        string fileName,
        string text,
        CancellationToken cancellationToken = default)
    {
        var request = new ConverseRequest
        {
            ModelId = _options.SummarizationModelId,
            System = [new SystemContentBlock { Text = "Summarize the document in under 500 characters. Reply with the summary only." }],
            Messages = [new Message
            {
                Role = ConversationRole.User,
                Content = [new ContentBlock { Text = $"File: {fileName}\n\n{text}" }]
            }],
            // Claude Sonnet 5 rejects Temperature and TopP.
            InferenceConfig = new InferenceConfiguration { MaxTokens = _options.MaxTokens }
        };

        var response = await bedrock.ConverseAsync(request, cancellationToken);

        return response.Output?.Message?.Content?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
    }
}
