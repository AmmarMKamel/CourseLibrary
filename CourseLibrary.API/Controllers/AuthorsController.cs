
using AutoMapper;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController(
    ICourseLibraryRepository courseLibraryRepository,
    IMapper mapper,
    IPropertyMappingService propertyMappingService,
    IPropertyCheckerService propertyCheckerService,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
    private readonly IMapper _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
    private readonly IPropertyMappingService _propertyMappingService = propertyMappingService ??
            throw new ArgumentNullException(nameof(propertyMappingService));
    private readonly IPropertyCheckerService _propertyCheckerService = propertyCheckerService ??
            throw new ArgumentNullException(nameof(propertyCheckerService));
    private readonly ProblemDetailsFactory _problemDetailsFactory = problemDetailsFactory ??
            throw new ArgumentNullException(nameof(problemDetailsFactory));

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

        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    statusCode: 400,
                    detail: $"Not all requested data shaping fields exist on the resource {authorsResourceParameters.Fields}"));
        }

        // get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(authorsResourceParameters);

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages
        };

        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

        var links = CreateLinksForAuthors(
            authorsResourceParameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);

        var shapedAuthors = _mapper
            .Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .ShapeData(authorsResourceParameters.Fields);

        var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
        {
            IDictionary<string, object?> authorAsDictionary = author;
            var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });

        var linkedCollectionResource = new
        {
            value = shapedAuthorsWithLinks,
            links
        };

        // return them
        return Ok(linkedCollectionResource);
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
            case ResourceUriType.Current:
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

    private IEnumerable<LinkDto> CreateLinksForAuthors(
        AuthorsResourceParameters authorsResourceParameters, bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>();

        links.Add(new(
            CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.Current),
            "self",
            "GET"));

        if (hasNext)
        {
            links.Add(new(
                CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage),
                "next_page",
                "GET"));
        }

        if (hasPrevious)
        {
            links.Add(new(
                CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage),
                "previous_page",
                "GET"));
        }

        return links;
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<ActionResult> GetAuthor(Guid authorId, string? fields)
    {
        // get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        var links = CreateLinksForAuthor(authorId, fields);

        IDictionary<string, object?> linkedResourceToReturn = _mapper
            .Map<AuthorDto>(authorFromRepo)
            .ShapeData(fields);

        linkedResourceToReturn.Add("links", links);

        // return author
        return Ok(linkedResourceToReturn);
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string? fields)
    {
        var links = new List<LinkDto>();

        if (string.IsNullOrWhiteSpace(fields))
        {
            links.Add(
                new(Url.Link("GetAuthor", new { authorId }),
                "self",
                "GET"));
        }
        else
        {
            links.Add(
                new(Url.Link("GetAuthor", new { authorId, fields }),
                "self",
                "GET"));
        }

        links.Add(
            new(Url.Link("CreateCourseForAuthor", new { authorId }),
            "create_course_for_author",
            "POST"));

        links.Add(
            new(Url.Link("GetCoursesForAuthor", new { authorId }),
            "courses",
            "GET"));

        return links;
    }

    [HttpPost(Name = "CreateAuthor")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDto author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        IDictionary<string, object?> linkedResourceToReturn = authorToReturn
            .ShapeData(null);

        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            linkedResourceToReturn);
    }

    [HttpOptions]
    public ActionResult GetAuthorOptions()
    {
        Response.Headers.Append("Allow", "GET,POST,HEAD,OPTIONS");
        return Ok();
    }
}
