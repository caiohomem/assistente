using AssistenteExecutivo.Domain.Exceptions;
using AssistenteExecutivo.Domain.Notifications;
using FluentAssertions;

namespace AssistenteExecutivo.Domain.Tests.Notifications;

public class EmailOutboxMessageTests
{
    [Fact]
    public void Constructor_ValidData_ShouldCreate()
    {
        // Arrange
        var recipientEmail = "test@example.com";
        var recipientName = "John Doe";
        var subject = "Test Subject";
        var htmlBody = "<html><body>Test</body></html>";
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new EmailOutboxMessage(recipientEmail, recipientName, subject, htmlBody, createdAt);

        // Assert
        message.RecipientEmail.Should().Be(recipientEmail);
        message.RecipientName.Should().Be(recipientName);
        message.Subject.Should().Be(subject);
        message.HtmlBody.Should().Be(htmlBody);
        message.Status.Should().Be(EmailOutboxStatus.Pending);
        message.RetryCount.Should().Be(0);
        message.SentAt.Should().BeNull();
        message.FailedAt.Should().BeNull();
        message.ErrorMessage.Should().BeNull();
        message.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithoutRecipientName_ShouldCreate()
    {
        // Arrange
        var recipientEmail = "test@example.com";
        var subject = "Test Subject";
        var htmlBody = "<html><body>Test</body></html>";
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new EmailOutboxMessage(recipientEmail, null!, subject, htmlBody, createdAt);

        // Assert
        message.RecipientName.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmailWithWhitespace_ShouldTrim()
    {
        // Arrange
        var recipientEmail = "  test@example.com  ";
        var subject = "Test Subject";
        var htmlBody = "<html><body>Test</body></html>";
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new EmailOutboxMessage(recipientEmail, "Name", subject, htmlBody, createdAt);

        // Assert
        message.RecipientEmail.Should().Be("test@example.com");
    }

    [Fact]
    public void Constructor_RecipientNameWithWhitespace_ShouldTrim()
    {
        // Arrange
        var recipientEmail = "test@example.com";
        var recipientName = "  John Doe  ";
        var subject = "Test Subject";
        var htmlBody = "<html><body>Test</body></html>";
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new EmailOutboxMessage(recipientEmail, recipientName, subject, htmlBody, createdAt);

        // Assert
        message.RecipientName.Should().Be("John Doe");
    }

    [Fact]
    public void Constructor_SubjectWithWhitespace_ShouldTrim()
    {
        // Arrange
        var recipientEmail = "test@example.com";
        var subject = "  Test Subject  ";
        var htmlBody = "<html><body>Test</body></html>";
        var createdAt = DateTime.UtcNow;

        // Act
        var message = new EmailOutboxMessage(recipientEmail, "Name", subject, htmlBody, createdAt);

        // Assert
        message.Subject.Should().Be("Test Subject");
    }

    [Fact]
    public void Constructor_EmptyRecipientEmail_ShouldThrow()
    {
        // Act & Assert
        var act = () => new EmailOutboxMessage("", "Name", "Subject", "Body", DateTime.UtcNow);
        act.Should().Throw<DomainException>()
            .WithMessage("*Email do destinatário é obrigatório*");
    }

    [Fact]
    public void Constructor_EmptySubject_ShouldThrow()
    {
        // Act & Assert
        var act = () => new EmailOutboxMessage("test@example.com", "Name", "", "Body", DateTime.UtcNow);
        act.Should().Throw<DomainException>()
            .WithMessage("*Assunto do email é obrigatório*");
    }

    [Fact]
    public void Constructor_EmptyHtmlBody_ShouldThrow()
    {
        // Act & Assert
        var act = () => new EmailOutboxMessage("test@example.com", "Name", "Subject", "", DateTime.UtcNow);
        act.Should().Throw<DomainException>()
            .WithMessage("*Corpo HTML do email é obrigatório*");
    }

    [Fact]
    public void MarkAsSent_ShouldUpdateStatus()
    {
        // Arrange
        var message = CreateMessage();

        // Act
        message.MarkAsSent();

        // Assert
        message.Status.Should().Be(EmailOutboxStatus.Sent);
        message.SentAt.Should().NotBeNull();
        message.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatus()
    {
        // Arrange
        var message = CreateMessage();
        var errorMessage = "SMTP error";

        // Act
        message.MarkAsFailed(errorMessage);

        // Assert
        message.Status.Should().Be(EmailOutboxStatus.Failed);
        message.FailedAt.Should().NotBeNull();
        message.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void ScheduleRetry_ShouldUpdateStatusAndRetryCount()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsFailed("Error");
        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        message.ScheduleRetry(nextRetryAt);

        // Assert
        message.Status.Should().Be(EmailOutboxStatus.Pending);
        message.RetryCount.Should().Be(1);
        message.NextRetryAt.Should().Be(nextRetryAt);
        message.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ScheduleRetry_MultipleTimes_ShouldIncrementRetryCount()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsFailed("Error");
        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        message.ScheduleRetry(nextRetryAt);
        message.MarkAsFailed("Error again");
        message.ScheduleRetry(nextRetryAt.AddMinutes(5));

        // Assert
        message.RetryCount.Should().Be(2);
    }

    [Fact]
    public void ShouldRetry_FailedWithRetriesLeft_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsFailed("Error");

        // Act
        var result = message.ShouldRetry(maxRetries: 3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_FailedWithNoRetriesLeft_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsFailed("Error");
        message.ScheduleRetry(DateTime.UtcNow.AddMinutes(-1)); // Past retry time
        message.MarkAsFailed("Error");
        message.ScheduleRetry(DateTime.UtcNow.AddMinutes(-1));
        message.MarkAsFailed("Error");
        message.ScheduleRetry(DateTime.UtcNow.AddMinutes(-1));
        message.MarkAsFailed("Error"); // Now at max retries

        // Act
        var result = message.ShouldRetry(maxRetries: 3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_PendingStatus_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateMessage();

        // Act
        var result = message.ShouldRetry();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_SentStatus_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsSent();

        // Act
        var result = message.ShouldRetry();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_FutureRetryTime_ShouldReturnFalse()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsFailed("Error");
        message.ScheduleRetry(DateTime.UtcNow.AddHours(1)); // Future retry time

        // Act
        var result = message.ShouldRetry();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_PastRetryTime_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsFailed("Error");
        message.ScheduleRetry(DateTime.UtcNow.AddMinutes(-1)); // Past retry time
        // After ScheduleRetry, status is Pending, so mark as failed again
        message.MarkAsFailed("Error again");

        // Act
        var result = message.ShouldRetry();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_NullNextRetryAt_ShouldReturnTrue()
    {
        // Arrange
        var message = CreateMessage();
        message.MarkAsFailed("Error");
        // Don't schedule retry, so NextRetryAt is null

        // Act
        var result = message.ShouldRetry();

        // Assert
        result.Should().BeTrue();
    }

    private EmailOutboxMessage CreateMessage()
    {
        return new EmailOutboxMessage(
            "test@example.com",
            "John Doe",
            "Test Subject",
            "<html><body>Test</body></html>",
            DateTime.UtcNow);
    }
}

