# auxua.OpenProject

[![Version](https://img.shields.io/github/v/tag/auxua/OpenProjectClient?sort=semver)](https://github.com/auxua/OpenProjectClient/tags)
[![License](https://img.shields.io/badge/license-MIT-green)](./LICENSE)
![.NET](https://img.shields.io/badge/.NET-Standard2.0-purple?logo=dotnet)
[![NuGet](https://img.shields.io/nuget/v/auxua.OpenProjectClient?color=blue&logo=nuget)](https://www.nuget.org/packages/auxua.OpenProjectClient/)]

## OpenProject Client

To connect to an OpenProject instance and interact with its REST API, you can use this package.

It is based on the v3 API of OpenProject and .Net Standard 2.0.

## Usage

### Step 1: Configuration

Key to usage is the Base Adress and an API Key. You can get the API Key from your OpenProject profile settings.
```csharp
var config = new BaseConfig()
{
    PersonalAccessToken = Settings.ApiKey,
    BaseUrl = Settings.ApiBaseUrl
};



```

### Step 2: Creating and Using Client

```csharp
var client = new auxua.OpenProject.OpenProjectClient(config);
```

### Step 3: Example Usage

For example, to get a list of projects:
```csharp

```

Or getting the according work packages:
```csharp
```

For more details of work packages, you may want to create the facade, which catches additional information in the background:
```csharp
```

## Current Status

table...


## Issues and Contributions

This project is open for issues and contributions. Feel free to open issues on GitHub or submit pull requests.

## License and Usage

This project is licensed under the MIT License. See the [LICENSE](./LICENSE) file for details.
