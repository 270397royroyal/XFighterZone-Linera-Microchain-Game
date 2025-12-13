// LineraController.cs
using System.Diagnostics;
using System.Text.Json;
using LineraOrchestrator.Models;
using LineraOrchestrator.Services;
using Microsoft.AspNetCore.Mvc;

namespace LineraOrchestrator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LineraController : ControllerBase
    {
        private readonly LineraOrchestratorService _orchestratorSvc;
        private readonly LineraCliRunner _cliRunner;

        private readonly MatchChainService _matchchainSvc;
        private readonly LeaderboardService _leaderboardSvc;
        private readonly TournamentService _tournamentSvc;
        private readonly UserService _userSvc;

        private readonly FriendService _friendSvc; // User - Friend contracts
        private readonly BettingService _bettingSvc; // Tournament - Fungible - User contracts
        private readonly AirdropService _airdropSvc;
        // Khởi tạo Controller với các services
        public LineraController(
            LineraOrchestratorService orchestratorSvc,
            LineraCliRunner cliRunner,
            MatchChainService matchchainSvc,
            LeaderboardService leaderboardSvc,
            TournamentService tournamentSvc,
            UserService userSvc,
            FriendService friendSvc,
            BettingService bettingSvc,
            AirdropService airdropSvc)
        {
            _orchestratorSvc = orchestratorSvc;
            _matchchainSvc = matchchainSvc;
            _leaderboardSvc = leaderboardSvc;
            _tournamentSvc = tournamentSvc;
            _userSvc = userSvc;
            _cliRunner = cliRunner;
            _friendSvc = friendSvc;
            _bettingSvc = bettingSvc;
            _airdropSvc = airdropSvc;
        }

        #region Linera setup, config, service
        [HttpPost("start-linera-node")]
        // API để khởi động Linera Node
        public async Task<IActionResult> StartLineraNode()
        {
            try
            {
                var config = await _orchestratorSvc.StartLineraNodeAsync();
                // Xác định chế độ hiện tại
                var mode = config.UseRemoteTestnet ? "Conway (Remote Testnet)" : "Local Net Backup";

                return Ok(new
                {
                    success = true,
                    message = $"Linera Node succesfully start with {mode} and environment variables setted.",
                    linera_wallet = config.LineraWallet,
                    linera_keystore = config.LineraKeystore,
                    linera_storage = config.LineraStorage,
                    publisher_chain_id = config.PublisherChainId,
                    publisher_owner = config.PublisherOwner,
                    xfighter_module_id = config.XFighterModuleId,
                    xfighter_app_id = config.XFighterAppId,
                    leaderboard_app_id = config.LeaderboardAppId,
                    userxfighter_module_id = config.UserXFighterModuleId,
                    tournament_app_id = config.TournamentAppId,
                    fungible_app_id = config.FungibleAppId,
                    friend_app_id = config.FriendAppId,
                    isReady = config.IsReady
                });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("start-linera-service")]
        public async Task<IActionResult> StartLineraService([FromQuery] int port = 8080)
        {
            try
            {
                var pid = await _orchestratorSvc.StartLineraServiceAsync(port);
                return Ok(new { success = true, pid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        [HttpPost("stop-linera-service")]
        public async Task<IActionResult> StopLineraService()
        {
            try
            {
                var pid = _orchestratorSvc.GetServicePid();
                if (!pid.HasValue)
                    return Ok(new { success = true, message = "Service not running" });

                var process = Process.GetProcessById(pid.Value);
                process.CloseMainWindow(); // Gửi close signal

                // Đợi 5 giây cho graceful shutdown
                if (await Task.Run(() => process.WaitForExit(5000)))
                {
                    return Ok(new { success = true, message = "Service stopped gracefully", pid });
                }
                else
                {
                    process.Kill();
                    return Ok(new { success = true, message = "Service force stopped", pid });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("linera-health")]
        public IActionResult GetServiceHealth()
        {
            try
            {
                bool isRunning = _orchestratorSvc.IsServiceRunning();
                var pid = _orchestratorSvc.GetServicePid();

                return Ok(new
                {
                    success = true,
                    pid,
                    isRunning,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet("linera-config")]
        public IActionResult GetLineraConfig()
        {
            try
            {
                var config = _orchestratorSvc.GetCurrentConfig();
                // Xác định chế độ hiện tại
                var mode = config.UseRemoteTestnet ? "Conway Testnet" : "Local Net Backup";

                return Ok(new
                {
                    success = true,
                    mode,
                    linera_wallet = config.LineraWallet,
                    linera_keystore = config.LineraKeystore,
                    linera_storage = config.LineraStorage,
                    publisher_chain_id = config.PublisherChainId,
                    publisher_owner = config.PublisherOwner,
                    xfighter_module_id = config.XFighterModuleId,
                    xfighter_app_id = config.XFighterAppId,
                    leaderboard_app_id = config.LeaderboardAppId,
                    userxfighter_module_id = config.UserXFighterModuleId,
                    tournament_app_id = config.TournamentAppId,
                    fungible_app_id = config.FungibleAppId,
                    friend_app_id = config.FriendAppId,
                    isReady = config.IsReady
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        #endregion

        #region MatchChain APIs
        [HttpPost("open-and-create")]
        public async Task<IActionResult> OpenAndCreate([FromBody] CreateMatchRequest req)
        {
            try
            {
                var matchType = (req.MatchType ?? "rank").Trim().ToLowerInvariant();
                var (chainId, appId) = await _matchchainSvc.EnqueueOpenAsync();

                return Ok(new { success = true, chainId, appId, matchType });
            }
            catch (TimeoutException tex)
            {
                Console.WriteLine($"[API-ERROR] OpenAndCreate timeout: {tex.Message}");
                return StatusCode(504, new { success = false, error = tex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API-ERROR] OpenAndCreate Error 500 timeout: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // submit-match-result endpoint
        [HttpPost("submit-match-result")]
        public async Task<IActionResult> SubmitMatchResult([FromBody] SubmitMatchRequest request)
        {
            try
            {
                if (request == null || request.MatchResult == null)
                    return BadRequest(new { success = false, message = "Invalid payload" });

                // Nếu có chainId mà chưa có matchId → dùng chainId làm matchId
                if (string.IsNullOrWhiteSpace(request.MatchResult.MatchId) && !string.IsNullOrWhiteSpace(request.ChainId))
                    request.MatchResult.MatchId = request.ChainId;

                // Gọi service EnqueueSubmitAsync (đợi worker xử lý xong)
                var json = await _matchchainSvc.EnqueueSubmitAsync(request.ChainId, request.AppId, request.MatchResult);

                // Parse JSON gốc từ service
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Bóc tách các field chính để trả về cho client
                var response = new
                {
                    success = root.GetProperty("success").GetBoolean(),
                    matchId = root.GetProperty("matchId").GetString(),
                    chainId = root.GetProperty("chainId").GetString(),
                    appId = root.GetProperty("appId").GetString(),
                    opId = root.TryGetProperty("opId", out var op) ? op.GetString() : null,
                    verified = root.TryGetProperty("verified", out var ver) && ver.GetBoolean(),
                    queued = root.TryGetProperty("queued", out var q) && q.GetBoolean()
                };

                Console.WriteLine($"[DEBUG] Raw Linera response: {json}");

                return Ok(response);
            }
            catch (InvalidOperationException ioe)
            {
                Console.WriteLine($"[API] Submit failed: {ioe.Message}");
                return StatusCode(503, new { success = false, error = ioe.Message });
            }
            catch (TimeoutException tex)
            {
                Console.WriteLine($"[API] Submit timeout: {tex.Message}");
                return StatusCode(504, new { success = false, error = tex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API-ERROR] Submit Error 500 timeout: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Submit match data error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }
        #endregion

        #region Leaderboard APIs

        [HttpGet("get-leaderboard-data")]
        public async Task<IActionResult> GetLeaderboardData()
        {
            try
            {
                var json = await _leaderboardSvc.GetLeaderboardDataAsync();
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Get leaderboard data error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }
        /// lấy thông tin của top 8 mùa hiện tại
        [HttpGet("get-tournament-top8")]
        public async Task<IActionResult> GetTournamentTop8()
        {
            try
            {
                var json = await _leaderboardSvc.GetTournamentTop8Async();
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        /// lấy thông tin của season hiện tại
        [HttpGet("get-current-season-info")]
        public async Task<IActionResult> GetCurrentSeasonInfo()
        {
            try
            {
                var json = await _leaderboardSvc.GetCurrentSeasonInfoAsync();
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        /// lấy thông tin của season cũ
        [HttpGet("get-season-info")]
        public async Task<IActionResult> GetSeasonInfo([FromQuery] ulong seasonNumber)
        {
            try
            {
                var json = await _leaderboardSvc.GetSeasonInfoAsync(seasonNumber);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        /// lấy leaderboard của season cũ
        [HttpGet("get-season-leaderboard")]
        public async Task<IActionResult> GetSeasonLeaderboard([FromQuery] ulong seasonNumber, [FromQuery] int? limit = null)
        {
            try
            {
                var json = await _leaderboardSvc.GetSeasonLeaderboardAsync(seasonNumber, limit);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// lấy thông tin của user toàn server
        [HttpGet("get-user-global-stats")]
        public async Task<IActionResult> GetUserGlobalStats([FromQuery] string userName)
        {
            try
            {
                var userStats = await _leaderboardSvc.GetUserGlobalStatsAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(userStats) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        /// Lấy lịch sử trận đấu của user từ XFighter app
        [HttpGet("get-user-match-history")]
        public async Task<IActionResult> GetMatchHistoryByUser([FromQuery] string userName)
        {
            try
            {
                var resultJson = await _leaderboardSvc.GetMatchHistoryByUserAsync(userName);
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                return result.TryGetProperty("success", out var success) && success.GetBoolean()
                    ? Ok(result)
                    : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Lấy lịch sử trận đấu của user theo userChain
        [HttpGet("get-userchain-match-history")]
        public async Task<IActionResult> GetMatchHistoryByUserChain([FromQuery] string userChain)
        {
            try
            {
                var resultJson = await _leaderboardSvc.GetMatchHistoryByUserChainAsync(userChain);
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                return result.TryGetProperty("success", out var success) && success.GetBoolean()
                    ? Ok(result)
                    : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Lấy lịch sử trận đấu theo chainId của match
        [HttpGet("get-chainid-match-history")]
        public async Task<IActionResult> GetMatchHistoryByChainId([FromQuery] string chainId)
        {
            try
            {
                var resultJson = await _leaderboardSvc.GetMatchHistoryByChainIdAsync(chainId);
                var result = JsonSerializer.Deserialize<JsonElement>(resultJson);
                return result.TryGetProperty("success", out var success) && success.GetBoolean()
                    ? Ok(result)
                    : BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Start Leaderboard  Managerment API
        [HttpPost("leaderboard/start")]
        public async Task<IActionResult> StartSeason([FromQuery] string? name = null)
        {
            try
            {
                var result = await _leaderboardSvc.StartSeasonAsync(name);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi tạo leaderboard.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        /// End Leaderboard Managerment API
        [HttpPost("leaderboard/end")]
        public async Task<IActionResult> EndSeason()
        {
            try
            {
                var result = await _leaderboardSvc.EndSeasonAsync();
                return Ok(new { success = true, message = $"Leaderboard completed on-chain.", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi end leaderboard.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        #endregion

        #region Tournament APIs
        /// Lấy leaderboard tournament hiện tại
        [HttpGet("tournament/leaderboard")]
        public async Task<IActionResult> GetTournamentLeaderboard()
        {
            try
            {
                var json = await _tournamentSvc.GetTournamentLeaderboardDataAsync();
                if (string.IsNullOrWhiteSpace(json))
                    return StatusCode(500, new { success = false, error = "Empty response from Linera tournament leaderboard" });

                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Lấy thông tin tournament hiện tại
        [HttpGet("tournament/season-info")]
        public async Task<IActionResult> GetTournamentSeasonInfo()
        {
            try
            {
                var json = await _tournamentSvc.GetCurrentTournamentInfoAsync();
                if (string.IsNullOrWhiteSpace(json))
                    return StatusCode(500, new { success = false, error = "Empty response from Linera tournament season info" });

                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        /// Lấy thông tin tournament cũ theo số
        [HttpGet("get-tournament-info")]
        public async Task<IActionResult> GetTournamentInfo([FromQuery] ulong tournamentNumber)
        {
            try
            {
                var json = await _tournamentSvc.GetSeasonTournamentInfoAsync(tournamentNumber);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Lấy leaderboard tournament cũ theo số
        [HttpGet("get-tournament-leaderboard")]
        public async Task<IActionResult> GetTournamentLeaderboard([FromQuery] ulong tournamentNumber)
        {
            try
            {
                var json = await _tournamentSvc.GetSeasonTournamentLeaderboardAsync(tournamentNumber);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Get tournament data error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        /// Tournament Get participants
        [HttpGet("tournament/participants")]
        public async Task<IActionResult> GetTournamentParticipants()
        {
            try
            {
                var participants = await _tournamentSvc.GetTournamentParticipantsAsync();
                return Ok(new { success = true, participants });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        [HttpPost("tournament/set-participants")]
        public async Task<IActionResult> SetTournamentParticipants([FromBody] SetParticipantsRequest req)
        {
            if (req?.Participants == null || req.Participants.Count < 8)
                return BadRequest(new { success = false, message = "Need 8 participants" });

            try
            {
                var opId = await _tournamentSvc.SetParticipantsAsync(req.Participants);
                return Ok(new { success = true, opId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Set tournament participants data error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpPost("tournament/init")]
        public async Task<IActionResult> InitTournamentMatches()
        {
            try
            {
                // Gọi service để khởi tạo bracket trên chain
                var ok = await _tournamentSvc.InitTournamentMatchesOnChain();
                if (!ok)
                    return StatusCode(500, new { success = false, message = "Failed to initialize tournament matches on chain" });

                return Ok(new { success = true, message = "Tournament matches initialized on chain" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Init tournament data error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // GO SF, F bracket
        [HttpPost("tournament/advance-bracket")]
        public async Task<IActionResult> AdvanceTournamentBracket()
        {
            try
            {
                var ok = await _tournamentSvc.AdvanceTournamentBracketAsync();
                if (!ok)
                    return StatusCode(500, new { success = false, message = "Failed to advance tournament bracket" });

                return Ok(new { success = true, message = "Tournament bracket advanced to next round" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Advance bracket match data error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        /// Tournament Managerment API
        [HttpPost("tournament/start")]
        public async Task<IActionResult> StartTournament([FromQuery] string? name = null)
        {
            try
            {
                var opId = await _tournamentSvc.StartTournamentSeasonAsync(name);
                return Ok(new
                {
                    success = true,
                    message = $"Tournament Start on-chain.",
                    opId
                });
            }
            catch (Exception ex)
            {
                // Other unexpected exceptions → 500
                return StatusCode(500, new { success = false, message = "Start Tournament error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpPost("tournament/end")]
        public async Task<IActionResult> EndTournament()
        {
            try
            {
                // Gọi orchestrator, orchestrator sẽ tự đọc từ chain
                var opId = await _tournamentSvc.EndTournamentSeasonAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Tournament completed on-chain.",
                    opId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "End tournament error. Please try again later.", error = ex.Message, details = ex.InnerException?.Message });
            }
        }


        [HttpPost("tournament/submit-match")]
        public async Task<IActionResult> SubmitTournamentMatch([FromBody] MatchResult match)
        {
            if (match == null || string.IsNullOrWhiteSpace(match.MatchId))
                return BadRequest(new { success = false, message = "match.MatchId is required" });

            try
            {
                var resultJson = await _tournamentSvc.SubmitTournamentMatchResultAsync(match);
                Console.WriteLine($"[TOURNAMENT] Submitted match {match.MatchId}: {resultJson}");
                return Ok(JsonDocument.Parse(resultJson).RootElement);
            }
            catch (Exception ex)
            {
                // Other unexpected exceptions → 500
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("tournament/match-list")]
        public async Task<IActionResult> GetTournamentMatchList()
        {
            try
            {
                // Lấy bracket trực tiếp từ onchain
                var bracket = await _tournamentSvc.GetTournamentBracketAsync();
                return Ok(new
                {
                    success = true,
                    matches = bracket
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("tournament/set-bracket")]
        public async Task<IActionResult> SetTournamentBracket([FromBody] SetBracketRequest req)
        {
            if (req == null || req.Matches == null || req.Matches.Count == 0)
                return BadRequest(new { success = false, message = "Matches list is required" });

            try
            {
                Console.WriteLine($"[SET-BRACKET-API] Received {req.Matches.Count} matches");

                var ok = await _tournamentSvc.SetBracketAsync(req.Matches);

                return Ok(new
                {
                    success = true,
                    message = $"Bracket set with {req.Matches.Count} matches",
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SetBracket failed: {ex}");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        #endregion

        #region Betting APIs
        [HttpPost("user/place-bet")]
        public async Task<IActionResult> UserPlaceBet([FromBody] UserPlaceBetRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserName) ||
               string.IsNullOrWhiteSpace(req.MatchId) ||
               string.IsNullOrWhiteSpace(req.Player) || req.Amount == 0)
                return BadRequest(new { success = false, message = "invalid payload" });

            try
            {
                var json = await _bettingSvc.UserPlaceBetAsync(req.UserName, req.MatchId, req.Player, req.Amount);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        [HttpGet("user/{userName}/betting-transactions")]
        public async Task<IActionResult> GetBettingTransactions([FromRoute] string userName)
        {
            try
            {
                var json = await _bettingSvc.GetTransactionsAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("tournament/settle")]
        public async Task<IActionResult> SettleMatch([FromBody] SettleMatchRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Winner)) return BadRequest(new { success = false, message = "invalid payload" });
            try
            {
                var json = await _bettingSvc.SettleMatchAsync(req.MatchId, req.Winner);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("tournament/set-match-metadata")]
        public async Task<IActionResult> SetMatchMetadata([FromBody] SetMatchMetadataRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.MatchId))
                return BadRequest(new { success = false, message = "MatchId is required" });

            if (req.DurationMinutes <= 0)
                return BadRequest(new { success = false, message = "DurationMinutes must be positive" });

            try
            {
                // Chỉ truyền matchId và durationMinutes, các tham số khác để null (dùng default)
                var json = await _bettingSvc.SetMatchMetadataAsync(
                    req.MatchId,
                    req.DurationMinutes);

                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("tournament/get-bet-metadata")]
        public async Task<IActionResult> GetMatchMetadata([FromQuery] string matchId)
        {
            try
            {
                var json = await _bettingSvc.GetMatchMetadataAsync(matchId);
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                // Kiểm tra lỗi GraphQL
                if (root.TryGetProperty("errors", out var errors))
                {
                    var errorMsg = errors[0].GetProperty("message").GetString() ?? "Unknown GraphQL error";
                    return NotFound(new { success = false, error = errorMsg, matchId });
                }

                // Kiểm tra matchMetadata có tồn tại không
                if (!root.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("matchMetadata", out var matchDataElement) ||
                    matchDataElement.ValueKind == JsonValueKind.Null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = $"Match '{matchId}' not found",
                        matchId
                    });
                }

                // Trích xuất data từ GraphQL response
                var matchData = root.GetProperty("data").GetProperty("matchMetadata");

                // Chuyển đổi sang MatchMetadataData
                var metadata = new MatchMetadataData
                {
                    MatchId = matchData.GetProperty("matchId").GetString() ?? "",
                    Player1 = matchData.GetProperty("player1").GetString() ?? "",
                    Player2 = matchData.GetProperty("player2").GetString() ?? "",
                    BetStatus = matchData.GetProperty("betStatus").GetString() ?? "",
                    OddsA = matchData.GetProperty("oddsA").GetDouble(),
                    OddsB = matchData.GetProperty("oddsB").GetDouble(),
                    TotalBetsA = matchData.GetProperty("totalBetsA").GetDouble(),
                    TotalBetsB = matchData.GetProperty("totalBetsB").GetDouble(),
                    TotalPool = matchData.GetProperty("totalPool").GetDouble(),
                    TotalBetsCount = matchData.GetProperty("totalBetsCount").GetDouble(),
                    BetDistributionA = matchData.GetProperty("betDistributionA").GetDouble(),
                    BetDistributionB = matchData.GetProperty("betDistributionB").GetDouble(),
                    BettingDeadlineUnix = matchData.GetProperty("bettingDeadlineUnix").GetInt64(),
                    BettingStartUnix = matchData.GetProperty("bettingStartUnix").ValueKind != JsonValueKind.Null
                    ? matchData.GetProperty("bettingStartUnix").GetInt64()
                    : 0
                };

                var serverNowUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
                if (serverNowUnix >= metadata.BettingDeadlineUnix && metadata.BetStatus == "Open")
                {
                    metadata.BetStatus = "Closed"; // Đổi luôn status
                }


                return Ok(new
                {
                    success = true,
                    data = metadata,
                    timestamps = new
                    {
                        // trả về timestamps, Unity sẽ tự tính
                        bettingDeadlineUnix = metadata.BettingDeadlineUnix, // microseconds
                        matchStartUnix = metadata.BettingStartUnix,           // microseconds
                        serverNowUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000 // microseconds
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetMatchMetadata failed: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }


        // Danh sách bets theo trận matchId
        [HttpGet("tournament/get-bets")]
        public async Task<IActionResult> GetBets([FromQuery] string matchId)
        {
            try
            {
                var json = await _bettingSvc.GetBetsAsync(matchId);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("user/transfer-tokens")]
        public async Task<IActionResult> TransferTokensToUser([FromBody] TransferTokensRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserName) || req.Amount == 0)
                return BadRequest(new { success = false, message = "invalid payload" });

            try
            {
                var json = await _bettingSvc.TransferTokensToUserAsync(req.UserName, req.Amount);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/balance")]
        public async Task<IActionResult> GetUserBalance([FromRoute] string userName)
        {
            try
            {
                var json = await _bettingSvc.GetUserBalanceAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("publisher/balance")]
        public async Task<IActionResult> GetPublisherBalance()
        {
            try
            {
                var json = await _bettingSvc.GetPublisherBalanceAsync();
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/token-transactions")]
        public async Task<IActionResult> GetUserTransactions([FromRoute] string userName)
        {
            try
            {
                var json = await _bettingSvc.GetUserTransactionsAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/token-transactions-from-publisher")]
        public async Task<IActionResult> GetUserTransactionsFromPublisher([FromRoute] string userName)
        {
            try
            {
                var json = await _bettingSvc.GetUserTransactionsFromPublisherAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Lấy betting analytics của tournaments hiện tại
        [HttpGet("tournament/betting-analytics")]
        public async Task<IActionResult> GetBettingAnalytics()
        {
            try
            {
                var (placed, settled, payouts, season) = await _tournamentSvc.GetBettingAnalyticsAsync();

                return Ok(new
                {
                    success = true,
                    currentTotalBetsPlaced = placed,
                    currentTotalBetsSettled = settled,
                    currentTotalPayouts = payouts,
                    currentTournament = season
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetBettingAnalytics: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Lấy betting analytics của tournaments cũ theo number
        [HttpGet("tournament/past-betting-analytics")]
        public async Task<IActionResult> GetPastBettingAnalytics([FromQuery] ulong tournamentNumber)
        {
            try
            {
                var analytics = await _tournamentSvc.GetPastTournamentAnalyticsAsync(tournamentNumber);

                if (analytics == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        error = $"No analytics for tournament {tournamentNumber}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    tournamentNumber = analytics.TournamentNumber,
                    totalBetsPlaced = analytics.TotalBetsPlaced,
                    totalBetsSettled = analytics.TotalBetsSettled,
                    totalPayouts = analytics.TotalPayouts
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetPastBettingAnalytics: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// Lấy betting analytics của tất cả tournaments cũ
        [HttpGet("tournament/all-past-betting-analytics")]
        public async Task<IActionResult> GetAllPastBettingAnalytics()
        {
            try
            {
                var analyticsList = await _tournamentSvc.GetAllTournamentAnalyticsAsync();

                // Sắp xếp giảm dần theo tournament number
                analyticsList = [.. analyticsList.OrderByDescending(a => a.TournamentNumber)];

                return Ok(new
                {
                    success = true,
                    count = analyticsList.Count,
                    analytics = analyticsList.Select(a => new
                    {
                        tournamentNumber = a.TournamentNumber,
                        totalBetsPlaced = a.TotalBetsPlaced,
                        totalBetsSettled = a.TotalBetsSettled,
                        totalPayouts = a.TotalPayouts
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetAllPastBettingAnalytics: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        #endregion

        #region UserXFighter APIs
        // Auth = create userchain, deploy userxfighter, check user info
        [HttpPost("user/auth")]
        public async Task<IActionResult> AuthUser([FromBody] AuthUserRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserName))
                return BadRequest(new { success = false, message = "userName is required" });

            try
            {
                var (chainId, userInfoJson) = await _userSvc.RegisterOrLoginAsync(req.UserName);
                var userInfoDoc = JsonDocument.Parse(userInfoJson);

                return Ok(new
                {
                    success = true,
                    userName = req.UserName,
                    userInfo = userInfoDoc
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        [HttpGet("user/list")]
        public IActionResult GetUserList()
        {
            try
            {
                var users = _userSvc.GetUserList();
                return Ok(new { success = true, users });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// DEBUG check user service manually
        [HttpGet("user/check-service/{userName}")]
        public IActionResult CheckUserService(string userName)
        {
            try
            {
                var userPort = _userSvc.GetUserPortFromStorage(userName);
                var isRunning = _cliRunner.IsUserServiceRunning(userPort);

                return Ok(new
                {
                    success = true,
                    userName,
                    port = userPort,
                    isRunning
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    userName,
                    error = ex.Message
                });
            }
        }

        /// DEBUG Start user service manually
        [HttpPost("user/start-service/{userName}")]
        public async Task<IActionResult> StartUserService(string userName)
        {
            try
            {
                var (wallet, keystore, storage) = _userSvc.CreateUserContext(userName);
                var userPort = _userSvc.GetUserPortFromStorage(userName);

                await _cliRunner.StartUserBackgroundService(wallet, keystore, storage, userPort);

                return Ok(new
                {
                    success = true,
                    userName,
                    port = userPort
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    userName,
                    error = ex.Message
                });
            }
        }

        /// DEBUG Stop user service manually (test monitor)
        [HttpPost("user/stop-service/{userName}")]
        public IActionResult StopUserService(string userName)
        {
            try
            {
                var userPort = _userSvc.GetUserPortFromStorage(userName);

                // Kill service bằng port
                Process.Start("pkill", $"-f \"linera.*service.*{userPort}\"").WaitForExit();

                return Ok(new
                {
                    success = true,
                    userName,
                    port = userPort,
                    message = "Service stopped"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    userName,
                    error = ex.Message
                });
            }
        }
        [HttpPost("cleanup-appids")]
        public IActionResult CleanupUserAppIds()
        {
            try
            {
                _userSvc.DeleteAllUserAppIds();
                return Ok(new { success = true, message = "User appid.txt cleanup completed" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        #endregion

        #region Friend APIs 

        [HttpPost("user/send-friend-request")]
        public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.ToUserChain))
                return BadRequest(new { success = false, message = "userName and ToUserChain are required" });

            try
            {
                var json = await _friendSvc.SendFriendRequestAsync(req.UserName, req.ToUserChain);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("user/accept-friend-request")]
        public async Task<IActionResult> AcceptFriendRequest([FromBody] AcceptFriendRequestRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.RequestId))
                return BadRequest(new { success = false, message = "userName and RequestId are required" });

            try
            {
                var json = await _friendSvc.AcceptFriendRequestAsync(req.UserName, req.RequestId);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("user/reject-friend-request")]
        public async Task<IActionResult> RejectFriendRequest([FromBody] RejectFriendRequestRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.RequestId))
                return BadRequest(new { success = false, message = "userName and RequestId are required" });

            try
            {
                var json = await _friendSvc.RejectFriendRequestAsync(req.UserName, req.RequestId);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("user/remove-friend")]
        public async Task<IActionResult> RemoveFriend([FromBody] RemoveFriendRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.FriendChain))
                return BadRequest(new { success = false, message = "userName and FriendChain are required" });

            try
            {
                var json = await _friendSvc.RemoveFriendAsync(req.UserName, req.FriendChain);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/friends")]
        public async Task<IActionResult> GetFriends([FromRoute] string userName)
        {
            try
            {
                var json = await _friendSvc.GetFriendsAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/pending-requests")]
        public async Task<IActionResult> GetPendingRequests([FromRoute] string userName)
        {
            try
            {
                var json = await _friendSvc.GetPendingRequestsAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/sent-requests")]
        public async Task<IActionResult> GetSentRequests([FromRoute] string userName)
        {
            try
            {
                var json = await _friendSvc.GetSentRequestsAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/is-friend/{targetChainId}")]
        public async Task<IActionResult> IsFriend([FromRoute] string userName, [FromRoute] string targetChainId)
        {
            try
            {
                var json = await _friendSvc.IsFriendAsync(userName, targetChainId);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("user/{userName}/friend-request/{requestId}")]
        public async Task<IActionResult> GetFriendRequest([FromRoute] string userName, [FromRoute] string requestId)
        {
            try
            {
                var json = await _friendSvc.GetFriendRequestAsync(userName, requestId);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        #endregion

        #region Airdrop APIs

        // API User gởi request để nhận airdrop lần đầu
        [HttpPost("user/{userName}/request-initial-claim")]
        public async Task<IActionResult> UserRequestInitialClaim([FromRoute] string userName)
        {
            try
            {
                var json = await _airdropSvc.UserRequestInitialClaimAsync(userName);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // API Thiết lập airdrop ban đầu
        [HttpPost("tournament/set-airdrop-amount")]
        public async Task<IActionResult> SetAirdropAmount([FromBody] ulong amount = 5000)
        {

            try
            {
                var json = await _airdropSvc.SetAirdropAmountAsync(amount);
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // API xử lý các yêu cầu nhận airdrop ban đầu từ user
        [HttpPost("tournament/process-pending-claims")]
        public async Task<IActionResult> ProcessPendingClaims()
        {
            try
            {
                var json = await _airdropSvc.ProcessPendingClaimsAsync();
                return Ok(new { success = true, body = JsonDocument.Parse(json) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // API Kiểm tra các yêu cầu nhận airdrop khi user gởi yêu cầu
        [HttpGet("tournament/pending-claims")]
        public async Task<IActionResult> GetPendingClaims()
        {
            try
            {
                var json = await _airdropSvc.GetPendingClaimsAsync();
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                var pendingClaims = root.GetProperty("data").GetProperty("pendingClaims");
                var claims = new List<object>();

                foreach (var claim in pendingClaims.EnumerateArray())
                {
                    claims.Add(new
                    {
                        userKey = claim.GetProperty("userKey").GetString(),
                        userChain = claim.GetProperty("userChain").GetString(),
                        userPublicKey = claim.GetProperty("userPublicKey").GetString(),
                        requestedAt = claim.GetProperty("requestedAt").GetInt64()
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = claims
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // API Kiểm tra thông tin airdrop
        [HttpGet("tournament/airdrop-info")]
        public async Task<IActionResult> GetAirdropInfo()
        {
            try
            {
                var json = await _airdropSvc.GetAirdropInfoAsync();
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                var airdropInfo = root.GetProperty("data").GetProperty("airdropInfo");
                var amount = airdropInfo.GetProperty("amount").GetUInt64();

                return Ok(new
                {
                    success = true,
                    data = new { amount }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }


        #endregion

    }
}