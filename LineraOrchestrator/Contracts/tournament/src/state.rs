// Copyright (c) Zefchain Labs, Inc.
// SPDX-License-Identifier: Apache-2.0
// tournament/src/state.rs

use linera_sdk::views::{linera_views, MapView, RegisterView, RootView, ViewStorageContext, SetView};
use linera_sdk::linera_base_types::ChainId;
use serde::{Deserialize, Serialize};
use tournament_shared::TournamentMetadata;

//=== Tournament Betting ===
/// Entry info bets
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct BetEntry {
    pub bet_id: String,
    pub match_id: String,
    pub bettor: String,
    pub bettor_public_key: String, //AccountOwner
    pub predicted: String,
    pub amount: u64,
    pub user_chain: ChainId,
}

#[derive(Clone, Debug, Serialize, Deserialize, PartialEq)]
pub enum MatchBetStatus {
    Waiting, // Chờ mở cược
    Open,    // Đang mở cược
    Closed,  // Đã đóng cược
    Settled, // Đã xử lý kết quả
}

/// Metadata cho một trận đấu (match) - betting
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct MatchMetadata {
    pub match_id: String,
    pub betting_deadline_unix: u64,
    pub betting_start_unix: Option<u64>,
    pub bet_status: MatchBetStatus,
    pub player1: String,
    pub player2: String,
    pub total_bets_a: u64,  // Tổng cược vào player1
    pub total_bets_b: u64,  // Tổng cược vào player2
    pub total_bets_count: u64, // Tổng số bet của trận này
    pub odds_a: u64,        // Tỷ lệ cược cho player1 (nhân 1000)
    pub odds_b: u64,        // Tỷ lệ cược cho player2 (nhân 1000)
}

// Struct cho tournament match onchain
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct TournamentMatch {
    pub match_id: String,
    pub player1: String,
    pub player2: String,
    pub winner: Option<String>,
    pub round: String,
    pub match_status: String,
}

// Struct cho tournament check pending airdrop
#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct PendingClaim {
    pub user_chain: ChainId,
    pub user_public_key: String,
    pub requested_at: u64,
}


/// Định nghĩa trạng thái của hợp đồng Tournament
#[derive(RootView)]
#[view(context = ViewStorageContext)]
pub struct TournamentState {
	// === Tournament Management State ===
    // Tournament current season
    pub tournament_name: RegisterView<String>,
    pub start_time: RegisterView<u64>,
    pub end_time: RegisterView<u64>,
    pub participants: RegisterView<Vec<String>>,
    pub results: MapView<String, (String, String)>,
    pub status: RegisterView<String>,
    pub current_round: RegisterView<String>,
    pub bracket: MapView<String, TournamentMatch>,
    
    // Quản lý tournament theo season
    pub current_tournament: RegisterView<u64>,
	pub current_champion: RegisterView<String>,
    pub current_runner_up: RegisterView<String>,
    pub tournament_metadata: MapView<u64, TournamentMetadata>,
	pub tournament_leaderboard: MapView<String, u64>,
    
    // Lưu trữ dữ liệu tournament cũ
    pub past_tournament_leaderboards: MapView<String, u64>, // Key: "tournament_{number}:{username}"

    // === Tournament Betting State ===
    pub bets: MapView<String, Vec<BetEntry>>, 
    pub matches: MapView<String, MatchMetadata>,
    pub bet_counter: RegisterView<u64>,
	pub user_xfighter_app_ids: MapView<ChainId, linera_sdk::linera_base_types::ApplicationId<userxfighter::UserXfighterAbi>>,
	
	// Betting analytics
    pub current_total_bets_placed: RegisterView<u64>, // Total bets placed in current season
    pub current_total_bets_settled: RegisterView<u64>, // Total bets settled in current season
    pub current_total_payouts: RegisterView<u64>, // Total payouts in current season
    
    // Lưu analytics của các season cũ 
    pub past_tournament_bets_placed: MapView<String, u64>,  // Key: "tournament_{number}"
    pub past_tournament_bets_settled: MapView<String, u64>, // Key: "tournament_{number}"
    pub past_tournament_payouts: MapView<String, u64>,      // Key: "tournament_{number}"
	
	// Airdrop system
    pub airdrop_amount: RegisterView<u64>, // Số token airdrop mặc định
    pub airdrop_recipients: SetView<String>, // Danh sách user đã nhận airdrop (format: "chain_id:public_key")
	pub pending_claims: MapView<String, PendingClaim>, // key: "chain:public_key"
}