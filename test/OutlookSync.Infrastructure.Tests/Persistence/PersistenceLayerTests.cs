using Microsoft.EntityFrameworkCore;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;
using OutlookSync.Domain.ValueObjects;
using OutlookSync.Infrastructure.Persistence;
using OutlookSync.Infrastructure.Repositories;

namespace OutlookSync.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests for the persistence layer including DbContext, Repository, and Unit of Work
/// </summary>
public class PersistenceLayerTests : IDisposable
{
    private readonly OutlookSyncDbContext _context;
    private readonly IRepository<Credential> _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PersistenceLayerTests()
    {
        var options = new DbContextOptionsBuilder<OutlookSyncDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new OutlookSyncDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _credentialRepository = new Repository<Credential>(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task AddCredential_WithStatusData_ShouldPersist()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Test Account" };
        var statusData = "test_status_data"u8.ToArray();
        credential.UpdateStatusData(statusData);

        // Act
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        var savedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.NotNull(savedCredential);
        Assert.Equal(TokenStatus.Valid, savedCredential.TokenStatus);
        Assert.NotNull(savedCredential.StatusData);
        Assert.Equal(statusData, savedCredential.StatusData);
    }

    [Fact]
    public async Task UpdateCredentialStatus_ShouldPersist()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Test Account" };
        credential.UpdateStatusData("test_data"u8.ToArray());
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();

        // Act
        credential.MarkTokenAsExpired();
        await _credentialRepository.UpdateAsync(credential);
        await _unitOfWork.SaveChangesAsync();

        // Clear context to force reload from DB
        _context.ChangeTracker.Clear();

        // Assert
        var updatedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.NotNull(updatedCredential);
        Assert.Equal(TokenStatus.Expired, updatedCredential.TokenStatus);
        Assert.NotNull(updatedCredential.UpdatedAt);
    }

    [Fact]
    public async Task UnitOfWork_Transaction_ShouldCommit()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Test Account" };
        credential.UpdateStatusData("test_data"u8.ToArray());

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitTransactionAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.NotNull(savedCredential);
    }

    [Fact]
    public async Task UnitOfWork_Transaction_ShouldRollback()
    {
        // Arrange
        var credential = new Credential { FriendlyName = "Test Account" };
        credential.UpdateStatusData("test_data"u8.ToArray());

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _credentialRepository.AddAsync(credential);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.RollbackTransactionAsync();

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var savedCredential = await _credentialRepository.GetByIdAsync(credential.Id);
        Assert.Null(savedCredential);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
