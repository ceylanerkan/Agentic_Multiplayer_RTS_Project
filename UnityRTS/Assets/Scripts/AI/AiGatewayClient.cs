using System;
using System.Text;
using System.Threading.Tasks;
using RTS.Core.Rules;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Talks to the Python gateway. Only this class holds the session token; it is never shown to the player or the LLM.
/// Errors are returned as text so that AiCommanderPanel can show them instead of crashing the game.
/// </summary>
public class AiGatewayClient
{
    readonly string baseUrl;
    readonly int timeoutSeconds;
    string token;

    public AiGatewayClient(string baseUrl = "http://127.0.0.1:8765", int timeoutSeconds = 40)
    {
        this.baseUrl = baseUrl.TrimEnd('/');
        this.timeoutSeconds = timeoutSeconds;
    }

    public async Task<string> CreateSession()
    {
        var (ok, body) = await Send("/session", "{}", false);
        if (!ok) return body;
        token = Newtonsoft.Json.Linq.JObject.Parse(body)["token"].ToString();
        return null; // null = success
    }

    /// <summary>Returns (answer, error). Exactly one of them is null.</summary>
    public async Task<(AiAnswer answer, string error)> Ask(AiSafeSnapshot snapshot, string question)
    {
        string payload = Newtonsoft.Json.JsonConvert.SerializeObject(new { snapshot, question });
        return await Call("/ask", payload);
    }

    public Task<(AiAnswer answer, string error)> AskWhy()
    {
        return Call("/why", "{}");
    }

    async Task<(AiAnswer, string)> Call(string path, string json)
    {
        if (token == null)
        {
            string err = await CreateSession();
            if (err != null) return (null, err);
        }
        var (ok, body) = await Send(path, json, true);
        if (!ok) return (null, body);
        try { return (AiAnswer.Parse(body), null); }
        catch (Exception e) { return (null, "Bad answer from AI gateway: " + e.Message); }
    }

    async Task<(bool, string)> Send(string path, string json, bool withToken)
    {
        using (var req = new UnityWebRequest(baseUrl + path, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (withToken) req.SetRequestHeader("X-Session-Token", token);
            req.timeout = timeoutSeconds;
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
                return (false, "AI gateway error: " + req.error + " " + req.downloadHandler.text);
            return (true, req.downloadHandler.text);
        }
    }
}
