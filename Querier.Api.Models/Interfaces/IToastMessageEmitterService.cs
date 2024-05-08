using Querier.Api.Models.Notifications.MQMessages;

namespace Querier.Api.Models.Interfaces
{
    public interface IToastMessageEmitterService
    {
        void PublishToast(ToastMessage message);
    }
}