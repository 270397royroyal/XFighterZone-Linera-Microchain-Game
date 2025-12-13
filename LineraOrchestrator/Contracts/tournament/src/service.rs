// Copyright (c) Zefchain Labs, Inc.
// SPDX-License-Identifier: Apache-2.0
// tournament/src/service.rs

#![cfg_attr(target_arch = "wasm32", no_main)]

mod state;
use log::info;
use std::sync::Arc;
use async_graphql::{EmptySubscription, Object, Request, Response, Schema, SimpleObject};
use linera_sdk::{abi::WithServiceAbi, views::View, Service, ServiceRuntime};
use tournament_shared::{LeaderboardEntry, TournamentInfo, TournamentStatus};
use tournament::{TournamentAbi, Operation, TournamentMatchInput, Parameters};
use self::state::{TournamentState, MatchBetStatus};

linera_sdk::service!(TournamentService);

#[derive(SimpleObject, Clone)]
struct TournamentMatchOutput {
    pub match_id: String,
    pub player1: String,
    pub player2: String,
    pub winner: Option<String>,
    pub round: String,
    pub match_status: String,
}

#[derive(SimpleObject, Clone)]
struct MatchResultOutput {
    match_id: String,
    winner: String,
    loser: String,
}

#[derive(SimpleObject, Clone)]
struct BetEntryOutput {
    pub bet_id: String,
	pub match_id: String,
    pub bettor: String,
	pub bettor_public_key: String,
    pub predicted: String,
    pub amount: u64,
    pub user_chain: String,
}

#[derive(SimpleObject, Clone)]
struct MatchMetadataOutput {
    // Thông tin cơ bản
    pub match_id: String,
    pub player1: String,
    pub player2: String,
    
    // Thời gian
    pub betting_deadline_unix: u64,        // micros timestamp
    pub betting_start_unix: Option<u64>,     // micros timestamp !
    
    // Odds và cược
    pub odds_a: f64,
    pub odds_b: f64,
    pub total_bets_a: u64,
    pub total_bets_b: u64,
    pub total_pool: u64,
    pub total_bets_count: u64,
    
    // Phân phối cược
    pub bet_distribution_a: u64,  // %
    pub bet_distribution_b: u64,  // %
    
    // Trạng thái
    pub bet_status: String,
}

#[derive(SimpleObject, Clone)]
struct BettingAnalytics {
    pub current_total_bets_placed: u64,
    pub current_total_bets_settled: u64,
    pub current_total_payouts: u64,
    pub current_tournament: u64,
}

#[derive(SimpleObject, Clone)]
struct PastTournamentAnalytics {
    pub tournament_number: u64,
    pub total_bets_placed: u64,
    pub total_bets_settled: u64,
    pub total_payouts: u64,
}

#[derive(SimpleObject, Clone)]
struct AirdropInfo {
    pub amount: u64,
}

#[derive(SimpleObject, Clone)]
struct PendingClaimOutput {
    pub user_key: String,
    pub user_chain: String,
    pub user_public_key: String,
    pub requested_at: u64,
}

pub struct TournamentService {
    state: Arc<TournamentState>,
    runtime: Arc<ServiceRuntime<Self>>,
}

impl WithServiceAbi for TournamentService {
    type Abi = TournamentAbi;
}

impl Service for TournamentService {
    type Parameters = Parameters;

    async fn new(runtime: ServiceRuntime<Self>) -> Self {
        let state = TournamentState::load(runtime.root_view_storage_context())
            .await
            .expect("Failed to load state");
        TournamentService {
            state: Arc::new(state),
            runtime: Arc::new(runtime),
        }
    }

    async fn handle_query(&self, request: Request) -> Response {
        let schema = Schema::build(
            QueryRoot { 
                state: self.state.clone(),
                //runtime: self.runtime.clone(),
            },
            MutationRoot { runtime: self.runtime.clone() },
            EmptySubscription,
        ).finish();
        schema.execute(request).await
    }
}

struct MutationRoot {
    runtime: Arc<ServiceRuntime<TournamentService>>,
}

#[Object]
impl MutationRoot {
	// === Tournament Season Management ===
     async fn start_tournament_season(&self, name: String) -> bool {
        let op = Operation::StartTournamentSeason { name };
        self.runtime.schedule_operation(&op);
        true
    }

    async fn end_tournament_season(&self) -> bool {
		let op = Operation::EndTournamentSeason;
		self.runtime.schedule_operation(&op);
		true
	}

    async fn record_match(&self, match_id: String, winner: String, loser: String) -> bool {
        let op = Operation::RecordMatch { match_id, winner, loser };
        self.runtime.schedule_operation(&op);
        true
    }
    
    async fn set_bracket(&self, matches: Vec<TournamentMatchInput>) -> bool {
        let op = Operation::SetBracket { matches };
        self.runtime.schedule_operation(&op);
        true
    }
    
    async fn set_participants(&self, participants: Vec<String>) -> bool {
        let op = Operation::SetParticipants { participants };
        self.runtime.schedule_operation(&op);
        true
    }
	
	// === Tournament Betting  ===
    async fn settle_match(&self, match_id: String, winner: String) -> bool {
        let op = Operation::SettleMatch { match_id, winner };
        self.runtime.schedule_operation(&op);
        true
    }
    
	/// Open bet, set match data
    async fn set_match_metadata(&self, match_id: String, duration_minutes: u64) -> bool {		
		let op = Operation::SetMatchMetadata {
			match_id,
			duration_minutes,
			betting_start_unix: None, // Cũng là micros
			status: Some("Open".to_string()), 
		};
		
		self.runtime.schedule_operation(&op);
		true
	}
	
    /// Set default airdrop amount for new users
    async fn set_airdrop_amount(&self, amount: u64) -> bool {
        let op = Operation::SetAirdropAmount { amount };
        self.runtime.schedule_operation(&op);
        true
    }
	
	/// process pending claims to user
	async fn process_pending_claims(&self, limit: Option<u32>) -> bool {
		let op = Operation::ProcessPendingClaims { limit };
		self.runtime.schedule_operation(&op);
		true
	}
}

struct QueryRoot {
    state: Arc<TournamentState>,
    //runtime: Arc<ServiceRuntime<TournamentService>>,
}

#[Object]
impl QueryRoot {
	// === Tournament Management Service === 
	/// Tournament check info current season
    async fn current_tournament_info(&self) -> Option<TournamentInfo> {
        let tournament_number = *self.state.current_tournament.get();
        self.get_tournament_info(tournament_number).await
    }
	
	/// Tournament check info via number
	 async fn tournament_info(&self, tournament_number: u64) -> Option<TournamentInfo> {
        self.get_tournament_info(tournament_number).await
    }
	
	/// DEBUG Lấy danh sách tất cả tournament numbers
    async fn tournament_numbers(&self) -> Vec<u64> {
        match self.state.tournament_metadata.indices().await {
            Ok(numbers) => numbers.into_iter().collect(),
            Err(_) => vec![],
        }
    }
	/// Tournament check past leaderboard data
	async fn past_tournament_leaderboard(&self, tournament_number: u64) -> Vec<LeaderboardEntry> {
        self.get_past_tournament_entries(tournament_number).await
    }
	
	/// Tournament check participants
    async fn participants(&self) -> Vec<String> {
        self.state.participants.get().clone()
    }
	
	/// Match result for leaderboard tournament
    async fn results(&self) -> Vec<MatchResultOutput> {
        let mut output = vec![];
        if let Ok(ids) = self.state.results.indices().await {
            for id in ids {
                if let Ok(Some((winner, loser))) = self.state.results.get(&id).await {
                    output.push(MatchResultOutput { match_id: id, winner, loser });
                }
            }
        }
        output
    }

	/// Get data Leaderboard tournament
    async fn tournament_leaderboard(&self) -> Vec<LeaderboardEntry> {
        let mut results = vec![];
        if let Ok(keys) = self.state.tournament_leaderboard.indices().await {
            for k in keys {
                if let Ok(Some(score)) = self.state.tournament_leaderboard.get(&k).await {
                    results.push(LeaderboardEntry { username: k, score });
                }
            }
        }
        results.sort_by(|a, b| b.score.cmp(&a.score));
        results
    }
	
	/// Set bracket cho tournament
    async fn bracket(&self) -> Vec<TournamentMatchOutput> {
        let mut matches = vec![];
        if let Ok(ids) = self.state.bracket.indices().await {
            for id in ids {
                if let Ok(Some(m)) = self.state.bracket.get(&id).await {
                    matches.push(TournamentMatchOutput {
                        match_id: m.match_id,
                        player1: m.player1,
                        player2: m.player2,
                        winner: m.winner,
                        round: m.round,
                        match_status: m.match_status,
                    });
                }
            }
        }
        matches
    }
	
	// === Tournament Betting Service === 
	/// Tournament check participants
	async fn get_bets(&self, match_id: String) -> Vec<BetEntryOutput> {
		let mut bets_for_match = Vec::new();
		match self.state.bets.get(&match_id).await {
			Ok(Some(entries)) => {
				info!("[SERVICE] Found {} bets for match {}", entries.len(), match_id);
				for bet in entries {
					bets_for_match.push(BetEntryOutput {
						bet_id: bet.bet_id,
						match_id: bet.match_id.clone(),
						bettor: bet.bettor,
						bettor_public_key: bet.bettor_public_key.clone(),
						predicted: bet.predicted,
						amount: bet.amount,
						user_chain: bet.user_chain.to_string(),
					});
				}
			}
			_ => {
				info!("[SERVICE] No bets found for match {}", match_id);
			}
		}

		bets_for_match
	}

	 /// Get complete match info including odds, time, bet_status
    async fn match_metadata(&self, match_id: String) -> Option<MatchMetadataOutput> {
        match self.state.matches.get(&match_id).await {
            Ok(Some(metadata)) => {
                // Lấy total_bets dựa trên trạng thái match
				let total_bets_count  = match metadata.bet_status {
					MatchBetStatus::Settled => {
						// Match has settled: dùng total_bets_count đã lưu
						metadata.total_bets_count
					}
					_ => {
						// Match chưa settled: đếm số bets hiện tại
						match self.state.bets.get(&match_id).await {
							Ok(Some(bets)) => bets.len() as u64,
							_ => 0
						}
					}
				};
				
                let total_pool = metadata.total_bets_a + metadata.total_bets_b;
				
				// Handle odds real-time
				let odds_a = if metadata.total_bets_a > 0 {
					(total_pool * 1000) / metadata.total_bets_a
				} else {
					1000
				};
				
				let odds_b = if metadata.total_bets_b > 0 {
					(total_pool * 1000) / metadata.total_bets_b
				} else {
					1000
				};
							
				 // Handle bet distribution
				let bet_distribution_a = if total_pool > 0 {
					(metadata.total_bets_a * 100) / total_pool
				} else {
					50 // mặc định 50% nếu chưa có cược
				};
				
				let bet_distribution_b = 100 - bet_distribution_a; // Đảm bảo tổng 100%
               
                Some(MatchMetadataOutput {
                    match_id: metadata.match_id,
                    player1: metadata.player1,
                    player2: metadata.player2,
                    total_bets_a: metadata.total_bets_a,
                    total_bets_b: metadata.total_bets_b,
                    odds_a: odds_a as f64 / 1000.0,  // odds real-time vừa tính
					odds_b: odds_b as f64 / 1000.0,
                    total_pool,
                    total_bets_count,
					bet_status: format!("{:?}", metadata.bet_status),
					bet_distribution_a,  // % cược vào A
					bet_distribution_b,  // % cược vào B
					betting_deadline_unix: metadata.betting_deadline_unix, // giữ nguyên cho debug
					betting_start_unix: metadata.betting_start_unix, // giữ nguyên cho debug
                })
            },
            _ => None,
        }
    }
	/// Debug query để kiểm tra registered UserXFighter apps
	async fn registered_userxfighter_apps(&self) -> Vec<String> {
        let mut apps = Vec::new();
        if let Ok(chain_ids) = self.state.user_xfighter_app_ids.indices().await {
            for chain_id in chain_ids {
                if let Ok(Some(app_id)) = self.state.user_xfighter_app_ids.get(&chain_id).await {
                    apps.push(format!("Chain: {} -> App: {:?}", chain_id, app_id));
                }
            }
        }
        apps
    }

	/// Tournament betting analytics for current season
    async fn betting_analytics(&self) -> async_graphql::Result<BettingAnalytics> {
        Ok(BettingAnalytics {
			current_total_bets_placed: *self.state.current_total_bets_placed.get(),
			current_total_bets_settled: *self.state.current_total_bets_settled.get(),
			current_total_payouts: *self.state.current_total_payouts.get(),
			current_tournament: *self.state.current_tournament.get(),
        })
    }
	
	/// Get past tournament betting analytics
	async fn past_tournament_analytics(&self, tournament_number: u64) -> Option<PastTournamentAnalytics> {
		self.get_past_tournament_analytics_internal(tournament_number).await
	}
	
	/// Get all tournaments with betting analytics
	async fn all_tournament_analytics(&self) -> Vec<PastTournamentAnalytics> {
		self.get_all_tournament_analytics_internal().await
	}

	/// Get current airdrop settings
    async fn airdrop_info(&self) -> AirdropInfo {
        AirdropInfo {
            amount: *self.state.airdrop_amount.get(),
        }
    }
	
	/// Get all pending airdrop claims request
	async fn pending_claims(&self) -> Vec<PendingClaimOutput> {
		let mut claims = Vec::new();
		if let Ok(keys) = self.state.pending_claims.indices().await {
			for key in keys {
				if let Ok(Some(claim)) = self.state.pending_claims.get(&key).await {
					claims.push(PendingClaimOutput {
						user_key: key.clone(),
						user_chain: claim.user_chain.to_string(),
						user_public_key: claim.user_public_key,
						requested_at: claim.requested_at,
					});
				}
			}
		}
		claims
	}
	
}

//  === Query Management Services ===
impl QueryRoot {
    async fn get_tournament_info(&self, tournament_number: u64) -> Option<TournamentInfo> {
		if let Some(metadata) = self.state.tournament_metadata.get(&tournament_number).await.ok().flatten() {
			
			 // TÍNH duration_days từ start_time và end_time
            let duration_days = if let Some(end_time) = metadata.end_time {
                let duration_micros = end_time - metadata.start_time;
                Some(duration_micros as f64 / (24.0 * 60.0 * 60.0 * 1_000_000.0))
            } else {
                None
            };
			
			Some(TournamentInfo {
				number: tournament_number,
				name: metadata.name,
				start_time: metadata.start_time,
				end_time: metadata.end_time,
				duration_days,
				status: match metadata.status {
					TournamentStatus::Active => "active".to_string(),
					TournamentStatus::Ended => "ended".to_string(),
				},
				champion: metadata.champion,
				runner_up: metadata.runner_up,
			})
		} else {
			// Fallback old seasons if do not have metadata
			Some(TournamentInfo {
				number: tournament_number,
				name: format!("Tournament {}", tournament_number),
				start_time: 0,
				end_time: None,
				duration_days: None,
				status: if tournament_number < *self.state.current_tournament.get() {
					"ended".to_string()
				} else {
					"active".to_string()
				},
				champion: None,
				runner_up: None,
			})
		}
	}
    
    async fn get_past_tournament_entries(&self, tournament_number: u64) -> Vec<LeaderboardEntry> {
        let mut entries = Vec::new();
        let prefix = format!("{}:", tournament_number);
        
        let all_keys = self.state.past_tournament_leaderboards.indices().await.unwrap_or_default();
        
        for key in &all_keys {
            if key.starts_with(&prefix) {
                if let Some(username) = key.strip_prefix(&prefix) {
                    if let Some(score) = self.state.past_tournament_leaderboards.get(&**key).await.ok().flatten() {
                        entries.push(LeaderboardEntry {
                            username: username.to_string(),
                            score,
                        });
                    }
                }
            }
        }
        
        entries.sort_by(|a, b| b.score.cmp(&a.score));
        entries
    }
	
}

//  === Query Betting Services ===
impl QueryRoot {
    async fn get_past_tournament_analytics_internal(&self, tournament_number: u64) -> Option<PastTournamentAnalytics> {
        let key = format!("{}", tournament_number);
        
        let bets_placed = self.state.past_tournament_bets_placed.get(&key).await.ok().flatten();
        let bets_settled = self.state.past_tournament_bets_settled.get(&key).await.ok().flatten();
        let payouts = self.state.past_tournament_payouts.get(&key).await.ok().flatten();
        
        if let (Some(bets_placed), Some(bets_settled), Some(payouts)) = (bets_placed, bets_settled, payouts) {
            Some(PastTournamentAnalytics {
                tournament_number,
                total_bets_placed: bets_placed,
                total_bets_settled: bets_settled,
                total_payouts: payouts,
            })
        } else {
            None
        }
    }
    
    // Helper function cho all_tournament_analytics
    async fn get_all_tournament_analytics_internal(&self) -> Vec<PastTournamentAnalytics> {
        let mut analytics = Vec::new();
        
        // Get all tournament numbers from past analytics
        if let Ok(keys) = self.state.past_tournament_bets_placed.indices().await {
            for key in keys {
                if let Ok(tournament_number) = key.parse::<u64>() {
                    if let Some(analytic) = self.get_past_tournament_analytics_internal(tournament_number).await {
                        analytics.push(analytic);
                    }
                }
            }
        }
        
        // Sort by tournament number (newest first)
        analytics.sort_by(|a, b| b.tournament_number.cmp(&a.tournament_number));
        analytics
    }
}