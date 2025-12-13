// Copyright (c) Zefchain Labs, Inc.
// SPDX-License-Identifier: Apache-2.0  
// userxfighter/src/contract.rs

#![cfg_attr(target_arch = "wasm32", no_main)]

mod state;

use linera_sdk::{
    abi::WithContractAbi,
    views::{RootView, View},
    Contract, ContractRuntime,
};
use log::info;
use std::str::FromStr; 
use userxfighter::{Operation, Parameters};
use tournament_shared::TournamentOperation;
use friendxfighter::{Operation as FriendOperation, FriendAbi};
use self::state::{UserXfighterState, Transaction};

use linera_sdk::linera_base_types::{ChainId, AccountOwner, Amount};
use linera_sdk::abis::fungible::{self, FungibleOperation, FungibleTokenAbi};
linera_sdk::contract!(UserXfighterContract);

pub struct UserXfighterContract {
    state: UserXfighterState,
    runtime: ContractRuntime<Self>,
}

impl WithContractAbi for UserXfighterContract {
    type Abi = userxfighter::UserXfighterAbi;
}

impl Contract for UserXfighterContract {
    type Parameters = Parameters;
    type InstantiationArgument = ();
    type Message = TournamentOperation;
    type EventValue = ();

    async fn load(runtime: ContractRuntime<Self>) -> Self {
        let state = UserXfighterState::load(runtime.root_view_storage_context())
            .await
            .expect("Failed to load state");
        UserXfighterContract { state, runtime }
    }

    async fn instantiate(&mut self, _argument: Self::InstantiationArgument) {
        let params: Parameters = self.runtime.application_parameters();
        info!("UserXFighter initialized with local tournament app: {:?}", params.local_tournament_app_id);

        // Register with Tournament local app & Trigger token distribution for test betting
        let register_op = TournamentOperation::RegisterUserXFighter {
            user_xfighter_app_id: self.runtime.application_id().forget_abi(),
        };
        
        self.runtime.call_application::<tournament_shared::TournamentAbi>(
            true,
            params.local_tournament_app_id,
            &register_op,
        );
        
        info!("Registered UserXFighter with local Tournament app");
    }

    async fn store(mut self) {
        self.state.save().await.expect("Failed to save state");
    }
    
    async fn execute_operation(&mut self, operation: Self::Operation) {
        match operation {
			// === Betting Operations Methos ===
            Operation::PlaceBet {user_chain, user_public_key, match_id, player, amount } => {
				info!("[USER-XFIGHTER] PlaceBet - match: {}, player: {}, amount: {}", 
                  match_id, player, amount);
				 info!("[USER-XFIGHTER-NEW] PlaceBet từ chain {:?}, user: {}", 
                      user_chain, user_public_key);
				  
                let signer = self.runtime.authenticated_signer().unwrap();
				
				// USER TRẢ TIỀN BET NGAY KHI PLACE BET
				self.transfer_bet_to_tournament(signer, amount, &match_id).await;
				
                // SEND TOURNAMENT LOCAL ON SAME USER CHAIN
                let params: Parameters = self.runtime.application_parameters();
                let tournament_call = TournamentOperation::PlaceBet {
                    bet_id: format!("bet_{}_{}", user_chain, self.runtime.system_time().micros()),
                    match_id: match_id.clone(),
                    player: player.clone(),
                    amount,
                    bettor: signer.to_string(),
					bettor_public_key: user_public_key.clone(), // Gửi public key
                    user_chain,
                };
	
                self.runtime.call_application::<tournament_shared::TournamentAbi>(
					true,
					params.local_tournament_app_id,
					&tournament_call,
				);

                // Save local transaction
                let tx = Transaction {
                    tx_id: format!("bet_{}_{}", user_chain, self.runtime.system_time().micros()),
                    tx_type: "bet_placed".to_string(),
                    amount,
                    timestamp: self.runtime.system_time().micros(),
                    related_id: Some(match_id.clone()),
                    status: "paid".to_string(),
					player: Some(player.clone()),
					tournament_season: None,
                };
                self.state.transactions.insert(&tx.tx_id.clone(), tx).unwrap();
                
                info!("Bet sent to tournament with public key: {}", user_public_key);
				info!("Bet placed with payment: {} tokens for match {}", amount, match_id);
            }			
			 Operation::RecordPayout { bet_id, match_id, amount, user_public_key: _, is_win, tournament_season  } => {
				info!("[USER-XFIGHTER] RecordPayout operation - bet: {}, win: {}, season: {}", bet_id, is_win, tournament_season);
				self.record_payout(&bet_id, &match_id, amount, is_win, tournament_season).await;
				info!("[USER-XFIGHTER] RecordPayout completed for bet: {}", bet_id);
			} 
			
			// === Airdrop Operations Methos ===
			Operation::RequestInitialClaim { user_chain, user_public_key } => {
				info!("[USER-XFIGHTER] RequestInitialClaim từ chain {:?}, user: {}", 
					  user_chain, user_public_key);

				// Send request to tournament với user_chain và user_public_key từ parameters
				let params: Parameters = self.runtime.application_parameters();
				let claim_request = TournamentOperation::RequestInitialClaim {
					user_chain,
					user_public_key: user_public_key.clone(),
				};
				
				self.runtime.call_application::<tournament_shared::TournamentAbi>(
					true,
					params.local_tournament_app_id,
					&claim_request,
				);
				
				info!("[USER-XFIGHTER] Initial claim request sent for user: {}", user_public_key);
			}

			// === Friend Operations Methos ===
            Operation::SendFriendRequest { to_user_chain } => {
                self.send_friend_request(to_user_chain).await;
            }
            Operation::AcceptFriendRequest { request_id } => {
                self.accept_friend_request(request_id).await;
            }
            Operation::RejectFriendRequest { request_id } => {
                self.reject_friend_request(request_id).await;
            }
            Operation::RemoveFriend { friend_chain_id } => {
                self.remove_friend(friend_chain_id).await;
            }
        }
    }

    async fn execute_message(&mut self, message: Self::Message) {
        match message {
            TournamentOperation::PlaceBet { .. } => {
                info!("[USER-XFIGHTER] Received PlaceBet echo - ignoring");
            }
			
			TournamentOperation::Payout { bet_id, match_id, amount, user_public_key: _, user_chain, is_win, tournament_season } => {
                if user_chain == self.runtime.chain_id() {
					info!("[USER-XFIGHTER] Processing payout for bet: {} - amount: {}, win: {}, season: {}", 
                      bet_id, amount, is_win, tournament_season);
					info!("[USER-XFIGHTER] Verified user chain match confirmed: {:?}", user_chain);
                    self.record_payout(&bet_id, &match_id, amount, is_win, tournament_season).await;
                    info!("Direct payout recorded for bet {}", bet_id);
					
				} else {
					info!("[USER-XFIGHTER] Ignoring payout for different chain: {}", user_chain);
				}
            }

            _ => {
                info!("[USER-XFIGHTER] Ignoring non-payout message");
            }		
        }
    }
}

impl UserXfighterContract {
	// === TOURNAMENT PAYOUT METHODS  ===
	async fn record_payout(
		&mut self, 
		bet_id: &str, 
		match_id: &str, 
		amount: u64, 
		is_win: bool, 
		tournament_season: u64) {			
		
		info!("[USER-XFIGHTER] record_payout - bet: {}, match: {}, amount: {}, win: {}, season: {}", 
			  bet_id, match_id, amount, is_win, tournament_season);
		
		// 1. TÌM VÀ LẤY PLAYER TỪ BET GỐC TRƯỚC KHI UPDATE
		let mut player_from_bet = None;
		
		match self.state.transactions.get(bet_id).await {
			Ok(Some(original_tx)) => {
				// LƯU LẠI PLAYER trước khi update
				player_from_bet = original_tx.player.clone();
				
				// UPDATE bet gốc với status mới và tournament_season
				let mut updated_tx = original_tx.clone(); // Clone để không move
				if is_win {
					updated_tx.status = "won".to_string();
					info!("[USER-XFIGHTER] Bet {} updated to WON", bet_id);
				} else {
					updated_tx.status = "lost".to_string();
					info!("[USER-XFIGHTER] Bet {} updated to LOST", bet_id);
				}
				updated_tx.tournament_season = Some(tournament_season); // THÊM SEASON
				
				self.state.transactions.insert(bet_id, updated_tx)
					.expect("Failed to update original bet");
			}
			Ok(None) => {
				info!("[USER-XFIGHTER] Original bet not found: {}", bet_id);
			}
			Err(e) => {
				info!("[USER-XFIGHTER] Error retrieving original bet: {:?}", e);
			}
		}
		
		// 2. TẠO PAYOUT TRANSACTION với player và season
		if is_win {
			let payout_tx_id = format!("payout_{}", bet_id);
			let tx = Transaction {
				tx_id: payout_tx_id.clone(),
				tx_type: "payout_received".to_string(),
				amount,
				timestamp: self.runtime.system_time().micros(),
				related_id: Some(match_id.to_string()),
				status: "received".to_string(),
				player: player_from_bet,  // COPY PLAYER từ bet gốc
				tournament_season: Some(tournament_season),  // COPY SEASON
			};
			self.state.transactions.insert(&tx.tx_id.clone(), tx).unwrap();
			info!("[USER-XFIGHTER] Payout transaction created for winning bet (season {})", tournament_season);
		}

		info!("[USER-XFIGHTER] Payout processing completed for bet: {} (win: {})", bet_id, is_win);
	}
	
	// === TOURNAMENT TRANSFER BET METHODS  ===
    async fn transfer_bet_to_tournament(&mut self, user: AccountOwner, amount: u64, match_id: &str) {
		info!("[USER-XFIGHTER] Starting transfer - user: {}, amount: {}, match: {}", 
          user, amount, match_id);
		  
        let params: Parameters = self.runtime.application_parameters();
        let fungible_app_id = params.fungible_app_id;
        
		if amount == 0 {
			info!("[USER-XFIGHTER] Error invalid amount: 0");
			return;
		}
		
        let amount_attos = amount as u128 * 1_000_000_000_000_000_000;
        
        // Tournament owner (publisher chain owner)
        let tournament_owner = AccountOwner::from_str(&params.tournament_owner).unwrap();
        let target_account = fungible::Account {
            chain_id: params.publisher_chain_id, // PUBLISHER CHAIN
            owner: tournament_owner,
        };
        
        let transfer_op = FungibleOperation::Transfer {
            owner: user, // User transfer
            amount: Amount::from_attos(amount_attos),
            target_account,
        };
        
		// Call fungible token app
        self.runtime.call_application::<FungibleTokenAbi>(
            true,
            fungible_app_id,
            &transfer_op,
        );
        
         info!("Transferred {} tokens from {} to tournament (chain {:?}) for match {}",
			amount, user, params.publisher_chain_id, match_id);
    }
	
	// === FRIEND METHODS ===
	// Send request to user chain
	async fn send_friend_request(&mut self, to_user_chain: ChainId) {
        let params: Parameters = self.runtime.application_parameters();
        let operation = FriendOperation::SendFriendRequest { to_user_chain };
        
        self.runtime.call_application::<FriendAbi>(
            true,
            params.friend_app_id,
            &operation,
        );
        
        info!("[USER-XFIGHTER] Sent friend request to chain: {}", to_user_chain);
    }
    
	// User chain accept pending request from user chain
    async fn accept_friend_request(&mut self, request_id: String) {
        let params: Parameters = self.runtime.application_parameters();
        let operation = FriendOperation::AcceptFriendRequest { request_id: request_id.clone() };
        
        self.runtime.call_application::<FriendAbi>(
            true,
            params.friend_app_id,
            &operation,
        );
        
        info!("[USER-XFIGHTER] Accepted friend request: {}", request_id);
    }
    
	// User chain reject pending request from user chain
    async fn reject_friend_request(&mut self, request_id: String) {
        let params: Parameters = self.runtime.application_parameters();
        let operation = FriendOperation::RejectFriendRequest { request_id: request_id.clone() };
        
        self.runtime.call_application::<FriendAbi>(
            true,
            params.friend_app_id,
            &operation,
        );
        
        info!("[USER-XFIGHTER] Rejected friend request: {}", request_id);
    }
    
	// User chain remove friend
    async fn remove_friend(&mut self, friend_chain_id: ChainId) {
        let params: Parameters = self.runtime.application_parameters();
        let operation = FriendOperation::RemoveFriend { friend_chain_id };
        
        self.runtime.call_application::<FriendAbi>(
            true,
            params.friend_app_id,
            &operation,
        );
        
        info!("[USER-XFIGHTER] Removed friend: {}", friend_chain_id);
    }
}