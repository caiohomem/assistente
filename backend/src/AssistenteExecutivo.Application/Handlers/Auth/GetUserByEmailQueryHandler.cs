using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Application.Queries.Auth;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Application.Handlers.Auth;

public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, UserProfile?>
{
    private readonly IApplicationDbContext _db;

    public GetUserByEmailQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfile?> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return null;

        try
        {
            var email = EmailAddress.Create(request.Email);
            
            var userProfile = await _db.UserProfiles
                .FirstOrDefaultAsync(
                    u => u.Email.Value == email.Value,
                    cancellationToken);

            return userProfile;
        }
        catch
        {
            // Se EmailAddress.Create falhar ou qualquer outro erro, retornar null
            return null;
        }
    }
}



