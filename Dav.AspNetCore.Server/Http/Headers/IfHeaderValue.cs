using System.Diagnostics.CodeAnalysis;

namespace Dav.AspNetCore.Server.Http.Headers;

public class IfHeaderValue
{
    /// <summary>
    /// Initializes a new <see cref="IfHeaderValue"/> class.
    /// </summary>
    /// <param name="resourceConditions">The resource conditions.</param>
    public IfHeaderValue(IEnumerable<IfHeaderValueCondition> resourceConditions)
    {
        ArgumentNullException.ThrowIfNull(resourceConditions, nameof(resourceConditions));
        Conditions = new List<IfHeaderValueCondition>(resourceConditions).AsReadOnly();
    }
    
    /// <summary>
    /// Gets the resource conditions.
    /// </summary>
    public IReadOnlyCollection<IfHeaderValueCondition> Conditions { get; }

    /// <summary>
    /// Parses the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The if header value.</returns>
    /// <exception cref="FormatException"></exception>
    public static IfHeaderValue Parse(string input)
    {
        if (TryParse(input, out var parsedValue))
            return parsedValue;

        throw new FormatException("The If header could not be parsed.");
    }

    /// <summary>
    /// Try to parse the input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="parsedValue">The if header value.</param>
    /// <returns>True on success, otherwise false.</returns>
    public static bool TryParse(string? input, [NotNullWhen(true)] out IfHeaderValue? parsedValue)
    {
        parsedValue = null;
        
        if (string.IsNullOrWhiteSpace(input))
            return false;
        
        // we either start with a tagged list or an untagged list
        // the untagged list will always refer to the requested resource while
        // an tagged list specifies arbitrary resources to match.

        var currentResourceTag = string.Empty;
        var resources = new List<ResourceTag>();
        var conditions = new List<Condition>();
        var stateTokens = new List<IfHeaderValueStateToken>();
        var tags = new List<IfHeaderValueEntityTag>();

        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == ' ')
                continue;
            
            if (input[i] == '<')
            {
                var closingIndex = input.IndexOf('>', i);
                if (closingIndex < 0)
                    return false;
                
                var resourceTag = input.Substring(i + 1, closingIndex - i - 1);

                if (!string.IsNullOrWhiteSpace(currentResourceTag))
                {
                    resources.Add(new ResourceTag(currentResourceTag, conditions.ToArray()));
                    conditions.Clear();
                }

                currentResourceTag = resourceTag;

                i = closingIndex;
                continue;
            }

            if (input[i] == '(')
            {
                var closingIndex = input.IndexOf(')', i);
                if (closingIndex < 0)
                    return false;
                
                var conditionList = input.Substring(i + 1, closingIndex - i - 1);

                var negate = false;
                for (var j = 0; j < conditionList.Length; j++)
                {
                    if (conditionList[j] == ' ')
                        continue;

                    // state token
                    if (conditionList[j] == '<')
                    {
                        var closeStateTokenIndex = conditionList.IndexOf('>', j);
                        if (closeStateTokenIndex < 0)
                            return false;
                        
                        var stateToken = conditionList.Substring(j + 1, closeStateTokenIndex - j - 1);
                        
                        stateTokens.Add(new IfHeaderValueStateToken(stateToken, negate));

                        negate = false;
                        j = closeStateTokenIndex;
                        continue;
                    }

                    // not
                    if (conditionList[j] == 'N')
                    {
                        if (conditionList.Length < j + 3)
                            return false;

                        if (!conditionList.Substring(j, 3).Equals("NOT", StringComparison.InvariantCultureIgnoreCase))
                            return false;

                        negate = true;
                        
                        j += 3;
                        continue;
                    }
                    
                    // etag
                    if (conditionList[j] == '[')
                    {
                        var closingEtagIndex = conditionList.IndexOf(']', j);
                        if (closingEtagIndex < 0)
                            return false;
                        
                        var etag = conditionList.Substring(j + 1, closingEtagIndex - j - 1);
                        if (!etag.EndsWith("\"") || (!etag.StartsWith("\"") && !etag.StartsWith("W/\"")))
                            return false;
                        
                        var isWeak = etag.StartsWith("W/");
                        var etagValue = isWeak ? etag.Substring(3, etag.Length - 4) : etag.Substring(1, etag.Length - 2);
                        
                        tags.Add(new IfHeaderValueEntityTag(etagValue, isWeak, negate));
                        
                        negate = false;
                        j = closingEtagIndex;
                        continue;
                    }

                    return false;
                }

                conditions.Add(new Condition(
                    stateTokens.ToArray(),
                    tags.ToArray()));
                
                stateTokens.Clear();
                tags.Clear();
                
                i = closingIndex;
                continue;
            }

            return false;
        }

        if (resources.All(x => x.Name != currentResourceTag))
            resources.Add(new ResourceTag(currentResourceTag, conditions.ToArray()));

        var resourceConditions = new List<IfHeaderValueCondition>();
        foreach (var resourceTag in resources)
        foreach (var condition in resourceTag.Conditions)
            resourceConditions.Add(new IfHeaderValueCondition(string.IsNullOrWhiteSpace(resourceTag.Name) ? null : new Uri(resourceTag.Name.TrimEnd('/')), condition.Tokens, condition.Tags));

        parsedValue = new IfHeaderValue(resourceConditions);
        return true;
    }

    private record ResourceTag(string Name, Condition[] Conditions);

    private record Condition(IfHeaderValueStateToken[] Tokens, IfHeaderValueEntityTag[] Tags);
}