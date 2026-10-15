using Renegade.Core;
using Xunit;

namespace Renegade.Core.Tests;

public class AdScheduleTests
{
	[Fact]
	public void AdShowsAfterEveryFifthGame()
	{
		var schedule = new AdSchedule();
		var due = new bool[12];
		for (int i = 0; i < due.Length; i++)
			due[i] = schedule.GameFinished();
		Assert.Equal(new[] { false, false, false, false, true, false, false, false, false, true, false, false }, due);
	}

	[Fact]
	public void AdCountSurvivesARestart()
	{
		var schedule = new AdSchedule(gamesSinceAd: 3);
		Assert.False(schedule.GameFinished());
		Assert.True(schedule.GameFinished());
	}
}
