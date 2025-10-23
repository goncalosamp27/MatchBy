using Bunit;
using MatchBy.Client.Pages;

namespace MatchBy.UnitTests;

public class CounterTests
{
    [Fact]
    public void OnCounterClick_ThenClickCounterButton_ShouldIncrementByOne()
    {
        //Arrange
        using var ctx = new BunitContext();
        IRenderedComponent<Counter> cut = ctx.Render<Counter>();
        cut.Find("p").MarkupMatches("<p role='status'>Current count: 0</p>");
        
        //Act
        cut.Find("button").Click();
        
        //Assert
        cut.Find("p").MarkupMatches("<p role='status'>Current count: 1</p>");
    }
}
