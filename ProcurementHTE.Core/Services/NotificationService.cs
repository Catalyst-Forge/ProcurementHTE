using Microsoft.Extensions.Logging;
using ProcurementHTE.Core.Interfaces;

namespace ProcurementHTE.Core.Services
{
    public partial class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly INotificationPusher _pusher;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repository,
            INotificationPusher pusher,
            IUserRepository userRepository,
            ILogger<NotificationService> logger
        )
        {
            _repository = repository;
            _pusher = pusher;
            _userRepository = userRepository;
            _logger = logger;
        }
    }
}
