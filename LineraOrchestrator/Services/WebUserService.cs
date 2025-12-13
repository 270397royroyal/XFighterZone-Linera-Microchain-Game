// WebUserService.cs
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LineraOrchestrator.Models;
using LineraOrchestrator.Services;

public class WebUserService
{
    private readonly HttpClient _httpClient;
    private readonly LineraConfig _config;

    public WebUserService(HttpClient httpClient, LineraConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> PlaceBetAsync(
        string chainId,
        string appId,
        string walletPath,
        string matchId,
        string player,
        ulong amount)
    {
        if (!File.Exists(walletPath))
            throw new Exception($"Wallet not found: {walletPath}");

        // 1. Đọc public key từ wallet
        var publicKey = UserService.GetPublicKeyFromWallet(walletPath);
        Console.WriteLine($"[WEB-BET-SERVICE] ChainId: {chainId}");
        Console.WriteLine($"[WEB-BET-SERVICE] AppId: {appId}");
        Console.WriteLine($"[WEB-BET-SERVICE] PublicKey: {publicKey}");

        // 2. Chuẩn bị GraphQL mutation theo đúng format của Linera
        // Linera sử dụng URL format: /chains/{chainId}/applications/{appId}
        var graphqlPayload = new
        {
            query = @"
                mutation PlaceBet($userChain: String!, $userPublicKey: String!, $matchId: String!, $player: String!, $amount: Int!) {
                    placeBet(
                        user_chain: $userChain
                        user_public_key: $userPublicKey
                        match_id: $matchId
                        player: $player
                        amount: $amount
                    )
                }",
            variables = new
            {
                userChain = chainId,
                userPublicKey = publicKey,
                matchId = matchId,
                player = player,
                amount = (int)amount
            }
        };

        var jsonPayload = JsonSerializer.Serialize(graphqlPayload);
        Console.WriteLine($"[WEB-BET-SERVICE] GraphQL: {jsonPayload}");

        // 3. Gửi đến Linera Node Service ở port 8080
        // Format đúng: http://localhost:8080/chains/{chainId}/applications/{appId}
        var url = $"http://localhost:8080/chains/{chainId}/applications/{appId}";
        Console.WriteLine($"[WEB-BET-SERVICE] URL: {url}");

        try
        {
            var response = await _httpClient.PostAsync(
                url,
                new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[WEB-BET-SERVICE] Response: {result}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"HTTP {response.StatusCode}: {result}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WEB-BET-SERVICE-ERROR] {ex.Message}");
            throw;
        }
    }

}