namespace UI.SharedComponents.Components
{
	public record class ExecutableTask
	{
		Func<Task> Action { get; init; }
        // Func<TItem, (bool, string)> ResultConverter { get; init; }

		public ExecutableTask(
			Func<Task> action,
			// Func<TItem, (bool, string)> resultConverter,
			string description
		)
		{
			Action = action;
			Description = description;
			// ResultConverter = resultConverter;
		}

		public string Description { get; init; }

        public bool IsSuccess { get; private set; }
		public string Message { get; private set; }
		public Status Status { get; private set; }


		public async Task Execute()
		{
			try
			{                
                Status = Status.InProgress;				
				// (IsSuccess, Message) = ResultConverter(await Action());				
			}
			catch { }
			finally
			{
				Status = Status.Done;
			}
		}
	}

	public enum Status
	{
		Queued,
		InProgress,
		Done
	}
}

