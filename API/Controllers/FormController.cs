using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SHARED;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/form")]
[ApiController]
public class FormController(IFormRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new form.
    /// </summary>
    /// <param name="request">The CreateFormRequest object.</param>
    /// <returns>Returns the ID of the created form.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateForm([FromBody] CreateFormRequest request)
    {
        var result = await repository.CreateForm(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific form by its ID.
    /// </summary>
    /// <param name="formId">The ID of the form.</param>
    /// <returns>Returns the form details.</returns>
    [HttpGet("{formId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetForm(Guid formId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetForm(formId, userId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of forms.
    /// </summary>
    /// <param name="filter">The FormFilter object for filtering forms.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of forms.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<FormDto>>))]
    public async Task<IResult> GetForms([FromQuery] FormFilter filter, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetForms(filter, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific form by its ID.
    /// </summary>
    /// <param name="request">The CreateFormRequest object containing updated form data.</param>
    /// <param name="formId">The ID of the form to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{formId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateForm([FromBody] CreateFormRequest request, Guid formId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateForm(request, formId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific form by its ID.
    /// </summary>
    /// <param name="formId">The ID of the form to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{formId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteForm(Guid formId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteForm(formId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits a response to a form.
    /// </summary>
    /// <param name="request">The CreateResponseRequest object containing response data.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("responses")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SubmitFormResponse([FromBody] CreateResponseRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.SubmitFormResponse(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific form response by its ID.
    /// </summary>
    /// <param name="formResponseId">The ID of the form response.</param>
    /// <returns>Returns the form response details.</returns>
    [HttpGet("responses/{formResponseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormResponseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormResponse(Guid formResponseId)
    {
        var result = await repository.GetFormResponse(formResponseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Creates a new question.
    /// </summary>
    /// <param name="request">The CreateQuestionRequest object containing question data.</param>
    /// <returns>Returns the ID of the created question.</returns>
    [HttpPost("question")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateQuestion([FromBody] CreateQuestionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateQuestion(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific question by its ID.
    /// </summary>
    /// <param name="questionId">The ID of the question.</param>
    /// <returns>Returns the question details.</returns>
    [HttpGet("question/{questionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QuestionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetQuestion(Guid questionId)
    {
        var result = await repository.GetQuestion(questionId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of questions.
    /// </summary>
    /// <param name="filter">The PagedQuery object for pagination and filtering.</param>
    /// <param name="searchQuery">Search query for filtering questions.</param>
    /// <returns>Returns a paginated list of questions.</returns>
    [HttpGet("question")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<QuestionDto>>))]
    public async Task<IResult> GetQuestions([FromQuery] PagedQuery filter, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetQuestions(filter, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific question by its ID.
    /// </summary>
    /// <param name="request">The CreateQuestionRequest object containing updated question data.</param>
    /// <param name="questionId">The ID of the question to be updated.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("question/{questionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateQuestion([FromBody] CreateQuestionRequest request, Guid questionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateQuestion(request, questionId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific question by its ID.
    /// </summary>
    /// <param name="questionId">The ID of the question to be deleted.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("question/{questionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteQuestion(Guid questionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteQuestion(questionId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
