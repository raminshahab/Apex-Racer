using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

public class NakamaAuthTest : MonoBehaviour
{
    private IClient _client;
    private ISession _session;

    // called when the GameObject is created
    private async void Awake()
    {
        // 1. Create client that points to your Docker Nakama
        // scheme: "http" (no SSL locally)
        // host: "127.0.0.1" or "localhost"
        // port: 7350
        // serverKey: "defaultkey" (Nakama default)
        _client = new Client("http", "127.0.0.1", 7350, "defaultkey");

        // 2. Authenticate with a device ID (creates a user if not exists)
        var deviceId = SystemInfo.deviceUniqueIdentifier;
        var username = "racer_" + UnityEngine.Random.Range(1000, 9999);

        try
        {
            Debug.Log($"Authenticating with deviceId={deviceId}, username={username}");

            _session = await _client.AuthenticateDeviceAsync(
                deviceId,
                username: username,
                create: true
            );

            Debug.Log("Auth success!");
            Debug.Log("UserId: " + _session.UserId);
            Debug.Log("Username: " + _session.Username);
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"Nakama API error: {e.StatusCode} - {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError("Unexpected error: " + e);
        }
    }
}
