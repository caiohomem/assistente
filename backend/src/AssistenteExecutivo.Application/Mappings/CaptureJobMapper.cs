using AssistenteExecutivo.Application.DTOs;
using AssistenteExecutivo.Domain.Entities;

namespace AssistenteExecutivo.Application.Mappings;

public static class CaptureJobMapper
{
    public static CaptureJobDto MapToDto(CaptureJob job)
    {
        var dto = new CaptureJobDto
        {
            JobId = job.JobId,
            OwnerUserId = job.OwnerUserId,
            Type = job.Type,
            ContactId = job.ContactId,
            MediaId = job.MediaId,
            Status = job.Status,
            RequestedAt = job.RequestedAt,
            CompletedAt = job.CompletedAt,
            ErrorCode = job.ErrorCode,
            ErrorMessage = job.ErrorMessage,
            AudioSummary = job.AudioSummary
        };

        // Map CardScanResult
        if (job.CardScanResult != null)
        {
            dto.CardScanResult = new CardScanResultDto
            {
                RawText = job.CardScanResult.RawText,
                Name = job.CardScanResult.Name,
                Email = job.CardScanResult.Email,
                Phone = job.CardScanResult.Phone,
                Company = job.CardScanResult.Company,
                JobTitle = job.CardScanResult.JobTitle,
                ConfidenceScores = job.CardScanResult.ConfidenceScores
            };
        }

        // Map AudioTranscript
        if (job.AudioTranscript != null)
        {
            dto.AudioTranscript = new AudioTranscriptDto
            {
                Text = job.AudioTranscript.Text,
                Segments = job.AudioTranscript.Segments?.Select(s => new TranscriptSegmentDto
                {
                    Text = s.Text,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Confidence = s.Confidence
                }).ToList()
            };
        }

        // Map ExtractedTasks
        if (job.ExtractedTasks != null && job.ExtractedTasks.Any())
        {
            dto.ExtractedTasks = job.ExtractedTasks.Select(t => new ExtractedTaskDto
            {
                Description = t.Description,
                DueDate = t.DueDate,
                Priority = t.Priority
            }).ToList();
        }

        return dto;
    }
}


