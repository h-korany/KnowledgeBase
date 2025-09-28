using FAQ.Application.DTOs;
using FAQ.Application.Interfaces;
using FAQ.Presentation.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using FAQ.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace FAQ.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [Authorize]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionsService _questionService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QuestionsController> _logger;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        public QuestionsController(IQuestionsService questionService, HttpClient httpClient, IConfiguration configuration, IKnowledgeBaseService knowledgeBaseService, ILogger<QuestionsController> logger)
        {
            _questionService = questionService;
            _httpClient = httpClient;
            _configuration = configuration;
            _knowledgeBaseService = knowledgeBaseService;
            _logger = logger;
        }
        private string GetUserId() => User.FindFirstValue("UserID")!;


        [HttpGet]

        public async Task<IActionResult> GetQuestions()
        {
            var tasks = await _questionService.GetAllAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestion(int id)
        {
            var task = await _questionService.GetByIdAsync(id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionDto createQuestionDto)
        {
            var question = await _questionService.AddAsync(createQuestionDto, Guid.Parse(GetUserId()));
            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, question);
        }
        [HttpPost("{questionId}")]
        public async Task<IActionResult> CreateAnswer([FromBody] AnswerDto answerDto, int questionId)
        {
            var question = await _questionService.AddAnswerAsync(answerDto, questionId, Guid.Parse(GetUserId()));
            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, question);
        }

        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateQuestion(int id, UpdateQuestionDto updateQuestionDto)
        //{
        //    var result = await _taskService.UpdateAsync(id, updateQuestionDto);
        //    if (!result) return NotFound();
        //    return NoContent();
        //}

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var result = await _questionService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }


        [Authorize(Roles ="manager")]
        [HttpPost("summary")]
        public async Task<IActionResult> GenerateSummary([FromBody] SummaryRequest request)
        {
            try
            {
                // Try Hugging Face first
                var huggingFaceResult = await TryHuggingFaceSummary(request);
                if (!string.IsNullOrEmpty(huggingFaceResult))
                {
                    return Ok(new { summary = huggingFaceResult });
                }

                var ruleBasedSummary = GenerateRuleBasedSummary(request);
                return Ok(new { summary = ruleBasedSummary });
            }
            catch (Exception ex)
            {
                var fallbackSummary = GenerateRuleBasedSummary(request);
                return Ok(new { summary = fallbackSummary });
            }
        }

        private async Task<string?> TryHuggingFaceSummary(SummaryRequest request)
        {
            try
            {
                var apiKey = _configuration["HuggingFace:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return null;
                }

                // Format the text for summarization
                var textToSummarize = $"{request.QuestionTitle}. {request.QuestionContent}";
                if (request.Answers.Any())
                {
                    textToSummarize += " Answers: " + string.Join(" ", request.Answers);
                }

                var payload = new
                {
                    inputs = textToSummarize,
                    parameters = new
                    {
                        max_length = 150,
                        min_length = 40,
                        do_sample = false
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // CORRECT API endpoint and headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/facebook/bart-large-cnn",
                    content
                );


                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Parse the response correctly
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var results = JsonSerializer.Deserialize<List<HuggingFaceResponse>>(responseContent, options);

                    return results?.FirstOrDefault()?.SummaryText;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
            }

            return null;
        }

        // Rule-based fallback (same as before)
        private string GenerateRuleBasedSummary(SummaryRequest request)
        {
            var answerCount = request.Answers.Count;
            var keyTopics = ExtractKeyTopics(request.QuestionTitle + " " + request.QuestionContent);

            var summary = new StringBuilder();
            summary.Append($"This discussion focuses on {string.Join(", ", keyTopics)}. ");

            if (answerCount == 0)
            {
                summary.Append("The question has not received any answers yet.");
            }
            else if (answerCount == 1)
            {
                summary.Append($"One solution was provided: {SummarizeAnswer(request.Answers[0])}");
            }
            else
            {
                summary.Append($"Among {answerCount} responses, key suggestions include: {GetMainPoints(request.Answers)}");
            }

            return summary.ToString();
        }

        private List<string> ExtractKeyTopics(string text)
        {
            var words = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commonWords = new HashSet<string> { "the", "a", "an", "is", "are", "how", "what", "why", "when", "where" };

            return words
                .Where(word => word.Length > 3 && !commonWords.Contains(word))
                .Distinct()
                .Take(3)
                .ToList();
        }

        private string SummarizeAnswer(string answer)
        {
            var sentences = answer.Split('.');
            var firstSentence = sentences.FirstOrDefault(s => s.Trim().Length > 20);
            return firstSentence?.Trim() + "." ?? answer.Substring(0, Math.Min(100, answer.Length)) + "...";
        }

        private string GetMainPoints(List<string> answers)
        {
            var points = answers.Take(3).Select((answer, index) =>
                $"{index + 1}) {SummarizeAnswer(answer)}"
            );

            return string.Join("; ", points);
        }
        [HttpPost("ask")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> AskQuestion([FromBody] AIAssistantRequest request)
        {
            try
            {
                _logger.LogInformation("AI Assistant request: {Query}", request.Query);

                // Get relevant questions from knowledge base
                var relevantQuestions = await _knowledgeBaseService.GetRelevantQuestionsAsync(request.Query);

                // Try AI service first
                var aiResponse = await TryAIService(request.Query, relevantQuestions);
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    return Ok(new AIAssistantResponse
                    {
                        Response = aiResponse,
                        RelatedQuestionIds = relevantQuestions.Select(q => q.Id).Take(5).ToList(),
                        Source = "AI"
                    });
                }

                // Fallback to rule-based response
                var ruleBasedResponse = GenerateRuleBasedResponse(request.Query, relevantQuestions);
                return Ok(new AIAssistantResponse
                {
                    Response = ruleBasedResponse,
                    RelatedQuestionIds = relevantQuestions.Select(q => q.Id).Take(5).ToList(),
                    Source = "Rule-Based"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI Assistant");
                return StatusCode(500, new { error = "Failed to process your question" });
            }
        }

        [Authorize(Roles = "manager")]
        [HttpGet("analyze")]
        public async Task<IActionResult> AnalyzeKnowledgeBase(string? category = null)
        {
            try
            {
                var analysis = await _knowledgeBaseService.AnalyzeKnowledgeBaseAsync(category);

                return Ok(new
                {
                    Insights = GenerateKnowledgeBaseInsights(analysis),
                    Statistics = analysis,
                    PopularCategories = analysis.PopularCategories,
                    GeneratedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing knowledge base");
                return StatusCode(500, new { error = "Failed to analyze knowledge base" });
            }
        }
        [Authorize(Roles = "manager")]
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _knowledgeBaseService.ExtractCategoriesFromQuestionsAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { error = "Failed to get categories" });
            }
        }

        private async Task<string?> TryAIService(string query, List<Question> relevantQuestions)
        {
            try
            {
                var apiKey = _configuration["HuggingFace:ApiKey"];
                if (string.IsNullOrEmpty(apiKey)) return null;

                var context = BuildContextFromQuestions(relevantQuestions);
                var prompt = CreateAIPrompt(query, context);

                var payload = new
                {
                    inputs = prompt,
                    parameters = new
                    {
                        max_length = 300,
                        min_length = 50,
                        temperature = 0.7,
                        do_sample = true
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync(
                    "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var results = JsonSerializer.Deserialize<List<HuggingFaceAssistanceResponse>>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    return results?.FirstOrDefault()?.GeneratedText;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI service failed, using fallback");
            }

            return null;
        }

        private string GenerateRuleBasedResponse(string query, List<Question> relevantQuestions)
        {
            var queryLower = query.ToLower();
            var response = new StringBuilder();
            var questionCount = relevantQuestions.Count;

            // Extract potential categories from query using simple keyword matching
            var queryCategories = ExtractCategoriesFromQuery(query);

            if (queryLower.Contains("common") || queryLower.Contains("frequent") || queryLower.Contains("often"))
            {
                response.Append(GenerateCommonIssuesResponse(relevantQuestions, queryCategories));
            }
            else if (queryLower.Contains("how to") || queryLower.Contains("how do i") || queryLower.Contains("how can i"))
            {
                response.Append(GenerateHowToResponse(relevantQuestions));
            }
            else if (queryLower.Contains("trend") || queryLower.Contains("pattern") || queryLower.Contains("analysis"))
            {
                response.Append(GenerateTrendAnalysisResponse(relevantQuestions));
            }
            else
            {
                response.Append(GenerateGeneralResponse(relevantQuestions, query, queryCategories));
            }

            if (questionCount > 0)
            {
                response.Append($"\n\nI found {questionCount} related questions in our knowledge base.");
            }
            else
            {
                response.Append("\n\nNo specific matches found in our knowledge base. Consider asking this question to the community.");
            }

            return response.ToString();
        }

        private List<string> ExtractCategoriesFromQuery(string query)
        {
            var commonCategories = new List<string>
            {
                "Password", "VPN", "Finance", "HR", "IT", "Software", "Hardware",
                "Network", "Email", "Benefits", "Onboarding", "Expense", "Travel"
            };

            var queryLower = query.ToLower();
            return commonCategories.Where(category => queryLower.Contains(category.ToLower())).ToList();
        }

        private string GenerateCommonIssuesResponse(List<Question> questions, List<string> categories)
        {
            var response = new StringBuilder();

            if (categories.Any())
            {
                response.Append($"Based on questions about {string.Join(", ", categories)}, here are common topics:\n\n");
            }
            else
            {
                response.Append("Based on our knowledge base, here are common discussion topics:\n\n");
            }

            var topCategories = questions
                .Where(q => !string.IsNullOrEmpty(q.InferredCategory))
                .GroupBy(q => q.InferredCategory)
                .OrderByDescending(g => g.Count())
                .Take(3);

            if (topCategories.Any())
            {
                foreach (var group in topCategories)
                {
                    var exampleQuestion = group.First().Title;
                    response.AppendLine($"• **{group.Key}**: {group.Count()} questions (e.g., \"{exampleQuestion}\")");
                }
            }
            else
            {
                response.AppendLine("• IT Support: Various technical issues");
                response.AppendLine("• HR Processes: Employee-related questions");
                response.AppendLine("• Finance: Expense and reimbursement topics");
            }

            return response.ToString();
        }

        private string GenerateHowToResponse(List<Question> questions)
        {
            if (!questions.Any())
                return "I can help with procedural questions. Based on your query, here are some common how-to topics in our knowledge base: IT procedures, HR processes, and finance guidelines.";

            var response = new StringBuilder();
            response.Append("Here are some solutions from our knowledge base:\n\n");

            var answeredQuestions = questions.Where(q => q.Answers.Any()).Take(3);

            if (answeredQuestions.Any())
            {
                foreach (var question in answeredQuestions)
                {
                    var bestAnswer = question.Answers.OrderByDescending(a => a.Content.Length).First();
                    var summary = SummarizeAnswer(bestAnswer.Content);
                    response.AppendLine($"• **{question.Title}**: {summary}");
                }
            }
            else
            {
                response.AppendLine("• Check our IT documentation for technical procedures");
                response.AppendLine("• Review HR guidelines for employee-related processes");
                response.AppendLine("• Consult finance department for expense-related questions");
            }

            return response.ToString();
        }

        private string GenerateTrendAnalysisResponse(List<Question> questions)
        {
            var response = new StringBuilder();

            if (!questions.Any())
            {
                return "I can analyze trends in our knowledge base. Currently, there's not enough data for a comprehensive analysis.";
            }

            var recentQuestions = questions.Where(q => q.CreatedAt >= DateTime.UtcNow.AddMonths(-1)).ToList();
            var popularCategories = questions
                .Where(q => !string.IsNullOrEmpty(q.InferredCategory))
                .GroupBy(q => q.InferredCategory)
                .OrderByDescending(g => g.Count())
                .Take(5);

            response.AppendLine("**Knowledge Base Trends Analysis**\n");
            response.AppendLine($"• Recent activity: {recentQuestions.Count} questions in the last month");

            if (popularCategories.Any())
            {
                response.AppendLine($"• Most discussed topics: {string.Join(", ", popularCategories.Select(g => g.Key))}");
            }

            if (questions.Any(q => q.Answers.Any()))
            {
                var answerRate = (double)questions.Count(q => q.Answers.Any()) / questions.Count * 100;
                response.AppendLine($"• Answer rate: {answerRate:F1}% of questions have answers");
            }

            return response.ToString();
        }

        private string GenerateGeneralResponse(List<Question> questions, string query, List<string> queryCategories)
        {
            if (!questions.Any())
            {
                return $"I understand you're asking about \"{query}\". While I don't have specific information on this topic in our knowledge base yet, this would be a great question to ask our community.";
            }

            var response = new StringBuilder();

            if (queryCategories.Any())
            {
                response.AppendLine($"Based on your question about {string.Join(", ", queryCategories)}, I found relevant information:\n");
            }
            else
            {
                response.AppendLine($"Based on your question about \"{query}\", I found relevant information in our knowledge base:\n");
            }

            foreach (var question in questions.Take(3))
            {
                response.AppendLine($"• **{question.Title}**");
                if (question.Answers.Any())
                {
                    response.AppendLine($"  - {question.Answers.Count} answers available");
                }
                if (!string.IsNullOrEmpty(question.InferredCategory))
                {
                    response.AppendLine($"  - Category: {question.InferredCategory}");
                }
                response.AppendLine();
            }

            return response.ToString();
        }

        
        private string BuildContextFromQuestions(List<Question> questions)
        {
            var context = new StringBuilder();
            context.AppendLine("Knowledge Base Context:\n");

            foreach (var question in questions.Take(5))
            {
                context.AppendLine($"Q: {question.Title}");
                context.AppendLine($"A: {(question.Answers.Any() ? question.Answers.First().Content : "No answers yet")}");
                context.AppendLine();
            }

            return context.ToString();
        }

        private string CreateAIPrompt(string query, string context)
        {
            return $@"
            You are a helpful AI assistant for a company knowledge base. Use the following context from existing questions and answers to respond helpfully.

            {context}

            User Question: {query}

            Please provide a helpful response based on the knowledge base context. If the context doesn't contain relevant information, suggest asking the community. Keep your response concise and practical.";
        }

        private string GenerateKnowledgeBaseInsights(KnowledgeBaseAnalysis analysis)
        {
            var insights = new List<string>();

            if (analysis.AnswerRate > 70)
                insights.Add("Excellent community engagement with high answer rate");
            else if (analysis.AnswerRate > 50)
                insights.Add("Good community participation with moderate answer rate");
            else
                insights.Add("Opportunity to improve community engagement");

            if (analysis.TotalQuestions > 100)
                insights.Add("Comprehensive knowledge base with extensive coverage");
            else if (analysis.TotalQuestions > 50)
                insights.Add("Growing knowledge base with good content diversity");
            else
                insights.Add("Knowledge base is developing, consider adding more content");

            if (analysis.PopularCategories.Any())
                insights.Add($"Top topics include: {string.Join(", ", analysis.PopularCategories.Take(3))}");

            return string.Join(". ", insights) + ".";
        }

        

       
        

        private string GenerateHowToResponse(List<Question> questions, string query)
        {
            if (!questions.Any())
                return "I can help with procedural questions. Based on your query, here's what I found:";

            var response = new StringBuilder();
            response.Append("Here are some solutions from our knowledge base:\n\n");

            foreach (var question in questions.Take(3))
            {
                if (question.Answers.Any())
                {
                    var bestAnswer = question.Answers.OrderByDescending(a => a.Content.Length).First();
                    var summary = ShorteningAnswer(bestAnswer.Content);
                    response.AppendLine($"• **{question.Title}**: {summary}");
                }
            }

            return response.ToString();
        }

        

        private string GenerateGeneralResponse(List<Question> questions, string query)
        {
            if (!questions.Any())
            {
                return $"I understand you're asking about \"{query}\". While I don't have specific information on this topic in our knowledge base yet, this would be a great question to ask our community.";
            }

            var response = new StringBuilder();
            response.AppendLine($"Based on your question about \"{query}\", I found relevant information in our knowledge base:\n");

            foreach (var question in questions.Take(3))
            {
                response.AppendLine($"• **{question.Title}**");
                if (question.Answers.Any())
                {
                    response.AppendLine($"  - {question.Answers.Count} answers available");
                }
            }

            return response.ToString();
        }

       

        private async Task<string> GenerateKnowledgeBaseInsights(KnowledgeBaseAnalysis analysis, string category)
        {
            var prompt = $@"
            Analyze this knowledge base statistics and provide 3 key insights:

            Total Questions: {analysis.TotalQuestions}
            Questions with Answers: {analysis.AnsweredQuestions}
            Average Answers per Question: {analysis.AverageAnswersPerQuestion:F1}
            {(!string.IsNullOrEmpty(category) ? $"Category Filter: {category}" : "")}

            Provide 3 concise insights about the knowledge base health and content trends.";

            // Similar AI call implementation as above
            return "Knowledge base is growing healthily with good community engagement. Focus areas include IT support and HR processes. Answer rate indicates active community participation.";
        }

        private string ShorteningAnswer(string answer)
        {
            if (answer.Length <= 100) return answer;
            return answer.Substring(0, 100) + "...";
        }
        //[HttpGet("unanswered")]
        //public async Task<ActionResult<ApiResponse<IEnumerable<Question>>>> GetUnansweredQuestions(
        //[FromQuery] int page = 1,
        //[FromQuery] int pageSize = 10)
        //{
        //    try
        //    {
        //        if (page < 1) page = 1;
        //        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        //        var query = _context.Questions
        //            .Where(q => !q.IsAnswered) // Adjust based on your schema
        //            .OrderByDescending(q => q.CreatedDate);

        //        var totalCount = await query.CountAsync();
        //        var questions = await query
        //            .Skip((page - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        var response = new ApiResponse<IEnumerable<Question>>
        //        {
        //            Data = questions,
        //            TotalCount = totalCount,
        //            Page = page,
        //            PageSize = pageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        //        };

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching unanswered questions");
        //        return StatusCode(500, new ApiResponse<object>
        //        {
        //            Error = "Failed to fetch unanswered questions"
        //        });
        //    }
        //}

        //// GET: api/questions/search?q=query
        //[HttpGet("search")]
        //public async Task<ActionResult<ApiResponse<IEnumerable<Question>>>> SearchQuestions(
        //    [FromQuery] string q,
        //    [FromQuery] int page = 1,
        //    [FromQuery] int pageSize = 10)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        //        {
        //            return BadRequest(new ApiResponse<object>
        //            {
        //                Error = "Search query must be at least 2 characters long"
        //            });
        //        }

        //        if (page < 1) page = 1;
        //        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        //        var searchQuery = q.ToLower().Trim();

        //        var query = _context.Questions
        //            .Where(question =>
        //                question.Title.ToLower().Contains(searchQuery) ||
        //                question.Content.ToLower().Contains(searchQuery) ||
        //                (question.Answer != null && question.Answer.ToLower().Contains(searchQuery)) ||
        //                question.Category.ToLower().Contains(searchQuery))
        //            .OrderByDescending(q => q.CreatedDate);

        //        var totalCount = await query.CountAsync();
        //        var results = await query
        //            .Skip((page - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        var response = new ApiResponse<IEnumerable<Question>>
        //        {
        //            Data = results,
        //            TotalCount = totalCount,
        //            Page = page,
        //            PageSize = pageSize,
        //            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        //            SearchQuery = q
        //        };

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error searching questions for query: {Query}", q);
        //        return StatusCode(500, new ApiResponse<object>
        //        {
        //            Error = "Search failed due to server error"
        //        });
        //    }
        //}
    }

    public class HuggingFaceResponse
    {
        public string SummaryText { get; set; } = string.Empty;
    }
    public class HuggingFaceAssistanceResponse
    {
        public string GeneratedText { get; set; } = string.Empty;
    }
    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string SearchQuery { get; set; }
        public string Error { get; set; }
    }
}


