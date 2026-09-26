using TestTask.Models;

namespace TestTask.Interfaces
{
    public interface ITestService
    {
        Task<ResponseModel> Process(RequestModel request);
    }
}
