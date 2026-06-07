using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProcurementHTE.Core.Interfaces;
using ProcurementHTE.Core.Models;

namespace ProcurementHTE.Core.Services;

public partial class AccountService : IAccountService
{
    private const string PasswordResetPurpose = "PasswordReset";

    private readonly UserManager<User> _userManager;
    private readonly IObjectStorage _objectStorage;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IUserSecurityLogRepository _logRepository;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        UserManager<User> userManager,
        IObjectStorage objectStorage,
        IOptions<ObjectStorageOptions> storageOptions,
        IUserSessionRepository sessionRepository,
        IUserSecurityLogRepository logRepository,
        IEmailSender emailSender,
        ISmsSender smsSender,
        ILogger<AccountService> logger
    )
    {
        _userManager = userManager;
        _objectStorage = objectStorage ?? throw new ArgumentNullException(nameof(objectStorage));
        _storageOptions =
            storageOptions?.Value ?? throw new ArgumentNullException(nameof(storageOptions));
        _sessionRepository = sessionRepository;
        _logRepository = logRepository;
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _smsSender = smsSender ?? throw new ArgumentNullException(nameof(smsSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_storageOptions.Bucket))
        {
            throw new ArgumentException(
                "Object storage bucket belum dikonfigurasi.",
                nameof(storageOptions)
            );
        }
    }
}
