using KeeperData.Api.Controllers.RequestDtos.Parties;
using KeeperData.Application;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Application.Queries.Parties;
using KeeperData.Core.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    /// <summary>
    /// Operations related to parties (keepers, organisations).
    /// </summary>
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "public")]
    [Produces("application/json")]
    [Tags("party")]
    public class PartiesController(IRequestExecutor executor) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        /// <summary>
        /// Retrieve a list of parties.
        /// </summary>
        /// <remarks>
        /// The endpoint allows searching and retrieving a list of parties. Supports filtering by name, email, and Change Data Capture (CDC) via the lastUpdatedDate parameter.
        /// </remarks>
        /// <param name="request">Query parameters to filter parties.</param>
        /// <response code="200">OK - Successful</response>
        /// <response code="400">The request was malformed or could not be processed.</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<PartyDocument>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetParties([FromQuery] GetPartiesRequest request)
        {
            // TEMPORARY: Return dummy data for local testing
            var dummyParties = new List<PartyDocument>
            {
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440001",
                    Title = "Mr",
                    FirstName = "John",
                    LastName = "Smith",
                    Name = "John Smith",
                    CustomerNumber = "C12345",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-6),
                    LastUpdatedDate = DateTime.UtcNow.AddDays(-5),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-001",
                            Email = "john.smith@example.com",
                            Mobile = "07700900123",
                            Landline = "01234567890",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddDays(-5)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440002",
                    Title = "Mrs",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Name = "Sarah Johnson",
                    CustomerNumber = "C23456",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-12),
                    LastUpdatedDate = DateTime.UtcNow.AddDays(-2),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-002",
                            Email = "sarah.johnson@farmmail.co.uk",
                            Mobile = "07700900456",
                            Landline = "01234567891",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddDays(-2)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440003",
                    FirstName = "David",
                    LastName = "Williams",
                    Name = "David Williams",
                    CustomerNumber = "L98765",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-8),
                    LastUpdatedDate = DateTime.UtcNow.AddDays(-10),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-003",
                            Email = "d.williams@livestock.org",
                            Mobile = "07700900789",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddDays(-10)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440004",
                    FirstName = "Emma",
                    LastName = "Brown",
                    Name = "Emma Brown",
                    CustomerNumber = "C34567",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-3),
                    LastUpdatedDate = DateTime.UtcNow.AddDays(-1),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-004",
                            Email = "emma.brown@agriculture.com",
                            Mobile = "07700900321",
                            Landline = "01234567892",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddDays(-1)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440005",
                    Title = "Mr",
                    FirstName = "Michael",
                    LastName = "Davies",
                    Name = "Michael Davies",
                    CustomerNumber = "L11223",
                    PartyType = "Person",
                    State = "Inactive",
                    CreatedDate = DateTime.UtcNow.AddYears(-2),
                    LastUpdatedDate = DateTime.UtcNow.AddMonths(-6),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-005",
                            Email = "michael.davies@rural.net",
                            Landline = "01234567893",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddMonths(-6)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440006",
                    Title = "Ms",
                    FirstName = "Rachel",
                    LastName = "Taylor",
                    Name = "Rachel Taylor",
                    CustomerNumber = "C45678",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-4),
                    LastUpdatedDate = DateTime.UtcNow.AddHours(-12),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-006",
                            Email = "r.taylor@keeper.gov.uk",
                            Mobile = "07700900654",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddHours(-12)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440007",
                    FirstName = "James",
                    LastName = "Wilson",
                    Name = "James Wilson",
                    CustomerNumber = "L33445",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-9),
                    LastUpdatedDate = DateTime.UtcNow.AddDays(-7),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-007",
                            Email = "james.wilson@holdings.co.uk",
                            Mobile = "07700900987",
                            Landline = "01234567894",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddDays(-7)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440008",
                    Title = "Mrs",
                    FirstName = "Linda",
                    LastName = "Evans",
                    Name = "Linda Evans",
                    CustomerNumber = "C56789",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-15),
                    LastUpdatedDate = DateTime.UtcNow.AddDays(-3),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-008",
                            Email = "linda.evans@farming.org",
                            Mobile = "07700900147",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddDays(-3)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440009",
                    Title = "Mr",
                    FirstName = "Thomas",
                    LastName = "Roberts",
                    Name = "Thomas Roberts",
                    CustomerNumber = "L44556",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-7),
                    LastUpdatedDate = DateTime.UtcNow.AddDays(-4),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-009",
                            Email = "t.roberts@countryside.net",
                            Mobile = "07700900258",
                            Landline = "01234567895",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddDays(-4)
                        }
                    }
                },
                new PartyDocument
                {
                    Id = "550e8400-e29b-41d4-a716-446655440010",
                    Title = "Ms",
                    FirstName = "Sophie",
                    LastName = "Anderson",
                    Name = "Sophie Anderson",
                    CustomerNumber = "C67890",
                    PartyType = "Person",
                    State = "Active",
                    CreatedDate = DateTime.UtcNow.AddMonths(-5),
                    LastUpdatedDate = DateTime.UtcNow.AddHours(-6),
                    Communication = new List<CommunicationDocument>
                    {
                        new CommunicationDocument
                        {
                            IdentifierId = "com-010",
                            Email = "sophie.anderson@ranch.co.uk",
                            Mobile = "07700900369",
                            PrimaryContactFlag = true,
                            LastUpdatedDate = DateTime.UtcNow.AddHours(-6)
                        }
                    }
                }
            };

            var page = request.Page ?? 1;
            var pageSize = request.PageSize ?? 10;
            var totalCount = dummyParties.Count;
            var pagedParties = dummyParties.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new PaginatedResult<PartyDocument>
            {
                Values = pagedParties,
                TotalCount = totalCount
            };

            return Ok(result);

            /* ORIGINAL CODE - Uncomment to restore:
            var query = new GetPartiesQuery
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                LastUpdatedDate = request.LastUpdatedDate,
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 10,
                Order = request.Order,
                Sort = request.Sort
            };

            var result = await _executor.ExecuteQuery(query);
            return Ok(result);
            */
        }

        /// <summary>
        /// Retrieve details of a party.
        /// </summary>
        /// <remarks>
        /// The endpoint allows to retrieve details of a given party, including their communication details, correspondence address, and roles.
        /// </remarks>
        /// <param name="id">The unique identifier (UUID) of the party.</param>
        /// <response code="200">OK - Successful</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="404">The requested resource was not found.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PartyDocument), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartyById(string id)
        {
            var result = await _executor.ExecuteQuery(new GetPartyByIdQuery(id));
            return Ok(result);
        }
    }
}