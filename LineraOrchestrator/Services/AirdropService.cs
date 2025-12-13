// AirdropService.cs
using System.Text;
using System.Text.Json;
using LineraOrchestrator.Models;

namespace LineraOrchestrator.Services
{
    public class AirdropService
    {
        private readonly LineraConfig _config;
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        private readonly LineraOrchestratorService _orchestrator;

        public AirdropService(LineraConfig config, HttpClient httpClient, UserService userService, LineraOrchestratorService orchestrator)
        {
            _config = config;
            _httpClient = httpClient;
            _userService = userService;
            _orchestrator = orchestrator;
        }

        #region User Claim Aidrop
        // User gởi request để nhận airdrop lần đầu
        public async Task<string> UserRequestInitialClaimAsync(string userName)
        {
            var userChainId = _userService.GetUserChainIdFromLocal(userName);
            var userAppId = _userService.GetUserAppId(userName);
            var userPort = _userService.GetUserPortFromStorage(userName);

            var userDir = Path.Combine(_config.UserChainPath!, userName);
            var walletPath = Path.Combine(userDir, "wallet.json");
            var userPublicKey = UserService.GetPublicKeyFromWallet(walletPath);

            var url = $"http://localhost:{userPort}/chains/{userChainId}/applications/{userAppId}";

            var graphql = $@"mutation {{
                requestInitialClaim(
                    userChain: ""{userChainId}""
                    userPublicKey: ""{userPublicKey}""
                )
            }}";

            var payload = new { query = graphql };
            var body = JsonSerializer.Serialize(payload, JsonOptions.Write);

            using var resp = await _orchestrator.PostSingleWithServiceWaitAsync(
                url,
                () => new StringContent(body, Encoding.UTF8, "application/json"),
                waitSeconds: 8,
                postTimeoutSeconds: 20);

            var text = await resp.Content.ReadAsStringAsync();
            return text;
        }
        #endregion

        #region Tournament Airdrop Service
        // Thiết lập airdrop ban đầu
        public async Task<string> SetAirdropAmountAsync(ulong amount)
        {
            var url = $"http://localhost:8080/chains/{_config.PublisherChainId}/applications/{_config.TournamentAppId}";

            var graphql = $@"mutation {{
                    setAirdropAmount(amount: {amount})
                }}";

            var payload = new { query = graphql };
            var body = JsonSerializer.Serialize(payload, JsonOptions.Write);

            using var resp = await _orchestrator.PostSingleWithServiceWaitAsync(
                url,
                () => new StringContent(body, Encoding.UTF8, "application/json"),
                waitSeconds: 8,
                postTimeoutSeconds: 20);

            var text = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();
            return text;
        }

        // Kiểm tra các yêu cầu nhận airdrop khi user gởi yêu cầu
        public async Task<string> GetPendingClaimsAsync()
        {

            while (!await _orchestrator.WaitForServiceViaMonitorAsync(timeoutSeconds: 10, pollMs: 500, stableMs: 1000))
            {
                Console.WriteLine("[TOURNAMENT] Waiting for Linera service to stabilize before get data...");
                await Task.Delay(500);
            }
            var url = $"http://localhost:8080/chains/{_config.PublisherChainId}/applications/{_config.TournamentAppId}";

            var graphql = @"query {
                pendingClaims {
                    userKey
                    userChain
                    userPublicKey
                    requestedAt
                }
            }";

            var payload = new { query = graphql };
            var body = JsonSerializer.Serialize(payload, JsonOptions.Write);

            using var resp = await _orchestrator.PostSingleWithServiceWaitAsync(
                url,
                () => new StringContent(body, Encoding.UTF8, "application/json"),
                waitSeconds: 8,
                postTimeoutSeconds: 20);

            var text = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();
            return text;
        }

        // xử lý các yêu cầu nhận airdrop ban đầu từ user
        public async Task<string> ProcessPendingClaimsAsync()
        {
            var url = $"http://localhost:8080/chains/{_config.PublisherChainId}/applications/{_config.TournamentAppId}";

            // Vì không có limit → không có () luôn
            var graphql = @"mutation {
                processPendingClaims
            }";

            var payload = new { query = graphql };
            var body = JsonSerializer.Serialize(payload, JsonOptions.Write);

            using var resp = await _orchestrator.PostSingleWithServiceWaitAsync(
                url,
                () => new StringContent(body, Encoding.UTF8, "application/json"),
                waitSeconds: 8,
                postTimeoutSeconds: 20);

            var text = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();
            return text;
        }


        // Kiểm tra thông tin airdrop
        public async Task<string> GetAirdropInfoAsync()
        {
            var url = $"http://localhost:8080/chains/{_config.PublisherChainId}/applications/{_config.TournamentAppId}";

            var graphql = @"query {
                airdropInfo {
                    amount
                }
            }";

            var payload = new { query = graphql };
            var body = JsonSerializer.Serialize(payload, JsonOptions.Write);

            using var resp = await _orchestrator.PostSingleWithServiceWaitAsync(
                url,
                () => new StringContent(body, Encoding.UTF8, "application/json"),
                waitSeconds: 8,
                postTimeoutSeconds: 20);

            var text = await resp.Content.ReadAsStringAsync();
            resp.EnsureSuccessStatusCode();
            return text;
        }
        #endregion
    }
}