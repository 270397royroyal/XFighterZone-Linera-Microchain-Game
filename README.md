
# XFighterZone-Linera-Microchain-Game
This is the temporary storage location for the XFighterZone Linera Microchain Game for Wave 4
=======
# ⚔️ XFighterZone — Real-Time Gaming & Prediction Metaverse on Linera
<p align="center">
  <a href="https://www.youtube.com/watch?v=FTH6rmuN_Bg">
    <img src="https://img.youtube.com/vi/FTH6rmuN_Bg/maxresdefault.jpg" width="720" alt="Watch the demo">
  </a>
</p>

### ⚠️ Important Notice for Judges - Check Docker
All large frontend game client/server files are **not included in the GitHub repository** due to size limits.  
Please download all build `.zip` files if possible. They are provided in the **Release section**:
- [ClientFrontend.zip](https://github.com/270397royroyal/XFighterZone-Linera-Microchain-Game/releases/tag/Multiplatform-DockerTest)  
- [ServerLobby.zip](https://github.com/270397royroyal/XFighterZone-Linera-Microchain-Game/releases/tag/Multiplatform-DockerTest)  
- [AdminTournamentFrontend.zip](https://github.com/270397royroyal/XFighterZone-Linera-Microchain-Game/releases/tag/Multiplatform-DockerTest)
- [ServerTournament.zip](https://github.com/270397royroyal/XFighterZone-Linera-Microchain-Game/releases/tag/Multiplatform-DockerTest)

**Instructions:**  
1. Download and unzip each file in the root directory of the project.
2. After extraction, your folder structure should match the repository structure.
3. Run `chmod +x ./scripts/local-dev-start.sh` and then `./scripts/local-dev-start.sh` to launch the full system locally.  
> This ensures that `./scripts/local-dev-start.sh` can find all necessary files and run the complete XFighterZone system.

# 🚀 Deployment Environments
## A. Local Development Run with Docker
```text
# Clone this repository and run locally
git clone https://github.com/270397royroyal/XFighterZone-Linera-Microchain-Game.git
cd LineraOrchestrator_XFighterZone

# Run script to complete system (LineraOrchestrator + Game Server)
chmod +x ./scripts/local-dev-start.sh
./scripts/local-dev-start.sh

Wait until you see the following message in the logs:  
XFighterZone Docker setup completed successfully!

# Test basic local endpoints when finish setup Localhost + Docker
curl http://localhost:5290/health
curl http://localhost:5290/linera/linera-config
curl http://localhost:5290/linera/user/list
curl http://localhost:5290/linera/get-leaderboard-data

```
### Local Features:
- Full debugging capabilities
- Direct Linera GraphQL access (port 8080)
- Game Client: http://localhost:5173
- Fastest response times, easy run with Docker

## B. Live Demo (Not Production-Ready in Wave 4)
For demo and evaluation purposes only.  
**🎮 WebGL Game Client Live Demo:** https://xfighterzone.com/game/
```bash
# Test the live application (accessible globally)
curl -X POST https://api.xfighterzone.com/linera/start-linera-node
curl -X GET https://api.xfighterzone.com/linera/linera-config
curl -X GET https://api.xfighterzone.com/linera/user/list
curl -X GET https://api.xfighterzone.com/linera/get-leaderboard-data
```
### 🛡️ Production Features:
- Cloudflare Tunnel (no open ports required)
- Automatic SSL/TLS encryption
- Global CDN & DDoS protection
- Multi-chain Conway Testnet integration

All test commands Orchestrator: [all-test-commands.md](https://github.com/270397royroyal/LineraOrchestrator_XFighterZone/blob/main/all-test-commands.md).  
Create a new publisher chain & user chain data on Docker
```text
cd LineraOrchestrator_XFighterZone
chmod +x TestScripts/reset_data.sh
./TestScripts/reset_data.sh
```

## Performance Metrics
| Environment | Response Time | Availability | Features |
|-------|-------------|-------|-------------|
| **Live Demo** | ~200ms | Ready | Production |
| **Localhost** | ~50ms | Local | Full debugging |

### LIVE ON TESTNET CONWAY
⚡ Publisher chain / Appchains
- Publisher Chain ID: `07db9ad3cf3cc818ed1d5ce543f0889420209100aef2b22795a6409fe02d97fa`  
- XFighter App: `a5f73711b4425e1e3c6d75680d9112b071fa4ae4dcbc095e07e67fdd25418017`  
- Leaderboard App: `896f0c19a9fe7357cbbf64cb9cb1f122b9d825fb0b03fc6fae0c5da40ee3e832`  
- Tournament App: `ed485ccdc113f162ca54f8e2ed87f3be9e6989bd5155b20c099af528815701e6`  
- Fungible Token App: `a52b5122a22eb7a6e7bba00adfa7800c3c324a2f18c81622f62c2bbfe868535a`
- Friend App:  `09c6e33c9e0b507aa969abd0c52dc5e42336981683c1fa2a6bfaa96438fffb42`

## Tech Stack
| Layer | Technology |
|-------|-------------|
| **Blockchain** | Linera Protocol Conway Testnet |
| **Smart Contracts** | Rust 1.86.0, Linera SDK v0.15.7 |
| **Orchestrator** | C#, ASP.NET, GraphQL Client |
| **Infrastructure** | Docker, Multi-wallet Management, Cloudflare Tunnel |
| **Frontend** | Unity Client WebGL: localhost:5173 |
| **Database** | RocksDB (Linera Native) |

## Development Roadmap
| Wave | Focus | Status |
|------|--------|--------|
| **Wave 1** | MVP Foundation: Core Gameplay, On-chain Integration | ✅ Complete |
| **Wave 2** | Multiplatform Support, Friend System, Hero System, Normal/Rank Modes | ✅ Complete |
| **Wave 3** | Tournament Expansion, User Chains, Social on-chain features & Cross-chain Betting | ✅ Complete |
| **Wave 4** | Metaverse Lobby, Prediction Bet System & Cross-chain Asset Management  | ✅ Complete |
| **Wave 5** | Marketplace, Quest System & Advanced Prediction Pools | 🔄 In Progress |
| **Wave 6** | Full Metaverse: Decentralization & Social Features | ⏳ Planned |

## Wave 4: Major updates 
- Environment 1: Local Development & Testing
- Multi-chain architecture with user chains
- Complete cross-chain betting system
- **Live tournament betting: users can place bets directly on ongoing Tournament matches via cross-chain calls**
- User global metrics / User profile
- Tournament and Leaderboard management with multi-season support
- Social features with friend system
- Decentralized economy with Fungible Token integration
- Real-time gameplay with good performance for WebGL

Details upgrades: [CHANGELOG.md](https://github.com/270397royroyal/LineraOrchestrator_XFighterZone/blob/main/CHANGELOG.md).
## System Architecture
```plaintext
Multi-Chain Gaming Infrastructure
┌───────────────────────────────────────────────────────────────────────────────────────────────┐
│                                      PUBLISHER CHAIN                                          │
├──────────────┬──────────────┬──────────────┬──────────────┬──────────────┬────────────────────┤
│ TOURNAMENT   │ LEADERBOARD  │  XFIGHTER    │ FUNGIBLE     │ FRIEND       │ XFIGHTER           │
│ APP          │ APP          │ APP          │ TOKEN APP    │ XFIGHTER     │ (Game Logic)       │
├──────────────┼──────────────┼──────────────┼──────────────┼──────────────┼────────────────────┤
│ - Seasons    │ - Rankings   │ - Match Flow │ - Token Mgmt │ - Social     │ - Real-time fights │
│ - Brackets   │ - Stats      │ - Score Sync │ - Transfers  │ - FriendReq  │ - Match meta store │
│ - Betting    │ - Leaderboard│ - Chain Mgmt │ - Balances   │ - Graph Sync │ - Result messages  │
│ - Payouts    │ - Rewards    │ - FactoryOps │              │              │                    │
└──────────────┴──────────────┴──────────────┴──────────────┴──────────────┴────────────────────┘
       │              │              │              │                │
       │   Cross-app  │   Cross-app  │ Cross-chain  │ Cross-chain    │ Cross-chain
       │   calls      │   calls      │ messages     │ transfers      │ messages
       ▼              ▼              ▼              ▼                ▼
┌───────────────────────────────────────────────────────────────────────────────────────────────┐
│                                         USER CHAINS                                           │
├──────────┬──────────┬──────────┬──────────┬──────────┬────────────────────────────────────────┤
│ USER 1   │ USER 2   │ USER 3   │ USER 4   │ USER N   │ ...                                    │
│ CHAIN    │ CHAIN    │ CHAIN    │ CHAIN    │ CHAIN    │                                        │
├──────────┼──────────┼──────────┼──────────┼──────────┼────────────────────────────────────────┤
│ UserXF   │ UserXF   │ UserXF   │ UserXF   │ UserXF   │                                        │
│ Local Tournament    │ Local Fungible Token│ Local FriendXFighter │                            │
└──────────┴──────────┴──────────┴──────────┴──────────┴────────────────────────────────────────┘
```

## Real-Time on-chain Gaming Flow 
```text
SETUP & REGISTRATION
1. Deploy Tournament Main on Publisher Chain
2. Each user initializes UserXFighter on their own chain
3. UserXFighter auto-registers to Tournament Local

TOURNAMENT MANAGEMENT
1. Admin starts a season (e.g., "Summer Championship")
2. Admin sets participant list & bracket
3. Tournament stores season metadata

BETTING FLOW (Userchain → Tournament → Userchain)
User Chain → Publisher Chain Cross-chain Betting:
1. Userchain → UserXFighter: Send Placebet crosschain message to Tournament
2. UserXFighter → Fungible Token: Transfer 100 tokens to Tournament Owner
3. UserXFighter → Tournament Local: PlaceBet operation (CALL APPLICATION)
5. Tournament Main saves bet to state with user chain local transaction

MATCH SETTLEMENT & PAYOUT
1. Admin → Tournament Main: SettleMatch(match_id="MATCH_1", winner="p1")
2. Tournament Main: 
- Find all winning bets 
- Send payouts via Fungible Token to users 
- Send cross-chain payout messages to User chains
3. Tournament Local receives message → calls UserXFighter
4. UserXFighter updates transaction status: "paid" → "won/lost"

SOCIAL FEATURES
1. Userchain → UserXFighter: SendFriendRequest(other_user_chain)
2. UserXFighter → FriendXFighter UserXFighter → Userchain: Call Crosschain-message to each other
3. Friend system manages friend lists, match chain history, user metrics & friend profiles
```
## Key Flows Explained
- Cross-app Calls: Tournament calls Fungible for payouts; Xfighter calls Leaderboard for score updates.
- Cross-chain Messages: User chains send PlaceBet messages to Tournament; Tournament sends Payout messages to User chains.
- Cross-chain Transfers: Fungible Token handles token movements between chains for bets and winnings.
- Social Flow: FriendXFighter manages friend requests and updates across user chains via cross-chain messages.
---
### Player Client Flow
Each browser tab represents a separate player
**1. Launch Client**
```
- Player opens the game at: http://localhost:5173
- To simulate multiple users, open multiple tabs (e.g., Player test1 and test2).
``` 
**2. Login**
```
- Each tab logs in with a separate test account.
- A unique wallet, chain, keystore, and storage are automatically created per user.
```
**3. Play Matchmaking (PvP)**
```
When a player clicks Play Matchmaking: The client sends a matchmaking calling to the Orchestrator request match chainID.  
When two players match:  
- A duel session is created with match chainID
- Both players receive the same match chainID & fighter data  
- The fight runs deterministically on the client  
- Submit match send cross-chain message to publisher and leaderboard appchain
```
**4. Submit Result**
```
The Leaderboard app updates:
- Auto-updated as players win/lose, total match, winrate
- All recent duels store on global leaderboard
- Fully verifiable across microchains
```
**5. Check Profile Features**
```
- Friend List
- Match History, User Stats
- Add friend, Remove friend
- Accept requests
- Show friend status (online/offline), check friend profile onchain
```
### Admin Tournament Flow
**1. Launch Admin**: 
```
- Admin runs the Tournament Orchestrator UI at: http://localhost:5174 
- Admin uses tournament code (example): `18124`
```
**2. Tournament Setup**
```text
1. Start Tournament: Validate Top 8 from Leaderboard & Initializes the tournament session.

2. Generate Brackets
- Quarterfinal → Semifinal → Final
- Bracket seeds follow Leaderboard rank.
3. Run Tournament
For each match in the bracket:
Admin calls Start Match
Two users play PvP using the normal matchmaking → but with forced pairing
Results are pushed back to Tournament App

4. Progress Tournament
- After each match:
- Tournament App updates bracket winners
- Continues until the final champion is determined

5. End Tournament
- Admin clicks End Tournament:
- Winner / Runner Up is auto recorded onchain
- Rewards distributed NFT Trophies (Wave4)
- Tournament marked as completed
```

### Media
- **XFighterZone Plan document:** [[Google Drive]](https://drive.google.com/drive/folders/11QJzlPwjiQ3K2e7OFV5Q67909xzfoAjP?usp=drive_link)
- **Full Playlists Buildathon Demo:** [https://youtu.be/tf6PkybCmtI?si=ZZ2fSCO7kMLJCqa5 ](https://youtu.be/tf6PkybCmtI?si=ZZ2fSCO7kMLJCqa5 )
  
## 📞 Team Support
**Team:** Roystudios  
**Discord:** `roycrypto`  
**Author:** [roycrypto](https://x.com/AriesLLC1)










































>>>>>>> b95ba18 (Initial submission)
