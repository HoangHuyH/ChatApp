using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text;

namespace ChatApp.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Optional: Remove if you want anonymous access
    public class AiChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiChatController> _logger;

        public AiChatController(IConfiguration configuration, HttpClient httpClient, ILogger<AiChatController> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] AiChatRequest request)
        {
            try
            {
                // Get from environment variables (loaded by DotNetEnv in Program.cs)
                var apiKey = Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY");
                var model = Environment.GetEnvironmentVariable("AI_CHAT_MODEL") ?? "gemini-pro";
                var systemPrompt = Environment.GetEnvironmentVariable("AI_SYSTEM_PROMPT") ?? 
                    "You are a helpful AI assistant. Be concise but helpful in your responses.";
                
                _logger.LogInformation($"Using model: {model}");
                
                if (string.IsNullOrEmpty(apiKey) || apiKey == "your_google_ai_studio_api_key_here")
                {
                    return Ok(new AiChatResponse 
                    { 
                        Response = "AI chat is not configured yet. Please set up your Google AI Studio API key in the .env file.",
                        Success = false
                    });
                }

                // Prepare conversation history with system prompt
                var contents = new List<object>();
                
                // Add system prompt as the first message
                contents.Add(new
                {
                    parts = new[] { new { text = systemPrompt } },
                    role = "model"
                });

                // Add conversation history (last 5 exchanges to keep context manageable)
                if (request.ConversationHistory?.Count > 0)
                {
                    var recentHistory = request.ConversationHistory.TakeLast(10).ToList();
                    foreach (var historyMessage in recentHistory)
                    {
                        contents.Add(new
                        {
                            parts = new[] { new { text = historyMessage.Message } },
                            role = historyMessage.Sender == "user" ? "user" : "model"
                        });
                    }
                }

                // Add current user message
                contents.Add(new
                {
                    parts = new[] { new { text = request.Message } },
                    role = "user"
                });

                // Prepare the request for Google AI Studio API
                var aiRequest = new
                {
                    contents = contents
                };

                var jsonContent = JsonSerializer.Serialize(aiRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Call Google AI Studio API
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                
                _logger.LogInformation($"Calling Google AI API with {contents.Count} messages");
                
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation($"AI API Response received: {responseContent.Length} characters");
                    
                    var aiResponse = JsonSerializer.Deserialize<GoogleAiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    var aiText = aiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                    
                    if (string.IsNullOrEmpty(aiText))
                    {
                        _logger.LogWarning($"No AI text found in response: {responseContent}");
                        return Ok(new AiChatResponse 
                        { 
                            Response = "I'm having trouble generating a response right now. Please try rephrasing your question.",
                            Success = false
                        });
                    }

                    return Ok(new AiChatResponse { Response = aiText, Success = true });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Google AI API error: {response.StatusCode} - {errorContent}");
                    return Ok(new AiChatResponse 
                    { 
                        Response = $"I'm experiencing some technical difficulties. Please try again in a moment.",
                        Success = false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI chat request");
                return Ok(new AiChatResponse 
                { 
                    Response = "I'm sorry, something went wrong. Please try again later.",
                    Success = false
                });
            }
        }[HttpGet("status")]
        public IActionResult GetStatus()
        {
            // Get from configuration (which includes environment variables loaded by Program.cs)
            var apiKey = _configuration["GOOGLE_AI_API_KEY"];
            var model = _configuration["AI_CHAT_MODEL"] ?? "gemini-pro";
            var isConfigured = !string.IsNullOrEmpty(apiKey) && apiKey != "your_google_ai_studio_api_key_here";
            
            // Also check direct environment variables for debugging
            var envApiKey = Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY");
            var envModel = Environment.GetEnvironmentVariable("AI_CHAT_MODEL");
            
            return Ok(new { 
                Configured = isConfigured, 
                Model = model,
                ApiKeyExists = !string.IsNullOrEmpty(apiKey),
                ApiKeyLength = apiKey?.Length ?? 0,
                ApiKeyPreview = apiKey?.Substring(0, Math.Min(10, apiKey?.Length ?? 0)) + "..." ?? "null",
                ConfigurationApiKey = apiKey,
                EnvironmentApiKey = envApiKey,
                ConfigurationModel = model,
                EnvironmentModel = envModel
            });
        }

        [HttpGet("test-env")]
        public IActionResult TestEnvironmentVariables()
        {
            // Check all possible ways to access the environment variables
            var fromEnv = Environment.GetEnvironmentVariable("GOOGLE_AI_API_KEY");
            var fromConfig = _configuration["GOOGLE_AI_API_KEY"];
            var fromConfigGet = _configuration.GetValue<string>("GOOGLE_AI_API_KEY");
            
            return Ok(new {
                DirectEnvironment = fromEnv,
                FromConfiguration = fromConfig,
                FromConfigurationGetValue = fromConfigGet,
                AllEnvironmentVars = Environment.GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .Where(x => x.Key.ToString().Contains("GOOGLE") || x.Key.ToString().Contains("AI"))
                    .ToDictionary(x => x.Key, x => x.Value)
            });
        }
    }

    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ConversationMessage>? ConversationHistory { get; set; }
    }

    public class ConversationMessage
    {
        public string Sender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AiChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    // Google AI Studio API response models
    public class GoogleAiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    public class Candidate
    {
        public Content? Content { get; set; }
    }

    public class Content
    {
        public List<Part>? Parts { get; set; }
    }

    public class Part
    {
        public string? Text { get; set; }
    }
}
