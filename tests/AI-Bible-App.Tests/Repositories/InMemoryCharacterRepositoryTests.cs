using AI_Bible_App.Infrastructure.Repositories;

namespace AI_Bible_App.Tests.Repositories;

public class InMemoryCharacterRepositoryTests
{
    [Fact]
    public async Task GetAllCharactersAsync_ShouldReturnDavidAndPaul()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var characters = await repository.GetAllCharactersAsync();

        // Assert
        Assert.NotNull(characters);
        Assert.Equal(2, characters.Count);
        Assert.Contains(characters, c => c.Id == "david");
        Assert.Contains(characters, c => c.Id == "paul");
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldReturnDavid()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("david");

        // Assert
        Assert.NotNull(character);
        Assert.Equal("david", character.Id);
        Assert.Equal("David", character.Name);
        Assert.Contains("King", character.Title);
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldReturnPaul()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("paul");

        // Assert
        Assert.NotNull(character);
        Assert.Equal("paul", character.Id);
        Assert.Equal("Paul (Saul of Tarsus)", character.Name);
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldReturnNullForUnknownId()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("unknown");

        // Assert
        Assert.Null(character);
    }

    [Fact]
    public async Task GetCharacterAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var repository = new InMemoryCharacterRepository();

        // Act
        var character = await repository.GetCharacterAsync("DAVID");

        // Assert
        Assert.NotNull(character);
        Assert.Equal("david", character.Id);
    }
}
