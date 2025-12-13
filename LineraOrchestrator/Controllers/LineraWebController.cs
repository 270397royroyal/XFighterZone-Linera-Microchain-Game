// LineraWebController.cs
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LineraOrchestrator.Models;
using LineraOrchestrator.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("test")]
public class WebUserController : ControllerBase
{
    private readonly WebUserService _webUser;
    private readonly UserService _userService;
    private readonly LineraConfig _config;

    public WebUserController(WebUserService webUser, UserService userService, LineraConfig config)
    {
        _webUser = webUser;
        _userService = userService;
        _config = config;
    }


    [HttpPost("place-bet-web")]
    public async Task<IActionResult> PlaceBetWeb([FromBody] WebPlaceBetRequest req)
    {
        if (req == null)
            return BadRequest("Invalid payload");

        try
        {
            Console.WriteLine($"[WEB-BET] Received request: ChainId={req.ChainId}, MatchId={req.MatchId}");

            // 2. Tìm userName từ chainId
            var userName = _userService.GetUserNameByUserChain(req.ChainId);
            if (string.IsNullOrEmpty(userName))
            {
                return NotFound(new { success = false, error = $"User with chain {req.ChainId} not found" });
            }
            Console.WriteLine($"[WEB-BET] Found user: {userName}");

            // 3. Lấy AppId từ userName
            var appId = _userService.GetUserAppId(userName);
            Console.WriteLine($"[WEB-BET] User AppId: {appId}");

            // 4. Tạo wallet path từ userName
            var walletPath = Path.Combine(_config.UserChainPath!, userName, "wallet.json");
            Console.WriteLine($"[WEB-BET] Wallet path: {walletPath}");

            // 5. Gọi WebUserService để place bet
            var result = await _webUser.PlaceBetAsync(
                chainId: req.ChainId,
                appId: appId,
                walletPath: walletPath,
                matchId: req.MatchId,
                player: req.Player,
                amount: req.Amount
            );

            // 6. Parse kết quả
            var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);

            Console.WriteLine($"[WEB-BET] Result: {result}");
            return Ok(new
            {
                success = true,
                rawResult = result,
                parsedResult = jsonResult
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WEB-BET-ERROR] {ex.Message}");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                details = ex.StackTrace
            });
        }
    }

    public class WebPlaceBetRequest
    {
        public required string ChainId { get; set; }
        public required string MatchId { get; set; }
        public required string Player { get; set; }
        public ulong Amount { get; set; }
    }
}