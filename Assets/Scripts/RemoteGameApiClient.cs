using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RemoteCardsResponse
{
    public RemoteMapCardDefinition[] cards;
}

[Serializable]
public class RemoteMapCardDefinition
{
    public string cardId;
    public string displayName;
    public string description;
    public bool startsUnlocked;
    public string biomeId;
    public string floorColorHex;
    public string wallColorHex;
    public int segmentWidth;
    public int segmentHeight;
    public int entryX;
    public int exitX;
    public float obstacleChance;
    public float enemyChance;
    public string[] enemyIds;
}

[Serializable]
public class RemotePlayerProgressResponse
{
    public string playerId;
    public RemotePlayerProgress progress;
}

[Serializable]
public class RemotePlayerProgressUpsertRequest
{
    public RemotePlayerProgress progress;
}

[Serializable]
public class RemotePlayerProgress
{
    public string[] unlockedCardIds = Array.Empty<string>();
    public int completedRuns;
    public int failedRuns;
    public int totalRunsStarted;
    public int totalCardsUnlocked;

    public LocalPlayerProgress ToLocalProgress()
    {
        return new LocalPlayerProgress
        {
            unlockedCardIds = new System.Collections.Generic.List<string>(unlockedCardIds ?? Array.Empty<string>()),
            completedRuns = completedRuns,
            failedRuns = failedRuns,
            totalRunsStarted = totalRunsStarted,
            totalCardsUnlocked = totalCardsUnlocked
        };
    }

    public static RemotePlayerProgress FromLocal(LocalPlayerProgress progress)
    {
        return new RemotePlayerProgress
        {
            unlockedCardIds = progress?.unlockedCardIds?.ToArray() ?? Array.Empty<string>(),
            completedRuns = progress?.completedRuns ?? 0,
            failedRuns = progress?.failedRuns ?? 0,
            totalRunsStarted = progress?.totalRunsStarted ?? 0,
            totalCardsUnlocked = progress?.totalCardsUnlocked ?? 0
        };
    }
}

public sealed class RemoteGameApiClient
{
    private readonly string baseUrl;
    private readonly int timeoutSeconds;

    public RemoteGameApiClient(string baseUrl, int timeoutSeconds)
    {
        this.baseUrl = baseUrl.TrimEnd('/');
        this.timeoutSeconds = Mathf.Max(1, timeoutSeconds);
    }

    public IEnumerator FetchCards(Action<RemoteCardsResponse> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/api/cards";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildErrorMessage(request));
            request.Dispose();
            yield break;
        }

        RemoteCardsResponse response = JsonUtility.FromJson<RemoteCardsResponse>(request.downloadHandler.text);
        onSuccess?.Invoke(response);
        request.Dispose();
    }

    public IEnumerator FetchPlayerProgress(string playerId, Action<RemotePlayerProgressResponse> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/api/players/{UnityWebRequest.EscapeURL(playerId)}/progress";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

        if (request.responseCode == 404)
        {
            onSuccess?.Invoke(null);
            request.Dispose();
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildErrorMessage(request));
            request.Dispose();
            yield break;
        }

        RemotePlayerProgressResponse response = JsonUtility.FromJson<RemotePlayerProgressResponse>(request.downloadHandler.text);
        onSuccess?.Invoke(response);
        request.Dispose();
    }

    public IEnumerator UpsertPlayerProgress(
        string playerId,
        LocalPlayerProgress progress,
        Action<RemotePlayerProgressResponse> onSuccess,
        Action<string> onError)
    {
        string url = $"{baseUrl}/api/players/{UnityWebRequest.EscapeURL(playerId)}/progress";
        RemotePlayerProgressUpsertRequest payload = new()
        {
            progress = RemotePlayerProgress.FromLocal(progress)
        };

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = timeoutSeconds
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(BuildErrorMessage(request));
            request.Dispose();
            yield break;
        }

        RemotePlayerProgressResponse response = JsonUtility.FromJson<RemotePlayerProgressResponse>(request.downloadHandler.text);
        onSuccess?.Invoke(response);
        request.Dispose();
    }

    private static string BuildErrorMessage(UnityWebRequest request)
    {
        string error = string.IsNullOrWhiteSpace(request.error) ? "Error desconegut" : request.error;
        return $"HTTP {(long)request.responseCode}: {error}";
    }
}
