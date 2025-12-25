using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.ValueObjects;
using AssistenteExecutivo.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AssistenteExecutivo.Infrastructure.Tests.Repositories;

public class CaptureJobRepositoryTests : RepositoryTestBase
{
    private readonly ICaptureJobRepository _repository;

    public CaptureJobRepositoryTests()
    {
        _repository = new CaptureJobRepository(Context);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingJob_ShouldReturnJob()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var job = new CaptureJob(jobId, ownerUserId, JobType.CardScan, mediaId, null, Clock);
        
        await Context.CaptureJobs.AddAsync(job);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(jobId, ownerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.JobId.Should().Be(jobId);
        result.OwnerUserId.Should().Be(ownerUserId);
        result.Type.Should().Be(JobType.CardScan);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentJob_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(jobId, ownerUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DifferentOwner_ShouldReturnNull()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var job = new CaptureJob(Guid.NewGuid(), ownerUserId1, JobType.CardScan, mediaId, null, Clock);
        
        await Context.CaptureJobs.AddAsync(job);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(job.JobId, ownerUserId2);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByOwnerUserIdAsync_ShouldReturnAllJobsForOwner()
    {
        // Arrange
        var ownerUserId1 = Guid.NewGuid();
        var ownerUserId2 = Guid.NewGuid();
        var mediaId1 = Guid.NewGuid();
        var mediaId2 = Guid.NewGuid();
        var mediaId3 = Guid.NewGuid();
        
        var job1 = new CaptureJob(Guid.NewGuid(), ownerUserId1, JobType.CardScan, mediaId1, null, Clock);
        var job2 = new CaptureJob(Guid.NewGuid(), ownerUserId1, JobType.AudioNoteTranscription, mediaId2, null, Clock);
        var job3 = new CaptureJob(Guid.NewGuid(), ownerUserId2, JobType.CardScan, mediaId3, null, Clock);
        
        await Context.CaptureJobs.AddRangeAsync(job1, job2, job3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetAllByOwnerUserIdAsync(ownerUserId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(j => j.JobId == job1.JobId);
        result.Should().Contain(j => j.JobId == job2.JobId);
        result.Should().NotContain(j => j.JobId == job3.JobId);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnJobsWithStatus()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaId1 = Guid.NewGuid();
        var mediaId2 = Guid.NewGuid();
        var mediaId3 = Guid.NewGuid();
        
        var job1 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.CardScan, mediaId1, null, Clock);
        var job2 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.AudioNoteTranscription, mediaId2, null, Clock);
        var job3 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.CardScan, mediaId3, null, Clock);
        
        job2.Fail("ERROR_CODE", "Error message", Clock);
        job3.Fail("ERROR_CODE", "Error message", Clock);
        
        await Context.CaptureJobs.AddRangeAsync(job1, job2, job3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(JobStatus.Requested, ownerUserId);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(j => j.JobId == job1.JobId);
    }

    [Fact]
    public async Task GetByContactIdAsync_ShouldReturnJobsForContact()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var mediaId1 = Guid.NewGuid();
        var mediaId2 = Guid.NewGuid();
        
        var job1 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.CardScan, mediaId1, contactId, Clock);
        var job2 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.AudioNoteTranscription, mediaId2, contactId, Clock);
        var job3 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.CardScan, Guid.NewGuid(), null, Clock);
        
        await Context.CaptureJobs.AddRangeAsync(job1, job2, job3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByContactIdAsync(contactId, ownerUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(j => j.JobId == job1.JobId);
        result.Should().Contain(j => j.JobId == job2.JobId);
        result.Should().NotContain(j => j.JobId == job3.JobId);
    }

    [Fact]
    public async Task GetByMediaIdAsync_ShouldReturnJobsForMedia()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        
        var job1 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.CardScan, mediaId, null, Clock);
        var job2 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.AudioNoteTranscription, mediaId, null, Clock);
        var job3 = new CaptureJob(Guid.NewGuid(), ownerUserId, JobType.CardScan, Guid.NewGuid(), null, Clock);
        
        await Context.CaptureJobs.AddRangeAsync(job1, job2, job3);
        await SaveChangesAsync();

        // Act
        var result = await _repository.GetByMediaIdAsync(mediaId, ownerUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(j => j.JobId == job1.JobId);
        result.Should().Contain(j => j.JobId == job2.JobId);
        result.Should().NotContain(j => j.JobId == job3.JobId);
    }

    [Fact]
    public async Task AddAsync_ShouldAddJob()
    {
        // Arrange
        var job = new CaptureJob(Guid.NewGuid(), Guid.NewGuid(), JobType.CardScan, Guid.NewGuid(), null, Clock);

        // Act
        await _repository.AddAsync(job);
        await SaveChangesAsync();

        // Assert
        var result = await Context.CaptureJobs.FindAsync(job.JobId);
        result.Should().NotBeNull();
        result!.Type.Should().Be(JobType.CardScan);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateJob()
    {
        // Arrange
        var job = new CaptureJob(Guid.NewGuid(), Guid.NewGuid(), JobType.CardScan, Guid.NewGuid(), null, Clock);
        await Context.CaptureJobs.AddAsync(job);
        await SaveChangesAsync();

        job.Fail("ERROR_CODE", "Error message", Clock);

        // Act
        await _repository.UpdateAsync(job);
        await SaveChangesAsync();

        // Assert
        var result = await Context.CaptureJobs.FindAsync(job.JobId);
        result!.Status.Should().Be(JobStatus.Failed);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_NewJobBeforeSave_ShouldStayAddedAndInsertWithChanges()
    {
        // Arrange - job ainda nao persistido
        var job = new CaptureJob(Guid.NewGuid(), Guid.NewGuid(), JobType.AudioNoteTranscription, Guid.NewGuid(), null, Clock);
        await _repository.AddAsync(job);

        // altera status antes do primeiro SaveChanges
        job.MarkProcessing(Clock);

        // Act
        await _repository.UpdateAsync(job);

        // Assert - estado permanece Added e persiste dados ao salvar
        Context.Entry(job).State.Should().Be(EntityState.Added);
        await SaveChangesAsync();

        var saved = await Context.CaptureJobs.FindAsync(job.JobId);
        saved.Should().NotBeNull();
        saved!.Status.Should().Be(JobStatus.Processing);
    }
}
