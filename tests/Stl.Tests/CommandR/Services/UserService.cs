using Stl.Fusion.EntityFramework;
using Stl.RegisterAttributes;

namespace Stl.Tests.CommandR.Services;

[RegisterService, RegisterCommandHandlers]
public class UserService : DbServiceBase<TestDbContext>
{
    public UserService(IServiceProvider services) : base(services) { }

    [CommandHandler]
    private async Task RecAddUsers(
        RecAddUsersCommand command,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        CommandContext.GetCurrent().Should().Be(context);
        context.ExecutionState.Handlers.Count.Should().Be(6);

        var dbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _1 = dbContext.ConfigureAwait(false);

        var anotherDbContext = await CreateCommandDbContext(cancellationToken).ConfigureAwait(false);
        await using var _2 = anotherDbContext.ConfigureAwait(false);

        dbContext.Should().NotBeNull();
        anotherDbContext.Should().NotBeNull();
        anotherDbContext.Should().NotBe(dbContext);

        Log.LogInformation("User count: {UserCount}", command.Users.Length);
        if (command.Users.Length == 0)
            return;

        await Services.Commander().Call(
                new RecAddUsersCommand() { Users = command.Users[1..] },
                cancellationToken)
            .ConfigureAwait(false);

        var user = command.Users[0];
        if (string.IsNullOrEmpty(user.Id))
            throw new InvalidOperationException("User.Id must be set.");
        await dbContext.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
