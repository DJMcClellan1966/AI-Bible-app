# AI Bible App

A C# console application that allows users to interact with biblical figures through AI-powered conversations and receive personalized daily prayers.

## Features

### 1. Biblical Character Chat System
- **Interactive Conversations**: Chat with biblical figures who respond in character
- **Available Characters**:
  - **David** - King of Israel, Psalmist, and Shepherd
  - **Paul** - Apostle to the Gentiles, Missionary, and Letter Writer
- **Extensible Design**: Easy to add more biblical characters
- **Character Authenticity**: Each character has a unique personality and speaking style based on biblical context
- **Conversation History**: Save and review past conversations

### 2. Daily Prayer Generation
- Generate personalized prayers powered by AI
- Request prayers for specific topics or needs
- Save prayer history for future reference
- View all saved prayers

### 3. Technical Architecture
- **Clean Architecture**: Separation of concerns with distinct layers
- **Projects**:
  - `AI-Bible-App.Core` - Domain models and interfaces
  - `AI-Bible-App.Infrastructure` - AI service integration and data access
  - `AI-Bible-App.Console` - Console application entry point
  - `AI-Bible-App.Tests` - Unit tests
- **AI Integration**: Uses Azure OpenAI API
- **Data Persistence**: JSON file-based storage for chat and prayer history

## Prerequisites

- .NET 8.0 SDK or later
- Azure OpenAI API access (or OpenAI API key)

## Setup Instructions

### 1. Clone the Repository
```bash
git clone https://github.com/DJMcClellan1966/AI-Bible-app.git
cd AI-Bible-app
```

### 2. Configure API Keys

Create a local configuration file (this file is ignored by git):

**Option A: Create `appsettings.local.json` in the Console project directory:**

```bash
cd src/AI-Bible-App.Console
```

Create `appsettings.local.json`:
```json
{
  "OpenAI": {
    "ApiKey": "your-azure-openai-api-key",
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

**Option B: Edit `appsettings.json` directly (not recommended for production):**

Update the values in `src/AI-Bible-App.Console/appsettings.json`:
```json
{
  "OpenAI": {
    "ApiKey": "your-azure-openai-api-key",
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "DeploymentName": "gpt-4"
  }
}
```

**Configuration Options:**
- `ApiKey`: Your Azure OpenAI API key
- `Endpoint`: Your Azure OpenAI endpoint URL
- `DeploymentName`: The name of your deployed model (e.g., "gpt-4", "gpt-35-turbo")

### 3. Build the Solution

```bash
dotnet build
```

### 4. Run the Application

```bash
cd src/AI-Bible-App.Console
dotnet run
```

Or from the root directory:
```bash
dotnet run --project src/AI-Bible-App.Console/AI-Bible-App.Console.csproj
```

## How to Use the App

### Main Menu Options

1. **Chat with a Biblical Character**
   - Select a character (David or Paul)
   - Start a conversation by typing your message
   - Type `save` to save the conversation session
   - Type `exit` to end the conversation

2. **Generate Daily Prayer**
   - Enter a specific topic or press Enter for a general daily prayer
   - Choose to save the prayer to your history

3. **View Prayer History**
   - Browse all your saved prayers with topics and timestamps

4. **View Chat History**
   - See all saved chat sessions with character names and message counts

### Example Conversation Starters

**With David:**
- "Tell me about your experience facing Goliath"
- "How did you handle being pursued by King Saul?"
- "What inspired you to write the psalms?"
- "Can you share wisdom about leadership?"

**With Paul:**
- "Tell me about your conversion on the Damascus road"
- "What was your experience planting churches?"
- "Can you explain grace and faith?"
- "What motivated you during your missionary journeys?"

## How to Add New Biblical Characters

### Step 1: Update the Character Repository

Edit `src/AI-Bible-App.Infrastructure/Repositories/InMemoryCharacterRepository.cs` and add a new character to the `_characters` list:

```csharp
new BiblicalCharacter
{
    Id = "moses",
    Name = "Moses",
    Title = "Prophet, Lawgiver, Leader of the Exodus",
    Description = "Led the Israelites out of Egyptian slavery and received the Ten Commandments",
    Era = "circa 1400 BC",
    BiblicalReferences = new List<string> 
    { 
        "Exodus",
        "Leviticus",
        "Numbers",
        "Deuteronomy"
    },
    SystemPrompt = @"You are Moses from the Bible. You led the Israelites out of Egypt...
    
[Add detailed character prompt here describing their personality, speaking style, and perspective]",
    Attributes = new Dictionary<string, string>
    {
        { "Personality", "Humble, Faithful, Leader" },
        { "KnownFor", "Ten Commandments, Exodus, Burning Bush" },
        { "KeyVirtues", "Obedience, Humility, Intercession" }
    }
}
```

### Step 2: Craft the System Prompt

The system prompt is crucial for character authenticity. Include:
- Character's background and biblical context
- Speaking style and personality traits
- Key experiences and teachings
- Biblical references they might mention
- Their perspective on God and faith

### Step 3: Test the Character

Build and run the application to test the new character's responses.

## Running Tests

```bash
dotnet test
```

This will run all unit tests in the `AI-Bible-App.Tests` project.

## Project Structure

```
AI-Bible-App/
├── src/
│   ├── AI-Bible-App.Core/           # Domain models and interfaces
│   │   ├── Models/
│   │   │   ├── BiblicalCharacter.cs
│   │   │   ├── ChatMessage.cs
│   │   │   ├── ChatSession.cs
│   │   │   └── Prayer.cs
│   │   └── Interfaces/
│   │       ├── IAIService.cs
│   │       ├── ICharacterRepository.cs
│   │       ├── IChatRepository.cs
│   │       └── IPrayerRepository.cs
│   │
│   ├── AI-Bible-App.Infrastructure/  # Implementation layer
│   │   ├── Services/
│   │   │   └── OpenAIService.cs
│   │   └── Repositories/
│   │       ├── InMemoryCharacterRepository.cs
│   │       ├── JsonChatRepository.cs
│   │       └── JsonPrayerRepository.cs
│   │
│   └── AI-Bible-App.Console/        # Application entry point
│       ├── Program.cs
│       ├── BibleApp.cs
│       └── appsettings.json
│
└── tests/
    └── AI-Bible-App.Tests/          # Unit tests
        ├── Models/
        └── Repositories/
```

## Data Storage

The application stores data locally in JSON files:

- **Chat History**: `data/chat_sessions.json`
- **Prayer History**: `data/prayers.json`

These files are created automatically when you save conversations or prayers.

## Error Handling

The application includes comprehensive error handling:
- API connection errors are logged and displayed to users
- Invalid inputs are validated
- Rate limiting is respected
- All errors are logged for debugging

## Logging

The application uses Microsoft.Extensions.Logging for structured logging:
- Console logging is enabled by default
- Log level can be configured in `appsettings.json`
- Errors are logged with full stack traces

## API Configuration Notes

### Azure OpenAI
- Requires an Azure subscription
- Create an Azure OpenAI resource
- Deploy a model (GPT-4 or GPT-3.5-Turbo recommended)
- Use the endpoint and API key in configuration

### Rate Limits
- Be mindful of API rate limits
- The application limits conversation history to the last 10 messages to manage token usage
- Consider implementing retry logic for production use

## Future Enhancements

Potential features for future development:
- Web UI (Blazor)
- Desktop UI (WPF/MAUI)
- Bible verse lookup integration
- Multi-language support
- Audio prayers (text-to-speech)
- Prayer request sharing
- More biblical characters (Moses, Peter, Mary, etc.)
- Conversation themes and guided discussions
- Export conversations to PDF

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## License

This project is licensed under the MIT License.

## Acknowledgments

- Biblical character personalities and teachings are based on biblical texts
- AI responses are generated using Azure OpenAI
- This is an educational and spiritual growth tool
