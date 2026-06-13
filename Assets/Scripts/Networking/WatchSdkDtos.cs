using System;

[Serializable]
public class AuthenticatedUserDto
{
    public string userId;
    public string email;
    public string displayName;
}

[Serializable]
public class AuthRequestDto
{
    public string email;
    public string password;
    public string displayName;
}

[Serializable]
public class RefreshRequestDto
{
    public string refreshToken;
}

[Serializable]
public class AuthResponseDto
{
    public string accessToken;
    public string accessTokenExpiresOn;
    public string refreshToken;
    public string refreshTokenExpiresOn;
    public AuthenticatedUserDto user;
}

[Serializable]
public class GamePerformanceUploadDto
{
    public double accuracy;
    public int averageTimeSeconds;
    public MonthlyGamePerformanceDto[] monthlyPerformance;
    public string lastUpdated;
}

[Serializable]
public class MonthlyGamePerformanceDto
{
    public string month;
    public double accuracy;
    public int timeSeconds;
}

[Serializable]
public class StoredSessionDto
{
    public string userId;
    public string email;
    public string displayName;
    public string accessToken;
    public string accessTokenExpiresOn;
    public string refreshToken;
    public string refreshTokenExpiresOn;
}

public class WatchSdkApiException : Exception
{
    public int? StatusCode { get; }

    public WatchSdkApiException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
