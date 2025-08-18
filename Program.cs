using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

var builder = WebApplication.CreateBuilder(args);

// Configure HTTP client for external GraphQL API
builder.Services.AddHttpClient();

// Register GraphQL client for Rick and Morty API
builder.Services.AddSingleton(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var options = new GraphQLHttpClientOptions
    {
        EndPoint = new Uri("https://rickandmortyapi.com/graphql")
    };
    return new GraphQLHttpClient(options, new NewtonsoftJsonSerializer(), httpClient);
});

// Register dynamic field mapper
builder.Services.AddSingleton<IFieldMapper>(provider =>
{
    var mapper = new DynamicFieldMapper();
    mapper.LoadMappingConfiguration("field-mappings.json");
    return mapper;
});

// Register our services
builder.Services.AddScoped<IRickAndMortyService, RickAndMortyService>();

// Configure GraphQL server with our transformed schema
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();

app.MapGraphQL();
app.Run();

// ===== Our Exposed Schema (Transformed) =====

public class Query
{
    public async Task<Person?> GetPersonAsync(int id, [Service] IRickAndMortyService service)
        => await service.GetPersonAsync(id);

    public async Task<IEnumerable<Person>> GetPersonsAsync(string? name, [Service] IRickAndMortyService service)
        => await service.GetPersonsAsync(name);

    public async Task<World?> GetWorldAsync(int id, [Service] IRickAndMortyService service)
        => await service.GetWorldAsync(id);

    public async Task<IEnumerable<StoryArc>> GetStoryArcsAsync(string? title, [Service] IRickAndMortyService service)
        => await service.GetStoryArcsAsync(title);
}

// ===== Our Transformed Schema Models =====

public class Person
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public LifeStatus LifeStatus { get; set; }
    public string Race { get; set; } = string.Empty;
    public string PersonalityType { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public World? HomeWorld { get; set; }
    public World? CurrentLocation { get; set; }
    public string ProfileImageUrl { get; set; } = string.Empty;
    public bool IsMainCharacter { get; set; }
    public DateTime FirstAppearance { get; set; }
    public IEnumerable<StoryArc> StoryArcs { get; set; } = Array.Empty<StoryArc>();
}

public class World
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Reality { get; set; } = string.Empty;
    public int InhabitantCount { get; set; }
    public DateTime Created { get; set; }
}

public class StoryArc
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public string Season { get; set; } = string.Empty;
    public string EpisodeCode { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
}

public enum LifeStatus
{
    Alive,
    Dead,
    Unknown
}

public enum Gender
{
    Male,
    Female,
    Genderless,
    Unknown
}

// ===== Service Interface and Implementation =====

public interface IRickAndMortyService
{
    Task<Person?> GetPersonAsync(int id);
    Task<IEnumerable<Person>> GetPersonsAsync(string? name);
    Task<World?> GetWorldAsync(int id);
    Task<IEnumerable<StoryArc>> GetStoryArcsAsync(string? title);
}

public class RickAndMortyService : IRickAndMortyService
{
    private readonly GraphQLHttpClient _client;
    private readonly IFieldMapper _fieldMapper;

    public RickAndMortyService(GraphQLHttpClient client, IFieldMapper fieldMapper)
    {
        _client = client;
        _fieldMapper = fieldMapper;
    }

    public async Task<Person?> GetPersonAsync(int id)
    {
        var query = @"
            query GetCharacter($id: ID!) {
                character(id: $id) {
                    id
                    name
                    status
                    species
                    type
                    gender
                    origin {
                        id
                        name
                        type
                        dimension
                        created
                    }
                    location {
                        id
                        name
                        type
                        dimension
                        created
                    }
                    image
                    created
                    episode {
                        id
                        name
                        air_date
                        episode
                        created
                    }
                }
            }";

        var request = new GraphQL.GraphQLRequest
        {
            Query = query,
            Variables = new { id = id.ToString() }
        };

        try
        {
            var response = await _client.SendQueryAsync<dynamic>(request);
            if (response.Data?.character == null) return null;

            var character = response.Data.character;
            return _fieldMapper.MapEntity<Person>(character, "character_to_person");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Person>> GetPersonsAsync(string? name)
    {
        var query = @"
            query GetCharacters($name: String) {
                characters(filter: { name: $name }) {
                    results {
                        id
                        name
                        status
                        species
                        type
                        gender
                        origin {
                            id
                            name
                            type
                            dimension
                            created
                        }
                        location {
                            id
                            name
                            type
                            dimension
                            created
                        }
                        image
                        created
                        episode {
                            id
                            name
                            air_date
                            episode
                            created
                        }
                    }
                }
            }";

        var request = new GraphQL.GraphQLRequest
        {
            Query = query,
            Variables = name != null ? new { name } : null
        };

        try
        {
            var response = await _client.SendQueryAsync<dynamic>(request);
            if (response.Data?.characters?.results == null) return Array.Empty<Person>();

            var persons = new List<Person>();
            foreach (var character in response.Data.characters.results)
            {
                persons.Add(_fieldMapper.MapEntity<Person>(character, "character_to_person"));
            }
            return persons;
        }
        catch
        {
            return Array.Empty<Person>();
        }
    }

    public async Task<World?> GetWorldAsync(int id)
    {
        var query = @"
            query GetLocation($id: ID!) {
                location(id: $id) {
                    id
                    name
                    type
                    dimension
                    created
                    residents {
                        id
                    }
                }
            }";

        var request = new GraphQL.GraphQLRequest
        {
            Query = query,
            Variables = new { id = id.ToString() }
        };

        try
        {
            var response = await _client.SendQueryAsync<dynamic>(request);
            if (response.Data?.location == null) return null;

            var location = response.Data.location;
            return _fieldMapper.MapEntity<World>(location, "location_to_world");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<StoryArc>> GetStoryArcsAsync(string? title)
    {
        var query = @"
            query GetEpisodes($name: String) {
                episodes(filter: { name: $name }) {
                    results {
                        id
                        name
                        air_date
                        episode
                        created
                        characters {
                            id
                        }
                    }
                }
            }";

        var request = new GraphQL.GraphQLRequest
        {
            Query = query,
            Variables = title != null ? new { name = title } : null
        };

        try
        {
            var response = await _client.SendQueryAsync<dynamic>(request);
            if (response.Data?.episodes?.results == null) return Array.Empty<StoryArc>();

            var storyArcs = new List<StoryArc>();
            foreach (var episode in response.Data.episodes.results)
            {
                storyArcs.Add(_fieldMapper.MapEntity<StoryArc>(episode, "episode_to_storyarc"));
            }
            return storyArcs;
        }
        catch
        {
            return Array.Empty<StoryArc>();
        }
    }

    // ===== Dynamic Mapping (Configuration-Driven) =====
    // All field mappings are now handled by the DynamicFieldMapper
    // configured through field-mappings.json
}
