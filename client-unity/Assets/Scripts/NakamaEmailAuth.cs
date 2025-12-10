/*
 * Apex-Racer - Racing event simulator using Unity and Nakama
 * Copyright (C) 2024  Ramin Shahab
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

public class NakamaEmailAuth : MonoBehaviour
{
    private IClient _client;
    private ISession _session;

    [Header("Test Credentials")]
    public string email = "testuser@example.com";
    public string password = "Password123!";
    public string username = "test_racer12";

    private void Awake()
    {
        _client = new Client("http", "127.0.0.1", 7350, "defaultkey");
    }

    // Call this from a UI button to "register" (email + password)
    public async void Register()
    {
        try
        {
            Debug.Log($"Registering email={email}, username={username}");

            _session = await _client.AuthenticateEmailAsync(
                email,
                password,
                username: username,
                create: true // create if not exists
            );

            Debug.Log("Registration success! UserId: " + _session.UserId);
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"Registration failed: {e.StatusCode} - {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError("Unexpected error: " + e);
        }
    }

    // Call this from a UI button to "login" existing user
    public async void Login()
    {
        try
        {
            Debug.Log($"Logging in with email={email}");

            _session = await _client.AuthenticateEmailAsync(
                email,
                password,
                create: false // don't auto-create; must already exist
            );

            Debug.Log("Login success! UserId: " + _session.UserId);
        }
        catch (ApiResponseException e)
        {
            Debug.LogError($"Login failed: {e.StatusCode} - {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError("Unexpected error: " + e);
        }
    }
}
