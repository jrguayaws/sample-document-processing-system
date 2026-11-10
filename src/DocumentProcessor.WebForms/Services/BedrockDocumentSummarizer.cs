using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using DocumentProcessor.WebForms.Configuration;
using Microsoft.Extensions.Logging;

namespace DocumentProcessor.WebForms.Services
{
    public class BedrockDocumentSummarizer : IDocumentSummarizer
    {
        private const string SystemPrompt =
            "Summarize the document in under 500 characters. Reply with the summary only.";

        // The Bedrock client is registered once as a singleton in Program.cs (configured with
        // Bedrock.Region); Amazon's clients are thread safe and expensive to build.
        private readonly IAmazonBedrockRuntime _client;
        private readonly AppSettings _settings;
        private readonly ILogger<BedrockDocumentSummarizer> _logger;

        public BedrockDocumentSummarizer(
            IAmazonBedrockRuntime client,
            AppSettings settings,
            ILogger<BedrockDocumentSummarizer> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> SummarizeAsync(string fileName, string text, CancellationToken cancellationToken = default)
        {
            var request = new ConverseRequest
            {
                ModelId = _settings.BedrockSummarizationModelId,
                System = new List<SystemContentBlock>
                {
                    new SystemContentBlock { Text = SystemPrompt }
                },
                Messages = new List<Message>
                {
                    new Message
                    {
                        Role = ConversationRole.User,
                        Content = new List<ContentBlock>
                        {
                            new ContentBlock { Text = "File: " + fileName + "\n\n" + text }
                        }
                    }
                },
                // Claude Sonnet 5 rejects Temperature and TopP.
                InferenceConfig = new InferenceConfiguration
                {
                    MaxTokens = _settings.BedrockMaxTokens
                }
            };

            var response = await _client.ConverseAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.Output == null ||
                response.Output.Message == null ||
                response.Output.Message.Content == null ||
                response.Output.Message.Content.Count == 0)
            {
                _logger.LogWarning(
                    "Bedrock returned no content for '{FileName}'. StopReason={StopReason}, HTTP={HttpStatusCode}.",
                    fileName, response.StopReason, response.HttpStatusCode);

                return string.Empty;
            }

            // Claude Sonnet 5 can put a reasoning block ahead of the answer, so the first
            // block is not reliably the summary. Take the first one that carries text.
            foreach (var block in response.Output.Message.Content)
            {
                if (!string.IsNullOrWhiteSpace(block.Text))
                {
                    return block.Text.Trim();
                }
            }

            _logger.LogWarning(
                "Bedrock returned {BlockCount} content block(s) but no text for '{FileName}'. StopReason={StopReason}.",
                response.Output.Message.Content.Count, fileName, response.StopReason);

            return string.Empty;
        }
    }
}
