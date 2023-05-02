using Dav.AspNetCore.Server.Http.Headers;
using Xunit;

namespace Dav.AspNetCore.Server.Tests.Http;

public class IfHeaderValueTest
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("http://www.example.com/specs/")]
    [InlineData("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>")]
    [InlineData("(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>")]
    [InlineData("(urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>)")]
    [InlineData("([b44s])")]
    public void TryParseInvalid(string? input)
    {
        // act
        var result = IfHeaderValue.TryParse(input, out _);

        // assert
        Assert.False(result);
    }
    
    [Fact]
    public void TryParseTaggedList()
    {
        // arrange
        var input = "<http://www.example.com/specs/> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.NotNull(condition.Uri);
        Assert.Equal("http://www.example.com/specs", condition.Uri.AbsoluteUri);
        Assert.Single(condition.Tokens);
        Assert.Empty(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseTaggedListMultiple()
    {
        // arrange
        var input = "<https://www.contoso.com/resource1> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>) <http://www.fabrikam.com/random> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(2, parsedValue.Conditions.Count);

        var condition1 = parsedValue.Conditions.First();
        Assert.NotNull(condition1.Uri);
        Assert.Equal("https://www.contoso.com/resource1", condition1.Uri.AbsoluteUri);
        Assert.Single(condition1.Tokens);
        Assert.Empty(condition1.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition1.Tokens[0].Value);
        Assert.False(condition1.Tokens[0].Negate);
        
        var condition2 = parsedValue.Conditions.Last();
        Assert.NotNull(condition2.Uri);
        Assert.Equal("http://www.fabrikam.com/random", condition2.Uri.AbsoluteUri);
        Assert.Single(condition2.Tokens);
        Assert.Empty(condition2.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition2.Tokens[0].Value);
        Assert.False(condition2.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseTaggedListNot()
    {
        // arrange
        var input = "<http://www.example.com/specs/> (Not <urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.NotNull(condition.Uri);
        Assert.Equal("http://www.example.com/specs", condition.Uri.AbsoluteUri);
        Assert.Single(condition.Tokens);
        Assert.Empty(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.True(condition.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseTaggedListOr()
    {
        // arrange
        var input = "<http://www.example.com/specs/> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>) (<urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(2, parsedValue.Conditions.Count);

        var condition1 = parsedValue.Conditions.First();
        Assert.NotNull(condition1.Uri);
        Assert.Equal("http://www.example.com/specs", condition1.Uri.AbsoluteUri);
        Assert.Single(condition1.Tokens);
        Assert.Empty(condition1.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition1.Tokens[0].Value);
        Assert.False(condition1.Tokens[0].Negate);
        
        var condition2 = parsedValue.Conditions.Last();
        Assert.NotNull(condition2.Uri);
        Assert.Equal("http://www.example.com/specs", condition2.Uri.AbsoluteUri);
        Assert.Single(condition2.Tokens);
        Assert.Empty(condition2.Tags);
        Assert.Equal("urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092", condition2.Tokens[0].Value);
        Assert.False(condition2.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseTaggedListAnd()
    {
        // arrange
        var input = "<http://www.example.com/specs/> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> <urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.NotNull(condition.Uri);
        Assert.Equal("http://www.example.com/specs", condition.Uri.AbsoluteUri);
        Assert.Equal(2, condition.Tokens.Length);
        Assert.Empty(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092", condition.Tokens[1].Value);
        Assert.False(condition.Tokens[1].Negate);
    }
    
    [Fact]
    public void TryParseTaggedListEtag()
    {
        // arrange
        var input = "<http://www.example.com/specs/> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> [\"I am an ETag\"])";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.NotNull(condition.Uri);
        Assert.Equal("http://www.example.com/specs", condition.Uri.AbsoluteUri);
        Assert.Single(condition.Tokens);
        Assert.Single(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("I am an ETag", condition.Tags[0].Value);
        Assert.False(condition.Tags[0].Negate);
        Assert.False(condition.Tags[0].IsWeak);
    }
    
    [Fact]
    public void TryParseTaggedListWeakEtag()
    {
        // arrange
        var input = "<http://www.example.com/specs/> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> [W/\"I am an ETag\"])";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.NotNull(condition.Uri);
        Assert.Equal("http://www.example.com/specs", condition.Uri.AbsoluteUri);
        Assert.Single(condition.Tokens);
        Assert.Single(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("I am an ETag", condition.Tags[0].Value);
        Assert.False(condition.Tags[0].Negate);
        Assert.True(condition.Tags[0].IsWeak);
    }
    
    [Fact]
    public void TryParseTaggedListNotWeakEtag()
    {
        // arrange
        var input = "<http://www.example.com/specs/> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> Not [W/\"I am an ETag\"])";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.NotNull(condition.Uri);
        Assert.Equal("http://www.example.com/specs", condition.Uri.AbsoluteUri);
        Assert.Single(condition.Tokens);
        Assert.Single(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("I am an ETag", condition.Tags[0].Value);
        Assert.True(condition.Tags[0].Negate);
        Assert.True(condition.Tags[0].IsWeak);
    }

    [Fact]
    public void TryParseTaggedListMixed()
    {
        // arrange
        var input = "<https://www.contoso.com/resource1> (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> [W/\"A weak ETag\"]) ([\"strong ETag\"]) <http://www.fabrikam.com/random> ([\"another strong ETag\"])";
        
        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(3, parsedValue.Conditions.Count);

        var conditions = parsedValue.Conditions.ToList();
        
        var condition1 = conditions[0];
        Assert.NotNull(condition1.Uri);
        Assert.Equal("https://www.contoso.com/resource1", condition1.Uri.AbsoluteUri);
        Assert.Single(condition1.Tokens);
        Assert.Single(condition1.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition1.Tokens[0].Value);
        Assert.False(condition1.Tokens[0].Negate);
        Assert.Equal("A weak ETag", condition1.Tags[0].Value);
        Assert.False(condition1.Tags[0].Negate);
        Assert.True(condition1.Tags[0].IsWeak);
        
        var condition2 = conditions[1];
        Assert.NotNull(condition2.Uri);
        Assert.Equal("https://www.contoso.com/resource1", condition2.Uri.AbsoluteUri);
        Assert.Empty(condition2.Tokens);
        Assert.Single(condition2.Tags);
        Assert.Equal("strong ETag", condition2.Tags[0].Value);
        Assert.False(condition2.Tags[0].Negate);
        Assert.False(condition2.Tags[0].IsWeak);
        
        var condition3 = conditions[2];
        Assert.NotNull(condition3.Uri);
        Assert.Equal("http://www.fabrikam.com/random", condition3.Uri.AbsoluteUri);
        Assert.Empty(condition3.Tokens);
        Assert.Single(condition3.Tags);
        Assert.Equal("another strong ETag", condition3.Tags[0].Value);
        Assert.False(condition3.Tags[0].Negate);
        Assert.False(condition3.Tags[0].IsWeak);
    }
    
    [Fact]
    public void TryParseUnTaggedList()
    {
        // arrange
        var input = "(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.Null(condition.Uri);
        Assert.Single(condition.Tokens);
        Assert.Empty(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseUnTaggedListMultiple()
    {
        // arrange
        var input = "(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>) (<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(2, parsedValue.Conditions.Count);

        var condition1 = parsedValue.Conditions.First();
        Assert.Null(condition1.Uri);
        Assert.Single(condition1.Tokens);
        Assert.Empty(condition1.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition1.Tokens[0].Value);
        Assert.False(condition1.Tokens[0].Negate);
        
        var condition2 = parsedValue.Conditions.Last();
        Assert.Null(condition2.Uri);
        Assert.Single(condition2.Tokens);
        Assert.Empty(condition2.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition2.Tokens[0].Value);
        Assert.False(condition2.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseUnTaggedListNot()
    {
        // arrange
        var input = "(Not <urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.Null(condition.Uri);
        Assert.Single(condition.Tokens);
        Assert.Empty(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.True(condition.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseUnTaggedListOr()
    {
        // arrange
        var input = "(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2>) (<urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(2, parsedValue.Conditions.Count);

        var condition1 = parsedValue.Conditions.First();
        Assert.Null(condition1.Uri);
        Assert.Single(condition1.Tokens);
        Assert.Empty(condition1.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition1.Tokens[0].Value);
        Assert.False(condition1.Tokens[0].Negate);
        
        var condition2 = parsedValue.Conditions.Last();
        Assert.Null(condition2.Uri);
        Assert.Single(condition2.Tokens);
        Assert.Empty(condition2.Tags);
        Assert.Equal("urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092", condition2.Tokens[0].Value);
        Assert.False(condition2.Tokens[0].Negate);
    }
    
    [Fact]
    public void TryParseUnTaggedListAnd()
    {
        // arrange
        var input = "(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> <urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092>)";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.Null(condition.Uri);
        Assert.Equal(2, condition.Tokens.Length);
        Assert.Empty(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("urn:uuid:58f202ac-22cf-11d1-b12d-002035b29092", condition.Tokens[1].Value);
        Assert.False(condition.Tokens[1].Negate);
    }
    
    [Fact]
    public void TryParseUnTaggedListEtag()
    {
        // arrange
        var input = "(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> [\"I am an ETag\"])";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.Null(condition.Uri);
        Assert.Single(condition.Tokens);
        Assert.Single(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("I am an ETag", condition.Tags[0].Value);
        Assert.False(condition.Tags[0].Negate);
        Assert.False(condition.Tags[0].IsWeak);
    }
    
    [Fact]
    public void TryParseUnTaggedListWeakEtag()
    {
        // arrange
        var input = "(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> [W/\"I am an ETag\"])";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.Null(condition.Uri);
        Assert.Single(condition.Tokens);
        Assert.Single(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("I am an ETag", condition.Tags[0].Value);
        Assert.False(condition.Tags[0].Negate);
        Assert.True(condition.Tags[0].IsWeak);
    }
    
    [Fact]
    public void TryParseUnTaggedListNotWeakEtag()
    {
        // arrange
        var input = "(<urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2> Not [W/\"I am an ETag\"])";

        // act
        var result = IfHeaderValue.TryParse(input, out var parsedValue);

        // assert
        Assert.True(result);
        Assert.NotNull(parsedValue);
        Assert.Equal(1, parsedValue.Conditions.Count);

        var condition = parsedValue.Conditions.First();
        Assert.Null(condition.Uri);
        Assert.Single(condition.Tokens);
        Assert.Single(condition.Tags);
        Assert.Equal("urn:uuid:181d4fae-7d8c-11d0-a765-00a0c91e6bf2", condition.Tokens[0].Value);
        Assert.False(condition.Tokens[0].Negate);
        Assert.Equal("I am an ETag", condition.Tags[0].Value);
        Assert.True(condition.Tags[0].Negate);
        Assert.True(condition.Tags[0].IsWeak);
    }
}