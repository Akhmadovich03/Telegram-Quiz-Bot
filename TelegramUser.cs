namespace QuizBot;

internal class TelegramUser
{
	public long? Id { get; set; }
	public string? FirstName { get; set; }
	public string? UserName { get; set; }
	public string? TellNumber { get; set; }
	public int InformaticsAnswers { get; set; } = 0;
	public int HistoryAnswers { get; set; } = 0;
	public int ProgramingAnswers { get; set; } = 0;
	public int MothTongAnswers { get; set; } = 0;

	internal void SavingResult(string subjectName, int answerCounter)
	{
		switch(subjectName)
		{
			case nameof(InformaticsAnswers):
				if (InformaticsAnswers < answerCounter)
				{
					InformaticsAnswers = answerCounter;
				}
				break;
				
			case nameof(HistoryAnswers):
				if (HistoryAnswers < answerCounter)
				{
					HistoryAnswers = answerCounter;
				}
				break;
				
			case nameof(ProgramingAnswers):
				if (ProgramingAnswers < answerCounter)
				{
					ProgramingAnswers = answerCounter;
				}
				break;
			
			case nameof(MothTongAnswers):
				if (MothTongAnswers < answerCounter)
				{
					MothTongAnswers = answerCounter;
				}
				break;
		}
	}
}
