using AssistenteExecutivo.Application.Commands.Capture;
using AssistenteExecutivo.Application.Interfaces;
using AssistenteExecutivo.Domain.Entities;
using AssistenteExecutivo.Domain.Enums;
using AssistenteExecutivo.Domain.Interfaces;
using AssistenteExecutivo.Domain.ValueObjects;
using MediatR;

namespace AssistenteExecutivo.Application.Handlers.Capture;

public class UploadCardCommandHandler : IRequestHandler<UploadCardCommand, UploadCardCommandResult>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly ICaptureJobRepository _captureJobRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IOcrProvider _ocrProvider;
    private readonly IFileStore _fileStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public UploadCardCommandHandler(
        IMediaAssetRepository mediaAssetRepository,
        ICaptureJobRepository captureJobRepository,
        IContactRepository contactRepository,
        IOcrProvider ocrProvider,
        IFileStore fileStore,
        IUnitOfWork unitOfWork,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _captureJobRepository = captureJobRepository;
        _contactRepository = contactRepository;
        _ocrProvider = ocrProvider;
        _fileStore = fileStore;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<UploadCardCommandResult> Handle(UploadCardCommand request, CancellationToken cancellationToken)
    {
        // 1. Store the file and create MediaAsset
        var storageKey = await _fileStore.StoreAsync(request.ImageBytes, request.FileName, request.MimeType, cancellationToken);
        var hash = await _fileStore.ComputeHashAsync(request.ImageBytes, cancellationToken);
        var mediaRef = MediaRef.Create(storageKey, hash, request.MimeType, request.ImageBytes.Length);

        var mediaId = _idGenerator.NewGuid();
        var mediaAsset = new MediaAsset(mediaId, request.OwnerUserId, mediaRef, MediaKind.Image, _clock);
        await _mediaAssetRepository.AddAsync(mediaAsset, cancellationToken);

        // 2. Create CaptureJob
        var jobId = _idGenerator.NewGuid();
        var captureJob = CaptureJob.RequestCardScan(jobId, request.OwnerUserId, mediaId, _clock);
        await _captureJobRepository.AddAsync(captureJob, cancellationToken);

        // 3. Process OCR
        captureJob.MarkProcessing(_clock);
        await _captureJobRepository.UpdateAsync(captureJob, cancellationToken);

        OcrExtract ocrResult;
        try
        {
            ocrResult = await _ocrProvider.ExtractFieldsAsync(request.ImageBytes, request.MimeType, cancellationToken);
            captureJob.CompleteCardScan(ocrResult, _clock);
        }
        catch (Exception ex)
        {
            captureJob.Fail("OCR_ERROR", ex.Message, _clock);
            await _captureJobRepository.UpdateAsync(captureJob, cancellationToken);
            throw;
        }

        await _captureJobRepository.UpdateAsync(captureJob, cancellationToken);

        // 4. Create or update Contact
        Contact? contact = null;

        // Try to find existing contact by email or phone
        if (!string.IsNullOrWhiteSpace(ocrResult.Email))
        {
            contact = await _contactRepository.GetByEmailAsync(ocrResult.Email, request.OwnerUserId, cancellationToken);
        }

        if (contact == null && !string.IsNullOrWhiteSpace(ocrResult.Phone))
        {
            contact = await _contactRepository.GetByPhoneAsync(ocrResult.Phone, request.OwnerUserId, cancellationToken);
        }

        if (contact == null)
        {
            // Create new contact
            var contactId = _idGenerator.NewGuid();
            contact = Contact.CreateFromCardExtract(contactId, request.OwnerUserId, ocrResult, _clock);
            await _contactRepository.AddAsync(contact, cancellationToken);
        }
        else
        {
            // Update existing contact with OCR data
            if (!string.IsNullOrWhiteSpace(ocrResult.Name))
            {
                var nameParts = ocrResult.Name.Split(' ', 2);
                var firstName = nameParts[0];
                var lastName = nameParts.Length > 1 ? nameParts[1] : null;
                var personName = PersonName.Create(firstName, lastName);
                contact.UpdateDetails(name: personName);
            }

            if (!string.IsNullOrWhiteSpace(ocrResult.JobTitle))
            {
                contact.UpdateDetails(jobTitle: ocrResult.JobTitle);
            }

            if (!string.IsNullOrWhiteSpace(ocrResult.Company))
            {
                contact.UpdateDetails(company: ocrResult.Company);
            }

            if (!string.IsNullOrWhiteSpace(ocrResult.Email))
            {
                var email = EmailAddress.Create(ocrResult.Email);
                if (!contact.Emails.Any(e => e == email))
                {
                    contact.AddEmail(email);
                }
            }

            if (!string.IsNullOrWhiteSpace(ocrResult.Phone))
            {
                var phone = PhoneNumber.Create(ocrResult.Phone);
                if (!contact.Phones.Any(p => p == phone))
                {
                    contact.AddPhone(phone);
                }
            }

            await _contactRepository.UpdateAsync(contact, cancellationToken);
        }

        // 5. Save all changes to database
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadCardCommandResult
        {
            ContactId = contact.ContactId,
            JobId = jobId,
            MediaId = mediaId
        };
    }
}

