using Jobbr.ComponentModel.Execution.Model;

namespace Jobbr.Server.ForkedExecution.BackChannel.Model
{
    public class JobRunUpdateDto
    {
        public JobRunStates State { get; set; }
    }
}