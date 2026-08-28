using KeeperData.Api.Controllers.RequestDtos.UserAccounts;
using KeeperData.Application;
using KeeperData.Application.Commands.UserAccounts;
using KeeperData.Application.Queries.UserAccounts;
using KeeperData.Core.DTOs;
using KeeperData.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KeeperData.Api.Controllers
{
    /// <summary>
    /// Operations related to user accounts and their SAM mastered CPH associations.
    /// </summary>
    [Authorize(Policy = "BasicOrBearer")]
    [ApiController]
    [Route("api/v2/user-accounts")]
    [ApiExplorerSettings(GroupName = "public")]
    [Produces("application/json")]
    [Tags("user-accounts")]
    public class UserAccountsController(IRequestExecutor executor, IReadModelSqliteCacheService readModelCache) : ControllerBase
    {
        private readonly IRequestExecutor _executor = executor;

        /// <summary>
        /// Ensure a user account exists for the supplied identity provider claims.
        /// </summary>
        /// <remarks>
        /// Called on every successful logon. Resolves the account by subject, otherwise adopts an account
        /// which matches on email and has no subject bound yet (the subject is stamped once and never
        /// overwritten), otherwise creates a new account. Profile fields are overwritten from the claims and
        /// the CPH association graph is rebuilt from the SAM mastered read model. An empty association result
        /// is valid and empties the snapshot, but a cold read model cache returns 503 and leaves the stored
        /// snapshot untouched.
        /// </remarks>
        /// <param name="request">The identity provider claims.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="200">OK - The existing account was refreshed.</response>
        /// <response code="201">Created - A new account was created.</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="409">The supplied email is already associated with a different account.</response>
        /// <response code="422">The request body failed validation.</response>
        /// <response code="503">The SAM read model cache is not yet available, so associations cannot be refreshed.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpPost]
        [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EnsureUserAccount([FromBody] EnsureUserAccountRequest request, CancellationToken cancellationToken)
        {
            if (!readModelCache.IsLoaded)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "CPH associations are not yet available. The SQLite read model cache is still loading."
                });
            }

            var command = new EnsureUserAccountCommand(
                Subject: request.Sub ?? string.Empty,
                Email: request.Email ?? string.Empty,
                GivenName: request.GivenName ?? string.Empty,
                FamilyName: request.FamilyName ?? string.Empty);

            var result = await _executor.ExecuteCommand(command, cancellationToken);

            if (!result.Created)
            {
                return Ok(result.Account);
            }

            return Created($"/api/v2/user-accounts/{Uri.EscapeDataString(result.Account.Subject!)}", result.Account);
        }

        /// <summary>
        /// Retrieve a user account by identity provider subject.
        /// </summary>
        /// <remarks>
        /// Read only session lookup. The CPH associations returned are those captured by the most recent
        /// ensure call; no master data refresh is performed.
        /// </remarks>
        /// <param name="subject">The identity provider subject claim, percent encoded in the path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <response code="200">OK - The user account.</response>
        /// <response code="401">Access token is not set or invalid.</response>
        /// <response code="403">The requestor is not authorized to perform this operation on the resource.</response>
        /// <response code="404">The subject is not recognised.</response>
        /// <response code="422">The subject failed validation.</response>
        /// <response code="500">The server encountered an unexpected error</response>
        [HttpGet("{subject}")]
        [ProducesResponseType(typeof(UserAccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserAccountBySubject([FromRoute] string subject, CancellationToken cancellationToken)
        {
            var result = await _executor.ExecuteQuery(new GetUserAccountBySubjectQuery(subject), cancellationToken);

            return Ok(result);
        }
    }
}