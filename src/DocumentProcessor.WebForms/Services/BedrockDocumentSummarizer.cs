using System.Collections.Generic;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using DocumentProcessor.WebForms.Configuration;
using Microsoft.Extensions.Options;

// Amazon.BedrockRuntime has a Trace type of its own, so the logger needs an alias.
using DiagnosticsTrace = System.Diagnostics.Trace;

namespace DocumentProcessor.WebForms.Services
{
    public class BedrockDocumentSummarizer : IDocumentSummarizer
    {
        private const string SystemPrompt =
            "Summarize the document in under 500 characters. Reply with the summary only.";

        private readonly IAmazonBedrockRuntime _client;
        private readonly BedrockOptions _options;

        public BedrockDocumentSummarizer(IOptions<BedrockOptions> bedrockOptions)
        {
            _options = bedrockOptions.Value;

            var config = new AmazonBedrockRuntimeConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
            };

            _client = new AmazonBedrockRuntimeClient(config);
        }

        public string Summarize(string fileName, string text)
        {
            var request = new ConverseRequest
            {
                ModelId = _options.SummarizationModelId,
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
                    MaxTokens = _options.MaxTokens
                }
            };

            // Blocking call on the request thread. The .NET Framework build of the AWS SDK
            // exposes real synchronous operations, so there is no task to wait on.
            var response = _client.ConverseAsync(request).GetAwaiter().GetResult();

            if (response.Output == null ||
                response.Output.Message == null ||
                response.Output.Message.Content == null ||
                response.Output.Message.Content.Count == 0)
            {
                DiagnosticsTrace.TraceWarning(
                    "Bedrock returned no content for '{0}'. StopReason={1}, HTTP={2}.",
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

            DiagnosticsTrace.TraceWarning(
                "Bedrock returned {0} content block(s) but no text for '{1}'. StopReason={2}.",
                response.Output.Message.Content.Count, fileName, response.StopReason);

            return string.Empty;
        }
    }
}
