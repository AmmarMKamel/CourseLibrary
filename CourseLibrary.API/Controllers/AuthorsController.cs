
using AutoMapper;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController(
    ICourseLibraryRepository courseLibraryRepository,
    IMapper mapper,
    IPropertyMappingService propertyMappingService) : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
    private readonly IMapper _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
    private readonly IPropertyMappingService _propertyMappingService = propertyMappingService ??
            throw new ArgumentNullException(nameof(propertyMappingService));

    [HttpGet(Name = "GetAuthors")]
    [HttpHead]
    public async Task<ActionResult> GetAuthors(
        [FromQuery] AuthorsResourceParameters authorsResourceParameters)
    {
        if (!_propertyMappingService
            .ValidMappingExistsFor<AuthorDto, Entities.Author>(authorsResourceParameters.OrderBy))
        {
            return BadRequest();
        }

        // get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorsResourceParameters);

        var previousPageLink = authorsFromRepo.HasPrevious
            ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage)
            : null;

        var nextPageLink = authorsFromRepo.HasNext
            ? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage)
            : null;

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
            previousPageLink,
            nextPageLink
        };

        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

        // return them
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorsResourceParameters.Fields));
    }

    private string? CreateAuthorsResourceUri(
        AuthorsResourceParameters authorsResourceParameters, ResourceUriType type)
    {
        switch (type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        pageNumber = authorsResourceParameters.PageNumber - 1,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery,
                        orderBy = authorsResourceParameters.OrderBy,
                        fields = authorsResourceParameters.Fields
                    });
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        pageNumber = authorsResourceParameters.PageNumber + 1,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery,
                        orderBy = authorsResourceParameters.OrderBy,
                        fields = authorsResourceParameters.Fields
                    });
            default:
                return Url.Link("GetAuthors",
                    new
                    {
                        pageNumber = authorsResourceParameters.PageNumber,
                        pageSize = authorsResourceParameters.PageSize,
                        mainCategory = authorsResourceParameters.MainCategory,
                        searchQuery = authorsResourceParameters.SearchQuery,
                        orderBy = authorsResourceParameters.OrderBy,
                        fields = authorsResourceParameters.Fields
                    });
        }
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<ActionResult<AuthorDto>> GetAuthor(Guid authorId)
    {
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        // return author
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo));
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            authorToReturn);
    }

    [HttpOptions]
    public ActionResult GetAuthorOptions()
    {
        Response.Headers.Append("Allow", "GET,POST,HEAD,OPTIONS");
        return Ok();
    }
}
