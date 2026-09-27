using TestJob.Models;

namespace TestJob.Services;

public interface IDevSecService
{
    Task<DevSecResult> ProcessAsync(RequestDto requestDto, CancellationToken cancellationToken);
}
