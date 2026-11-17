using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Voluntro.BuildingBlocks.Infrastructure.Persistence;
using Voluntro.SharedKernel;

namespace Voluntro.BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Models the audit interceptor can't stamp correctly fail the save with a message that names the problem. The
/// interceptor runs before EF connects, so no database is needed.
/// </summary>
public sealed class AuditInterceptorModelTests
{
    // A comment belongs to its post, and replies point at their parent comment: two ways up, so no single root.
    [Fact]
    public void A_child_with_two_owning_navigations_fails_the_save_naming_both()
    {
        var services = new ServiceCollection();
        services.AddSingleton(NpgsqlDataSource.Create("Host=never-connected"));
        services.AddModuleDbContext<ThreadDbContext>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ThreadDbContext>();

        var post = Post.Create();
        post.AddComment();
        db.Add(post);

        var exception = Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Contains("more than one owning navigation", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Post.Comments", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Comment.Replies", exception.Message, StringComparison.Ordinal);
    }

    private readonly record struct PostId(Guid Value) : IEntityId<PostId>
    {
        public static PostId New() => new(Guid.CreateVersion7());

        public static PostId From(Guid value) => new(value);
    }

    private readonly record struct CommentId(Guid Value) : IEntityId<CommentId>
    {
        public static CommentId New() => new(Guid.CreateVersion7());

        public static CommentId From(Guid value) => new(value);
    }

    private sealed class Post : AggregateRoot<PostId>
    {
        private readonly List<Comment> _comments = [];

        private Post(PostId id)
            : base(id)
        {
        }

        public IReadOnlyList<Comment> Comments => _comments.AsReadOnly();

        public static Post Create() => new(PostId.New());

        public void AddComment() => _comments.Add(new Comment(CommentId.New(), Id));
    }

    private sealed class Comment : Entity<CommentId>
    {
        private readonly List<Comment> _replies = [];

        public Comment(CommentId id, PostId postId)
            : base(id) => PostId = postId;

        public PostId PostId { get; private set; }

        public CommentId? ParentId { get; private set; }

        public IReadOnlyList<Comment> Replies => _replies.AsReadOnly();
    }

    private sealed class ThreadDbContext(DbContextOptions<ThreadDbContext> options) : ModuleDbContext(options)
    {
        public DbSet<Post> Posts => Set<Post>();

        protected override string Schema => "thread";

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>().HasMany(post => post.Comments).WithOne().HasForeignKey(comment => comment.PostId);
            modelBuilder.Entity<Comment>().HasMany(comment => comment.Replies).WithOne().HasForeignKey(reply => reply.ParentId);
        }
    }
}
