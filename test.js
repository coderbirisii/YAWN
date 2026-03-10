const axios = require('axios');
const fs = require('fs');

// Lockfile'dan bilgileri parse et
const lockfile = 'Riot Client:22012:52695:0ffAp7iXrA1-taM7hdfYtQ:https';
const parts = lockfile.split(':');
const PORT = parts[2];
const PASSWORD = parts[3];
const PROTOCOL = parts[4];

// API base URL
const BASE_URL = `${PROTOCOL}://127.0.0.1:${PORT}`;

// Basic auth için credentials
const auth = {
    username: 'riot',
    password: PASSWORD
};

// Platform bilgisini dinamik olarak oluştur (C# Constants.Platform gibi)
function generatePlatformInfo() {
    // Windows sistem bilgilerini almaya çalış
    const os = require('os');

    const platformData = {
        platformType: "PC",
        platformOS: "Windows",
        platformOSVersion: `${os.release()}.1.256.64bit`, // Windows version + build
        platformChipset: "Unknown"
    };

    // JSON'u base64'e çevir (C# Constants.Platform gibi)
    const jsonString = JSON.stringify(platformData, null, 4);
    return Buffer.from(jsonString).toString('base64');
}

// Platform bilgisini global olarak ayarla
const PLATFORM_INFO = generatePlatformInfo();

// Axios instance oluştur
const client = axios.create({
    baseURL: BASE_URL,
    auth: auth,
    headers: {
        'User-Agent': 'RiotClient/60.0.0 (Windows;10;;Professional, x64)'
    },
    httpsAgent: new (require('https').Agent)({
        rejectUnauthorized: false // Self-signed certificate için
    })
});

console.log('🚀 NOWT Riot API Test Scripti');
console.log('=====================================');
console.log(`API URL: ${BASE_URL}`);
console.log(`Auth: riot:${PASSWORD.substring(0, 10)}...`);
console.log('');

// Valorant API'den güncel versiyonu çek
async function getValorantVersion() {
    try {
        console.log('🔄 Valorant versiyonu çekiliyor...');
        const response = await axios.get('https://valorant-api.com/v1/version');
        if (response.data?.data?.riotClientVersion) {
            console.log(`📦 Client Version: ${response.data.data.riotClientVersion}`);
            return response.data.data.riotClientVersion;
        }
    } catch (error) {
        console.log('⚠️  Versiyon çekilemedi, varsayılan kullanılıyor');
    }
    return 'release-12.04-shipping-24-4354757'; // Fallback
}

// Region'ı presence data'dan belirle
function getRegionFromPresence(presenceData) {
    // Presence data'dan region tespiti (şimdilik basit mantık)
    // Gerçek uygulamada daha karmaşık logic kullanılır
    const regions = ['na', 'eu', 'ap', 'kr'];
    // Şimdilik eu varsayalım, gerçekte presence data'dan çıkarılır
    return 'eu';
}

// PUUID'yi presence data'dan çıkar
function extractOwnPuuid(presenceData) {
    if (presenceData?.presences && presenceData.presences.length > 0) {
        // İlk presence kendi PUUID'miz
        return presenceData.presences[0].puuid;
    }
    return null;
}

async function testPresenceAPI() {
    console.log('📡 Presence API Testi (/chat/v4/presences)');
    console.log('------------------------------------------');

    try {
        const response = await client.get('/chat/v4/presences');
        console.log('✅ Presence API: BAŞARILI');
        console.log(`Oyuncu sayısı: ${response.data.presences?.length || 'Bilinmiyor'}`);

        if (response.data.presences && response.data.presences.length > 0) {
            console.log('\nİlk 3 oyuncu:');
            response.data.presences.slice(0, 3).forEach((presence, index) => {
                const name = presence.game_name || presence.product;
                console.log(`${index + 1}. ${name} (${presence.puuid?.substring(0, 8)}...)`);
            });
        }
        return response.data;
    } catch (error) {
        console.log('❌ Presence API: BAŞARISIZ');
        console.log(`Hata: ${error.response?.status} - ${error.response?.statusText}`);
        console.log(`Detay: ${error.message}`);
        return null;
    }
}

async function testEntitlements() {
    console.log('\n🔑 Entitlements Token Testi (/entitlements/v1/token)');
    console.log('---------------------------------------------------');

    try {
        const response = await client.get('/entitlements/v1/token');
        console.log('✅ Entitlements: BAŞARILI');
        console.log(`Token: ${response.data.accessToken?.substring(0, 20)}...`);
        console.log(`Subject: ${response.data.subject}`);
        return response.data;
    } catch (error) {
        console.log('❌ Entitlements: BAŞARISIZ');
        console.log(`Hata: ${error.response?.status} - ${error.response?.statusText}`);
        return null;
    }
}

async function testPlayerInfo(puuid, entitlementsToken, region = 'eu', clientVersion = null) {
    if (!puuid) {
        console.log('\n⚠️  Player Info testi için PUUID gerekli');
        return null;
    }

    if (!entitlementsToken) {
        console.log('\n⚠️  Player Info testi için Entitlements Token gerekli');
        return null;
    }

    console.log(`\n👤 Player MMR Testi (${puuid.substring(0, 8)}...) - Region: ${region}`);
    console.log('-------------------------------------------------------');

    try {
        // Client version'ı parametre olarak al veya varsayılan kullan
        const version = clientVersion || await getValorantVersion();

        const response = await axios.get(`https://pd.${region}.a.pvp.net/mmr/v1/players/${puuid}`, {
            headers: {
                'Authorization': `Bearer ${entitlementsToken.accessToken}`,
                'X-Riot-Entitlements-JWT': entitlementsToken.token,
                'X-Riot-ClientPlatform': PLATFORM_INFO,
                'X-Riot-ClientVersion': version
            },
            httpsAgent: new (require('https').Agent)({
                rejectUnauthorized: false
            })
        });

        console.log('✅ Player MMR: BAŞARILI');
        const competitiveData = response.data.QueueSkills?.Competitive?.SeasonalInfoBySeasonId?.Act;
        if (competitiveData) {
            console.log(`🏆 Competitive Tier: ${competitiveData.CompetitiveTier || 'N/A'}`);
            console.log(`📊 Rank Rating: ${competitiveData.RankedRating || 'N/A'}`);
            console.log(`🎯 Wins: ${competitiveData.NumberOfWins || 'N/A'}`);
        } else {
            console.log('❌ Competitive data bulunamadı (henüz competitive oynamadıysanız normal)');
        }
        return response.data;
    } catch (error) {
        console.log('❌ Player MMR: BAŞARISIZ');
        console.log(`Hata: ${error.response?.status} - ${error.response?.statusText}`);
        if (error.response?.status === 404) {
            console.log('💡 404 hatası: Oyuncu bulunamadı, yanlış region, veya competitive veri yok');
        } else if (error.response?.status === 403) {
            console.log('💡 403 hatası: Authentication hatası - token geçersiz');
        }
        return null;
    }
}

// Match ID çekme testi
async function testMatchId(puuid, entitlementsToken, region = 'eu', clientVersion = null) {
    if (!puuid || !entitlementsToken) {
        console.log('\n⚠️  Match ID testi için PUUID ve Entitlements gerekli');
        return null;
    }

    console.log(`\n🎮 Match ID Testi (${puuid.substring(0, 8)}...)`);
    console.log('------------------------------------------');

    try {
        const version = clientVersion || await getValorantVersion();
        
        // Core-game API ile maç ID'sini çek
        const response = await axios.get(`https://glz-${region}-1.${region}.a.pvp.net/core-game/v1/players/${puuid}`, {
            headers: {
                'Authorization': `Bearer ${entitlementsToken.accessToken}`,
                'X-Riot-Entitlements-JWT': entitlementsToken.token,
                'X-Riot-ClientPlatform': PLATFORM_INFO,
                'X-Riot-ClientVersion': version
            },
            httpsAgent: new (require('https').Agent)({
                rejectUnauthorized: false
            })
        });

        if (response.data?.MatchID) {
            console.log('✅ Match ID: BAŞARILI');
            console.log(`🎯 Match ID: ${response.data.MatchID}`);
            console.log(`🔗 Maç URL: https://tracker.gg/valorant/match/${response.data.MatchID}`);
            return response.data.MatchID;
        } else {
            console.log('⚠️  Maçta değilsiniz (MatchID bulunamadı)');
            return null;
        }
    } catch (error) {
        console.log('❌ Match ID: BAŞARISIZ');
        console.log(`Hata: ${error.response?.status} - ${error.response?.statusText}`);
        if (error.response?.status === 404) {
            console.log('💡 404: Maçta değilsiniz veya region yanlış');
        }
        return null;
    }
}

// Oyuncu isimlerini çek (Name Service API)
async function testPlayerNames(puuids, entitlementsToken, region = 'eu', clientVersion = null) {
    if (!puuids || !entitlementsToken) {
        console.log('\n⚠️  Player names testi için PUUID\'ler ve Entitlements gerekli');
        return null;
    }

    console.log(`\n👥 Player Names Testi (${puuids.length} oyuncu)`);
    console.log('------------------------------------------');

    try {
        const version = clientVersion || await getValorantVersion();
        
        const response = await axios.put(`https://pd.${region}.a.pvp.net/name-service/v2/players`, puuids, {
            headers: {
                'Authorization': `Bearer ${entitlementsToken.accessToken}`,
                'X-Riot-Entitlements-JWT': entitlementsToken.token,
                'X-Riot-ClientPlatform': PLATFORM_INFO,
                'X-Riot-ClientVersion': version
            },
            httpsAgent: new (require('https').Agent)({
                rejectUnauthorized: false
            })
        });

        if (response.data && Array.isArray(response.data)) {
            console.log('✅ Player Names: BAŞARILI');
            response.data.forEach((player, index) => {
                if (player.GameName && player.TagLine) {
                    console.log(`👤 ${index + 1}. ${player.GameName}#${player.TagLine} (${player.Subject?.substring(0, 8)}...)`);
                } else {
                    console.log(`👤 ${index + 1}. Incognito (${player.Subject?.substring(0, 8)}...)`);
                }
            });
            return response.data;
        } else {
            console.log('⚠️  Player names verisi bulunamadı');
            return null;
        }
    } catch (error) {
        console.log('❌ Player Names: BAŞARISIZ');
        console.log(`Hata: ${error.response?.status} - ${error.response?.statusText}`);
        return null;
    }
}

// Local JSON'dan skin isimlerini çek (NOWT gibi)
async function getSkinNames() {
    try {
        console.log('\n🔫 Skin Names Testi (Local JSON)');
        console.log('------------------------------------------');
        
        // Local app data path'i belirle
        const localAppData = process.env.LOCALAPPDATA || 'C:\\Users\\Coder\\AppData\\Local';
        const valApiPath = `${localAppData}\\NOWT\\ValAPI\\skinchromas.json`;
        
        if (require('fs').existsSync(valApiPath)) {
            const skinData = require('fs').readFileSync(valApiPath, 'utf8');
            const skins = JSON.parse(skinData);
            console.log('✅ Local JSON: BAŞARILI');
            console.log(`📦 Toplam skin sayısı: ${Object.keys(skins).length}`);
            
            // İlk 5 skin'i göster
            const sampleSkins = Object.entries(skins).slice(0, 5);
            sampleSkins.forEach(([uuid, skin], index) => {
                console.log(`🔫 ${index + 1}. ${skin.Name} (${uuid.substring(0, 8)}...)`);
            });
            
            return skins;
        } else {
            console.log('⚠️  Local JSON bulunamadı:', valApiPath);
            console.log('💡 NOWT uygulamasını bir kez çalıştırarak JSON dosyalarını oluşturun');
            return null;
        }
    } catch (error) {
        console.log('❌ Local JSON: BAŞARISIZ');
        console.log(`Hata: ${error.message}`);
        return null;
    }
}

// Tüm oyuncuların rank bilgilerini çek (NOWT gibi)
async function testAllPlayersRank(playerPuuids, entitlementsToken, region = 'eu', clientVersion = null) {
    if (!playerPuuids || !entitlementsToken) {
        console.log('\n⚠️  Rank testi için PUUID\'ler ve Entitlements gerekli');
        return null;
    }

    console.log(`🏆 Rank Testi (${playerPuuids.length} oyuncu)`);
    console.log('------------------------------------------');

    try {
        const version = clientVersion || await getValorantVersion();
        
        // Player names çek
        const playerNames = await testPlayerNames(playerPuuids, entitlementsToken, region, version);
        
        // Her oyuncu için rank bilgilerini çek
        for (let i = 0; i < playerPuuids.length; i++) {
            const puuid = playerPuuids[i];
            const playerName = playerNames?.find(p => p.Subject === puuid);
            
            console.log(`\n🎮 Oyuncu ${i + 1}:`);
            
            // Player name
            if (playerName?.GameName && playerName?.TagLine) {
                console.log(`   👤 İsim: ${playerName.GameName}#${playerName.TagLine}`);
            } else {
                console.log(`   👤 İsim: Incognito`);
            }
            
            // Rank bilgilerini çek
            try {
                const response = await axios.get(`https://pd.${region}.a.pvp.net/mmr/v1/players/${puuid}`, {
                    headers: {
                        'Authorization': `Bearer ${entitlementsToken.accessToken}`,
                        'X-Riot-Entitlements-JWT': entitlementsToken.token,
                        'X-Riot-ClientPlatform': PLATFORM_INFO,
                        'X-Riot-ClientVersion': version
                    },
                    httpsAgent: new (require('https').Agent)({
                        rejectUnauthorized: false
                    })
                });

                if (response.data?.QueueSkills?.Competitive?.SeasonalInfoBySeasonId?.Act) {
                    const competitiveData = response.data.QueueSkills.Competitive.SeasonalInfoBySeasonId.Act;
                    const tier = competitiveData.CompetitiveTier || 0;
                    const rr = competitiveData.RankedRating || 0;
                    const wins = competitiveData.NumberOfWins || 0;
                    
                    // Rank isimlerini çevir
                    const rankNames = {
                        0: 'Unranked',
                        1: 'Iron 1', 2: 'Iron 2', 3: 'Iron 3',
                        4: 'Bronze 1', 5: 'Bronze 2', 6: 'Bronze 3',
                        7: 'Silver 1', 8: 'Silver 2', 9: 'Silver 3',
                        10: 'Gold 1', 11: 'Gold 2', 12: 'Gold 3',
                        13: 'Platinum 1', 14: 'Platinum 2', 15: 'Platinum 3',
                        16: 'Diamond 1', 17: 'Diamond 2', 18: 'Diamond 3',
                        19: 'Ascendant 1', 20: 'Ascendant 2', 21: 'Ascendant 3',
                        22: 'Immortal 1', 23: 'Immortal 2', 24: 'Immortal 3',
                        25: 'Radiant'
                    };
                    
                    const rankName = rankNames[tier] || 'Unknown';
                    const rrDisplay = tier >= 24 ? `${rr} RR` : `${rr}/100`;
                    
                    console.log(`   🏆 Rank: ${rankName}`);
                    console.log(`   📊 RR: ${rrDisplay}`);
                    console.log(`   🎯 Wins: ${wins}`);
                } else {
                    console.log(`   🏆 Rank: Unranked (henüz competitive oynamamış)`);
                }
            } catch (error) {
                console.log(`   ❌ Rank: Hata (${error.response?.status || 'Network'})`);
            }
        }
        
        console.log('\n✅ Tüm oyuncuların rank bilgileri çekildi');
        return true;
    } catch (error) {
        console.log('❌ Rank testi: BAŞARISIZ');
        console.log(`Genel hata: ${error.message}`);
        return false;
    }
}

// Maç loadout bilgilerini çek (tam NOWT versiyonu)
async function testMatchLoadout(matchId, entitlementsToken, region = 'eu', clientVersion = null) {
    if (!matchId || !entitlementsToken) {
        console.log('\n⚠️  Loadout testi için MatchID ve Entitlements gerekli');
        return null;
    }

    console.log(`\n🔫 Match Loadout Testi (Tam NOWT Versiyonu)`);
    console.log('------------------------------------------');

    try {
        const version = clientVersion || await getValorantVersion();
        
        const response = await axios.get(`https://glz-${region}-1.${region}.a.pvp.net/core-game/v1/matches/${matchId}/loadouts`, {
            headers: {
                'Authorization': `Bearer ${entitlementsToken.accessToken}`,
                'X-Riot-Entitlements-JWT': entitlementsToken.token,
                'X-Riot-ClientPlatform': PLATFORM_INFO,
                'X-Riot-ClientVersion': version
            },
            httpsAgent: new (require('https').Agent)({
                rejectUnauthorized: false
            })
        });

        if (response.data?.Loadouts) {
            console.log('✅ Loadout: BAŞARILI');
            console.log(`👥 Oyuncu sayısı: ${response.data.Loadouts.length}`);
            
            // PUUID'leri topla
            const playerPuuids = response.data.Loadouts.map(loadout => loadout.Subject);
            
            // Player names çek
            const playerNames = await testPlayerNames(playerPuuids, entitlementsToken, region, version);
            
            // Skin names çek
            const skinNames = await getSkinNames();
            
            // Tüm oyuncuların detaylı bilgilerini göster (match kaç oyuncu varsa o kadar)
            response.data.Loadouts.forEach((loadout, index) => {
                console.log(`\n🎮 Oyuncu ${index + 1}:`);
                
                // Player name
                const playerName = playerNames?.find(p => p.Subject === loadout.Subject);
                if (playerName?.GameName && playerName?.TagLine) {
                    console.log(`   👤 İsim: ${playerName.GameName}#${playerName.TagLine}`);
                } else {
                    console.log(`   👤 İsim: Incognito`);
                }
                
                // Vandal skin
                const vandal = loadout.Loadout?.Items?.['9c82e19d-4575-0200-1a81-3eacf00cf872'];
                if (vandal?.Sockets?.['3ad1b2b2-acdb-4524-852f-954a76ddae0a']?.Item) {
                    const vandalUuid = vandal.Sockets['3ad1b2b2-acdb-4524-852f-954a76ddae0a'].Item.ID;
                    const vandalSkin = skinNames?.[vandalUuid];
                    console.log(`   🔫 Vandal: ${vandalSkin?.Name || vandalUuid} (${vandalUuid.substring(0, 8)}...)`);
                } else {
                    console.log(`   🔫 Vandal: Default`);
                }
                
                // Phantom skin
                const phantom = loadout.Loadout?.Items?.['e9575d18-a3a5-4d32-9479-8c4ea3f5d4f1'];
                if (phantom?.Sockets?.['3ad1b2b2-acdb-4524-852f-954a76ddae0a']?.Item) {
                    const phantomUuid = phantom.Sockets['3ad1b2b2-acdb-4524-852f-954a76ddae0a'].Item.ID;
                    const phantomSkin = skinNames?.[phantomUuid];
                    console.log(`   🔫 Phantom: ${phantomSkin?.Name || phantomUuid} (${phantomUuid.substring(0, 8)}...)`);
                } else {
                    console.log(`   🔫 Phantom: Default`);
                }
                
                // Classic skin
                const classic = loadout.Loadout?.Items?.['29a0cfab-485b-f5cc-9a26-7d8dcbb074cc'];
                if (classic?.Sockets?.['3ad1b2b2-acdb-4524-852f-954a76ddae0a']?.Item) {
                    const classicUuid = classic.Sockets['3ad1b2b2-acdb-4524-852f-954a76ddae0a'].Item.ID;
                    const classicSkin = skinNames?.[classicUuid];
                    console.log(`   🔫 Classic: ${classicSkin?.Name || classicUuid} (${classicUuid.substring(0, 8)}...)`);
                } else {
                    console.log(`   🔫 Classic: Default`);
                }
            });
            
            return response.data;
        } else {
            console.log('⚠️  Loadout verisi bulunamadı');
            return null;
        }
    } catch (error) {
        console.log('❌ Loadout: BAŞARISIZ');
        console.log(`Hata: ${error.response?.status} - ${error.response?.statusText}`);
        return null;
    }
}

// Ana test fonksiyonu
async function runTests() {
    try {
        console.log('🔍 Riot API Bağlantısı Test Ediliyor...\n');

        // 1. Presence API test
        const presenceData = await testPresenceAPI();
        
        // Debug: Presence PUUID'leri göster
        if (presenceData?.presences) {
            console.log('\n🔍 Presence PUUID Debug:');
            presenceData.presences.forEach((presence, index) => {
                console.log(`   ${index + 1}. ${presence.puuid} (${presence.game_name || 'Unknown'})`);
            });
            console.log(`   Toplam: ${presenceData.presences.length} oyuncu`);
        }

        // 2. Entitlements test
        const entitlementsData = await testEntitlements();

        // 3. Dynamic değerleri çıkar (C# Constants gibi)
        let puuid = null;
        let region = 'eu'; // Default
        let clientVersion = null;

        if (presenceData?.presences && presenceData.presences.length > 0) {
            puuid = extractOwnPuuid(presenceData);
            region = getRegionFromPresence(presenceData);
            console.log(`\n📍 Dynamic Değerler:`);
            console.log(`   PUUID: ${puuid}`);
            console.log(`   Region: ${region}`);
            console.log(`   Oyuncu sayısı: ${presenceData.presences.length}`);
        }

        // 4. Client version'ı çek
        clientVersion = await getValorantVersion();

        // 5. İlk oyuncunun MMR bilgilerini test et
        if (presenceData?.presences && presenceData.presences.length > 0 && entitlementsData && puuid) {
            await testPlayerInfo(puuid, entitlementsData, region, clientVersion);
            
            // 5a. Tüm oyuncuların rank bilgilerini test et
            console.log('\n🏆 Tüm Oyuncuların Rank Bilgileri:');
            console.log('------------------------------------------');
            
            const playerPuuids = presenceData.presences.map(p => p.puuid);
            await testAllPlayersRank(playerPuuids, entitlementsData, region, clientVersion);
        } else {
            if (!entitlementsData) {
                console.log('\n⚠️  MMR testi için entitlements token gerekli, atlanıyor...');
            }
            if (!puuid) {
                console.log('\n⚠️  MMR testi için PUUID gerekli, atlanıyor...');
            }
        }

        // 6. Match ID testi (maçta ise)
        if (presenceData?.presences && presenceData.presences.length > 0 && entitlementsData && puuid) {
            const matchId = await testMatchId(puuid, entitlementsData, region, clientVersion);
            
            // 7. Loadout testi (match ID varsa)
            if (matchId) {
                await testMatchLoadout(matchId, entitlementsData, region, clientVersion);
            }
        }

        console.log('\n🎉 Test tamamlandı!');
        console.log('\n💡 İpuçları:');
        console.log('- Presence API çalışıyorsa Riot Client bağlantısı başarılı');
        console.log('- MMR API 404 veriyorsa competitive oynamadıysanız normal');
        console.log('- Match ID API maçta değilseniz 404 verir (normal)');
        console.log('- Loadout API sadece maçta çalışır');
        console.log('- Client version otomatik çekiliyor (valorant-api.com)');
        console.log('- Region/Shard presence data\'dan belirleniyor');
        console.log('- Constants.cs\'teki gibi dynamic değerler kullanılıyor');

    } catch (error) {
        console.error('💥 Genel hata:', error.message);
    }
}

// Script çalıştır
if (require.main === module) {
    runTests();
}

module.exports = { client, testPresenceAPI, testEntitlements, testPlayerInfo };
