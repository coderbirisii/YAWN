import requests
import json

# Auth bilgileri
headers = {
    "Authorization": "Bearer eyJraWQiOiJyc28tcHJvZC0yMDI0LTExIiwiYWxnIjoiUlMyNTYifQ.eyJwcCI6eyJjIjoiZXUifSwic3ViIjoiYjRjNjFmZjAtNmJmOS01ZWY4LTkyZWEtY2ZmNjAxZWI2MDg3Iiwic2NwIjpbIm9wZW5pZCIsImxpbmsiLCJiYW4iLCJsb2xfcmVnaW9uIiwibG9sIiwic3VtbW9uZXIiLCJvZmZsaW5lX2FjY2VzcyJdLCJjbG0iOlsiIVRZRXRJUDAiXSwiYW1yIjpbInBhc3N3b3JkIiwibWZhIl0sImlzcyI6Imh0dHBzOi8vYXV0aC5yaW90Z2FtZXMuY29tIiwiY3R5IjoiYXV0IiwicHJtIjoiNVFBPSIsImxjdHkiOiJ0dXIiLCJhY3IiOiJ1cm46cmlvdDpnb2xkIiwiZGF0Ijp7ImMiOiJlYzEiLCJsaWQiOiJKSlhnX0V4aWpGWTNOV202RlF6WHRBIn0sInBsdCI6eyJkZXYiOiJ1bmtub3duIiwiaWQiOiJ3aW5kb3dzIn0sImV4cCI6MTc3MzA5NjA0MSwiaWF0IjoxNzczMDkyNDQxLCJqdGkiOiJpVnRYTXdaVUZjQSIsImNpZCI6InJpb3QtY2xpZW50In0.plDOrTYLy-ywo_7ZjdfOWXNbB8pKS5Yb9HpzndvdFOw_EThDTGvibadnf4ef3ZfyI-mMbTFZ-lajCN5kgCGz7SHEkszIVw2LN1lIJiV4mhvSwZNLHemGucB8KlQQ72CjOkHMyLne9OzmldE2ClWl204nBI_6pIRhP6e_o2PduLZBF3LgRqs3yELhZvWOtI988m5VHZOu8L5Fw5AG_i72GOw3vm9QgnppFTjOEKBOXtLJL8pZeQrL3yA6u4iwmRg_azMSkrmr4NVsarn1oW8-CL3TfMtocAbPF1Di9OGWrKWPghuMfLhA90v7xoAd99t7f4--xFsiKwkyFETrQn13UA",
    "X-Riot-Entitlements-JWT": "eyJraWQiOiJrMSIsImFsZyI6IlJTMjU2In0.eyJlbnRpdGxlbWVudHMiOltdLCJhdF9oYXNoIjoiZE8yUGVROFVfNEUxNlRnX0Q0TzAzUSIsInN1YiI6ImI0YzYxZmYwLTZiZjktNWVmOC05MmVhLWNmZjYwMWViNjA4NyIsImlzcyI6Imh0dHBzOi8vZW50aXRsZW1lbnRzLmF1dGgucmlvdGdhbWVzLmNvbSIsImlhdCI6MTc3MzA5MjQ0MSwianRpIjoiaVZ0WE13WlVGY0EifQ.RaSepR4Jc1Pd7KIoisxmAeW3PYHDVUBkCCwf9p2yLx84K4BM9yBHL4o1yIbhQiYmXTk9Nc-SFy0_-absyJAsQGece15M22b81u4YSHmndauejMbaZbybsEIE0QsFoHe2eC7mQ0AcbW2Jb8bzXlqtRRkX2x4Ns5mTvqJ92lxcdIewUAU_vt5Dglv4FG6VY14rGFYj0TiOSP_M0fsxT85A3GrEqdl-__hdfQfSHkufNy2yssPRBho6V7hSq8qoiv9uInjMCE2lSrwIHbI3zLWoO67Ft7jaaDA2zwY0q5Ln-6Z1ncjlbfWIRMj6xzO7IT30-MTelGlY3DLE94ChowMihA",
    "X-Riot-ClientPlatform": "ew0KCSJwbGF0Zm9ybVR5cGUiOiAiUEMiLA0KCSJwbGF0Zm9ybU9TIjogIldpbmRvd3MiLA0KCSJwbGF0Zm9ybU9TVmVyc2lvbiI6ICIxMC4wLjE5MDQyLjEuMjU2LjY0Yml0IiwNCgkicGxhdGZvcm1DaGlwc2V0IjogIlVua25vd24iDQp9",
    "X-Riot-ClientVersion": "release-12.04-shipping-24-4354757",
}

puuid = "b4c61ff0-6bf9-5ef8-92ea-cff601eb6087"
shard = "eu"
region = "a"

# Tüm loadout preset'leri (Flex dahil)
url_loadouts = f"https://pd.{shard}.{region}.pvp.net/personalization/v2/players/{puuid}/presets"
response = requests.get(url_loadouts, headers=headers, verify=False)

if response.status_code == 200:
    data = response.json()
    print("Tüm Loadout Preset'leri (Flex dahil):")
    print(json.dumps(data, indent=2))
    
    # Flex'i bul
    for preset in data.get("Presets", []):
        if "Flex" in preset.get("Name", "") or "Flex Queue" in preset.get("Name", ""):
            print("\nFLEX LOADOUT BULUNDU:")
            print(json.dumps(preset, indent=2))
else:
    print("Hata:", response.status_code, response.text)

# Direkt Flex endpoint (eğer varsa)
url_flex = f"https://pd.{shard}.{region}.pvp.net/personalization/v2/players/{puuid}/flex-loadout"
response_flex = requests.get(url_flex, headers=headers, verify=False)

if response_flex.status_code == 200:
    print("\nDirekt Flex Loadout:")
    print(json.dumps(response_flex.json(), indent=2))
else:
    print("Flex endpoint hata:", response_flex.status_code)
